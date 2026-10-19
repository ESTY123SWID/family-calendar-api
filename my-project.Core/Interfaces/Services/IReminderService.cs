namespace my_project.Core.Interfaces.Services;

// Called periodically by a background job
public interface IReminderService
{
    // Sends every reminder that is due and was not sent yet. Returns the number of emails sent
    Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
