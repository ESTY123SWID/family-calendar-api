namespace my_project.Core.Interfaces.Infrastructure;

// The authenticated user of the current request.
// Implemented in the API layer (from the JWT claims)
public interface ICurrentUser
{
    int UserId { get; }

    string Email { get; }

    bool IsSystemAdmin { get; }
}
