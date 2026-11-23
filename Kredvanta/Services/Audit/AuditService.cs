using System.Security.Claims;
using System.Text.Json;
using Kredvanta.Data;
using Kredvanta.Models.System;

namespace Kredvanta.Services.Audit;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public void Record(string action, string entityName, string? entityId,
                       string? description = null, object? changes = null)
        => _db.AuditLogs.Add(Build(action, entityName, entityId, description, changes));

    public async Task LogAsync(string action, string entityName, string? entityId,
                               string? description = null, object? changes = null,
                               CancellationToken ct = default)
    {
        Record(action, entityName, entityId, description, changes);
        await _db.SaveChangesAsync(ct);
    }

    private AuditLog Build(string action, string entityName, string? entityId,
                           string? description, object? changes)
    {
        var ctx = _http.HttpContext;
        return new AuditLog
        {
            UserId = ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            ChangesJson = changes is null ? null : JsonSerializer.Serialize(changes),
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(ctx?.Request.Headers.UserAgent.ToString(), 500),
            ActionDate = DateTime.UtcNow
        };
    }

    private static string? Truncate(string? s, int max)
        => s is null ? null : s.Length <= max ? s : s[..max];
}