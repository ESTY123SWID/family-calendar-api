using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using my_project.API.Middleware;
using my_project.Core.Common;
using my_project.Core.DTOs.Common;

namespace my_project.API.Controllers;

// Translates a ServiceResult into an HTTP response, so controllers stay thin:
// receive → call the service → return the result
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    // Success → 204 No Content
    protected IActionResult FromResult(ServiceResult result)
    {
        return result.IsSuccess ? NoContent() : Error(result);
    }

    // Success → 200 OK with the data
    protected IActionResult FromResult<T>(ServiceResult<T> result)
    {
        return result.IsSuccess ? Ok(result.Data) : Error(result);
    }

    // Success → 201 Created with a Location header pointing at createdAtAction
    protected IActionResult CreatedFromResult<T>(
        ServiceResult<T> result,
        string actionName,
        Func<T, object> routeValues,
        string? controllerName = null)
    {
        return result.IsSuccess
            ? CreatedAtAction(actionName, controllerName, routeValues(result.Data!), result.Data)
            : Error(result);
    }

    // The single error format of the API (ErrorResponse)
    private ObjectResult Error(ServiceResult result)
    {
        var status = result.ErrorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var body = new ErrorResponse
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = result.ErrorMessage,
            CorrelationId = HttpContext.Items[CorrelationIdMiddleware.ItemKey] as string
        };

        return StatusCode(status, body);
    }
}
