using Microsoft.Extensions.Options;
using my_project.Service.Options;

namespace my_project.Service.Common;

// Converts stored UTC times to the display time zone (Israel by default, including daylight saving time)
public class LocalTimeConverter
{
    private readonly TimeZoneInfo _timeZone;

    public LocalTimeConverter(IOptions<DisplaySettings> settings)
    {
        // Throws at startup if the id is wrong, instead of sending emails with wrong times
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.TimeZoneId);
    }

    public DateTime ToLocal(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _timeZone);
    }

    // The local calendar date of a UTC moment, e.g. for "the event is today"
    public DateTime LocalDate(DateTime utc)
    {
        return ToLocal(utc).Date;
    }
}
