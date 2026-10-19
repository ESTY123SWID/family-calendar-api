using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Users;

public class UpdateProfileRequest
{
    [Display(Name = "שם מלא")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    public string FullName { get; set; } = string.Empty;
}
