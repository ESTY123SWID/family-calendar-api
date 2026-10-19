namespace my_project.Core.Interfaces.Infrastructure;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
