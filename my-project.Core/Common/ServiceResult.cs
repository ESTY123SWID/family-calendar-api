namespace my_project.Core.Common;

// The outcome of a service operation without a return value.
// Expected failures (not found, forbidden, conflict...) are returned, not thrown
public class ServiceResult
{
    protected ServiceResult(ErrorType errorType, string? errorMessage)
    {
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess => ErrorType == ErrorType.None;

    public ErrorType ErrorType { get; }

    public string? ErrorMessage { get; }

    public static ServiceResult Ok() => new(ErrorType.None, null);

    public static ServiceResult Validation(string message) => new(ErrorType.Validation, message);

    public static ServiceResult NotFound(string message) => new(ErrorType.NotFound, message);

    public static ServiceResult Forbidden(string message) => new(ErrorType.Forbidden, message);

    public static ServiceResult Conflict(string message) => new(ErrorType.Conflict, message);

    public static ServiceResult Unauthorized(string message) => new(ErrorType.Unauthorized, message);
}

// The outcome of a service operation that returns data on success
public class ServiceResult<T> : ServiceResult
{
    private ServiceResult(T? data, ErrorType errorType, string? errorMessage)
        : base(errorType, errorMessage)
    {
        Data = data;
    }

    public T? Data { get; }

    public static ServiceResult<T> Ok(T data) => new(data, ErrorType.None, null);

    public static new ServiceResult<T> Validation(string message) => new(default, ErrorType.Validation, message);

    public static new ServiceResult<T> NotFound(string message) => new(default, ErrorType.NotFound, message);

    public static new ServiceResult<T> Forbidden(string message) => new(default, ErrorType.Forbidden, message);

    public static new ServiceResult<T> Conflict(string message) => new(default, ErrorType.Conflict, message);

    public static new ServiceResult<T> Unauthorized(string message) => new(default, ErrorType.Unauthorized, message);

    // Carries a failure of a non-generic result (e.g. a failed permission check) over to this type
    public static ServiceResult<T> FromFailure(ServiceResult failure)
    {
        if (failure.IsSuccess)
        {
            throw new ArgumentException("Only a failed result can be converted.", nameof(failure));
        }

        return new(default, failure.ErrorType, failure.ErrorMessage);
    }
}
