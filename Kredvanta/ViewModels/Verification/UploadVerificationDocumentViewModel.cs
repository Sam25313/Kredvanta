using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Kredvanta.ViewModels.Verification
{
    public class UploadVerificationDocumentViewModel
    {
        [Required]
        [Display(Name = "Document type")]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Choose document")]
        public IFormFile? File { get; set; }
    }
}