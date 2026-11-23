namespace Kredvanta.Services.Audit;

public interface IAuditService
{
    void Record(
        string action,
        string entityName,
        string? entityId,
        string? description = null,
        object? changes = null);

    Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? description = null,
        object? changes = null,
        CancellationToken ct = default);
}
