using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using my_project.API.Middleware;
using my_project.Core.DTOs.Common;

namespace my_project.API.Validation;

// Replaces ASP.NET's default 400 (ValidationProblemDetails) with the API's single error format.
// "errors" holds the messages of every invalid field, keyed by the field name as it appears in the JSON
// (camelCase), so the client can show each message next to its field.
// Used as ApiBehaviorOptions.InvalidModelStateResponseFactory
public static class ValidationErrorResponseFactory
{
    private const string ValidationFailedMessage = "חלק מהנתונים שנשלחו אינם תקינים.";

    // The JSON parser's own messages are in English and mention internal type names – never sent to the client
    private const string InvalidFormatMessage = "הערך שנשלח אינו בפורמט תקין.";

    private const string JsonPathRoot = "$";

    public static IActionResult Create(ActionContext context)
    {
        var invalidEntries = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToList();

        // When the body could not be parsed, ASP.NET also reports the action parameter itself
        // ("The request field is required.") – a duplicate of the parser error, and in English
        if (invalidEntries.Any(entry => IsJsonPath(entry.Key)))
        {
            var parameterNames = context.ActionDescriptor.Parameters.Select(parameter => parameter.Name).ToHashSet();
            invalidEntries.RemoveAll(entry => parameterNames.Contains(entry.Key));
        }

        var errors = invalidEntries
            .ToDictionary(
                entry => ToClientFieldName(entry.Key),
                entry => IsJsonPath(entry.Key)
                    ? [InvalidFormatMessage]
                    : entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        var body = new ErrorResponse
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
            Detail = ValidationFailedMessage,
            CorrelationId = context.HttpContext.Items[CorrelationIdMiddleware.ItemKey] as string,
            Errors = errors
        };

        return new BadRequestObjectResult(body)
        {
            ContentTypes = { "application/json" }
        };
    }

    // An error from the JSON parser is keyed by its JSON path: "$" or "$.email"
    private static bool IsJsonPath(string key)
    {
        return key == JsonPathRoot || key.StartsWith(JsonPathRoot + ".", StringComparison.Ordinal);
    }

    // "Email" → "email", "$.email" → "email", "Items[0].Name" → "items[0].name"
    private static string ToClientFieldName(string key)
    {
        if (IsJsonPath(key))
        {
            key = key.Length > JsonPathRoot.Length ? key[(JsonPathRoot.Length + 1)..] : string.Empty;
        }

        return string.Join('.', key.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }
}
