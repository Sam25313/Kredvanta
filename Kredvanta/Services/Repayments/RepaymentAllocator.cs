namespace Kredvanta.Services.Repayments;

public record InstalmentOwed(Guid ScheduleId, int Number, decimal Fees, decimal Interest, decimal Principal)
{
    public decimal Total => Fees + Interest + Principal;
}

public record AllocationLine(Guid ScheduleId, decimal Fees, decimal Interest, decimal Principal)
{
    public decimal Total => Fees + Interest + Principal;
}

public static class RepaymentAllocator
{
    /// Oldest instalment first; within an instalment: fees, interest, principal.
    public static (List<AllocationLine> Lines, decimal Unallocated) Allocate(
        IEnumerable<InstalmentOwed> owed, decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        var lines = new List<AllocationLine>();
        var left = amount;

        foreach (var i in owed.Where(x => x.Total > 0).OrderBy(x => x.Number))
        {
            if (left <= 0) break;
            var fees = Math.Min(left, i.Fees); left -= fees;
            var interest = Math.Min(left, i.Interest); left -= interest;
            var principal = Math.Min(left, i.Principal); left -= principal;
            if (fees + interest + principal > 0)
                lines.Add(new AllocationLine(i.ScheduleId, fees, interest, principal));
        }
        return (lines, left);
    }
}