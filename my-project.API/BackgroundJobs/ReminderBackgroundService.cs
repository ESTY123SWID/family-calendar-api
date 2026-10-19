using Microsoft.Extensions.Options;
using my_project.API.Middleware;
using my_project.Core.Interfaces.Services;
using my_project.Service.Options;

namespace my_project.API.BackgroundJobs;

// Runs ReminderService right after startup and then every Reminders:IntervalMinutes.
//
// A hosted service is a Singleton, but IReminderService (and the DbContext behind it) is Scoped.
// So every run opens its own DI scope and resolves the service from it. Holding one scoped
// instance in this Singleton would keep a single DbContext forever – shared between runs,
// growing its change tracker, never disposed.
//
// Runs never overlap: the next run starts only after the previous one finished
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReminderSettings _settings;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ReminderSettings> settings,
        ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Reminders are disabled (Reminders:Enabled = false)");
            return;
        }

        // Lets the server finish starting before the first run
        await Task.Yield();

        var interval = TimeSpan.FromMinutes(_settings.IntervalMinutes);
        _logger.LogInformation("Reminder job started, running every {Interval}", interval);

        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The server is shutting down
        }

        _logger.LogInformation("Reminder job stopped");
    }

    // A failed run is logged and the job keeps going – the next run retries whatever was not sent.
    // (An exception escaping ExecuteAsync would stop the whole server)
    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        // Every run gets its own id, so all its log lines can be found together
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdMiddleware.ItemKey] = $"reminders-{Guid.NewGuid()}"
        });

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var reminders = scope.ServiceProvider.GetRequiredService<IReminderService>();

            await reminders.ProcessDueRemindersAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Reminder run failed; it will be retried on the next run");
        }
    }
}
