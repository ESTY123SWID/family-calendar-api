namespace my_project.Core.Entities;

// One-to-one with User. Created automatically on registration
public class NotificationSettings
{
    // Primary key and FK → User (Cascade)
    public int UserId { get; set; }

    // Reminder before an event
    public bool EventReminders { get; set; } = true;

    // 1–14
    public int DaysBeforeEvent { get; set; } = 3;

    // Reminder on the day of the event
    public bool EventDayReminder { get; set; } = true;

    // Task pending completion
    public bool TaskReminders { get; set; } = true;

    // My task is overdue or was changed
    public bool TaskChangeNotices { get; set; } = true;

    // Event changed or cancelled
    public bool EventChangeNotices { get; set; } = true;

    // Invitation + RSVP reminder
    public bool InvitationNotices { get; set; } = true;

    public User User { get; set; } = null!;
}
