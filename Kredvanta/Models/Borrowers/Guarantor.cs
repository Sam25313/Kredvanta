
using Kredvanta.Models.Common;
using Kredvanta.Models.Applications;

namespace Kredvanta.Models.Borrowers
{
    public class Guarantor : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string NationalIdNumber { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Occupation { get; set; }

        public string? PhysicalAddress { get; set; }

        public string? RelationshipToBorrower { get; set; }

        public ICollection<LoanApplicationGuarantor> LoanApplicationGuarantors
        { get; set; } = new List<LoanApplicationGuarantor>();
    }
}
