namespace my_project.Core.DTOs.Notifications;

public class NotificationSettingsDto
{
    public bool EventReminders { get; set; }

    public int DaysBeforeEvent { get; set; }

    public bool EventDayReminder { get; set; }

    public bool TaskReminders { get; set; }

    public bool TaskChangeNotices { get; set; }

    public bool EventChangeNotices { get; set; }

    public bool InvitationNotices { get; set; }
}
