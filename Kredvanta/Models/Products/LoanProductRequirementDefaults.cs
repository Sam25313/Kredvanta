namespace Kredvanta.Models.Products;

public static class LoanProductRequirementDefaults
{
    private static readonly (string Name, string Description, int? Months, bool Mandatory)[] Requirements =
    [
        ("National ID (front and back)", "Clear copy of both sides of your national ID.", null, true),
        ("Employment contract or appointment letter", "Current contract, including any amendments.", null, true),
        ("Payslips", "Your latest three payslips.", 3, true),
        ("Bank statements", "Official statements covering the latest three consecutive months.", 3, true),
        ("M-Pesa statement", "Statement covering the latest three consecutive months.", 3, true),
        ("Employer confirmation", "Only if our team asks for it.", null, false)
    ];

    public static IReadOnlyCollection<LoanProductRequiredDocument> AddMissingTo(LoanProduct product)
    {
        var existingNames = product.RequiredDocuments
            .Select(document => document.DocumentName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addedDocuments = new List<LoanProductRequiredDocument>();

        foreach (var requirement in Requirements)
        {
            if (!existingNames.Add(requirement.Name))
                continue;

            var document = new LoanProductRequiredDocument
            {
                DocumentName = requirement.Name,
                Description = requirement.Description,
                RequiredPeriodMonths = requirement.Months,
                IsMandatory = requirement.Mandatory,
                IsActive = true
            };
            product.RequiredDocuments.Add(document);
            addedDocuments.Add(document);
        }

        return addedDocuments;
    }
}
