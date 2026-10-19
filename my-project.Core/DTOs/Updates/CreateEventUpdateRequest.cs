using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Updates;

public class CreateEventUpdateRequest
{
    [Display(Name = "תוכן")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(1000, ErrorMessage = ValidationMessages.MaxLength)]
    public string Content { get; set; } = string.Empty;
}
