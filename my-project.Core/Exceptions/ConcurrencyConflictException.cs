namespace my_project.Core.Exceptions;

// Thrown by the Data layer when a concurrency token (Version) no longer matches,
// so that upper layers can return 409 without depending on EF Core
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
