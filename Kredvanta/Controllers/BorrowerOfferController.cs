using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Contracts;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Contracts;
using Kredvanta.Services.Credit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Roles = AppRoles.Borrower)]
[Route("offer/{id:guid}")]
public class BorrowerOfferController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IOfferService _offers;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    public BorrowerOfferController(ApplicationDbContext db, IOfferService offers, IAuditService audit,
        IConfiguration config, IHostEnvironment env)
    { _db = db; _offers = offers; _audit = audit; _config = config; _env = env; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private Task<bool> Owns(Guid id) => _db.LoanApplications.AnyAsync(a =>
        a.Id == id && a.Borrower.ApplicationUserId == UserId);

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid id)
    {
        if (!await Owns(id)) return NotFound();

        var app = await _db.LoanApplications.AsNoTracking().FirstAsync(a => a.Id == id);
        var offer = await _db.LoanOffers.AsNoTracking().Where(o => o.LoanApplicationId == id)
            .OrderByDescending(o => o.Version).FirstOrDefaultAsync();
        var agreement = offer is null ? null : await _db.LoanAgreements.AsNoTracking()
            .FirstOrDefaultAsync(a => a.LoanOfferId == offer.Id);

        return View(new OfferPage
        {
            ApplicationId = id,
            Number = app.ApplicationNumber,
            AppStatus = app.Status,
            Offer = offer,
            Agreement = agreement,
            Schedule = offer is null ? new() : JsonSerializer.Deserialize<List<ScheduleLine>>(offer.ScheduleJson) ?? new(),
            CanAccept = offer is { Status: OfferStatus.Issued } && offer.ExpiresAt > DateTime.UtcNow
                        && app.Status == LoanApplicationStatus.OfferIssued,
            CanSign = agreement is { Status: AgreementStatus.Generated } && offer is { Status: OfferStatus.Accepted }
                      && offer.ExpiresAt > DateTime.UtcNow && app.Status == LoanApplicationStatus.AgreementPending,
            SigningEnabled = _env.IsDevelopment() || _config.GetValue<bool>("Agreements:TemplateApproved")
        });
    }

    [HttpPost("accept")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(Guid id)
    {
        if (!await Owns(id)) return NotFound();
        var r = await _offers.AcceptAsync(id, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? r.Message : r.Error;
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("decline")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(Guid id, string? reason)
    {
        if (!await Owns(id)) return NotFound();
        var r = await _offers.DeclineAsync(id, reason, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? r.Message : r.Error;
        return r.Succeeded ? RedirectToAction("Index", "Dashboard") : RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("sign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sign(Guid id, string? password, bool agreed)
    {
        if (!await Owns(id)) return NotFound();
        if (!agreed) { TempData["Error"] = "Tick the box to confirm you have read the agreement."; return RedirectToAction(nameof(Index), new { id }); }
        if (string.IsNullOrEmpty(password)) { TempData["Error"] = "Enter your password to sign."; return RedirectToAction(nameof(Index), new { id }); }

        var r = await _offers.SignAsync(id, password, UserId, HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? r.Message : r.Error;
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpGet("agreement")]
    public async Task<IActionResult> Download(Guid id)
    {
        if (!await Owns(id)) return NotFound();
        var agreement = await _db.LoanAgreements.AsNoTracking().Where(a => a.LoanApplicationId == id)
            .OrderByDescending(a => a.GeneratedAt).FirstOrDefaultAsync();
        if (agreement is null) return NotFound();

        await _audit.LogAsync("AgreementDownloaded", nameof(LoanAgreement), agreement.Id.ToString());
        var footer = $"\n\nSHA-256: {agreement.Sha256Hash}\nStatus: {agreement.Status}\n";
        return File(Encoding.UTF8.GetBytes(agreement.Text + footer), "text/plain; charset=utf-8",
            $"{agreement.AgreementNumber}.txt");
    }

    public class OfferPage
    {
        public Guid ApplicationId { get; set; }
        public string Number { get; set; } = "";
        public LoanApplicationStatus AppStatus { get; set; }
        public LoanOffer? Offer { get; set; }
        public LoanAgreement? Agreement { get; set; }
        public List<ScheduleLine> Schedule { get; set; } = new();
        public bool CanAccept { get; set; }
        public bool CanSign { get; set; }
        public bool SigningEnabled { get; set; }
    }
}