using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;
using my_project.Core.Enums;

namespace my_project.Core.DTOs.Admin;

// Only the fields that are sent are changed
public class AdminUpdateUserRequest
{
    // false = block, true = unblock
    public bool? IsActive { get; set; }

    [Display(Name = "תפקיד מערכת")]
    [EnumDataType(typeof(SystemRole), ErrorMessage = ValidationMessages.InvalidValue)]
    public SystemRole? Role { get; set; }
}
