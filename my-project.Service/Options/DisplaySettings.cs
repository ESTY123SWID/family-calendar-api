namespace my_project.Service.Options;

// Bound from the "Display" configuration section.
// Dates are stored in UTC; this time zone is used only for what people read (emails, "today")
public class DisplaySettings
{
    public const string SectionName = "Display";

    // IANA id (Windows ids work too on .NET 8)
    public string TimeZoneId { get; set; } = "Asia/Jerusalem";
}
