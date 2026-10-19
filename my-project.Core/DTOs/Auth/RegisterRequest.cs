using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Auth;

public class RegisterRequest
{
    [Display(Name = "שם מלא")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "אימייל")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(200, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "סיסמה")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = ValidationMessages.LengthRange)]
    public string Password { get; set; } = string.Empty;
}
