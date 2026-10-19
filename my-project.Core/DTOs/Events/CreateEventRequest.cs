using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Events;

public class CreateEventRequest
{
    [Display(Name = "כותרת")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(150, ErrorMessage = ValidationMessages.MaxLength)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "תיאור")]
    [StringLength(2000, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Description { get; set; }

    // UTC. Must be in the future (checked in the service)
    [Display(Name = "מועד התחלה")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public DateTime? StartAt { get; set; }

    // UTC. Must be after StartAt (checked in the service)
    public DateTime? EndAt { get; set; }

    [Display(Name = "שם המקום")]
    [StringLength(150, ErrorMessage = ValidationMessages.MaxLength)]
    public string? LocationName { get; set; }

    [Display(Name = "כתובת")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(300, ErrorMessage = ValidationMessages.MaxLength)]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "הוראות הגעה")]
    [StringLength(1000, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Directions { get; set; }

    // true = invite every family member; false = only InviteeIds
    public bool InviteAll { get; set; } = true;

    // Used when InviteAll is false. Every id must be a member of the family
    public List<int>? InviteeIds { get; set; }
}
