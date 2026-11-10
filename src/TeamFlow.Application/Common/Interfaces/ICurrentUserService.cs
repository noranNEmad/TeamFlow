namespace TeamFlow.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

