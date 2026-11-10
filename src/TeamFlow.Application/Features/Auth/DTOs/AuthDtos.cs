namespace TeamFlow.Application.Features.Auth.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string? AvatarUrl
);

public record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    string Role
);

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string OrganizationName
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    UserDto User,
    OrganizationDto Organization
);
