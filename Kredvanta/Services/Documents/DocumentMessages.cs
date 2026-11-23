using Kredvanta.Models.Applications;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Documents;

public static class DocumentMessages
{
    public static string? ForBorrower(LoanApplicationDocument? d) => d?.ReviewStatus switch
    {
        DocumentReviewStatus.Rejected => d.RejectionReasonCode switch
        {
            "ILLEGIBLE" => "The file was not legible. Please upload a clearer copy.",
            "EXPIRED" => "This document has expired. Please upload a current one.",
            "WRONG_DOCUMENT" => "This is not the document requested. Please upload the correct one.",
            "INCOMPLETE" => "The document is incomplete. Please upload all pages.",
            // DETAILS_MISMATCH, SUSPECTED_ALTERATION and OTHER stay neutral on purpose
            _ => "We could not accept this document. Please upload a new copy or contact us."
        },
        DocumentReviewStatus.ClarificationRequired => d.ReviewComments,
        _ => null
    };
}