namespace Kredvanta.Services.Credit;

public record AffordabilityInput(decimal NetIncome, decimal ExistingLoanPayments,
    decimal OtherObligations, decimal LivingExpenses, decimal MonthlyInstalment);

public record AffordabilityPolicy(decimal MaxDebtServiceRatio, decimal MinNetMonthlyIncome,
    decimal MinDisposableAfterInstalment);

public record AffordabilityResult(decimal DisposableBefore, decimal DisposableAfter,
    decimal DebtServiceRatio, bool Passes, IReadOnlyList<string> Failures);

public static class AffordabilityCalculator
{
    public static AffordabilityResult Evaluate(AffordabilityInput i, AffordabilityPolicy p)
    {
        if (i.NetIncome <= 0) throw new ArgumentOutOfRangeException(nameof(i.NetIncome));

        var before = i.NetIncome - i.ExistingLoanPayments - i.OtherObligations - i.LivingExpenses;
        var after = before - i.MonthlyInstalment;
        var ratio = Math.Round(
            (i.ExistingLoanPayments + i.OtherObligations + i.MonthlyInstalment) / i.NetIncome,
            4, MidpointRounding.AwayFromZero);

        var failures = new List<string>();
        if (i.NetIncome < p.MinNetMonthlyIncome)
            failures.Add("Net income is below the policy minimum.");
        if (ratio > p.MaxDebtServiceRatio)
            failures.Add($"Debt service ratio {ratio:P1} exceeds the policy maximum of {p.MaxDebtServiceRatio:P1}.");
        if (after < p.MinDisposableAfterInstalment)
            failures.Add("Disposable income after the instalment is below the policy minimum.");

        return new AffordabilityResult(before, after, ratio, failures.Count == 0, failures);
    }
}