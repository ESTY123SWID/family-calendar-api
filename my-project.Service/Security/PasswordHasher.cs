using Microsoft.AspNetCore.Identity;
using my_project.Core.Entities;
using IPasswordHasher = my_project.Core.Interfaces.Infrastructure.IPasswordHasher;

namespace my_project.Service.Security;

// Wraps the ASP.NET Core Identity hasher (PBKDF2 with a random salt per password)
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        // The hasher does not use the user instance
        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(null!, passwordHash, password);
        return result != PasswordVerificationResult.Failed;
    }
}
