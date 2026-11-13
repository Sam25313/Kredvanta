
using Kredvanta.Models.Common;

namespace Kredvanta.Models.Products
{
    public class LoanProductRequiredDocument : BaseEntity
    {
        public Guid LoanProductId { get; set; }

        public LoanProduct LoanProduct { get; set; } = null!;

        public string DocumentName { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Number of months covered by the document, where applicable.
        public int? RequiredPeriodMonths { get; set; }

        public bool IsMandatory { get; set; } = true;

        public bool IsActive { get; set; } = true;
    }
}
