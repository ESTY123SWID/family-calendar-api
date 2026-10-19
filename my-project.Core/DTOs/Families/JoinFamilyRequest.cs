using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Families;

public class JoinFamilyRequest
{
    [Display(Name = "קוד הצטרפות")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(20, ErrorMessage = ValidationMessages.MaxLength)]
    public string JoinCode { get; set; } = string.Empty;
}
