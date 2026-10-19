namespace my_project.Core.Exceptions;

// Thrown by the Data layer when a unique constraint rejects a value that already exists
// (e.g. two registrations with the same email at the same moment),
// so that upper layers can return 409 without depending on EF Core or PostgreSQL
public class DuplicateValueException : Exception
{
    public DuplicateValueException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
