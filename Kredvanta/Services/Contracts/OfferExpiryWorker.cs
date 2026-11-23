namespace Kredvanta.Services.Contracts;

public class OfferExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OfferExpiryWorker> _log;
    public OfferExpiryWorker(IServiceScopeFactory scopes, ILogger<OfferExpiryWorker> log)
    { _scopes = scopes; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var n = await scope.ServiceProvider.GetRequiredService<IOfferService>().ExpireOverdueAsync(stoppingToken);
                if (n > 0) _log.LogInformation("Expired {Count} offer(s).", n);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Offer expiry run failed.");
            }
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}