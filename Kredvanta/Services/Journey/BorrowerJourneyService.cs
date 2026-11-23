using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Services.Documents;
using Kredvanta.Services.Eligibility;
using Kredvanta.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Journey;

public interface IBorrowerJourneyService
{
    Task<BorrowerJourneyViewModel> BuildAsync(string userId, CancellationToken ct = default);
}

public class BorrowerJourneyService : IBorrowerJourneyService
{
    private static readonly LoanApplicationStatus[] OpenStatuses =
    {
        LoanApplicationStatus.Draft, LoanApplicationStatus.Submitted,
        LoanApplicationStatus.UnderReview, LoanApplicationStatus.DocumentsRequired,
        LoanApplicationStatus.Recommended, LoanApplicationStatus.Approved,
        // Step 7: a borrower with a live offer must not see the Apply panel again
        LoanApplicationStatus.OfferIssued, LoanApplicationStatus.AgreementPending,
        LoanApplicationStatus.ReadyForDisbursement
    };

    private readonly ApplicationDbContext _db;
    private readonly IEligibilityService _eligibility;

    public BorrowerJourneyService(ApplicationDbContext db, IEligibilityService eligibility)
    {
        _db = db; _eligibility = eligibility;
    }

    public async Task<BorrowerJourneyViewModel> BuildAsync(string userId, CancellationToken ct = default)
    {
        var vm = new BorrowerJourneyViewModel();

        var borrower = await _db.Borrowers.AsNoTracking()
            .FirstOrDefaultAsync(b => b.ApplicationUserId == userId && b.IsActive, ct);

        vm.ProfileComplete = borrower?.IsProfileComplete ?? false;
        vm.ProfileStep = borrower?.ProfileCompletionStep ?? 1;

        int current;

        if (borrower is null || !vm.ProfileComplete)
        {
            current = 1;
            vm.NextAction = new JourneyNextAction
            {
                Title = "Complete your profile",
                Message = "Tell us about yourself and your employment so we can check what you qualify for.",
                ButtonText = borrower is null ? "Start my profile" : "Continue my profile",
                Controller = "Profile",
                Action = "Index",
                Css = "primary"
            };
        }
        else
        {
            var eligibility = await _eligibility.CheckAsync(borrower.Id, ct);
            vm.Eligible = eligibility.IsEligible;
            vm.EligibilityReasons = eligibility.Reasons.ToList();

            var app = await _db.LoanApplications.AsNoTracking()
                .Include(a => a.LoanProduct)
                .Where(a => a.BorrowerId == borrower.Id && OpenStatuses.Contains(a.Status))
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (!vm.Eligible && app is null)
            {
                current = 2;
                vm.NextAction = new JourneyNextAction
                {
                    Title = "You can't apply yet",
                    Message = string.Join(" ", vm.EligibilityReasons),
                    ButtonText = "Update my profile",
                    Controller = "Profile",
                    Action = "Edit",
                    Css = "warning"
                };
            }
            else if (app is null)
            {
                current = 3;
                vm.CanStartApplication = true;
                vm.Products = await LoadProductsAsync(ct);
                vm.NextAction = new JourneyNextAction
                {
                    Title = "Start your loan application",
                    Message = "Choose a product, enter the amount and term, and save a draft.",
                    ButtonText = "Apply now",
                    Anchor = "#apply",
                    Css = "primary"
                };
            }
            else
            {
                vm.OpenApplication = await BuildApplicationAsync(app, ct);
                (current, vm.NextAction) = NextActionFor(vm.OpenApplication);
            }
        }

        vm.Steps = BuildSteps(current);
        return vm;
    }

    private async Task<List<JourneyProduct>> LoadProductsAsync(CancellationToken ct) =>
        await _db.LoanProducts.AsNoTracking()
            .Where(p => p.IsActive && p.IsAvailableForApplication)
            .OrderBy(p => p.ProductName)
            .Select(p => new JourneyProduct
            {
                Id = p.Id,
                Name = p.ProductName,
                Code = p.ProductCode,
                Description = p.Description,
                MinimumAmount = p.MinimumAmount,
                MaximumAmount = p.MaximumAmount,
                AnnualInterestRate = p.AnnualInterestRate,
                MinimumTerm = p.MinimumTermPeriods,
                MaximumTerm = p.MaximumTermPeriods,
                Frequency = p.RepaymentFrequency.ToString(),
                InterestMethod = p.InterestMethod.ToString()
            })
            .ToListAsync(ct);

    private async Task<JourneyApplication> BuildApplicationAsync(LoanApplication app, CancellationToken ct)
    {
        var requirements = await _db.LoanProductRequiredDocuments.AsNoTracking()
            .Where(r => r.LoanProductId == app.LoanProductId && r.IsActive)
            .OrderByDescending(r => r.IsMandatory).ThenBy(r => r.DocumentName)
            .ToListAsync(ct);

        var docs = await _db.LoanApplicationDocuments.AsNoTracking()
            .Where(d => d.LoanApplicationId == app.Id && d.IsCurrent)
            .ToListAsync(ct);

        var items = requirements.Select(r =>
        {
            var d = docs.FirstOrDefault(x => x.LoanProductRequiredDocumentId == r.Id);
            return new JourneyDocument
            {
                RequirementId = r.Id,
                Name = r.DocumentName,
                Description = r.Description,
                IsMandatory = r.IsMandatory,
                IsUploaded = d != null,
                DocumentId = d?.Id,
                Version = d?.Version ?? 0,
                ReviewStatus = d?.ReviewStatus,
                Message = DocumentMessages.ForBorrower(d)
            };
        }).ToList();

        var canUpload = app.Status is LoanApplicationStatus.Draft or LoanApplicationStatus.DocumentsRequired;

        // Kept from your version: rejected or clarification-needed documents count as outstanding.
        var missing = items.Count(i => i.IsMandatory &&
            (!i.IsUploaded || i.ReviewStatus is DocumentReviewStatus.Rejected or DocumentReviewStatus.ClarificationRequired));

        return new JourneyApplication
        {
            Id = app.Id,
            Number = app.ApplicationNumber,
            ProductName = app.LoanProduct.ProductName,
            Amount = app.RequestedAmount,
            Term = app.RequestedTermPeriods,
            Status = app.Status,
            CanUpload = canUpload,
            // Display hint only: the workflow guards are the real enforcement.
            // The dashboard shows the Submit button only while the status is Draft.
            CanSubmit = canUpload && missing == 0,
            MissingMandatory = missing,
            Documents = items
        };
    }

    private static (int Step, JourneyNextAction Action) NextActionFor(JourneyApplication a)
    {
        switch (a.Status)
        {
            case LoanApplicationStatus.Draft when a.MissingMandatory > 0:
                return (4, new JourneyNextAction
                {
                    Title = "Upload or replace your documents",
                    Message = $"{a.MissingMandatory} required document(s) still need to be uploaded or replaced for {a.Number}.",
                    ButtonText = "Go to documents",
                    Anchor = "#documents",
                    Css = "warning"
                });

            case LoanApplicationStatus.Draft:
                return (5, new JourneyNextAction
                {
                    Title = "Submit your application",
                    Message = "Everything is uploaded. Submit it so our team can start the review.",
                    ButtonText = "Review and submit",
                    Anchor = "#documents",
                    Css = "success"
                });

            case LoanApplicationStatus.DocumentsRequired:
                return (4, new JourneyNextAction
                {
                    Title = "We need more from you",
                    Message = "Our team asked for new or corrected documents. Upload them below and we will resume the review.",
                    ButtonText = "Go to documents",
                    Anchor = "#documents",
                    Css = "warning"
                });

            case LoanApplicationStatus.Submitted:
            case LoanApplicationStatus.UnderReview:
            case LoanApplicationStatus.Recommended:
                return (6, new JourneyNextAction
                {
                    Title = "Your application is being reviewed",
                    Message = "We are verifying your documents and employment. Nothing is needed from you right now.",
                    Css = "info"
                });

            case LoanApplicationStatus.Approved:
                return (8, new JourneyNextAction
                {
                    Title = "Your application has been approved",
                    Message = "We are preparing your offer. It will appear here shortly.",
                    Css = "success"
                });

            case LoanApplicationStatus.OfferIssued:
                return (8, new JourneyNextAction
                {
                    Title = "Your loan offer is ready",
                    Message = "Review the terms and accept or decline before the offer expires.",
                    ButtonText = "View offer",
                    Controller = "BorrowerOffer",
                    Action = "Index",
                    RouteId = a.Id,
                    Css = "success"
                });

            case LoanApplicationStatus.AgreementPending:
                return (8, new JourneyNextAction
                {
                    Title = "Sign your agreement",
                    Message = "You accepted the offer. Read and sign the agreement to continue.",
                    ButtonText = "Read and sign",
                    Controller = "BorrowerOffer",
                    Action = "Index",
                    RouteId = a.Id,
                    Css = "warning"
                });

            default: // ReadyForDisbursement
                return (9, new JourneyNextAction
                {
                    Title = "Agreement signed",
                    Message = "Our finance team will release your funds. You will see the loan here once it is paid out.",
                    ButtonText = "View agreement",
                    Controller = "BorrowerOffer",
                    Action = "Index",
                    RouteId = a.Id,
                    Css = "info"
                });
        }
    }

    private static List<JourneyStep> BuildSteps(int current)
    {
        var titles = new[]
        {
            "Complete profile", "Check eligibility", "Start application", "Upload documents",
            "Submit", "Review and verification", "Decision", "Offer and agreement", "Disbursement"
        };

        return titles.Select((t, i) => new JourneyStep
        {
            Title = t,
            State = i + 1 < current ? StepState.Done
                  : i + 1 == current ? StepState.Current
                  : StepState.Pending
        }).ToList();
    }
}