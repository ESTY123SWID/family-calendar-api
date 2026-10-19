namespace my_project.Service.Options;

// Bound from the "Reminders" configuration section and validated when the server starts
public class ReminderSettings
{
    public const string SectionName = "Reminders";

    // false = the background job starts, logs that it is disabled, and does nothing
    public bool Enabled { get; set; } = true;

    // How often ReminderBackgroundService runs ReminderService. A fraction is allowed (used by tests)
    public double IntervalMinutes { get; set; } = 15;
}
