
using Kredvanta.Models.Products;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Data
{
    public static class LoanProductSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context)
        {
            Console.WriteLine("======================================");
            Console.WriteLine("KREDVANTA LOAN PRODUCT SEEDER STARTED");
            Console.WriteLine($"Database: {context.Database.GetDbConnection().Database}");
            Console.WriteLine($"Server: {context.Database.GetDbConnection().DataSource}");

            const string productCode = "SALARIED_PERSONAL";

            var product = await context.LoanProducts
                .FirstOrDefaultAsync(p =>
                    p.ProductCode == productCode);

            if (product == null)
            {
                Console.WriteLine(
                    "ERROR: SALARIED_PERSONAL product was not found.");

                return;
            }

            Console.WriteLine($"Product found: {product.ProductName}");
            Console.WriteLine($"Product ID: {product.Id}");

            var documentRequirements =
                new List<LoanProductRequiredDocument>
                {
                    new()
                    {
                        DocumentName = "Recent Payslips",
                        Description = "Upload your three most recent salary payslips.",
                        RequiredPeriodMonths = 3,
                        IsMandatory = true,
                        IsActive = true
                    },
                    new()
                    {
                        DocumentName = "Bank Statements",
                        Description = "Upload bank statements covering the most recent three months.",
                        RequiredPeriodMonths = 3,
                        IsMandatory = true,
                        IsActive = true
                    },
                    new()
                    {
                        DocumentName = "Employment Confirmation Letter",
                        Description = "A recent letter from your employer confirming your employment status and salary.",
                        IsMandatory = true,
                        IsActive = true
                    },
                    new()
                    {
                        DocumentName = "M-Pesa Statements",
                        Description = "Upload recent M-Pesa statements showing financial activity.",
                        RequiredPeriodMonths = 3,
                        IsMandatory = false,
                        IsActive = true
                    },
                    new()
                    {
                        DocumentName = "Other Supporting Financial Document",
                        Description = "Upload any additional financial document supporting your application.",
                        IsMandatory = false,
                        IsActive = true
                    }
                };

            var existingDocumentNames = await context
                .LoanProductRequiredDocuments
                .Where(d => d.LoanProductId == product.Id)
                .Select(d => d.DocumentName)
                .ToListAsync();

            Console.WriteLine(
                $"Existing requirements: {existingDocumentNames.Count}");

            var newRequirements = documentRequirements
                .Where(requirement =>
                    !existingDocumentNames.Any(existingName =>
                        string.Equals(
                            existingName,
                            requirement.DocumentName,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var requirement in newRequirements)
            {
                requirement.LoanProductId = product.Id;
            }

            Console.WriteLine(
                $"New requirements to insert: {newRequirements.Count}");

            if (newRequirements.Count > 0)
            {
                await context.LoanProductRequiredDocuments
                    .AddRangeAsync(newRequirements);
            }

            Console.WriteLine(
                $"Pending inserts: {context.ChangeTracker.Entries<LoanProductRequiredDocument>().Count(e => e.State == EntityState.Added)}");

            var changes = await context.SaveChangesAsync();

            Console.WriteLine($"SaveChanges result: {changes}");

            var finalCount = await context.LoanProductRequiredDocuments
                .CountAsync(d => d.LoanProductId == product.Id);

            Console.WriteLine(
                $"Final requirements for this product: {finalCount}");

            Console.WriteLine("KREDVANTA SEEDER FINISHED");
            Console.WriteLine("======================================");
        }
    }
}
