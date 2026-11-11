using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Auth.DTOs;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure(Error.Validation("Auth.InvalidInput", "Email and password are required."));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Result<AuthResponse>.Failure(Error.Conflict("Auth.EmailExists", "An account with this email already exists."));
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true
        };

        var baseSlug = GenerateSlug(request.OrganizationName);
        var slug = baseSlug;
        var counter = 1;
        while (await _context.Organizations.AnyAsync(o => o.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var organization = new Organization
        {
            Name = request.OrganizationName.Trim(),
            Slug = slug
        };

        var membership = new OrganizationMember
        {
            Organization = organization,
            User = user,
            Role = OrganizationRole.Owner
        };

        var refreshToken = GenerateRefreshToken(user.Id);

        _context.Users.Add(user);
        _context.Organizations.Add(organization);
        _context.OrganizationMembers.Add(membership);
        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync(cancellationToken);

        var role = OrganizationRole.Owner.ToString();
        var token = _jwtTokenGenerator.GenerateToken(user, organization.Id, role);

        return Result<AuthResponse>.Success(new AuthResponse(
            Token: token,
            RefreshToken: refreshToken.Token,
            User: new UserDto(user.Id, user.Email, user.FullName, user.AvatarUrl),
            Organization: new OrganizationDto(organization.Id, organization.Name, organization.Slug, role)
        ));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure(Error.Validation("Auth.InvalidInput", "Email and password are required."));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.OrganizationMemberships)
                .ThenInclude(om => om.Organization)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure(Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure(Error.Unauthorized("Auth.InactiveAccount", "Your account is inactive. Please contact support."));
        }

        var primaryMembership = user.OrganizationMemberships.FirstOrDefault();
        if (primaryMembership is null)
        {
            return Result<AuthResponse>.Failure(Error.Failure("Auth.NoOrganization", "User is not assigned to any organization."));
        }

        var organization = primaryMembership.Organization;
        var role = primaryMembership.Role.ToString();

        var token = _jwtTokenGenerator.GenerateToken(user, organization.Id, role);
        var refreshToken = GenerateRefreshToken(user.Id);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            Token: token,
            RefreshToken: refreshToken.Token,
            User: new UserDto(user.Id, user.Email, user.FullName, user.AvatarUrl),
            Organization: new OrganizationDto(organization.Id, organization.Name, organization.Slug, role)
        ));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<AuthResponse>.Failure(Error.Validation("Auth.InvalidToken", "Refresh token is required."));
        }

        var existingToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.OrganizationMemberships)
                    .ThenInclude(om => om.Organization)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (existingToken is null)
        {
            return Result<AuthResponse>.Failure(Error.Unauthorized("Auth.InvalidToken", "Invalid refresh token."));
        }

        // Security: Reuse detection (if revoked token is used, compromise could have happened)
        if (existingToken.IsRevoked)
        {
            // Revoke all tokens for this user
            var allUserTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == existingToken.UserId && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var t in allUserTokens)
            {
                t.RevokedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result<AuthResponse>.Failure(Error.Unauthorized("Auth.TokenCompromised", "Suspicious token reuse detected. Please log in again."));
        }

        if (existingToken.IsExpired)
        {
            return Result<AuthResponse>.Failure(Error.Unauthorized("Auth.TokenExpired", "Refresh token has expired. Please log in again."));
        }

        var user = existingToken.User;
        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure(Error.Unauthorized("Auth.InactiveAccount", "Your account is inactive."));
        }

        var primaryMembership = user.OrganizationMemberships.FirstOrDefault();
        if (primaryMembership is null)
        {
            return Result<AuthResponse>.Failure(Error.Failure("Auth.NoOrganization", "User has no active organization."));
        }

        // Token Rotation: Revoke current and issue brand new refresh token
        var newRefreshToken = GenerateRefreshToken(user.Id);
        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var organization = primaryMembership.Organization;
        var role = primaryMembership.Role.ToString();
        var newAccessToken = _jwtTokenGenerator.GenerateToken(user, organization.Id, role);

        return Result<AuthResponse>.Success(new AuthResponse(
            Token: newAccessToken,
            RefreshToken: newRefreshToken.Token,
            User: new UserDto(user.Id, user.Email, user.FullName, user.AvatarUrl),
            Organization: new OrganizationDto(organization.Id, organization.Name, organization.Slug, role)
        ));
    }

    public async Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (existingToken is null || existingToken.IsRevoked)
        {
            return Result.Failure(Error.NotFound("Auth.TokenNotFound", "Token not found or already revoked."));
        }

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static RefreshToken GenerateRefreshToken(Guid userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string GenerateSlug(string name)
    {
        var cleaned = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        cleaned = Regex.Replace(cleaned, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(cleaned) ? "workspace" : cleaned;
    }
}
