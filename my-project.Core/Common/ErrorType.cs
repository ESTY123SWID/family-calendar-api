namespace my_project.Core.Common;

// The reason a service operation failed. The API translates it to an HTTP status
public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Forbidden,
    Conflict,
    Unauthorized
}
