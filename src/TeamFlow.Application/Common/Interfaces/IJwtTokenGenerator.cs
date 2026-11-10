using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, Guid organizationId, string role);
}

