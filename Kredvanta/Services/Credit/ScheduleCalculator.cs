using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Credit;

public record ScheduleLine(int Number, DateOnly DueDate, decimal Principal, decimal Interest, decimal Total, decimal Balance);

public record ScheduleResult(IReadOnlyList<ScheduleLine> Lines, decimal TotalInterest,
                             decimal TotalRepayable, decimal RegularInstalment);

public static class ScheduleCalculator
{
    public static int PeriodsPerYear(RepaymentFrequency f) => f switch
    {
        RepaymentFrequency.Weekly => 52,
        RepaymentFrequency.BiWeekly => 26,
        _ => 12
    };

    public static DateOnly DueDate(DateOnly first, RepaymentFrequency f, int index) => f switch
    {
        RepaymentFrequency.Weekly => first.AddDays(7 * index),
        RepaymentFrequency.BiWeekly => first.AddDays(14 * index),
        _ => first.AddMonths(index)          // clamps month-end days correctly
    };

    public static ScheduleResult Calculate(decimal principal, decimal annualRatePercent, int periods,
        RepaymentFrequency frequency, InterestMethod method, DateOnly firstDueDate)
    {
        if (principal <= 0) throw new ArgumentOutOfRangeException(nameof(principal));
        if (periods <= 0) throw new ArgumentOutOfRangeException(nameof(periods));
        if (annualRatePercent < 0) throw new ArgumentOutOfRangeException(nameof(annualRatePercent));

        var ppy = PeriodsPerYear(frequency);
        var lines = method == InterestMethod.Flat
            ? Flat(principal, annualRatePercent, periods, ppy, frequency, firstDueDate)
            : Reducing(principal, annualRatePercent, periods, ppy, frequency, firstDueDate);

        var totalInterest = lines.Sum(l => l.Interest);
        return new ScheduleResult(lines, totalInterest, principal + totalInterest, lines[0].Total);
    }

    private static List<ScheduleLine> Flat(decimal p, decimal rate, int n, int ppy,
        RepaymentFrequency f, DateOnly first)
    {
        var totalInterest = Math.Round(p * rate / 100m * n / ppy, 2, MidpointRounding.AwayFromZero);
        var basePrincipal = Math.Round(p / n, 2, MidpointRounding.AwayFromZero);
        var baseInterest = Math.Round(totalInterest / n, 2, MidpointRounding.AwayFromZero);

        var lines = new List<ScheduleLine>();
        decimal balance = p, interestLeft = totalInterest;

        for (var i = 1; i <= n; i++)
        {
            // The last instalment absorbs all rounding differences.
            var principal = i == n ? balance : basePrincipal;
            var interest = i == n ? interestLeft : baseInterest;
            balance -= principal;
            interestLeft -= interest;
            lines.Add(new ScheduleLine(i, DueDate(first, f, i - 1), principal, interest,
                principal + interest, balance));
        }
        return lines;
    }

    private static List<ScheduleLine> Reducing(decimal p, decimal rate, int n, int ppy,
        RepaymentFrequency f, DateOnly first)
    {
        var r = rate / 100m / ppy;
        decimal payment;
        if (r == 0) payment = Math.Round(p / n, 2, MidpointRounding.AwayFromZero);
        else
        {
            decimal factor = 1m;
            for (var i = 0; i < n; i++) factor *= 1 + r;          // (1+r)^n in decimal
            payment = Math.Round(p * r * factor / (factor - 1), 2, MidpointRounding.AwayFromZero);
        }

        var lines = new List<ScheduleLine>();
        var balance = p;

        for (var i = 1; i <= n; i++)
        {
            var interest = Math.Round(balance * r, 2, MidpointRounding.AwayFromZero);
            var principal = i == n ? balance : payment - interest;
            if (principal > balance) principal = balance;
            balance -= principal;
            lines.Add(new ScheduleLine(i, DueDate(first, f, i - 1), principal, interest,
                principal + interest, balance));
        }
        return lines;
    }
}