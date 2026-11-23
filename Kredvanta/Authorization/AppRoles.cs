using System.Reflection.Metadata;

namespace Kredvanta.Authorization;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Borrower = "Borrower";
    public const string KycOfficer = "KYCOfficer";
    public const string VerificationOfficer = "VerificationOfficer";
    public const string CreditAnalyst = "CreditAnalyst";
    public const string CreditManager = "CreditManager";
    public const string LoanOfficer = "LoanOfficer";
    public const string FinanceOfficer = "FinanceOfficer";
    public const string Auditor = "Auditor";

    public static readonly string[] All =
    {
        Administrator, Borrower, KycOfficer, VerificationOfficer,
        CreditAnalyst, CreditManager, LoanOfficer, FinanceOfficer, Auditor
    };

    // Staff who may read a case

    public static readonly string[] CaseReaders =
    {
        Administrator, KycOfficer, VerificationOfficer, CreditAnalyst,
        CreditManager, LoanOfficer, FinanceOfficer, Auditor
    };
}
