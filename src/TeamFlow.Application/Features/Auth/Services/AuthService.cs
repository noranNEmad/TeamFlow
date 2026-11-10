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
        // 1. Validate inputs
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure(Error.Validation("Auth.InvalidInput", "Email and password are required."));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Check if email exists
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Result<AuthResponse>.Failure(Error.Conflict("Auth.EmailExists", "An account with this email already exists."));
        }

        // 3. Create User
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true
        };

        // 4. Generate unique slug for Organization
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

        // 5. Link User as Owner of Organization
        var membership = new OrganizationMember
        {
            Organization = organization,
            User = user,
            Role = OrganizationRole.Owner
        };

        _context.Users.Add(user);
        _context.Organizations.Add(organization);
        _context.OrganizationMembers.Add(membership);

        await _context.SaveChangesAsync(cancellationToken);

        // 6. Generate Token
        var role = OrganizationRole.Owner.ToString();
        var token = _jwtTokenGenerator.GenerateToken(user, organization.Id, role);

        return Result<AuthResponse>.Success(new AuthResponse(
            Token: token,
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

        return Result<AuthResponse>.Success(new AuthResponse(
            Token: token,
            User: new UserDto(user.Id, user.Email, user.FullName, user.AvatarUrl),
            Organization: new OrganizationDto(organization.Id, organization.Name, organization.Slug, role)
        ));
    }

    private static string GenerateSlug(string name)
    {
        var cleaned = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        cleaned = Regex.Replace(cleaned, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(cleaned) ? "workspace" : cleaned;
    }
}

