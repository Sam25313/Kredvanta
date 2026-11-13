
using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Products;
using Kredvanta.ViewModels.LoanApplications;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Borrower")]
    public class LoanApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoanApplicationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ====================================================
        // GET: /LoanApplications
        // Display applications belonging to the logged-in borrower
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

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

        // ====================================================
        // GET: /LoanApplications/Create
        // Display available loan products
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> Create(Guid? loanProductId)
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            if (borrower.KycStatus != KycStatus.Verified)
            {
                TempData["Warning"] =
                    "Complete identity verification before applying for a loan.";

                return RedirectToAction("Index", "Verification");
            }

            var model = new CreateLoanApplicationViewModel();

            await LoadAvailableProductsAsync(model);

            if (!model.AvailableProducts.Any())
            {
                TempData["Error"] =
                    "There are currently no loan products available for application.";

                return RedirectToAction("Index", "LoanProducts");
            }

            // Preselect the product if the borrower came
            // from a specific loan product details page.

            if (loanProductId.HasValue)
            {
                var selectedProduct = model.AvailableProducts
                    .FirstOrDefault(p => p.Id == loanProductId.Value);

                if (selectedProduct == null)
                {
                    TempData["Error"] =
                        "The selected loan product is unavailable for applications.";

                    return RedirectToAction("Index", "LoanProducts");
                }

                model.LoanProductId = selectedProduct.Id;
            }

            return View(model);
        }

        // ====================================================
        // POST: /LoanApplications/Create
        // Create a new draft loan application
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateLoanApplicationViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            if (borrower.KycStatus != KycStatus.Verified)
            {
                TempData["Warning"] =
                    "Complete identity verification before applying for a loan.";

                return RedirectToAction("Index", "Verification");
            }

            // Retrieve the selected product first.
            // We deliberately check its status separately
            // to provide meaningful validation messages.

            var product = await _context.LoanProducts
                .FirstOrDefaultAsync(p =>
                    p.Id == model.LoanProductId);

            if (product == null)
            {
                ModelState.AddModelError(
                    nameof(model.LoanProductId),
                    "The selected loan product could not be found.");
            }
            else if (!product.IsActive)
            {
                ModelState.AddModelError(
                    nameof(model.LoanProductId),
                    "This loan product is currently inactive.");
            }
            else if (!product.IsAvailableForApplication)
            {
                ModelState.AddModelError(
                    nameof(model.LoanProductId),
                    "This loan product is not currently accepting applications.");
            }
            else
            {
                // Validate requested loan amount.

                if (model.RequestedAmount < product.MinimumAmount ||
                    model.RequestedAmount > product.MaximumAmount)
                {
                    ModelState.AddModelError(
                        nameof(model.RequestedAmount),
                        $"The requested amount must be between " +
                        $"{product.MinimumAmount:N2} and " +
                        $"{product.MaximumAmount:N2}.");
                }

                // Validate repayment term.

                if (model.RequestedTermPeriods < product.MinimumTermPeriods ||
                    model.RequestedTermPeriods > product.MaximumTermPeriods)
                {
                    ModelState.AddModelError(
                        nameof(model.RequestedTermPeriods),
                        $"The repayment term must be between " +
                        $"{product.MinimumTermPeriods} and " +
                        $"{product.MaximumTermPeriods} periods.");
                }
            }

            // If validation fails, reload available products
            // and return the form with validation messages.

            if (!ModelState.IsValid)
            {
                await LoadAvailableProductsAsync(model);

                return View(model);
            }

            // Create the loan application as a Draft.
            // The borrower can upload supporting documents
            // before submitting the application.

            var application = new LoanApplication
            {
                ApplicationNumber =
                    await GenerateApplicationNumberAsync(),

                BorrowerId = borrower.Id,

                LoanProductId = product!.Id,

                RequestedAmount = model.RequestedAmount,

                RequestedTermPeriods = model.RequestedTermPeriods,

                LoanPurpose = model.LoanPurpose.Trim(),

                Status = LoanApplicationStatus.Draft
            };

            _context.LoanApplications.Add(application);

            // Record initial application history.

            var history = new LoanApplicationStatusHistory
            {
                LoanApplicationId = application.Id,

                PreviousStatus = null,

                NewStatus = LoanApplicationStatus.Draft,

                ChangedAt = DateTime.UtcNow,

                ChangedByUserId = userId,

                Notes = "Application draft created."
            };

            _context.LoanApplicationStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your loan application draft has been created successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = application.Id });
        }

        // ====================================================
        // GET: /LoanApplications/Details/{id}
        // Display an individual loan application
        // ====================================================

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            // Only retrieve an application belonging
            // to the authenticated borrower.

            var application = await _context.LoanApplications
                .AsNoTracking()
                .Include(a => a.LoanProduct)
                .Include(a => a.Documents)
                    .ThenInclude(d => d.RequiredDocument)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.BorrowerId == borrower.Id);

            if (application == null)
                return NotFound();

            // Retrieve active requirements for this product.

            var requiredDocuments = await _context
                .LoanProductRequiredDocuments
                .AsNoTracking()
                .Where(d =>
                    d.LoanProductId == application.LoanProductId &&
                    d.IsActive)
                .OrderByDescending(d => d.IsMandatory)
                .ThenBy(d => d.DocumentName)
                .ToListAsync();

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

                // Build the required-document checklist.

                RequiredDocuments = requiredDocuments.Select(requirement =>
                {
                    var latestDocument = application.Documents
                        .Where(d =>
                            d.LoanProductRequiredDocumentId == requirement.Id)
                        .OrderByDescending(d => d.CreatedAt)
                        .FirstOrDefault();

                    bool isSatisfied =
                        latestDocument != null &&
                        (latestDocument.ReviewStatus ==
                            DocumentReviewStatus.Pending ||
                         latestDocument.ReviewStatus ==
                            DocumentReviewStatus.Verified);

                    return new RequiredApplicationDocumentViewModel
                    {
                        Id = requirement.Id,

                        DocumentName = requirement.DocumentName,

                        Description = requirement.Description,

                        IsMandatory = requirement.IsMandatory,

                        RequiredPeriodMonths =
                            requirement.RequiredPeriodMonths,

                        IsUploaded = latestDocument != null,

                        ReviewStatus = latestDocument?.ReviewStatus,

                        UploadedAt = latestDocument?.CreatedAt,

                        IsSatisfied = isSatisfied
                    };
                }).ToList(),

                // Application status history.

                History = application.StatusHistory
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new ApplicationHistoryItemViewModel
                    {
                        PreviousStatus = h.PreviousStatus,

                        NewStatus = h.NewStatus,

                        ChangedAt = h.ChangedAt,

                        Notes = h.Notes,

                        ChangedByName = h.ChangedByUser == null
                            ? "System"
                            : $"{h.ChangedByUser.FirstName} " +
                              $"{h.ChangedByUser.LastName}"
                    })
                    .ToList(),

                // Uploaded application documents.

                Documents = application.Documents
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new ApplicationDocumentItemViewModel
                    {
                        Id = d.Id,

                        OriginalFileName = d.OriginalFileName,

                        RequiredDocumentName =
                            d.RequiredDocument != null
                                ? d.RequiredDocument.DocumentName
                                : "Additional supporting document",

                        ReviewStatus = d.ReviewStatus,

                        ReviewComments = d.ReviewComments,

                        UploadedAt = d.CreatedAt
                    })
                    .ToList()
            };

            return View(model);
        }

        // ====================================================
        // POST: /LoanApplications/Submit/{id}
        // Submit a draft loan application
        // ====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            var application = await _context.LoanApplications
                .Include(a => a.LoanProduct)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.BorrowerId == borrower.Id);

            if (application == null)
                return NotFound();

            // Only Draft applications can be submitted.

            if (application.Status != LoanApplicationStatus.Draft)
            {
                TempData["Error"] =
                    "Only draft applications can be submitted.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // KYC verification must be completed before
            // an application can proceed to submission.

            if (borrower.KycStatus != KycStatus.Verified)
            {
                TempData["Warning"] =
                    "Complete identity verification before submitting your loan application.";

                return RedirectToAction("Index", "Verification");
            }

            // Check that the product is still active.

            if (!application.LoanProduct.IsActive)
            {
                TempData["Error"] =
                    "This loan product is no longer active. Please contact support.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // Check that the product is still accepting applications.

            if (!application.LoanProduct.IsAvailableForApplication)
            {
                TempData["Error"] =
                    "This loan product is no longer accepting applications.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // Retrieve all active mandatory requirements.

            var mandatoryDocuments = await _context
                .LoanProductRequiredDocuments
                .AsNoTracking()
                .Where(d =>
                    d.LoanProductId == application.LoanProductId &&
                    d.IsActive &&
                    d.IsMandatory)
                .ToListAsync();

            // Retrieve uploaded documents linked to requirements.

            var uploadedDocuments = await _context
                .LoanApplicationDocuments
                .AsNoTracking()
                .Where(d =>
                    d.LoanApplicationId == application.Id &&
                    d.LoanProductRequiredDocumentId != null)
                .Select(d => new
                {
                    RequiredDocumentId =
                        d.LoanProductRequiredDocumentId!.Value,

                    d.ReviewStatus,

                    d.CreatedAt
                })
                .ToListAsync();

            // Only the latest document for each requirement
            // determines whether that requirement is satisfied.

            var suppliedDocumentIds = uploadedDocuments
                .GroupBy(d => d.RequiredDocumentId)
                .Where(group =>
                {
                    var latestDocument = group
                        .OrderByDescending(d => d.CreatedAt)
                        .First();

                    return latestDocument.ReviewStatus ==
                               DocumentReviewStatus.Pending ||
                           latestDocument.ReviewStatus ==
                               DocumentReviewStatus.Verified;
                })
                .Select(group => group.Key)
                .ToHashSet();

            // Identify outstanding mandatory documents.

            var missingDocuments = mandatoryDocuments
                .Where(required =>
                    !suppliedDocumentIds.Contains(required.Id))
                .ToList();

            var previousStatus = application.Status;

            application.SubmittedAt ??= DateTime.UtcNow;

            application.UpdatedAt = DateTime.UtcNow;

            if (missingDocuments.Any())
            {
                // The application has been received,
                // but mandatory supporting documents are missing.

                application.Status =
                    LoanApplicationStatus.DocumentsRequired;

                var missingNames = string.Join(
                    ", ",
                    missingDocuments.Select(d => d.DocumentName));

                _context.LoanApplicationStatusHistories.Add(
                    new LoanApplicationStatusHistory
                    {
                        LoanApplicationId = application.Id,

                        PreviousStatus = previousStatus,

                        NewStatus = application.Status,

                        ChangedAt = DateTime.UtcNow,

                        ChangedByUserId = userId,

                        Notes =
                            $"Initial submission received. Outstanding " +
                            $"mandatory documents: {missingNames}."
                    });

                await _context.SaveChangesAsync();

                TempData["Warning"] =
                    "Your application has been received. Please upload " +
                    "the outstanding mandatory documents to proceed.";
            }
            else
            {
                // All mandatory supporting documents have been supplied.

                application.Status =
                    LoanApplicationStatus.Submitted;

                _context.LoanApplicationStatusHistories.Add(
                    new LoanApplicationStatusHistory
                    {
                        LoanApplicationId = application.Id,

                        PreviousStatus = previousStatus,

                        NewStatus = application.Status,

                        ChangedAt = DateTime.UtcNow,

                        ChangedByUserId = userId,

                        Notes =
                            "Application submitted. All mandatory " +
                            "documents supplied."
                    });

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Your loan application has been submitted successfully.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // ====================================================
        // HELPER: Retrieve the authenticated borrower's profile
        // ====================================================

        private async Task<Borrower?> GetCurrentBorrowerAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return null;

            return await _context.Borrowers
                .FirstOrDefaultAsync(b =>
                    b.ApplicationUserId == userId &&
                    b.IsActive);
        }

        // ====================================================
        // HELPER: Load available products into the application form
        // ====================================================

        private async Task LoadAvailableProductsAsync(
            CreateLoanApplicationViewModel model)
        {
            model.AvailableProducts = await _context.LoanProducts
                .AsNoTracking()
                .Where(p =>
                    p.IsActive &&
                    p.IsAvailableForApplication)
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

                    RepaymentFrequency =
                        p.RepaymentFrequency.ToString(),

                    InterestMethod =
                        p.InterestMethod.ToString()
                })
                .ToListAsync();
        }

        // ====================================================
        // HELPER: Generate a unique application number
        // ====================================================

        private async Task<string> GenerateApplicationNumberAsync()
        {
            string applicationNumber;

            do
            {
                applicationNumber =
                    $"KRV-{DateTime.UtcNow:yyyyMMdd}-" +
                    $"{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            }
            while (await _context.LoanApplications
                .AnyAsync(a =>
                    a.ApplicationNumber == applicationNumber));

            return applicationNumber;
        }
    }
}
