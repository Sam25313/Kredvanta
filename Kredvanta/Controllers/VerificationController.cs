
using Kredvanta.Data;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Identity;
using Kredvanta.ViewModels.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Borrower")]
    public class VerificationController : Controller
    {
        // --------------------------------------------------
        // CONFIGURATION
        // --------------------------------------------------

        private const long MaxFileSize = 5 * 1024 * 1024;

        private const string NationalIdFront = "National ID - Front";
        private const string NationalIdBack = "National ID - Back";

        private const string UploadFolder = "borrower-verification";

        private static readonly string[] AllowedDocumentTypes =
        {
            NationalIdFront,
            NationalIdBack,
            "Passport",
            "Passport Photograph",
            "Proof of Residence",
            "Utility Bill",
            "Tenancy Agreement"
        };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public VerificationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // --------------------------------------------------
        // CURRENT BORROWER
        // --------------------------------------------------

        private async Task<Borrower?> GetCurrentBorrowerAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _context.Borrowers
                .FirstOrDefaultAsync(
                    b => b.ApplicationUserId == userId);
        }

        // --------------------------------------------------
        // VERIFICATION CENTRE
        // GET: /Verification
        // --------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            var documents = await _context.BorrowerVerificationDocuments
                .AsNoTracking()
                .Where(d => d.BorrowerId == borrower.Id)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var model = new VerificationCentreViewModel
            {
                KycStatus = borrower.KycStatus,
                Documents = documents
            };

            return View(model);
        }

        // --------------------------------------------------
        // UPLOAD PAGE
        // GET: /Verification/Upload
        // --------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Upload(string? documentType)
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            if (!string.IsNullOrWhiteSpace(documentType) &&
                !IsAllowedDocumentType(documentType))
            {
                return BadRequest("Invalid KYC document type.");
            }

            SetDocumentTypes();

            var model = new UploadVerificationDocumentViewModel
            {
                DocumentType = documentType ?? string.Empty
            };

            return View(model);
        }

        // --------------------------------------------------
        // PROCESS DOCUMENT UPLOAD
        // POST: /Verification/Upload
        // --------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            UploadVerificationDocumentViewModel model)
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            SetDocumentTypes();

            // 1. Validate document type.
            if (!IsAllowedDocumentType(model.DocumentType))
            {
                ModelState.AddModelError(
                    nameof(model.DocumentType),
                    "Select a valid KYC document type.");
            }

            // 2. Validate file.
            ValidateUploadedFile(model.File);

            // 3. Prevent duplicate pending or verified submissions.
            if (IsAllowedDocumentType(model.DocumentType))
            {
                await ValidateDuplicateDocumentAsync(
                    borrower.Id,
                    model.DocumentType);
            }

            if (!ModelState.IsValid)
                return View(model);

            var file = model.File!;

            // 4. Validate actual file content.
            var fileInfo = await IdentifyFileAsync(file);

            if (fileInfo == null)
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "The uploaded file is invalid. Only genuine PDF, JPEG and PNG files are accepted.");

                return View(model);
            }

            // National ID front and back must be images.
            if (IsNationalIdSide(model.DocumentType) &&
                fileInfo.Value.ContentType != "image/jpeg" &&
                fileInfo.Value.ContentType != "image/png")
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    "National ID front and back must be uploaded as JPEG or PNG images.");

                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // 5. Generate a private storage location.
            var folder = GetBorrowerStorageFolder(borrower.Id);

            Directory.CreateDirectory(folder);

            var generatedFileName =
                $"{Guid.NewGuid():N}{fileInfo.Value.Extension}";

            var fullPath = Path.Combine(folder, generatedFileName);

            try
            {
                // 6. Save the physical file.
                await using (var stream = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await file.CopyToAsync(stream);
                }

                // 7. Create the document record.
                var document = new BorrowerVerificationDocument
                {
                    BorrowerId = borrower.Id,
                    DocumentType = model.DocumentType,

                    OriginalFileName =
                        Path.GetFileName(file.FileName),

                    StorageKey = generatedFileName,
                    ContentType = fileInfo.Value.ContentType,
                    FileExtension = fileInfo.Value.Extension,
                    FileSizeBytes = file.Length,

                    UploadedByUserId = userId,

                    ReviewStatus = DocumentReviewStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BorrowerVerificationDocuments.Add(document);

                // A new KYC submission requires staff review.
                borrower.KycStatus = KycStatus.Pending;

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Prevent orphaned physical files if saving fails.
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                throw;
            }

            // 8. Determine the next destination.
            return await RedirectAfterUploadAsync(borrower.Id);
        }

        // --------------------------------------------------
        // POST-UPLOAD NAVIGATION
        // --------------------------------------------------

        private async Task<IActionResult> RedirectAfterUploadAsync(
            Guid borrowerId)
        {
            var hasFront = await HasSubmittedNationalIdSideAsync(
                borrowerId,
                NationalIdFront);

            var hasBack = await HasSubmittedNationalIdSideAsync(
                borrowerId,
                NationalIdBack);

            if (hasFront && hasBack)
            {
                TempData["SuccessMessage"] =
                    "Both sides of your National ID have been submitted successfully. " +
                    "You can now explore available loan products while our team reviews your documents.";

                return RedirectToAction("Index", "LoanProducts");
            }

            TempData["SuccessMessage"] =
                "Your document was uploaded successfully. " +
                "Please upload the remaining side of your National ID to continue.";

            return RedirectToAction(nameof(Index));
        }

        // --------------------------------------------------
        // NATIONAL ID COMPLETENESS
        // --------------------------------------------------

        private async Task<bool> HasSubmittedNationalIdSideAsync(
            Guid borrowerId,
            string documentType)
        {
            var latestDocument = await _context.BorrowerVerificationDocuments
                .AsNoTracking()
                .Where(d =>
                    d.BorrowerId == borrowerId &&
                    d.DocumentType == documentType)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            return latestDocument != null &&
                (latestDocument.ReviewStatus == DocumentReviewStatus.Pending ||
                 latestDocument.ReviewStatus == DocumentReviewStatus.Verified);
        }

        // --------------------------------------------------
        // DOWNLOAD DOCUMENT
        // GET: /Verification/Download/{id}
        // --------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Download(Guid id)
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            // Restrict access to the current borrower's documents.
            var document = await _context.BorrowerVerificationDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.BorrowerId == borrower.Id);

            if (document == null)
                return NotFound();

            // StorageKey is generated by the application.
            var safeFileName = Path.GetFileName(document.StorageKey);

            var fullPath = Path.Combine(
                GetBorrowerStorageFolder(borrower.Id),
                safeFileName);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(
                fullPath,
                document.ContentType,
                document.OriginalFileName);
        }

        // --------------------------------------------------
        // VALIDATION HELPERS
        // --------------------------------------------------

        private void ValidateUploadedFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(UploadVerificationDocumentViewModel.File),
                    "Please select a document to upload.");

                return;
            }

            if (file.Length > MaxFileSize)
            {
                ModelState.AddModelError(
                    nameof(UploadVerificationDocumentViewModel.File),
                    "The maximum permitted file size is 5 MB.");
            }
        }

        private async Task ValidateDuplicateDocumentAsync(
            Guid borrowerId,
            string documentType)
        {
            var latestDocument = await _context.BorrowerVerificationDocuments
                .Where(d =>
                    d.BorrowerId == borrowerId &&
                    d.DocumentType == documentType)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestDocument == null)
                return;

            if (latestDocument.ReviewStatus == DocumentReviewStatus.Pending)
            {
                ModelState.AddModelError(
                    nameof(UploadVerificationDocumentViewModel.DocumentType),
                    "This document is already awaiting verification. Please wait for the review.");
            }
            else if (latestDocument.ReviewStatus == DocumentReviewStatus.Verified)
            {
                ModelState.AddModelError(
                    nameof(UploadVerificationDocumentViewModel.DocumentType),
                    "This document has already been verified. A replacement is not currently required.");
            }

            // Rejected and ClarificationRequired documents can be replaced.
        }

        private static bool IsAllowedDocumentType(string? documentType)
        {
            return !string.IsNullOrWhiteSpace(documentType) &&
                   AllowedDocumentTypes.Contains(
                       documentType,
                       StringComparer.Ordinal);
        }

        private static bool IsNationalIdSide(string? documentType)
        {
            return documentType == NationalIdFront ||
                   documentType == NationalIdBack;
        }

        // --------------------------------------------------
        // STORAGE HELPERS
        // --------------------------------------------------

        private string GetBorrowerStorageFolder(Guid borrowerId)
        {
            return Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "uploads",
                UploadFolder,
                borrowerId.ToString());
        }

        private void SetDocumentTypes()
        {
            ViewBag.DocumentTypes = AllowedDocumentTypes;
        }

        // --------------------------------------------------
        // FILE SIGNATURE VALIDATION
        // --------------------------------------------------

        private static async Task<(string Extension, string ContentType)?>
            IdentifyFileAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();

            var header = new byte[8];

            var bytesRead = await stream.ReadAsync(
                header.AsMemory(0, header.Length));

            // PDF signature.
            if (bytesRead >= 4 &&
                header[0] == 0x25 &&
                header[1] == 0x50 &&
                header[2] == 0x44 &&
                header[3] == 0x46)
            {
                return (".pdf", "application/pdf");
            }

            // JPEG signature.
            if (bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                return (".jpg", "image/jpeg");
            }

            // PNG signature.
            byte[] pngSignature =
            {
                0x89, 0x50, 0x4E, 0x47,
                0x0D, 0x0A, 0x1A, 0x0A
            };

            if (bytesRead == 8 &&
                header.SequenceEqual(pngSignature))
            {
                return (".png", "image/png");
            }

            return null;
        }
    }
}
