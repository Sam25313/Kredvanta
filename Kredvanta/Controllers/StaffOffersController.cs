using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Services.Contracts;
using Kredvanta.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Roles = AppRoles.LoanOfficer)]
[Route("staff/offers")]
public class StaffOffersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IOfferService _offers;
    public StaffOffersController(ApplicationDbContext db, IOfferService offers) { _db = db; _offers = offers; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var applications = await _db.LoanApplications.AsNoTracking()
            .Where(application => application.Status == LoanApplicationStatus.Approved
                || application.Status == LoanApplicationStatus.OfferIssued
                || application.Status == LoanApplicationStatus.AgreementPending
                || application.Status == LoanApplicationStatus.ReadyForDisbursement)
            .OrderBy(application => application.DecisionAt)
            .Select(application => new
            {
                application.Id,
                application.ApplicationNumber,
                ApplicantName = application.Borrower.FirstName + " " + application.Borrower.LastName,
                ProductName = application.LoanProduct.ProductName,
                application.RequestedAmount,
                application.Status,
                application.DecisionAt
            })
            .ToListAsync(cancellationToken);

        var applicationIds = applications.Select(application => application.Id).ToArray();
        var approvals = await _db.LoanApprovals.AsNoTracking()
            .Where(approval => applicationIds.Contains(approval.LoanApplicationId)
                && approval.Decision == ApprovalDecision.Approved)
            .Select(approval => new
            {
                approval.LoanApplicationId,
                approval.ApprovalLevel,
                approval.ApprovedAmount,
                approval.DecidedAt
            })
            .ToListAsync(cancellationToken);
        var latestApprovals = approvals
            .GroupBy(approval => approval.LoanApplicationId)
            .ToDictionary(group => group.Key,
                group => group.OrderByDescending(approval => approval.ApprovalLevel)
                    .ThenByDescending(approval => approval.DecidedAt).First());

        var offers = await _db.LoanOffers.AsNoTracking()
            .Where(offer => applicationIds.Contains(offer.LoanApplicationId))
            .Select(offer => new
            {
                offer.LoanApplicationId,
                offer.Version,
                offer.Status,
                offer.ExpiresAt
            })
            .ToListAsync(cancellationToken);
        var latestOffers = offers
            .GroupBy(offer => offer.LoanApplicationId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(offer => offer.Version).First());

        var model = new OperationsOffersViewModel
        {
            Applications = applications.Select(application =>
            {
                latestApprovals.TryGetValue(application.Id, out var approval);
                latestOffers.TryGetValue(application.Id, out var offer);
                var canPrepare = application.Status == LoanApplicationStatus.Approved && approval is not null;
                var nextStep = application.Status switch
                {
                    LoanApplicationStatus.Approved when approval is null => "Approval record needs manager review",
                    LoanApplicationStatus.Approved => "Prepare and issue offer",
                    LoanApplicationStatus.OfferIssued => "Waiting for applicant response",
                    LoanApplicationStatus.AgreementPending => "Waiting for applicant signature",
                    LoanApplicationStatus.ReadyForDisbursement => "Ready for Finance hand-off",
                    _ => "Review application"
                };

                return new OperationsOfferRow
                {
                    Id = application.Id,
                    ApplicationNumber = application.ApplicationNumber,
                    ApplicantName = application.ApplicantName.Trim(),
                    ProductName = application.ProductName,
                    RequestedAmount = application.RequestedAmount,
                    ApprovedAmount = approval?.ApprovedAmount,
                    Status = application.Status,
                    DecisionAt = application.DecisionAt,
                    OfferVersion = offer?.Version,
                    OfferStatus = offer?.Status,
                    OfferExpiresAt = offer?.ExpiresAt,
                    CanPrepare = canPrepare,
                    NextStep = nextStep
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Issue(Guid id, DateOnly? firstDue, CancellationToken cancellationToken)
    {
        var app = await _db.LoanApplications.AsNoTracking().Include(a => a.Borrower)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (app is null) return NotFound();
        if (app.Status != LoanApplicationStatus.Approved)
        {
            TempData["Error"] = "An offer can only be prepared after the credit manager approves the application.";
            return RedirectToAction(nameof(Index));
        }

        var (terms, error) = await _offers.PreviewAsync(id, firstDue, cancellationToken);
        return View(new IssuePage
        {
            Id = id,
            Number = app.ApplicationNumber,
            Status = app.Status,
            Borrower = $"{app.Borrower.FirstName} {app.Borrower.LastName}".Trim(),
            Requested = app.RequestedAmount,
            Terms = terms,
            Error = error,
            FirstDue = terms?.FirstDue ?? firstDue
        });
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(Guid id, DateOnly firstDue, bool confirm)
    {
        if (!confirm) { TempData["Error"] = "Confirm that you checked the terms."; return RedirectToAction(nameof(Issue), new { id, firstDue }); }
        var r = await _offers.IssueAsync(id, firstDue, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? r.Message : r.Error;
        return r.Succeeded ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Issue), new { id, firstDue });
    }

    public class IssuePage
    {
        public Guid Id { get; set; }
        public string Number { get; set; } = "";
        public LoanApplicationStatus Status { get; set; }
        public string Borrower { get; set; } = "";
        public decimal Requested { get; set; }
        public OfferTerms? Terms { get; set; }
        public string? Error { get; set; }
        public DateOnly? FirstDue { get; set; }
    }
}