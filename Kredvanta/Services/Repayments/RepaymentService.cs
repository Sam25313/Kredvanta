using Kredvanta.Data;
using Kredvanta.Models.Loans;
using Kredvanta.Models.Repayments;
using Kredvanta.Services.Audit;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Repayments;

public class RecordPaymentRequest
{
    public Guid LoanId { get; set; }
    public decimal Amount { get; set; }
    public RepaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? PayerPhone { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string IdempotencyKey { get; set; } = "";
}

public record RepaymentResult(bool Succeeded, string? Error = null, string? Message = null)
{
    public static RepaymentResult Fail(string e) => new(false, e);
    public static RepaymentResult Ok(string m) => new(true, null, m);
}

public interface IRepaymentService
{
    Task<RepaymentResult> RecordAsync(RecordPaymentRequest r, string userId, CancellationToken ct = default);
    Task<RepaymentResult> ReverseAsync(Guid repaymentId, string reason, string userId, CancellationToken ct = default);
    Task<int> RefreshAllAsync(CancellationToken ct = default);
}

public class RepaymentService : IRepaymentService
{
    private static readonly LoanStatus[] Collectable = { LoanStatus.Active, LoanStatus.Overdue };

    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;

    public RepaymentService(ApplicationDbContext db, IAuditService audit, IConfiguration config)
    { _db = db; _audit = audit; _config = config; }

    // ---------- Record ----------

    public async Task<RepaymentResult> RecordAsync(RecordPaymentRequest r, string userId, CancellationToken ct = default)
    {
        if (r.Amount <= 0) return RepaymentResult.Fail("The amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(r.IdempotencyKey)) return RepaymentResult.Fail("Reload the page and try again.");

        var externalId = string.IsNullOrWhiteSpace(r.Reference) ? "IDEM-" + r.IdempotencyKey : r.Reference.Trim();
        if (await _db.Repayments.AnyAsync(p => p.ExternalTransactionId == externalId, ct))
            return RepaymentResult.Ok("This payment was already recorded.");

        if (r.Method != RepaymentMethod.Cash && string.IsNullOrWhiteSpace(r.Reference))
            return RepaymentResult.Fail("Enter the provider's transaction reference.");

        var receivedAt = r.ReceivedAt ?? DateTime.UtcNow;
        if (receivedAt > DateTime.UtcNow.AddMinutes(5)) return RepaymentResult.Fail("The received time cannot be in the future.");

        var loan = await _db.Loans.Include(l => l.Borrower).Include(l => l.RepaymentSchedules)
            .FirstOrDefaultAsync(l => l.Id == r.LoanId, ct);
        if (loan is null) return RepaymentResult.Fail("Loan not found.");
        if (!Collectable.Contains(loan.Status)) return RepaymentResult.Fail($"This loan is {loan.Status} and cannot take payments.");
        if (loan.Borrower.ApplicationUserId == userId) return RepaymentResult.Fail("You cannot record payments on your own loan.");

        var owed = await OwedAsync(loan, ct);
        var outstanding = owed.Sum(o => o.Total);
        if (r.Amount > outstanding)
            return RepaymentResult.Fail($"The payment is more than the amount owed ({loan.CurrencyCode} {outstanding:N2}).");

        var (lines, unallocated) = RepaymentAllocator.Allocate(owed, r.Amount);
        if (unallocated != 0) return RepaymentResult.Fail("The payment could not be fully allocated.");

        var repayment = new Repayment
        {
            LoanId = loan.Id,
            ReceiptNumber = $"RC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Amount = r.Amount,
            Method = r.Method,
            Status = RepaymentStatus.Confirmed,
            ReceivedAt = receivedAt,
            ReceivedByUserId = userId,
            TransactionReference = string.IsNullOrWhiteSpace(r.Reference) ? null : r.Reference.Trim(),
            ExternalTransactionId = externalId,
            PayerPhoneNumber = r.PayerPhone?.Trim()
        };
        _db.Repayments.Add(repayment);

        foreach (var l in lines)
        {
            var s = loan.RepaymentSchedules.First(x => x.Id == l.ScheduleId);
            s.AmountPaid += l.Total;
            _db.RepaymentAllocations.Add(new RepaymentAllocation
            {
                RepaymentId = repayment.Id,
                RepaymentScheduleId = s.Id,
                FeesAmount = l.Fees,
                InterestAmount = l.Interest,
                PrincipalAmount = l.Principal,
                AllocatedAmount = l.Total
            });
        }

        loan.OutstandingBalance = Math.Max(0, loan.OutstandingBalance - r.Amount);
        Refresh(loan, DateOnly.FromDateTime(DateTime.UtcNow));

        _audit.Record("RepaymentRecorded", nameof(Loan), loan.Id.ToString(), null, new
        {
            repayment.ReceiptNumber,
            r.Amount,
            r.Method,
            Reference = externalId,
            Balance = loan.OutstandingBalance,
            LoanStatus = loan.Status.ToString()
        });

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return RepaymentResult.Fail("This loan was changed by someone else. Reload and check before trying again."); }
        catch (DbUpdateException) { return RepaymentResult.Fail("This payment conflicts with one already recorded. Check the list first."); }

        return RepaymentResult.Ok($"Payment recorded. Receipt {repayment.ReceiptNumber}.");
    }

    // ---------- Reverse ----------

    public async Task<RepaymentResult> ReverseAsync(Guid repaymentId, string reason, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return RepaymentResult.Fail("A reason is required to reverse a payment.");

        var p = await _db.Repayments.Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == repaymentId, ct);
        if (p is null) return RepaymentResult.Fail("Payment not found.");
        if (p.Status != RepaymentStatus.Confirmed) return RepaymentResult.Fail("Only confirmed payments can be reversed.");

        if (p.ReceivedByUserId == userId && !_config.GetValue<bool>("Repayments:AllowSelfReversal"))
            return RepaymentResult.Fail("A different finance officer must reverse a payment you recorded.");

        var loan = await _db.Loans.Include(l => l.Borrower).Include(l => l.RepaymentSchedules)
            .FirstAsync(l => l.Id == p.LoanId, ct);
        if (loan.Borrower.ApplicationUserId == userId) return RepaymentResult.Fail("You cannot change payments on your own loan.");
        if (loan.Status is LoanStatus.WrittenOff or LoanStatus.Cancelled)
            return RepaymentResult.Fail($"A {loan.Status} loan cannot be changed here.");

        foreach (var a in p.Allocations)
        {
            var s = loan.RepaymentSchedules.First(x => x.Id == a.RepaymentScheduleId);
            s.AmountPaid = Math.Max(0, s.AmountPaid - a.AllocatedAmount);
        }

        p.Status = RepaymentStatus.Reversed;
        p.ReversalReason = reason.Trim();
        p.ReversedAt = DateTime.UtcNow;
        p.ReversedByUserId = userId;

        loan.OutstandingBalance = Math.Min(loan.TotalRepayable, loan.OutstandingBalance + p.Amount);
        if (loan.Status == LoanStatus.Completed) loan.Status = LoanStatus.Active;
        Refresh(loan, DateOnly.FromDateTime(DateTime.UtcNow));

        _audit.Record("RepaymentReversed", nameof(Repayment), p.Id.ToString(), reason.Trim(),
            new { p.ReceiptNumber, p.Amount, Balance = loan.OutstandingBalance });

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return RepaymentResult.Fail("This loan was changed by someone else. Reload and try again."); }

        return RepaymentResult.Ok($"Payment {p.ReceiptNumber} reversed.");
    }

    // ---------- Overdue refresh (used by the worker) ----------

    public async Task<int> RefreshAllAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var loans = await _db.Loans.Include(l => l.RepaymentSchedules)
            .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue).ToListAsync(ct);

        var changed = 0;
        foreach (var l in loans)
        {
            var before = l.Status;
            var schedulesBefore = l.RepaymentSchedules.Select(s => s.Status).ToList();
            Refresh(l, today);
            if (l.Status != before || !schedulesBefore.SequenceEqual(l.RepaymentSchedules.Select(s => s.Status)))
            {
                changed++;
                if (l.Status != before)
                    _audit.Record("LoanStatusChanged", nameof(Loan), l.Id.ToString(), null,
                        new { From = before.ToString(), To = l.Status.ToString() });
            }
        }
        if (changed > 0) await _db.SaveChangesAsync(ct);
        return changed;
    }

    // ---------- helpers ----------

    /// Recomputes schedule statuses and the loan status. Safe to run repeatedly.
    public static void Refresh(Loan loan, DateOnly today)
    {
        foreach (var s in loan.RepaymentSchedules)
            s.Status = s.AmountPaid >= s.TotalDue ? ScheduleStatus.Paid
                     : s.DueDate < today ? ScheduleStatus.Overdue
                     : s.AmountPaid > 0 ? ScheduleStatus.PartiallyPaid
                     : ScheduleStatus.Pending;

        if (loan.Status is LoanStatus.WrittenOff or LoanStatus.Cancelled or LoanStatus.Restructured
            or LoanStatus.PendingDisbursement) return;

        loan.Status =
            loan.RepaymentSchedules.Count > 0 && loan.RepaymentSchedules.All(s => s.Status == ScheduleStatus.Paid)
                ? LoanStatus.Completed
            : loan.RepaymentSchedules.Any(s => s.Status == ScheduleStatus.Overdue)
                ? LoanStatus.Overdue
                : LoanStatus.Active;
    }

    private async Task<List<InstalmentOwed>> OwedAsync(Loan loan, CancellationToken ct)
    {
        var allocated = (await _db.RepaymentAllocations
                .Where(a => a.RepaymentSchedule.LoanId == loan.Id && a.Repayment.Status == RepaymentStatus.Confirmed)
                .Select(a => new { a.RepaymentScheduleId, a.FeesAmount, a.InterestAmount, a.PrincipalAmount })
                .ToListAsync(ct))
            .GroupBy(a => a.RepaymentScheduleId)
            .ToDictionary(g => g.Key, g => (Fees: g.Sum(x => x.FeesAmount),
                Interest: g.Sum(x => x.InterestAmount), Principal: g.Sum(x => x.PrincipalAmount)));

        return loan.RepaymentSchedules.OrderBy(s => s.InstallmentNumber).Select(s =>
        {
            allocated.TryGetValue(s.Id, out var a);
            return new InstalmentOwed(s.Id, s.InstallmentNumber,
                Math.Max(0, s.FeesDue - a.Fees), Math.Max(0, s.InterestDue - a.Interest),
                Math.Max(0, s.PrincipalDue - a.Principal));
        }).ToList();
    }
}