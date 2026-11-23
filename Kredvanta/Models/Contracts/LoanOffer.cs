using System.ComponentModel.DataAnnotations;
using Kredvanta.Models.Applications;
using Kredvanta.Models.Common;
using Kredvanta.Models.Identity;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Contracts;

public class LoanOffer : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = null!;

    public int Version { get; set; } = 1;
    public OfferStatus Status { get; set; } = OfferStatus.Issued;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string IssuedByUserId { get; set; } = string.Empty;
    public ApplicationUser? IssuedByUser { get; set; }

    // Frozen terms
    public string ProductCodeSnapshot { get; set; } = string.Empty;
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "KES";
    public decimal Principal { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public InterestMethod InterestMethod { get; set; }
    public RepaymentFrequency RepaymentFrequency { get; set; }
    public int TermPeriods { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal TotalRepayable { get; set; }
    public decimal InstalmentAmount { get; set; }
    public DateOnly FirstDueDate { get; set; }
    public DateOnly MaturityDate { get; set; }
    public string ScheduleJson { get; set; } = "[]";
    public string? Conditions { get; set; }

    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedByUserId { get; set; }
    public ApplicationUser? AcceptedByUser { get; set; }
    public string? DeclineReason { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}