
using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Identity;
using Kredvanta.ViewModels.LoanApplications;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Borrower")]
    public class LoanApplicationDocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        private const long MaximumFileSize = 10 * 1024 * 1024;

        public LoanApplicationDocumentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _configuration = configuration;
        }

        // ============================================================
        // GET: /LoanApplicationDocuments/Upload?applicationId={id}
        // Display the loan application document upload form.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Upload(Guid applicationId)
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction("Step1", "Profile");
            }

            var application = await _context.LoanApplications
                .AsNoTracking()
                .Include(a => a.LoanProduct)
                .FirstOrDefaultAsync(a =>
                    a.Id == applicationId &&
                    a.BorrowerId == borrower.Id);

            if (application == null)
            {
                return NotFound();
            }

            if (!CanUpload(application.Status))
            {
                TempData["Error"] =
                    "Documents cannot currently be uploaded for this application.";

                return RedirectToAction(
                    "Details",
                    "LoanApplications",
                    new { id = application.Id });
            }

            var model = await BuildUploadViewModelAsync(
                application.Id,
                application.LoanProductId,
                application.ApplicationNumber,
                application.LoanProduct.ProductName);

            return View(model);
        }

        // ============================================================
        // POST: /LoanApplicationDocuments/Upload
        // Validate and upload a loan application document.
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            UploadLoanApplicationDocumentViewModel model)
        {
            // --------------------------------------------------------
            // 1. Identify the current borrower.
            // --------------------------------------------------------

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction("Step1", "Profile");
            }

            // --------------------------------------------------------
            // 2. Retrieve the borrower's loan application.
            // --------------------------------------------------------

            var application = await _context.LoanApplications
                .Include(a => a.LoanProduct)
                .FirstOrDefaultAsync(a =>
                    a.Id == model.LoanApplicationId &&
                    a.BorrowerId == borrower.Id);

            if (application == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // 3. Check whether document uploads are permitted.
            // --------------------------------------------------------

            if (!CanUpload(application.Status))
            {
                TempData["Error"] =
                    "Documents cannot currently be uploaded for this application.";

                return RedirectToAction(
                    "Details",
                    "LoanApplications",
                    new { id = application.Id });
            }

            // --------------------------------------------------------
            // 4. Validate the uploaded file.
            // --------------------------------------------------------

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "Please select a document to upload.");
            }

            string? originalFileName = null;
            string? extension = null;
            string? contentType = null;

            if (model.File != null && model.File.Length > 0)
            {
                originalFileName = Path.GetFileName(
                    model.File.FileName.Replace('\\', '/'));

                // Validate file size.
                if (model.File.Length > MaximumFileSize)
                {
                    ModelState.AddModelError(
                        nameof(model.File),
                        "The maximum permitted file size is 10 MB.");
                }

                // Validate original filename.
                if (string.IsNullOrWhiteSpace(originalFileName) ||
                    originalFileName.Length > 255)
                {
                    ModelState.AddModelError(
                        nameof(model.File),
                        "The document filename is invalid or too long.");
                }

                extension = Path.GetExtension(originalFileName)
                    .ToLowerInvariant();

                // Validate extension and determine content type.
                contentType = GetContentType(extension);

                if (contentType == null)
                {
                    ModelState.AddModelError(
                        nameof(model.File),
                        "Only PDF, JPG, JPEG and PNG documents are accepted.");
                }
                else
                {
                    // Validate the actual file signature.
                    var validSignature =
                        await HasValidFileSignatureAsync(
                            model.File,
                            extension);

                    if (!validSignature)
                    {
                        ModelState.AddModelError(
                            nameof(model.File),
                            "The selected file is invalid or its contents " +
                            "do not match its file extension.");
                    }
                }
            }

            // --------------------------------------------------------
            // 5. Validate the selected document requirement.
            // --------------------------------------------------------

            var requiredDocument = await _context
                .LoanProductRequiredDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.Id == model.LoanProductRequiredDocumentId &&
                    d.LoanProductId == application.LoanProductId &&
                    d.IsActive);

            if (requiredDocument == null)
            {
                ModelState.AddModelError(
                    nameof(model.LoanProductRequiredDocumentId),
                    "Select a valid active document requirement for this loan product.");
            }

            // --------------------------------------------------------
            // 6. Rebuild the form if validation fails.
            // --------------------------------------------------------

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildUploadViewModelAsync(
                    application.Id,
                    application.LoanProductId,
                    application.ApplicationNumber,
                    application.LoanProduct.ProductName);

                // Preserve the selected document requirement.
                invalidModel.LoanProductRequiredDocumentId =
                    model.LoanProductRequiredDocumentId;

                return View(invalidModel);
            }

            // --------------------------------------------------------
            // 7. Identify the authenticated user.
            // --------------------------------------------------------

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // --------------------------------------------------------
            // 8. Prepare private file storage.
            // --------------------------------------------------------

            // Generate a random filename.
            // Never use the original filename as the physical filename.

            var storageKey = $"{Guid.NewGuid():N}{extension}";

            var storageDirectory = GetStorageDirectory();

            Directory.CreateDirectory(storageDirectory);

            var physicalPath = Path.Combine(
                storageDirectory,
                storageKey);

            // Track the completion-check outcome.
            bool allMandatoryDocumentsSupplied = false;
            bool waitingForKycVerification = false;

            // Track whether the database save completed.
            bool databaseSaved = false;

            try
            {
                // ----------------------------------------------------
                // 9. Save the physical file.
                // ----------------------------------------------------

                await using (var fileStream = new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous))
                {
                    await model.File!.CopyToAsync(fileStream);
                }

                // ----------------------------------------------------
                // 10. Create the document database record.
                // ----------------------------------------------------

                var document = new LoanApplicationDocument
                {
                    LoanApplicationId = application.Id,

                    LoanProductRequiredDocumentId =
                        requiredDocument!.Id,

                    OriginalFileName = originalFileName!,

                    StorageKey = storageKey,

                    ContentType = contentType!,

                    FileExtension = extension!,

                    FileSizeBytes = model.File!.Length,

                    UploadedByUserId = userId,

                    ReviewStatus = DocumentReviewStatus.Pending
                };

                _context.LoanApplicationDocuments.Add(document);

                // ----------------------------------------------------
                // 11. Check mandatory document completion.
                // ----------------------------------------------------

                if (application.Status ==
                    LoanApplicationStatus.DocumentsRequired)
                {
                    var completionResult =
                        await CheckApplicationDocumentCompletionAsync(
                            application,
                            borrower,
                            requiredDocument.Id,
                            userId);

                    allMandatoryDocumentsSupplied =
                        completionResult.AllDocumentsSupplied;

                    waitingForKycVerification =
                        completionResult.WaitingForKycVerification;
                }

                // ----------------------------------------------------
                // 12. Save all database changes.
                // ----------------------------------------------------

                await _context.SaveChangesAsync();

                databaseSaved = true;

                // ----------------------------------------------------
                // 13. Set the appropriate user feedback.
                // ----------------------------------------------------

                if (waitingForKycVerification)
                {
                    TempData["Warning"] =
                        "Your document was uploaded successfully. All mandatory " +
                        "loan documents have been supplied, but your application " +
                        "is waiting for identity verification.";
                }
                else if (allMandatoryDocumentsSupplied &&
                         application.Status == LoanApplicationStatus.Submitted)
                {
                    TempData["Success"] =
                        "All mandatory loan documents have been supplied. " +
                        "Your application has been submitted successfully.";
                }
                else
                {
                    TempData["Success"] =
                        "Your document has been uploaded successfully.";
                }

                return RedirectToAction(
                    "Details",
                    "LoanApplications",
                    new { id = application.Id });
            }
            catch
            {
                // ----------------------------------------------------
                // 14. Clean up if the operation fails.
                // ----------------------------------------------------

                // Do not delete the file if the database save already
                // completed successfully.

                if (!databaseSaved &&
                    System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }

                throw;
            }
        }

        // ============================================================
        // HELPER: Check completion of mandatory loan documents.
        // ============================================================

        private async Task<DocumentCompletionResult>
            CheckApplicationDocumentCompletionAsync(
                LoanApplication application,
                Borrower borrower,
                Guid newlyUploadedRequirementId,
                string userId)
        {
            var result = new DocumentCompletionResult();

            // --------------------------------------------------------
            // 1. Retrieve active mandatory requirements.
            // --------------------------------------------------------

            var mandatoryRequirements = await _context
                .LoanProductRequiredDocuments
                .AsNoTracking()
                .Where(d =>
                    d.LoanProductId == application.LoanProductId &&
                    d.IsActive &&
                    d.IsMandatory)
                .Select(d => d.Id)
                .ToListAsync();

            // --------------------------------------------------------
            // 2. Retrieve existing uploads for this application.
            // --------------------------------------------------------

            var existingUploads = await _context
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

            // --------------------------------------------------------
            // 3. Determine the latest upload for each requirement.
            // --------------------------------------------------------

            var suppliedIds = existingUploads
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

            // Include the document currently being uploaded.
            // It is Pending and has not yet been saved to the database.

            suppliedIds.Add(newlyUploadedRequirementId);

            // --------------------------------------------------------
            // 4. Check whether all mandatory requirements are supplied.
            // --------------------------------------------------------

            result.AllDocumentsSupplied =
                mandatoryRequirements.All(
                    requiredId => suppliedIds.Contains(requiredId));

            if (!result.AllDocumentsSupplied)
            {
                return result;
            }

            // --------------------------------------------------------
            // 5. Check borrower KYC status.
            // --------------------------------------------------------

            if (borrower.KycStatus != KycStatus.Verified)
            {
                result.WaitingForKycVerification = true;

                return result;
            }

            // --------------------------------------------------------
            // 6. Transition application to Submitted.
            // --------------------------------------------------------

            var previousStatus = application.Status;

            application.Status = LoanApplicationStatus.Submitted;

            application.UpdatedAt = DateTime.UtcNow;

            _context.LoanApplicationStatusHistories.Add(
                new LoanApplicationStatusHistory
                {
                    LoanApplicationId = application.Id,

                    PreviousStatus = previousStatus,

                    NewStatus = LoanApplicationStatus.Submitted,

                    ChangedAt = DateTime.UtcNow,

                    ChangedByUserId = userId,

                    Notes =
                        "All mandatory loan documents have been supplied " +
                        "and borrower KYC is verified."
                });

            return result;
        }

        // ============================================================
        // HELPER: Retrieve the authenticated borrower's profile.
        // ============================================================

        private async Task<Borrower?> GetCurrentBorrowerAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _context.Borrowers
                .FirstOrDefaultAsync(b =>
                    b.ApplicationUserId == userId &&
                    b.IsActive);
        }

        // ============================================================
        // HELPER: Determine whether document uploads are permitted.
        // ============================================================

        private static bool CanUpload(LoanApplicationStatus status)
        {
            return status == LoanApplicationStatus.Draft ||
                   status == LoanApplicationStatus.DocumentsRequired;
        }

        // ============================================================
        // HELPER: Build the upload view model.
        // ============================================================

        private async Task<UploadLoanApplicationDocumentViewModel>
            BuildUploadViewModelAsync(
                Guid applicationId,
                Guid loanProductId,
                string applicationNumber,
                string productName)
        {
            var requiredDocuments = await _context
                .LoanProductRequiredDocuments
                .AsNoTracking()
                .Where(d =>
                    d.LoanProductId == loanProductId &&
                    d.IsActive)
                .OrderByDescending(d => d.IsMandatory)
                .ThenBy(d => d.DocumentName)
                .Select(d => new RequiredDocumentOptionViewModel
                {
                    Id = d.Id,

                    DocumentName = d.DocumentName,

                    Description = d.Description,

                    IsMandatory = d.IsMandatory,

                    RequiredPeriodMonths = d.RequiredPeriodMonths
                })
                .ToListAsync();

            return new UploadLoanApplicationDocumentViewModel
            {
                LoanApplicationId = applicationId,

                ApplicationNumber = applicationNumber,

                LoanProductName = productName,

                RequiredDocuments = requiredDocuments
            };
        }

        // ============================================================
        // HELPER: Resolve private document storage directory.
        // ============================================================

        private string GetStorageDirectory()
        {
            var configuredPath = _configuration[
                "FileStorage:LoanApplicationDocumentsPath"];

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.Combine(
                    _environment.ContentRootPath,
                    "App_Data",
                    "PrivateDocuments",
                    "LoanApplications");
            }

            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(
                    _environment.ContentRootPath,
                    configuredPath);
        }

        // ============================================================
        // HELPER: Resolve content type from file extension.
        // ============================================================

        private static string? GetContentType(string extension)
        {
            return extension switch
            {
                ".pdf" => "application/pdf",

                ".jpg" => "image/jpeg",

                ".jpeg" => "image/jpeg",

                ".png" => "image/png",

                _ => null
            };
        }

        // ============================================================
        // HELPER: Validate the actual file signature.
        // ============================================================

        private static async Task<bool> HasValidFileSignatureAsync(
            IFormFile file,
            string extension)
        {
            byte[] header = new byte[8];

            await using var stream = file.OpenReadStream();

            var bytesRead = await stream.ReadAsync(
                header,
                0,
                header.Length);

            return extension switch
            {
                ".pdf" => bytesRead >= 5 &&
                    header[0] == 0x25 &&
                    header[1] == 0x50 &&
                    header[2] == 0x44 &&
                    header[3] == 0x46 &&
                    header[4] == 0x2D,

                ".jpg" or ".jpeg" => bytesRead >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                ".png" => bytesRead >= 8 &&
                    header[0] == 0x89 &&
                    header[1] == 0x50 &&
                    header[2] == 0x4E &&
                    header[3] == 0x47 &&
                    header[4] == 0x0D &&
                    header[5] == 0x0A &&
                    header[6] == 0x1A &&
                    header[7] == 0x0A,

                _ => false
            };
        }

        // ============================================================
        // RESULT MODEL: Document completion check.
        // ============================================================

        private class DocumentCompletionResult
        {
            public bool AllDocumentsSupplied { get; set; }

            public bool WaitingForKycVerification { get; set; }
        }
    }
}
