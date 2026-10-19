using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.WebUtilities;
using my_project.Core.DTOs.Common;
using my_project.Core.Exceptions;

namespace my_project.API.Middleware;

// The safety net of the API: an exception that no service handled becomes an ErrorResponse JSON
// with a suitable status code, instead of a crash or an HTML error page.
// The full exception goes to the log only – the client never sees stack traces or internal messages.
// First in the pipeline, so it also catches exceptions thrown by the other middlewares
public class ExceptionHandlingMiddleware
{
    private const string UnexpectedErrorMessage = "אירעה שגיאה בלתי צפויה. נסו שוב מאוחר יותר.";
    private const string ConcurrencyConflictMessage = "הנתונים עודכנו על ידי משתמש אחר באותו רגע. יש לטעון אותם מחדש ולנסות שוב.";
    private const string DuplicateValueMessage = "הערך שנשלח כבר קיים במערכת.";
    private const string RequestTooLargeMessage = "הבקשה גדולה מהמותר. ייתכן שהקובץ גדול מדי.";
    private const string InvalidRequestMessage = "הבקשה אינה תקינה.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client closed the connection – nobody is waiting for a response, and it is not a server error
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] as string;
        var (status, detail) = MapException(exception);

        // The CorrelationId scope was already closed when the exception left the inner middlewares,
        // so it is opened again here for the log line
        using (_logger.BeginScope(new Dictionary<string, object?> { [CorrelationIdMiddleware.ItemKey] = correlationId }))
        {
            if (status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning("{ExceptionType} for {Method} {Path} returned {StatusCode}",
                    exception.GetType().Name, context.Request.Method, context.Request.Path, status);
            }
        }

        // Part of the response was already sent – it cannot be replaced with an error, only aborted
        if (context.Response.HasStarted)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        context.Response.Clear();
        context.Response.StatusCode = status;

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
            CorrelationId = correlationId
        });
    }

    private static (int Status, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            // Services already turn this into 409 where they expect it; this covers any place that does not
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, ConcurrencyConflictMessage),

            // A unique value (e.g. email) was taken between the service's check and the save
            DuplicateValueException => (StatusCodes.Status409Conflict, DuplicateValueMessage),

            // Thrown by ASP.NET itself for an invalid request (e.g. a body that is too large or malformed)
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } =>
                (StatusCodes.Status413PayloadTooLarge, RequestTooLargeMessage),
            BadHttpRequestException badRequest => (badRequest.StatusCode, InvalidRequestMessage),

            _ => (StatusCodes.Status500InternalServerError, UnexpectedErrorMessage)
        };
    }
}
