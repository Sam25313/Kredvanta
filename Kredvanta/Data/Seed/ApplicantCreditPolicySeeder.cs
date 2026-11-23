using Kredvanta.Models.Credit;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Data.Seed;

public static class ApplicantCreditPolicySeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.ApplicantCreditPolicyPublications.AnyAsync(cancellationToken))
            return;

        var publication = new ApplicantCreditPolicyPublication
        {
            VersionNumber = 1,
            Title = "Applicant Credit Assessment Policy",
            IsPublished = false,
            PurposeAndScope = "This document explains how Kings Microfinance currently intends to review applications for its loan products. It is an applicant-facing summary of the application and assessment process; it is not a loan offer, a credit agreement, or a guarantee of approval. Product-specific terms and the signed loan agreement govern an approved facility. The registered lending entity, its licence category, applicable regulator, and contact details must be confirmed before this draft is adopted.",
            EligibilityAndDocuments = "Applicants must complete the information requested for the selected product and provide the active mandatory documents shown for that product. The standard document set currently includes national identification, employment or appointment evidence, recent payslips, recent bank statements, and recent M-Pesa statements; the product checklist specifies the required period and whether an item is mandatory. Additional information may be requested where it is relevant to verification or assessment. An application is not complete until mandatory documents have been uploaded. Documents and declared information may be checked for completeness, consistency, and authenticity.",
            AssessmentAndDecision = "Before an application can be recommended, the app requires relevant identity and employment checks, including independent confirmation of employment and income where applicable. A credit assessment considers verified net income, existing loan payments, other financial obligations, declared living expenses, the proposed instalment, and the selected product's pricing and repayment schedule. The system calculates affordability indicators under the approved internal credit policy. These indicators support, but do not by themselves guarantee, a lending decision. A credit analyst records an assessment and recommendation; an authorised credit manager makes the decision. Some cases may require more than one manager's approval. Applications may be declined when required evidence is not verified, the request does not meet the applicable policy, or the assessment does not support the requested facility. The applicant will be informed of the outcome and any next steps through the contact channel on file, subject to applicable law.",
            PricingAndOffers = "Before an applicant accepts a facility, the product information and individual offer should clearly state the approved principal, interest rate and calculation method, fees and charges, repayment frequency, instalment schedule, total amount payable, and any conditions that apply. The actual amount and terms depend on the product and the individual offer. No amount should be treated as approved until the lender issues an offer and the required agreement is completed. This policy summary does not set or promise a price.",
            RepaymentAndComplaints = "Applicants should review the due dates and amounts in their offer and agreement and make repayments through the channels specified there. If a repayment difficulty or a concern about an application arises, contact the lender promptly through the verified customer-service details stated in the offer or agreement. Complaints should be acknowledged, reviewed fairly, and answered through the lender's documented complaints process. The lender must insert and verify its customer-service and complaint-escalation contacts, response process, and the correct regulator details before publishing an approved version. Any recovery activity must follow applicable law and the executed agreement.",
            PersonalInformation = "Personal information and application documents are used for purposes such as account administration, identity and employment verification, credit assessment, servicing, fraud prevention, security, and lawful regulatory obligations. Access should be limited to authorised people and information should be retained, shared, and secured in accordance with applicable data-protection requirements and the lender's privacy notice. Applicants can consult the privacy notice for details about their rights and how to make a data-protection request.",
            ImportantNotice = "DRAFT FOR MANAGEMENT AND KENYAN LEGAL/COMPLIANCE REVIEW. This is a working disclosure based on the features currently present in the Kredvanta application and the official references listed below. It is not legal advice and must not be presented as evidence that Kings Microfinance or any related entity is licensed or compliant. Confirm the legal entity, licence and regulatory regime, actual underwriting settings, all product prices and charges, privacy practices, complaints contacts, and applicable customer rights before approval and publication. The Central Bank of Kenya's 2025 Non-Deposit Taking Credit Providers Regulations page linked below describes a draft for public comment; that draft is cited for monitoring only and is not treated here as binding law. Internal numeric affordability thresholds are intentionally not disclosed in this applicant summary.",
            References =
            [
                new ApplicantCreditPolicyReference
                {
                    Title = "Central Bank of Kenya (Digital Credit Providers) Regulations, 2022",
                    Description = "CBK's publication page linking the gazetted regulations. Applicability must be confirmed against the lender's legal and licence status.",
                    Url = "https://www.centralbank.go.ke/2022/03/21/central-bank-of-kenya-digital-credit-providers-regulations-2022/"
                },
                new ApplicantCreditPolicyReference
                {
                    Title = "Central Bank of Kenya: Draft Non-Deposit Taking Credit Providers Regulations, 2025",
                    Description = "CBK identifies this as a draft exposed for public comments; it is not represented as an enacted regulation.",
                    Url = "https://www.centralbank.go.ke/2025/08/07/draft-non-deposit-taking-credit-providers-ndtcps-regulations-2025/"
                },
                new ApplicantCreditPolicyReference
                {
                    Title = "Central Bank of Kenya: Directory of Digital Credit Providers",
                    Description = "Official directory and licensing context; verify the lender's actual legal identity and current status directly.",
                    Url = "https://www.centralbank.go.ke/2022/09/19/directory-of-digital-credit-providers/"
                },
                new ApplicantCreditPolicyReference
                {
                    Title = "Kenya Law: Data Protection Act, No. 24 of 2019",
                    Description = "Official legislation reference for review of personal-data handling requirements.",
                    Url = "https://new.kenyalaw.org/akn/ke/act/2019/24/eng@2019-11-25"
                }
            ]
        };

        context.ApplicantCreditPolicyPublications.Add(publication);
        await context.SaveChangesAsync(cancellationToken);
    }
}
