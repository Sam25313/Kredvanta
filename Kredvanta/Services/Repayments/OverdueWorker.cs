namespace Kredvanta.Services.Repayments;

public class OverdueWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OverdueWorker> _log;
    public OverdueWorker(IServiceScopeFactory scopes, ILogger<OverdueWorker> log) { _scopes = scopes; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var n = await scope.ServiceProvider.GetRequiredService<IRepaymentService>().RefreshAllAsync(stoppingToken);
                if (n > 0) _log.LogInformation("Updated {Count} loan(s) for overdue status.", n);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Overdue refresh failed.");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}