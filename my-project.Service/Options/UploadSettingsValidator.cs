using Microsoft.Extensions.Options;
using my_project.Service.Common;

namespace my_project.Service.Options;

// Checked when the server starts: every allowed extension must be one the system can verify
// (see FileTypes), and the size limit must be sensible
public class UploadSettingsValidator : IValidateOptions<UploadSettings>
{
    private const int MaxAllowedSizeMb = 50;

    public ValidateOptionsResult Validate(string? name, UploadSettings options)
    {
        var errors = new List<string>();

        if (options.MaxSizeMb is < 1 or > MaxAllowedSizeMb)
        {
            errors.Add($"Uploads:MaxSizeMb must be between 1 and {MaxAllowedSizeMb}.");
        }

        if (options.AllowedExtensions.Length == 0)
        {
            errors.Add("Uploads:AllowedExtensions is empty.");
        }

        var unsupported = options.AllowedExtensions.Where(extension => !FileTypes.TryGet(extension, out _)).ToList();
        if (unsupported.Count > 0)
        {
            errors.Add($"Uploads:AllowedExtensions contains unsupported types: {string.Join(", ", unsupported)}.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
