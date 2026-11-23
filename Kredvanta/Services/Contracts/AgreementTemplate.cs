using System.Text;
using Kredvanta.Models.Contracts;
using Kredvanta.Services.Credit;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Contracts;

public static class AgreementTemplate
{
    public const string Version = "1.0-DRAFT";

    public static string Render(LoanOffer o, IReadOnlyList<ScheduleLine> schedule,
        string borrowerName, string idNumber, string agreementNumber)
    {
        var sb = new StringBuilder();
        sb.AppendLine("LOAN AGREEMENT");
        sb.AppendLine($"Agreement number: {agreementNumber}    Template version: {Version}");
        sb.AppendLine("*** DRAFT TEXT. NOT REVIEWED BY LEGAL COUNSEL. ***");
        sb.AppendLine();
        sb.AppendLine($"Borrower: {borrowerName} (ID/Passport: {idNumber})");
        sb.AppendLine($"Lender: [LEGAL ENTITY NAME AND REGISTRATION DETAILS]");
        sb.AppendLine($"Product: {o.ProductNameSnapshot} ({o.ProductCodeSnapshot})");
        sb.AppendLine();
        sb.AppendLine("1. LOAN TERMS");
        sb.AppendLine($"   Principal amount: {o.CurrencyCode} {o.Principal:N2}");
        sb.AppendLine($"   Interest rate: {o.AnnualInterestRate:N2}% per year, {o.InterestMethod}");
        sb.AppendLine($"   Processing fee: {o.CurrencyCode} {o.ProcessingFee:N2} (deducted from the amount paid out)");
        sb.AppendLine($"   Amount paid to the borrower: {o.CurrencyCode} {(o.Principal - o.ProcessingFee):N2}");
        sb.AppendLine($"   Total interest: {o.CurrencyCode} {o.TotalInterest:N2}");
        sb.AppendLine($"   Total cost of credit (interest and fees): {o.CurrencyCode} {(o.TotalInterest + o.ProcessingFee):N2}");
        sb.AppendLine($"   Total to be repaid (principal and interest): {o.CurrencyCode} {o.TotalRepayable:N2}");
        sb.AppendLine($"   Repayment: {o.TermPeriods} {o.RepaymentFrequency} instalments of about {o.CurrencyCode} {o.InstalmentAmount:N2}");
        sb.AppendLine($"   First instalment: {o.FirstDueDate:dd MMM yyyy}. Final instalment: {o.MaturityDate:dd MMM yyyy}");
        if (!string.IsNullOrWhiteSpace(o.Conditions))
            sb.AppendLine($"   Conditions of approval: {o.Conditions}");
        sb.AppendLine();
        sb.AppendLine("2. REPAYMENT SCHEDULE");
        foreach (var l in schedule)
            sb.AppendLine($"   {l.Number,2}. {l.DueDate:dd MMM yyyy}  principal {l.Principal:N2}  interest {l.Interest:N2}  total {l.Total:N2}");
        sb.AppendLine();
        sb.AppendLine("3. LATE PAYMENT AND DEFAULT");
        sb.AppendLine("   [TO BE COMPLETED BY LEGAL COUNSEL: late-payment charges, default, collection practices]");
        sb.AppendLine();
        sb.AppendLine("4. EARLY REPAYMENT");
        sb.AppendLine("   [TO BE COMPLETED BY LEGAL COUNSEL: early settlement conditions]");
        sb.AppendLine();
        sb.AppendLine("5. DATA PROTECTION, COMPLAINTS AND DISPUTES");
        sb.AppendLine("   [TO BE COMPLETED BY LEGAL COUNSEL: privacy notice, complaints channel, dispute resolution]");
        sb.AppendLine();
        sb.AppendLine($"6. OFFER VALIDITY: this offer expires on {o.ExpiresAt:dd MMM yyyy HH:mm} UTC.");
        return sb.ToString();
    }
}