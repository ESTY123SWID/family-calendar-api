using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;
using my_project.Core.Enums;

namespace my_project.Core.DTOs.Participants;

public class RsvpRequest
{
    // Coming / NotComing / Maybe (Pending is not an answer)
    [Display(Name = "אישור הגעה")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(RsvpStatus), ErrorMessage = ValidationMessages.InvalidValue)]
    public RsvpStatus? Status { get; set; }

    // How many people come with the participant
    [Display(Name = "מספר מלווים")]
    [Range(0, 20, ErrorMessage = ValidationMessages.Range)]
    public int GuestsCount { get; set; }
}
