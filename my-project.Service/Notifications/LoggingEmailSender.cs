using Microsoft.Extensions.Logging;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Models;

namespace my_project.Service.Notifications;

// Used when Email:Enabled is false (a machine without an SMTP server, automated tests):
// nothing is sent – a line is written to the log instead, so no real email goes out by mistake.
// The recipient's address and the body are not logged (personal data)
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email sending is disabled (Email:Enabled = false); not sent: subject {Subject}", message.Subject);
        return Task.CompletedTask;
    }
}
