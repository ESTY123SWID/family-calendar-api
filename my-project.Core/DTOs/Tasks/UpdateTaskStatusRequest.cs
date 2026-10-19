using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;
using my_project.Core.Enums;

namespace my_project.Core.DTOs.Tasks;

public class UpdateTaskStatusRequest
{
    // Assigned / InProgress / Done. A task goes back to Open only by removing its assignee
    [Display(Name = "סטטוס")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(FamilyTaskStatus), ErrorMessage = ValidationMessages.InvalidValue)]
    public FamilyTaskStatus? Status { get; set; }
}
