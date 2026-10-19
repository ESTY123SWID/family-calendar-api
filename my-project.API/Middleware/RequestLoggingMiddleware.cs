using System.Diagnostics;

namespace my_project.API.Middleware;

// One Information line per request: method, path, status code, duration and the CorrelationId.
// Never logs the query string, headers or body – they may contain passwords, tokens or personal data.
// Placed after CorrelationIdMiddleware and inside ExceptionHandlingMiddleware
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        // Written when the response is complete, not right after _next: when a request fails, the status code
        // is set later by ExceptionHandlingMiddleware (e.g. 409 or 500), so only then is it the real one
        context.Response.OnCompleted(() =>
        {
            LogRequest(context, Stopwatch.GetElapsedTime(startTimestamp));
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private void LogRequest(HttpContext context, TimeSpan elapsed)
    {
        // OnCompleted runs after the CorrelationId scope was closed, so it is opened again for this line
        var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] as string;

        using (_logger.BeginScope(new Dictionary<string, object?> { [CorrelationIdMiddleware.ItemKey] = correlationId }))
        {
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0} ms{Aborted}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds,
                context.RequestAborted.IsCancellationRequested ? " (aborted by the client)" : string.Empty);
        }
    }
}
