using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Identity;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Eligibility;
using Kredvanta.Services.Workflow;
using Kredvanta.ViewModels.LoanApplications;
using Kredvanta.Services.Documents;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = AppRoles.Borrower)]
    public class LoanApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationWorkflowService _workflow;
        private readonly IEligibilityService _eligibility;
        private readonly IAuditService _audit;

        public LoanApplicationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IApplicationWorkflowService workflow,
            IEligibilityService eligibility,
            IAuditService audit)
        {
            _context = context;
            _userManager = userManager;
            _workflow = workflow;
            _eligibility = eligibility;
            _audit = audit;
        }

        // ---------- Index ----------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var borrower = await GetCurrentBorrowerAsync();
            if (borrower == null) return RedirectToAction("Step1", "Profile");

            var applications = _context.LoanApplications
                .AsNoTracking()
                .Where(a => a.BorrowerId == borrower.Id);

            var model = new LoanApplicationIndexViewModel
            {
                TotalApplications = await applications.CountAsync(),

                PendingApplications = await applications.CountAsync(a =>
                    a.Status == LoanApplicationStatus.Submitted ||
                    a.Status == LoanApplicationStatus.UnderReview ||
                    a.Status == LoanApplicationStatus.DocumentsRequired ||
                    a.Status == LoanApplicationStatus.Recommended),

                ApprovedApplications = await applications.CountAsync(a =>
                    a.Status == LoanApplicationStatus.Approved),

                RejectedApplications = await applications.CountAsync(a =>
                    a.Status == LoanApplicationStatus.Rejected)
            };

            model.Applications = await applications
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new LoanApplicationListItemViewModel
                {
                    Id = a.Id,
                    ApplicationNumber = a.ApplicationNumber,
                    LoanProductName = a.LoanProduct.ProductName,
                    RequestedAmount = a.RequestedAmount,
                    RequestedTermPeriods = a.RequestedTermPeriods,
                    LoanPurpose = a.LoanPurpose,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt,
                    SubmittedAt = a.SubmittedAt,
                    DecisionAt = a.DecisionAt
                })
                .ToListAsync();

            return View(model);
        }

        // ---------- Create (GET) ----------

        [HttpGet]
        public async Task<IActionResult> Create(Guid? loanProductId)
        {
            var borrower = await GetCurrentBorrowerAsync();
            if (borrower == null) return RedirectToAction("Step1", "Profile");

            var blocked = await BlockIfNotEligibleAsync(borrower);
            if (blocked != null) return blocked;

            var model = new CreateLoanApplicationViewModel();
            await LoadAvailableProductsAsync(model);

            if (!model.AvailableProducts.Any())
            {
                TempData["Error"] = "There are currently no loan products available for application.";
                return RedirectToAction("Index", "LoanProducts");
            }

            if (loanProductId.HasValue)
            {
                var selected = model.AvailableProducts.FirstOrDefault(p => p.Id == loanProductId.Value);
                if (selected == null)
                {
                    TempData["Error"] = "The selected loan product is unavailable for applications.";
                    return RedirectToAction("Index", "LoanProducts");
                }
                model.LoanProductId = selected.Id;
            }

            return View(model);
        }

        // ---------- Create (POST) ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateLoanApplicationViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var borrower = await GetCurrentBorrowerAsync();
            if (borrower == null) return RedirectToAction("Step1", "Profile");

            var blocked = await BlockIfNotEligibleAsync(borrower);
            if (blocked != null) return blocked;

            var product = await _context.LoanProducts
                .FirstOrDefaultAsync(p => p.Id == model.LoanProductId);

            if (product == null)
            {
                ModelState.AddModelError(nameof(model.LoanProductId),
                    "The selected loan product could not be found.");
            }
            else if (!product.IsActive)
            {
                ModelState.AddModelError(nameof(model.LoanProductId),
                    "This loan product is currently inactive.");
            }
            else if (!product.IsAvailableForApplication)
            {
                ModelState.AddModelError(nameof(model.LoanProductId),
                    "This loan product is not currently accepting applications.");
            }
            else
            {
                if (model.RequestedAmount < product.MinimumAmount ||
                    model.RequestedAmount > product.MaximumAmount)
                {
                    ModelState.AddModelError(nameof(model.RequestedAmount),
                        $"The requested amount must be between " +
                        $"{product.MinimumAmount:N2} and {product.MaximumAmount:N2}.");
                }

                if (model.RequestedTermPeriods < product.MinimumTermPeriods ||
                    model.RequestedTermPeriods > product.MaximumTermPeriods)
                {
                    ModelState.AddModelError(nameof(model.RequestedTermPeriods),
                        $"The repayment term must be between " +
                        $"{product.MinimumTermPeriods} and {product.MaximumTermPeriods} periods.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadAvailableProductsAsync(model);
                return View(model);
            }

            var application = new LoanApplication
            {
                ApplicationNumber = await GenerateApplicationNumberAsync(),
                BorrowerId = borrower.Id,
                LoanProductId = product!.Id,
                RequestedAmount = model.RequestedAmount,
                RequestedTermPeriods = model.RequestedTermPeriods,
                LoanPurpose = model.LoanPurpose.Trim(),
                Status = LoanApplicationStatus.Draft
            };

            _context.LoanApplications.Add(application);

            _context.LoanApplicationStatusHistories.Add(new LoanApplicationStatusHistory
            {
                LoanApplicationId = application.Id,
                PreviousStatus = null,
                NewStatus = LoanApplicationStatus.Draft,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                Notes = "Application draft created."
            });

            _audit.Record("ApplicationCreated", nameof(LoanApplication), application.Id.ToString(),
                null, new
                {
                    application.ApplicationNumber,
                    application.RequestedAmount,
                    application.RequestedTermPeriods,
                    product.ProductCode
                });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your loan application draft has been created successfully.";
            return RedirectToAction(nameof(Details), new { id = application.Id });
        }

        // ---------- Details ----------

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var borrower = await GetCurrentBorrowerAsync();
            if (borrower == null) return RedirectToAction("Step1", "Profile");

            var application = await _context.LoanApplications
                .AsNoTracking()
                .Include(a => a.LoanProduct)
                .Include(a => a.Documents).ThenInclude(d => d.RequiredDocument)
                .Include(a => a.StatusHistory).ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(a => a.Id == id && a.BorrowerId == borrower.Id);

            if (application == null) return NotFound();

            var requiredDocuments = await _context.LoanProductRequiredDocuments
                .AsNoTracking()
                .Where(d => d.LoanProductId == application.LoanProductId && d.IsActive)
                .OrderByDescending(d => d.IsMandatory)
                .ThenBy(d => d.DocumentName)
                .ToListAsync();

            var currentUserId = _userManager.GetUserId(User);

            var model = new LoanApplicationDetailsViewModel
            {
                Id = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                LoanProductName = application.LoanProduct.ProductName,
                RequestedAmount = application.RequestedAmount,
                RequestedTermPeriods = application.RequestedTermPeriods,
                LoanPurpose = application.LoanPurpose,
                Status = application.Status,
                CreatedAt = application.CreatedAt,
                SubmittedAt = application.SubmittedAt,
                DecisionAt = application.DecisionAt,

                CanUploadDocuments = application.Status is LoanApplicationStatus.Draft
                                                         or LoanApplicationStatus.DocumentsRequired,

                RequiredDocuments = requiredDocuments.Select(requirement =>
                {
                    var doc = application.Documents.FirstOrDefault(d =>
                        d.IsCurrent && d.LoanProductRequiredDocumentId == requirement.Id);

                    return new RequiredApplicationDocumentViewModel
                    {
                        Id = requirement.Id,
                        DocumentName = requirement.DocumentName,
                        Description = requirement.Description,
                        IsMandatory = requirement.IsMandatory,
                        RequiredPeriodMonths = requirement.RequiredPeriodMonths,
                        IsUploaded = doc != null,
                        ReviewStatus = doc?.ReviewStatus,
                        UploadedAt = doc?.CreatedAt,
                        IsSatisfied = doc != null &&
                            (doc.ReviewStatus == DocumentReviewStatus.Pending ||
                             doc.ReviewStatus == DocumentReviewStatus.Verified),
                        DocumentId = doc?.Id,
                        Version = doc?.Version ?? 0,
                        
                        BorrowerMessage = BorrowerMessageFor(doc)
                    };
                }).ToList(),

                History = application.StatusHistory
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new ApplicationHistoryItemViewModel
                    {
                        PreviousStatus = h.PreviousStatus,
                        NewStatus = h.NewStatus,
                        ChangedAt = h.ChangedAt,
                        // Staff notes can be internal: borrowers see only their own.
                        Notes = h.ChangedByUserId == currentUserId ? h.Notes : null,
                        ChangedByName = h.ChangedByUser == null
                            ? "System"
                            : h.ChangedByUserId == currentUserId
                                ? "You"
                                : "Kredvanta staff"
                    })
                    .ToList(),

                Documents = application.Documents
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new ApplicationDocumentItemViewModel
                    {
                        Id = d.Id,
                        OriginalFileName = d.OriginalFileName,
                        RequiredDocumentName = d.RequiredDocument != null
                            ? d.RequiredDocument.DocumentName
                            : "Additional supporting document",
                        ReviewStatus = d.ReviewStatus,
                        ReviewComments = BorrowerMessageFor(d),   
                        UploadedAt = d.CreatedAt,
                        Version = d.Version,
                        IsCurrent = d.IsCurrent
                    })
                    .ToList()
            };

            return View(model);
        }

        // ---------- Submit ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var borrower = await GetCurrentBorrowerAsync();
            if (borrower == null) return RedirectToAction("Step1", "Profile");

            var owns = await _context.LoanApplications
                .AnyAsync(a => a.Id == id && a.BorrowerId == borrower.Id);
            if (!owns) return NotFound();

            // Eligibility, documents and product availability are enforced by the workflow guards.
            var result = await _workflow.TransitionAsync(
                id, LoanApplicationStatus.Submitted, userId, "Application submitted by applicant.");

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded
                    ? "Your loan application has been submitted for verification."
                    : result.Error;

            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Helpers ----------

        private async Task<IActionResult?> BlockIfNotEligibleAsync(Borrower borrower)
        {
            var result = await _eligibility.CheckAsync(borrower.Id);
            if (!result.IsEligible)
            {
                TempData["Warning"] = string.Join(" ", result.Reasons);
                return RedirectToAction("Index", "Dashboard");
            }

            var hasOpen = await _context.LoanApplications.AnyAsync(a =>
                a.BorrowerId == borrower.Id &&
                (a.Status == LoanApplicationStatus.Draft ||
                 a.Status == LoanApplicationStatus.Submitted ||
                 a.Status == LoanApplicationStatus.UnderReview ||
                 a.Status == LoanApplicationStatus.DocumentsRequired ||
                 a.Status == LoanApplicationStatus.Recommended ||
                 a.Status == LoanApplicationStatus.Approved));

            if (hasOpen)
            {
                TempData["Warning"] = "You already have an application in progress.";
                return RedirectToAction("Index", "Dashboard");
            }

            return null;
        }



        private static string? BorrowerMessageFor(LoanApplicationDocument? d) => d?.ReviewStatus switch
        {
            DocumentReviewStatus.Rejected => d.RejectionReasonCode switch
            {
                "ILLEGIBLE" => "The file was not legible. Please upload a clearer copy.",
                "EXPIRED" => "This document has expired. Please upload a current one.",
                "WRONG_DOCUMENT" => "This is not the document requested. Please upload the correct one.",
                "INCOMPLETE" => "The document is incomplete. Please upload all pages.",
                // DETAILS_MISMATCH, SUSPECTED_ALTERATION and OTHER stay neutral on purpose
                _ => "We could not accept this document. Please upload a new copy or contact us."
            },
            // Staff write this text for the borrower
            DocumentReviewStatus.ClarificationRequired => d.ReviewComments,
            _ => null
        };

        private async Task<Borrower?> GetCurrentBorrowerAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.Borrowers
                .FirstOrDefaultAsync(b => b.ApplicationUserId == userId && b.IsActive);
        }

        private async Task LoadAvailableProductsAsync(CreateLoanApplicationViewModel model)
        {
            model.AvailableProducts = await _context.LoanProducts
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsAvailableForApplication)
                .OrderBy(p => p.ProductName)
                .Select(p => new LoanProductOptionViewModel
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    MinimumAmount = p.MinimumAmount,
                    MaximumAmount = p.MaximumAmount,
                    AnnualInterestRate = p.AnnualInterestRate,
                    MinimumTermPeriods = p.MinimumTermPeriods,
                    MaximumTermPeriods = p.MaximumTermPeriods,
                    RepaymentFrequency = p.RepaymentFrequency.ToString(),
                    InterestMethod = p.InterestMethod.ToString()
                })
                .ToListAsync();
        }

        private async Task<string> GenerateApplicationNumberAsync()
        {
            string number;
            do
            {
                number = $"KRV-{DateTime.UtcNow:yyyyMMdd}-" +
                         $"{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            }
            while (await _context.LoanApplications.AnyAsync(a => a.ApplicationNumber == number));

            return number;
        }
    }
}