using System.Threading.Channels;

namespace Kredvanta.Services.Documents.Screening;

public interface IDocumentScreeningQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken ct = default);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct);
}

/// <summary>
/// Singleton queue. Work lost on restart is recovered by DocumentScreeningWorker,
/// which re-queues every document still marked Queued or Scanning.
/// </summary>
public sealed class DocumentScreeningQueue : IDocumentScreeningQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(documentId, ct);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}