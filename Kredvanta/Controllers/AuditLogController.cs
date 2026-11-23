using Kredvanta.Authorization;
using Kredvanta.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers;

[Authorize(Roles = $"{AppRoles.Auditor},{AppRoles.Administrator}")]
[Route("audit")]
public class AuditLogController : Controller
{
    private const int PageSize = 50;
    private readonly ApplicationDbContext _db;
    public AuditLogController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? action, string? entity, string? entityId,
        DateTime? from, DateTime? to, int page = 1)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action.Contains(action));
        if (!string.IsNullOrWhiteSpace(entity)) q = q.Where(a => a.EntityName == entity);
        if (!string.IsNullOrWhiteSpace(entityId)) q = q.Where(a => a.EntityId == entityId);
        if (from.HasValue) q = q.Where(a => a.ActionDate >= from.Value.ToUniversalTime());
        if (to.HasValue) q = q.Where(a => a.ActionDate < to.Value.Date.AddDays(1).ToUniversalTime());

        var total = await q.CountAsync();
        page = Math.Max(1, page);

        var rows = await q.OrderByDescending(a => a.ActionDate)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(a => new AuditRow(a.ActionDate, a.User != null ? a.User.Email : "System",
                a.Action, a.EntityName, a.EntityId, a.Description, a.ChangesJson, a.IpAddress))
            .ToListAsync();

        ViewBag.Total = total; ViewBag.Page = page; ViewBag.PageSize = PageSize;
        ViewBag.Action = action; ViewBag.Entity = entity; ViewBag.EntityId = entityId;
        ViewBag.From = from?.ToString("yyyy-MM-dd"); ViewBag.To = to?.ToString("yyyy-MM-dd");

        return View(rows);
    }

    public record AuditRow(DateTime At, string? User, string Action, string Entity,
        string? EntityId, string? Description, string? Changes, string? Ip);
}