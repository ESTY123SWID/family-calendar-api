namespace my_project.API.Middleware;

// Gives every request a correlation id, so all the log lines of one request can be found together.
// The id is taken from the X-Correlation-ID request header when the client sent a valid Guid,
// otherwise a new Guid is created. It is stored in HttpContext.Items, returned in the response
// header, and added to the logging scope (NLog: ${scopeproperty:CorrelationId})
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context.Request);
        context.Items[ItemKey] = correlationId;

        // Added right before the response is sent, so the header survives even when
        // the response is cleared and rewritten later (e.g. by the exception handler)
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            await _next(context);
        }
    }

    // Only a valid Guid is accepted from the client – any other text could be used to inject fake lines into the logs
    private static string GetOrCreateCorrelationId(HttpRequest request)
    {
        return Guid.TryParse(request.Headers[HeaderName].ToString(), out var clientId)
            ? clientId.ToString()
            : Guid.NewGuid().ToString();
    }
}
