using Kredvanta.Data;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Documents.Screening;

public class DocumentScreeningWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IDocumentScreeningQueue _queue;
    private readonly ILogger<DocumentScreeningWorker> _log;

    public DocumentScreeningWorker(IServiceScopeFactory scopes, IDocumentScreeningQueue queue,
        ILogger<DocumentScreeningWorker> log)
    {
        _scopes = scopes; _queue = queue; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingAsync(stoppingToken);

        await foreach (var documentId in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var screening = scope.ServiceProvider.GetRequiredService<IDocumentScreeningService>();
                await screening.ScreenAsync(documentId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Screening failed for document {DocumentId}.", documentId);
            }
        }
    }

    /// <summary>After a restart, re-queue anything that was waiting or mid-scan.</summary>
    private async Task RecoverPendingAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var ids = await db.LoanApplicationDocuments
                .Where(d => d.IsCurrent &&
                            (d.ScreeningStatus == DocumentScreeningStatus.Queued ||
                             d.ScreeningStatus == DocumentScreeningStatus.Scanning))
                .Select(d => d.Id)
                .ToListAsync(ct);

            foreach (var id in ids) await _queue.EnqueueAsync(id, ct);
            if (ids.Count > 0) _log.LogInformation("Re-queued {Count} document(s) for screening.", ids.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Could not recover pending screenings.");
        }
    }
}