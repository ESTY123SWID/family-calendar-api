using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace my_project.Data.Converters;

// PostgreSQL (timestamp with time zone) accepts only DateTimeKind.Utc.
// All dates in the system are UTC, so an Unspecified value is marked as UTC on write,
// a Local value is converted, and every value is marked as UTC when read
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
