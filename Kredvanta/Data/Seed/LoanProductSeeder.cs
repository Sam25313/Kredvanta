using Kredvanta.Models.Products;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Data.Seed;

public static class LoanProductSeeder
{
    public const string SalariedProductCode = "SAL-PL";

    public static async Task SeedAsync(ApplicationDbContext context, bool isDevelopment = false)
    {
        var products = await context.LoanProducts
            .Include(p => p.RequiredDocuments)
            .ToListAsync();
        var product = products.FirstOrDefault(p => p.ProductCode == SalariedProductCode);

        var isNew = product is null;

        if (product is null)
        {
            product = new LoanProduct
            {
                ProductCode = SalariedProductCode,
                ProductName = "Salaried Employee Personal Loan",
                Category = "Personal",
                Description = "A personal loan for employees with verifiable employment and a regular salary.",

                // PLACEHOLDER VALUES FOR DEVELOPMENT ONLY.
                // Replace with approved pricing before any real lending.
                MinimumAmount = 5_000m,
                MaximumAmount = 200_000m,
                AnnualInterestRate = 12m,
                InterestMethod = InterestMethod.ReducingBalance,
                RepaymentFrequency = RepaymentFrequency.Monthly,
                MinimumTermPeriods = 3,
                MaximumTermPeriods = 12,
                ProcessingFeePercentage = 0m,
                ProcessingFeeFixed = 0m,
                GracePeriodDays = 0,

                IsActive = isDevelopment,
                IsAvailableForApplication = isDevelopment
            };
            context.LoanProducts.Add(product);
            products.Add(product);
        }

        // Add missing defaults to every product without overwriting admin edits.
        foreach (var loanProduct in products)
        {
            var addedDocuments = LoanProductRequirementDefaults.AddMissingTo(loanProduct);
            context.LoanProductRequiredDocuments.AddRange(addedDocuments);
        }

        // Design rule: only the salaried product accepts applications at first.
        // Runs ONLY when the product is first created, so it never undoes an admin's choice.
        if (isNew)
        {
            var others = await context.LoanProducts
                .Where(p => p.ProductCode != SalariedProductCode && p.IsAvailableForApplication)
                .ToListAsync();
            foreach (var p in others) p.IsAvailableForApplication = false;
        }

        await context.SaveChangesAsync();
    }
}