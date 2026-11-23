
using Kredvanta.Data;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Identity;
using Kredvanta.Services.Documents;
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
        private readonly IDocumentService _documents;

        private const long MaximumFileSize = 5 * 1024 * 1024;

        public LoanApplicationDocumentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IDocumentService documents)
        {
            _context = context;
            _userManager = userManager;
            _documents = documents;
        }

     
        // GET: /LoanApplicationDocuments/Upload?applicationId={id}
        // Display the loan application document upload form.
       

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

        
        // POST: /LoanApplicationDocuments/Upload
        // Validate and upload a loan application document.
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            UploadLoanApplicationDocumentViewModel model)
        {
            
            // 1. Identify the current borrower.
           

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction("Step1", "Profile");
            }

            
            // 2. Retrieve the borrower's loan application.
            

            var application = await _context.LoanApplications
                .Include(a => a.LoanProduct)
                .FirstOrDefaultAsync(a =>
                    a.Id == model.LoanApplicationId &&
                    a.BorrowerId == borrower.Id);

            if (application == null)
            {
                return NotFound();
            }

            
            // 3. Check whether document uploads are permitted.
            

            if (!CanUpload(application.Status))
            {
                TempData["Error"] =
                    "Documents cannot currently be uploaded for this application.";

                return RedirectToAction(
                    "Details",
                    "LoanApplications",
                    new { id = application.Id });
            }

            
            // Validate the uploaded file.
            

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
                        "The maximum permitted file size is 5 MB.");
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

             
            // 5. Validate the selected document requirement.
         

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

            
            // Rebuild the form if validation fails.
          

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

            
            //Identify the authenticated user.
            

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            await using var fileStream = model.File!.OpenReadStream();
            var uploadResult = await _documents.UploadAsync(
                application.Id,
                requiredDocument!.Id,
                fileStream,
                originalFileName!,
                userId);

            if (!uploadResult.Succeeded)
            {
                ModelState.AddModelError(
                    nameof(model.File),
                    uploadResult.Error ?? "The document could not be uploaded.");

                var invalidModel = await BuildUploadViewModelAsync(
                    application.Id,
                    application.LoanProductId,
                    application.ApplicationNumber,
                    application.LoanProduct.ProductName);
                invalidModel.LoanProductRequiredDocumentId = model.LoanProductRequiredDocumentId;

                return View(invalidModel);
            }

            TempData["Success"] =
                "Your document has been uploaded. Review the requirements and submit the application when ready.";

            return RedirectToAction("Screening", new { applicationId = application.Id });
            //return RedirectToAction(
               // "Details",
               // "LoanApplications",
                //new { id = application.Id });
        }
        [HttpGet]
        public async Task<IActionResult> Screening(Guid applicationId)
        {
            var borrower = await GetCurrentBorrowerAsync();
            if (borrower == null) return RedirectToAction("Step1", "Profile");

            var application = await _context.LoanApplications.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.BorrowerId == borrower.Id);
            if (application == null) return NotFound();

            return View(new ScreeningPageViewModel
            {
                LoanApplicationId = application.Id,
                ApplicationNumber = application.ApplicationNumber
            });
        }

        [HttpGet]
        public async Task<IActionResult> ScreeningStatus(Guid applicationId)
        {
            var borrower = await GetCurrentBorrowerAsync();
            if (borrower == null) return Unauthorized();

            var owns = await _context.LoanApplications
                .AnyAsync(a => a.Id == applicationId && a.BorrowerId == borrower.Id);
            if (!owns) return NotFound();

            var rows = await _context.LoanApplicationDocuments.AsNoTracking()
                .Where(d => d.LoanApplicationId == applicationId && d.IsCurrent)
                .OrderBy(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    Name = d.RequiredDocument != null ? d.RequiredDocument.DocumentName : d.OriginalFileName,
                    d.ScreeningStatus
                })
                .ToListAsync();

            var documents = rows.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                status = r.ScreeningStatus.ToString(),
                message = ApplicantMessage(r.ScreeningStatus)
            });

            var done = rows.All(r => r.ScreeningStatus is not
                (DocumentScreeningStatus.Queued or DocumentScreeningStatus.Scanning));

            return Json(new { done, documents });
        }

        private static string ApplicantMessage(DocumentScreeningStatus s) => s switch
        {
            DocumentScreeningStatus.Queued => "Waiting to be checked",
            DocumentScreeningStatus.Scanning => "Checking against your profile...",
            DocumentScreeningStatus.Passed => "Automatic checks passed",
            DocumentScreeningStatus.Flagged => "We couldn't confirm everything automatically. You can replace this document, or continue and a loan officer will review it.",
            DocumentScreeningStatus.Failed => "We couldn't check this file automatically. A loan officer will review it.",
            _ => "Not checked"
        };

        //Retrieve the authenticated borrower's profile.

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

        
        //Determine whether document uploads are permitted.

        private static bool CanUpload(LoanApplicationStatus status)
        {
            return status == LoanApplicationStatus.Draft ||
                   status == LoanApplicationStatus.DocumentsRequired;
        }

        //Build the upload view model.
        private async Task<UploadLoanApplicationDocumentViewModel> BuildUploadViewModelAsync(
    Guid applicationId,
    Guid loanProductId,
    string applicationNumber,
    string productName)
        {
            var requiredDocuments = await _context.LoanProductRequiredDocuments
                .AsNoTracking()
                .Where(d => d.LoanProductId == loanProductId && d.IsActive)
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

            var current = await _context.LoanApplicationDocuments
                .AsNoTracking()
                .Where(x => x.LoanApplicationId == applicationId
                         && x.IsCurrent
                         && x.LoanProductRequiredDocumentId != null)
                .Select(x => new
                {
                    RequirementId = x.LoanProductRequiredDocumentId!.Value,
                    x.OriginalFileName,
                    x.Version,
                    x.CreatedAt,
                    x.ReviewStatus,
                    x.ScreeningStatus,
                    x.ReviewComments
                })
                .ToListAsync();

            foreach (var requirement in requiredDocuments)
            {
                var c = current.FirstOrDefault(x => x.RequirementId == requirement.Id);
                if (c is null) continue;

                requirement.Current = new UploadedDocumentSummary
                {
                    OriginalFileName = c.OriginalFileName,
                    Version = c.Version,
                    UploadedAt = c.CreatedAt,
                    ReviewStatus = c.ReviewStatus,
                    ScreeningStatus = c.ScreeningStatus,
                    ReviewComments = c.ReviewComments
                };
            }

            return new UploadLoanApplicationDocumentViewModel
            {
                LoanApplicationId = applicationId,
                ApplicationNumber = applicationNumber,
                LoanProductName = productName,
                RequiredDocuments = requiredDocuments
            };
        }

        // Resolve content type from file extension.
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

        // Validate the actual file signature.

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

            }
        }
