using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 11);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
    }
}

