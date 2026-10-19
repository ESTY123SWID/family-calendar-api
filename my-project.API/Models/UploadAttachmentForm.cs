using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;
using my_project.Core.Enums;

namespace my_project.API.Models;

// The multipart form of an upload. Lives in the API because IFormFile is an ASP.NET type;
// the controller turns it into UploadAttachmentRequest (Stream + details) for the service
public class UploadAttachmentForm
{
    [Display(Name = "קובץ")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public IFormFile File { get; set; } = null!;

    [Display(Name = "סוג הקובץ")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(AttachmentKind), ErrorMessage = ValidationMessages.InvalidValue)]
    public AttachmentKind? Kind { get; set; }
}
