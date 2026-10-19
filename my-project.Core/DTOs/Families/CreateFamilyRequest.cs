using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Families;

public class CreateFamilyRequest
{
    [Display(Name = "שם המשפחה")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    public string Name { get; set; } = string.Empty;
}
