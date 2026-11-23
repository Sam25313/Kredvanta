using Kredvanta.Models.Enums;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Staff;

public class OperationsOffersViewModel
{
    public List<OperationsOfferRow> Applications { get; set; } = new();
    public int ReadyToPrepare => Applications.Count(item => item.CanPrepare);
    public int WaitingForApplicant => Applications.Count(item => item.Status == LoanApplicationStatus.OfferIssued);
    public int AgreementsPending => Applications.Count(item => item.Status == LoanApplicationStatus.AgreementPending);
    public int ReadyForFinance => Applications.Count(item => item.Status == LoanApplicationStatus.ReadyForDisbursement);
}

public class OperationsOfferRow
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public LoanApplicationStatus Status { get; set; }
    public DateTime? DecisionAt { get; set; }
    public int? OfferVersion { get; set; }
    public OfferStatus? OfferStatus { get; set; }
    public DateTime? OfferExpiresAt { get; set; }
    public bool CanPrepare { get; set; }
    public string NextStep { get; set; } = string.Empty;
}
