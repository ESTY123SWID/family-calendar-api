using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Tasks;

public class AssignTaskRequest
{
    // A member volunteers by sending their own id.
    // The organizer / family manager may send any family member
    [Display(Name = "משתמש")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public int? UserId { get; set; }
}
