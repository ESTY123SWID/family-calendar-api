using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebUtilities;
using my_project.API.Middleware;
using my_project.Core.DTOs.Common;

namespace my_project.API.Auth;

// JwtBearer events: 401 and 403 are returned as ErrorResponse, like every other error,
// instead of ASP.NET's default empty body
public static class AuthErrorResponses
{
    private const string UnauthorizedMessage = "נדרשת התחברות. יש להתחבר ולנסות שוב.";
    private const string ForbiddenMessage = "אין לך הרשאה לבצע פעולה זו.";

    // No token, or a token that is invalid / expired / signed with another key.
    // The reason is not returned (the default WWW-Authenticate header would say e.g. "the token expired at ...")
    public static Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;

        return WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, UnauthorizedMessage);
    }

    // A valid token, but the user's role is not allowed (e.g. a regular user on /api/admin)
    public static Task WriteForbiddenAsync(ForbiddenContext context)
    {
        return WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, ForbiddenMessage);
    }

    private static Task WriteAsync(HttpContext context, int status, string detail)
    {
        context.Response.StatusCode = status;

        return context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
            CorrelationId = context.Items[CorrelationIdMiddleware.ItemKey] as string
        });
    }
}
