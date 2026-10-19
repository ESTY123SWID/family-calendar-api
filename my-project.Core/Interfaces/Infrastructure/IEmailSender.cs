using my_project.Core.Models;

namespace my_project.Core.Interfaces.Infrastructure;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
