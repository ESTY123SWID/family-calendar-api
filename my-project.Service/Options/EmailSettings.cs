namespace my_project.Service.Options;

// Bound from the "Email" configuration section and validated when the server starts.
// Password is a secret: User Secrets in Development (Email:Password), the hosting environment's
// secret settings in Production (Email__Password) – never appsettings
public class EmailSettings
{
    public const string SectionName = "Email";

    // false = emails are not sent; LoggingEmailSender writes a line to the log instead
    // (a machine without an SMTP server, automated tests). Never sends a real email by mistake
    public bool Enabled { get; set; } = true;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public EmailSecurity Security { get; set; } = EmailSecurity.StartTls;

    // The sender address, e.g. no-reply@family-calendar.example
    public string From { get; set; } = string.Empty;

    public string FromName { get; set; } = "לוח אירועים משפחתי";

    // Both empty = a server without authentication (e.g. smtp4dev in Development)
    public string? Username { get; set; }

    public string? Password { get; set; }

    // Connect / send timeout, so a stuck SMTP server does not hold a request forever
    public int TimeoutSeconds { get; set; } = 30;
}

public enum EmailSecurity
{
    // Plain connection – only for a local test server such as smtp4dev
    None,

    // TLS from the first byte (usually port 465)
    SslOnConnect,

    // Plain connection upgraded with STARTTLS; fails if the server does not support it (usually port 587)
    StartTls
}
