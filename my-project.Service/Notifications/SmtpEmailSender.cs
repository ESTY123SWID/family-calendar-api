using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Models;
using my_project.Service.Options;

namespace my_project.Service.Notifications;

// Sends emails through an SMTP server with MailKit (used when Email:Enabled is true).
// One connection per email – the system sends few emails, and a fresh connection never reuses a broken one.
// A failure is thrown to the caller: NotificationService logs it and continues with the next recipient,
// ReminderService logs it and releases the reminder so the next run retries it.
// Never logged: the password, the recipient's address, the body
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var mimeMessage = BuildMessage(message);
        using var client = new SmtpClient { Timeout = (int)TimeSpan.FromSeconds(_settings.TimeoutSeconds).TotalMilliseconds };

        await client.ConnectAsync(_settings.Host, _settings.Port, ToSocketOptions(_settings.Security), cancellationToken);

        if (!string.IsNullOrEmpty(_settings.Username))
        {
            // The startup validation guarantees a password whenever a username is set
            await client.AuthenticateAsync(_settings.Username, _settings.Password!, cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        _logger.LogInformation("Email sent: subject {Subject}", message.Subject);
    }

    // Plain text (the templates are plain text), UTF-8 for Hebrew.
    // MimeKit encodes the subject and addresses, so a value from the user cannot inject extra headers
    private MimeMessage BuildMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.From));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new TextPart(TextFormat.Plain) { Text = message.Body };

        return mimeMessage;
    }

    private static SecureSocketOptions ToSocketOptions(EmailSecurity security)
    {
        return security switch
        {
            EmailSecurity.None => SecureSocketOptions.None,
            EmailSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            EmailSecurity.StartTls => SecureSocketOptions.StartTls,
            _ => throw new ArgumentOutOfRangeException(nameof(security), security, "Unknown Email:Security value.")
        };
    }
}
