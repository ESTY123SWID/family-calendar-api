using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using my_project.API.Middleware;
using my_project.Core.DTOs.Common;
using my_project.Service.Options;

namespace my_project.API.Filters;

// Limits the whole HTTP request of an upload to Uploads:MaxSizeMb plus room for the multipart framing.
// An oversized upload is then rejected while it is being received (413) – before ASP.NET buffers it –
// and not only afterwards by AttachmentService. The limit follows UploadSettings, so the two never drift apart
[AttributeUsage(AttributeTargets.Method)]
public sealed class UploadSizeLimitAttribute : Attribute, IFilterFactory
{
    // Boundaries, part headers and the "kind" field around the file itself
    public const long MultipartOverheadBytes = 64 * 1024;

    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return new UploadSizeLimitFilter(serviceProvider.GetRequiredService<IOptions<UploadSettings>>().Value);
    }

    // A resource filter runs before model binding, i.e. before the body is read
    private sealed class UploadSizeLimitFilter : IAsyncResourceFilter
    {
        private const string TooLargeMessage = "הקובץ גדול מהמותר ({0}MB).";
        private const string InvalidFormMessage = "הבקשה אינה תקינה. יש לשלוח קובץ אחד בטופס multipart/form-data.";

        private readonly UploadSettings _settings;

        public UploadSizeLimitFilter(UploadSettings settings)
        {
            _settings = settings;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var httpContext = context.HttpContext;

            var bodySize = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodySize is { IsReadOnly: false })
            {
                bodySize.MaxRequestBodySize = _settings.MaxSizeBytes + MultipartOverheadBytes;
            }

            // The form is read here, so a body that is too large or malformed gets a clear ErrorResponse.
            // Otherwise model binding swallows the error into a 400 with ASP.NET's English message.
            // The form is cached, so model binding does not read the body again
            if (httpContext.Request.HasFormContentType)
            {
                try
                {
                    await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
                }
                catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
                {
                    context.Result = Error(httpContext, StatusCodes.Status413PayloadTooLarge, string.Format(TooLargeMessage, _settings.MaxSizeMb));
                    return;
                }
                catch (Exception exception) when (exception is InvalidDataException or IOException)
                {
                    context.Result = Error(httpContext, StatusCodes.Status400BadRequest, InvalidFormMessage);
                    return;
                }
            }

            await next();
        }

        private static ObjectResult Error(HttpContext httpContext, int status, string detail)
        {
            return new ObjectResult(new ErrorResponse
            {
                Status = status,
                Title = ReasonPhrases.GetReasonPhrase(status),
                Detail = detail,
                CorrelationId = httpContext.Items[CorrelationIdMiddleware.ItemKey] as string
            })
            {
                StatusCode = status,
                ContentTypes = { "application/json" }
            };
        }
    }
}
