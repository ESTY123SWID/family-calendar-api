using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Participants;

public class InviteParticipantsRequest
{
    // Every id must be a member of the event family. Already invited users are skipped
    [Display(Name = "מוזמנים")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [MinLength(1, ErrorMessage = ValidationMessages.MinItems)]
    public List<int> UserIds { get; set; } = new();
}
