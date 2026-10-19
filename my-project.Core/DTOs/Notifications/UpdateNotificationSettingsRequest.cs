using System.ComponentModel.DataAnnotations;
using my_project.Core.Common;

namespace my_project.Core.DTOs.Notifications;

public class UpdateNotificationSettingsRequest
{
    public bool EventReminders { get; set; }

    [Display(Name = "ימים לפני האירוע")]
    [Range(1, 14, ErrorMessage = ValidationMessages.Range)]
    public int DaysBeforeEvent { get; set; } = 3;

    public bool EventDayReminder { get; set; }

    public bool TaskReminders { get; set; }

    public bool TaskChangeNotices { get; set; }

    public bool EventChangeNotices { get; set; }

    public bool InvitationNotices { get; set; }
}
