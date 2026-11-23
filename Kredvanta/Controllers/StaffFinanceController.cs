using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Contracts;
using Kredvanta.Services.Disbursement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Roles = AppRoles.FinanceOfficer)]
[Route("staff/finance")]
public class StaffFinanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDisbursementService _disbursements;
    private readonly IAuditService _audit;

    public StaffFinanceController(ApplicationDbContext db, IDisbursementService d, IAuditService audit)
    { _db = db; _disbursements = d; _audit = audit; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var ready = await _db.LoanApplications.AsNoTracking()
            .Where(a => a.Status == LoanApplicationStatus.ReadyForDisbursement &&
                        _db.LoanOffers.Any(o => o.LoanApplicationId == a.Id && o.Status == OfferStatus.Accepted))
            .OrderBy(a => a.UpdatedAt)
            .Select(a => new ReadyRow(a.Id, a.ApplicationNumber,
                a.Borrower.FirstName + " " + a.Borrower.LastName, a.LoanProduct.ProductName,
                _db.LoanOffers.Where(o => o.LoanApplicationId == a.Id && o.Status == OfferStatus.Accepted)
                    .OrderByDescending(o => o.Version).Select(o => o.Principal - o.ProcessingFee).First(),
                _db.LoanOffers.Where(o => o.LoanApplicationId == a.Id && o.Status == OfferStatus.Accepted)
                    .OrderByDescending(o => o.Version).Select(o => o.CurrencyCode).First()))
            .ToListAsync();

        var recent = await _db.LoanDisbursements.AsNoTracking()
            .OrderByDescending(d => d.DisbursedAt).Take(15)
            .Select(d => new RecentRow(d.Loan.LoanNumber, d.Amount, d.Loan.CurrencyCode, d.Method,
                d.TransactionReference, d.DisbursedAt, d.Status))
            .ToListAsync();

        return View(new FinanceHome { Ready = ready, Recent = recent });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Disburse(Guid id, CancellationToken ct)
    {
        var (page, error) = await LoadDisbursePageAsync(id, null, ct);
        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }
        if (page is null) return NotFound();

        await _audit.LogAsync("DisbursementCaseViewed", "LoanApplication", id.ToString(), ct: ct);
        return View(page);
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disburse(Guid id, DisburseRequest request, bool confirm,
        CancellationToken ct)
    {
        var (page, error) = await LoadDisbursePageAsync(id, request, ct);
        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }
        if (page is null) return NotFound();

        if (!ModelState.IsValid)
        {
            page.Method = request.Method;
            page.Reference = request.Reference;
            page.RecipientAccount = request.RecipientAccount;
            page.AmountPaid = request.AmountPaid;
            page.IdempotencyKey = request.IdempotencyKey;
            return View(page);
        }

        if (!confirm)
        {
            ModelState.AddModelError(string.Empty, "Confirm that the payout details were checked.");
            page.Method = request.Method;
            page.Reference = request.Reference;
            page.RecipientAccount = request.RecipientAccount;
            page.AmountPaid = request.AmountPaid;
            page.IdempotencyKey = request.IdempotencyKey;
            return View(page);
        }

        request.ApplicationId = id;
        var result = await _disbursements.DisburseAsync(request, UserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? result.Message : result.Error;
        return result.Succeeded ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Disburse), new { id });
    }

    private async Task<(DisbursePage? Page, string? Error)> LoadDisbursePageAsync(Guid id,
        DisburseRequest? submitted, CancellationToken ct)
    {
        var app = await _db.LoanApplications.AsNoTracking().Include(a => a.Borrower)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (app is null) return (null, null);
        if (app.Status != LoanApplicationStatus.ReadyForDisbursement)
            return (null, "This application is no longer ready for disbursement.");

        var offer = await _db.LoanOffers.AsNoTracking()
            .Where(o => o.LoanApplicationId == id && o.Status == OfferStatus.Accepted)
            .OrderByDescending(o => o.Version).FirstOrDefaultAsync(ct);
        if (offer is null) return (null, "There is no accepted offer for this application.");

        var agreement = await _db.LoanAgreements.AsNoTracking()
            .FirstOrDefaultAsync(a => a.LoanOfferId == offer.Id && a.Status == AgreementStatus.Signed, ct);
        if (agreement is null || OfferService.Hash(agreement.Text) != agreement.Sha256Hash ||
            !await _db.AgreementSignatures.AsNoTracking().AnyAsync(s =>
                s.LoanAgreementId == agreement.Id && s.AgreementHash == agreement.Sha256Hash, ct))
            return (null, "The signed agreement could not be verified. Do not disburse this loan.");

        var page = new DisbursePage
        {
            Id = id,
            Number = app.ApplicationNumber,
            Borrower = $"{app.Borrower.FirstName} {app.Borrower.LastName}".Trim(),
            Phone = app.Borrower.PhoneNumber,
            Offer = offer,
            AmountPaid = offer.Principal - offer.ProcessingFee,
            IdempotencyKey = submitted?.IdempotencyKey ?? Guid.NewGuid().ToString("N")
        };

        if (submitted is not null)
        {
            page.Method = submitted.Method;
            page.Reference = submitted.Reference;
            page.RecipientAccount = submitted.RecipientAccount;
            page.AmountPaid = submitted.AmountPaid;
        }
        return (page, null);
    }

    public record ReadyRow(Guid Id, string Number, string Borrower, string Product, decimal Net, string Currency);
    public record RecentRow(string LoanNumber, decimal Amount, string Currency, DisbursementMethod Method,
        string? Reference, DateTime? At, DisbursementStatus Status);

    public class FinanceHome
    {
        public List<ReadyRow> Ready { get; set; } = new();
        public List<RecentRow> Recent { get; set; } = new();
    }

    public class DisbursePage : DisburseRequest
    {
        public Guid Id { get; set; }
        public string Number { get; set; } = "";
        public string Borrower { get; set; } = "";
        public string Phone { get; set; } = "";
        public Kredvanta.Models.Contracts.LoanOffer? Offer { get; set; }
    }
}