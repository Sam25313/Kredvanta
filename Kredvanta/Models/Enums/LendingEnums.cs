
namespace Kredvanta.Models.Enums;

public static class LendingEnums
{
    public enum Gender { Male, Female, Other, PreferNotToSay }

    public enum EmploymentType
    {
        Employed, SelfEmployed, BusinessOwner, CasualWorker, Unemployed, Other
    }
    public enum DocumentScreeningStatus { NotStarted, Queued, Scanning, Passed, Flagged, Failed }

    // Append new values only; existing values are persisted as integers.
    public enum KycStatus { NotStarted, Pending, Verified, Rejected, Escalated }

    public enum InterestMethod { Flat, ReducingBalance }

    public enum RepaymentFrequency { Weekly, BiWeekly, Monthly }

    // APPEND ONLY. Never reorder or insert in the middle: values are stored as integers.
    public enum LoanApplicationStatus
    {
        Draft,
        Submitted,
        UnderReview,
        DocumentsRequired,
        Recommended,
        Approved,
        Rejected,
        Cancelled,
        VerificationInProgress,
        OfferIssued,
        AgreementPending,
        ReadyForDisbursement,
        Disbursed,
        Withdrawn,
        Expired
    }

    public enum DocumentReviewStatus { Pending, Verified, Rejected, ClarificationRequired }

    public enum CreditRecommendation { Pending, Recommend, RecommendWithConditions, DoNotRecommend }

    public enum ApprovalDecision { Pending, Approved, Rejected, ReturnedForReview }

    public enum LoanStatus
    {
        PendingDisbursement, Active, Overdue, Restructured, Completed, WrittenOff, Cancelled
    }

    public enum DisbursementMethod { Mpesa, BankTransfer, Cash, Cheque }

    public enum DisbursementStatus { Pending, Successful, Failed, Reversed }

    public enum RepaymentMethod { Mpesa, BankTransfer, Cash, Cheque, Other }

    public enum RepaymentStatus { Pending, Confirmed, Failed, Reversed }

    public enum ScheduleStatus { Pending, PartiallyPaid, Paid, Overdue }

    public enum NotificationType
    {
        General, LoanApplication, DocumentReview, LoanApproval, Disbursement, Repayment, System
    }
   

    public enum VerificationChannel { Phone, Email, InPerson, EmployerPortal, Other }

    public enum EmploymentVerificationOutcome { ContactAttempted, Verified, NotVerified, Escalated }
    public enum OfferStatus { Issued, Accepted, Declined, Expired, Superseded }
    public enum AgreementStatus { Generated, Signed, Expired }
}
