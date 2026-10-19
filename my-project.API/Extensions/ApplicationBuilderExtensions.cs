using my_project.API.Middleware;

namespace my_project.API.Extensions;

// The project's middlewares, used in Program.cs as app.UseXxx()
public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    // The policy registered by AddClientCors
    public static IApplicationBuilder UseClientCors(this IApplicationBuilder app)
    {
        return app.UseCors(ServiceCollectionExtensions.ClientCorsPolicy);
    }
}
