using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;
using my_project.Core.Enums;

namespace my_project.Core.DTOs.Families;

public class ChangeMemberRoleRequest
{
    // Nullable so that [Required] rejects a missing value instead of defaulting to Member
    [Display(Name = "תפקיד")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(FamilyRole), ErrorMessage = ValidationMessages.InvalidValue)]
    public FamilyRole? Role { get; set; }
}
