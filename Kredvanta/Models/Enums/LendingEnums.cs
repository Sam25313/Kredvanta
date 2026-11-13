namespace Kredvanta.Models.Enums
{
    public class LendingEnums
    {

        public enum Gender
        {
            Male,
            Female,
            Other,
            PreferNotToSay
        }

        public enum EmploymentType
        {
            Employed,
            SelfEmployed,
            BusinessOwner,
            CasualWorker,
            Unemployed,
            Other
        }

        public enum KycStatus
        {
            NotStarted,
            Pending,
            Verified,
            Rejected
        }

        public enum InterestMethod
        {
            Flat,
            ReducingBalance
        }

        public enum RepaymentFrequency
        {
            Weekly,
            BiWeekly,
            Monthly
        }

        public enum LoanApplicationStatus
        {
            Draft,
            Submitted,
            UnderReview,
            DocumentsRequired,
            Recommended,
            Approved,
            Rejected,
            Cancelled
        }

        public enum DocumentReviewStatus
        {
            Pending,
            Verified,
            Rejected,
            ClarificationRequired
        }

        public enum CreditRecommendation
        {
            Pending,
            Recommend,
            RecommendWithConditions,
            DoNotRecommend
        }

        public enum ApprovalDecision
        {
            Pending,
            Approved,
            Rejected,
            ReturnedForReview
        }

        public enum LoanStatus
        {
            PendingDisbursement,
            Active,
            Overdue,
            Restructured,
            Completed,
            WrittenOff,
            Cancelled
        }

        public enum DisbursementMethod
        {
            Mpesa,
            BankTransfer,
            Cash,
            Cheque
        }

        public enum DisbursementStatus
        {
            Pending,
            Successful,
            Failed,
            Reversed
        }

        public enum RepaymentMethod
        {
            Mpesa,
            BankTransfer,
            Cash,
            Cheque,
            Other
        }

        public enum RepaymentStatus
        {
            Pending,
            Confirmed,
            Failed,
            Reversed
        }

        public enum ScheduleStatus
        {
            Pending,
            PartiallyPaid,
            Paid,
            Overdue
        }

        public enum NotificationType
        {
            General,
            LoanApplication,
            DocumentReview,
            LoanApproval,
            Disbursement,
            Repayment,
            System
        }
    }

}
