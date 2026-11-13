
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Kredvanta.ViewModels.LoanApplications
{
    public class UploadLoanApplicationDocumentViewModel
    {
        public Guid LoanApplicationId { get; set; }

        public string ApplicationNumber { get; set; } = string.Empty;

        public string LoanProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select the document type.")]
        public Guid? LoanProductRequiredDocumentId { get; set; }

        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile? File { get; set; }

        public List<RequiredDocumentOptionViewModel> RequiredDocuments
        { get; set; } = new();
    }

    public class RequiredDocumentOptionViewModel
    {
        public Guid Id { get; set; }

        public string DocumentName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsMandatory { get; set; }

        public int? RequiredPeriodMonths { get; set; }
    }
}
