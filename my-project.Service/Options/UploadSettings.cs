namespace my_project.Service.Options;

// Bound from the "Uploads" configuration section
public class UploadSettings
{
    public const string SectionName = "Uploads";

    public int MaxSizeMb { get; set; } = 5;

    public long MaxSizeBytes => MaxSizeMb * 1024L * 1024L;

    // Lower case, with the leading dot. Comes only from configuration (Uploads:AllowedExtensions):
    // a default list here would be appended to by the configuration binder, not replaced.
    // Validated on startup by UploadSettingsValidator
    public string[] AllowedExtensions { get; set; } = [];
}
