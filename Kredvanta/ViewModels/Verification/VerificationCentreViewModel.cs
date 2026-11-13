using Kredvanta.Models.Borrowers;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Verification
{
    public class VerificationCentreViewModel
    {
        public KycStatus KycStatus { get; set; }
            = KycStatus.NotStarted;

        public List<BorrowerVerificationDocument> Documents { get; set; }
            = new();
    }
}