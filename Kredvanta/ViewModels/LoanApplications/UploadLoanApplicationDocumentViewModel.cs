using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Kredvanta.ViewModels.LoanApplications;

public class UploadLoanApplicationDocumentViewModel
{
    public Guid LoanApplicationId { get; set; }

    public Guid? LoanProductRequiredDocumentId { get; set; }

    public IFormFile? File { get; set; }

    [ValidateNever]
    public string ApplicationNumber { get; set; } = string.Empty;

    [ValidateNever]
    public string LoanProductName { get; set; } = string.Empty;

    [ValidateNever]
    public List<RequiredDocumentOptionViewModel> RequiredDocuments { get; set; } = new();
}

//One document requirement for the loan product, plus the applicant's current file for it.
public class RequiredDocumentOptionViewModel
{
    public Guid Id { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsMandatory { get; set; }

    public int? RequiredPeriodMonths { get; set; }

    //Null when nothing has been uploaded for this requirement yet.
    public UploadedDocumentSummary? Current { get; set; }
}