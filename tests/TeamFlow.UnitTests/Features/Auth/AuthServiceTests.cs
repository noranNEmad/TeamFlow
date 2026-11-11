using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TeamFlow.Application.Features.Auth.DTOs;
using TeamFlow.Application.Features.Auth.Services;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Infrastructure.Security;
using Xunit;

namespace TeamFlow.UnitTests.Features.Auth;

public class AuthServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TeamFlow_Test_{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static (AuthService service, ApplicationDbContext context) CreateService()
    {
        var context = CreateInMemoryDbContext();
        var passwordHasher = new BcryptPasswordHasher();

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "JwtSettings:Secret", "A_Super_Secret_Testing_Key_At_Least_32_Chars_Long!" },
            { "JwtSettings:Issuer", "TeamFlow" },
            { "JwtSettings:Audience", "TeamFlowUsers" },
            { "JwtSettings:ExpiryMinutes", "60" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        var tokenGenerator = new JwtTokenGenerator(configuration);
        var authService = new AuthService(context, passwordHasher, tokenGenerator);

        return (authService, context);
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsSuccessAndCreatesUserAndOrg()
    {
        // Arrange
        var (authService, context) = CreateService();
        var request = new RegisterRequest(
            FullName: "Sarah Connor",
            Email: "sarah@cyberdyne.io",
            Password: "SecurePassword123!",
            OrganizationName: "Cyberdyne Systems"
        );

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Token);
        Assert.Equal("sarah@cyberdyne.io", result.Value.User.Email);
        Assert.Equal("Cyberdyne Systems", result.Value.Organization.Name);
        Assert.Equal("Owner", result.Value.Organization.Role);

        // Verify database state
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "sarah@cyberdyne.io");
        Assert.NotNull(savedUser);
        Assert.Equal("Sarah Connor", savedUser.FullName);

        var savedOrg = await context.Organizations.FirstOrDefaultAsync(o => o.Name == "Cyberdyne Systems");
        Assert.NotNull(savedOrg);
        Assert.Equal("cyberdyne-systems", savedOrg.Slug);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsConflictError()
    {
        // Arrange
        var (authService, _) = CreateService();
        var request1 = new RegisterRequest("User One", "duplicate@test.com", "Password123!", "Org One");
        var request2 = new RegisterRequest("User Two", "duplicate@test.com", "Password456!", "Org Two");

        await authService.RegisterAsync(request1);

        // Act
        var result = await authService.RegisterAsync(request2);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.EmailExists", result.Error.Code);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessAndToken()
    {
        // Arrange
        var (authService, _) = CreateService();
        var registerRequest = new RegisterRequest("John Doe", "john@test.com", "MySecretPassword123!", "John Corp");
        await authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest("john@test.com", "MySecretPassword123!");

        // Act
        var result = await authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Token);
        Assert.Equal("john@test.com", result.Value.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var (authService, _) = CreateService();
        var registerRequest = new RegisterRequest("John Doe", "john2@test.com", "MySecretPassword123!", "John Corp");
        await authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest("john2@test.com", "WrongPassword!");

        // Act
        var result = await authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_RotatesTokenAndReturnsNewTokens()
    {
        // Arrange
        var (authService, context) = CreateService();
        var registerResult = await authService.RegisterAsync(new RegisterRequest("Alex Doe", "alex@test.com", "Password123!", "Alex Org"));
        var originalRefreshToken = registerResult.Value.RefreshToken;

        // Act
        var refreshResult = await authService.RefreshTokenAsync(originalRefreshToken);

        // Assert
        Assert.True(refreshResult.IsSuccess);
        Assert.NotEqual(originalRefreshToken, refreshResult.Value.RefreshToken);
        Assert.NotEmpty(refreshResult.Value.Token);

        // Verify old token is marked as revoked
        var oldToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == originalRefreshToken);
        Assert.NotNull(oldToken);
        Assert.True(oldToken.IsRevoked);
        Assert.Equal(refreshResult.Value.RefreshToken, oldToken.ReplacedByToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithReusedRevokedToken_RevokesAllTokensAndReturnsCompromised()
    {
        // Arrange
        var (authService, context) = CreateService();
        var registerResult = await authService.RegisterAsync(new RegisterRequest("Compromised User", "comp@test.com", "Password123!", "Comp Org"));
        var stolenToken = registerResult.Value.RefreshToken;

        // First legit refresh
        var legitimateRefresh = await authService.RefreshTokenAsync(stolenToken);
        Assert.True(legitimateRefresh.IsSuccess);

        // Act: Attacker attempts to use stolen token again (Token Reuse!)
        var maliciousAttempt = await authService.RefreshTokenAsync(stolenToken);

        // Assert
        Assert.True(maliciousAttempt.IsFailure);
        Assert.Equal("Auth.TokenCompromised", maliciousAttempt.Error.Code);

        // Verify ALL active tokens for this user were revoked for security
        var anyActive = await context.RefreshTokens.AnyAsync(rt => rt.UserId == registerResult.Value.User.Id && rt.RevokedAt == null);
        Assert.False(anyActive);
    }
}

