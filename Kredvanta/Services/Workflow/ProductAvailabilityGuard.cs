using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public class ProductAvailabilityGuard : ITransitionGuard
{
    private readonly ApplicationDbContext _db;
    public ProductAvailabilityGuard(ApplicationDbContext db) => _db = db;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to,
                                          CancellationToken ct)
    {
        if (to != LoanApplicationStatus.Submitted) return null;

        var product = await _db.LoanProducts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == app.LoanProductId, ct);

        return product is null || !product.IsActive || !product.IsAvailableForApplication
            ? "This loan product is no longer accepting applications."
            : null;
    }
}