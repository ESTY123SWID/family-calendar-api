namespace my_project.Service.Options;

// Bound from the "Jwt" configuration section. Key comes from User Secrets in Development and from
// the hosting environment's secret settings in Production (Jwt__Key) – never from appsettings
public class JwtSettings
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 needs a key of at least 256 bits
    public const int MinKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    // At least 32 bytes (HMAC-SHA256)
    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
