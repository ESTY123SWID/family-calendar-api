namespace my_project.Service.Common;

internal static class DateTimeExtensions
{
    // All dates are stored in UTC. A value without a time zone (Kind = Unspecified) is treated as UTC
    public static DateTime ToUtc(this DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime? ToUtc(this DateTime? value)
    {
        return value?.ToUtc();
    }
}
