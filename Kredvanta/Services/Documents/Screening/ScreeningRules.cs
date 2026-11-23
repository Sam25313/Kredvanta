using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Products;

namespace Kredvanta.Services.Documents.Screening;

public enum DocumentKind { Unknown, Identity, Payslip, Statement }

public enum CheckOutcome { Match, Mismatch, NotFound }

/// <summary>One automated check. Required=false checks are informational and never cause a flag.</summary>
public record ScreeningCheck(string Name, CheckOutcome Outcome, string Detail, bool Required = true);

public static class ScreeningJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(IEnumerable<ScreeningCheck> checks)
        => JsonSerializer.Serialize(checks, Options);

    public static List<ScreeningCheck> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<List<ScreeningCheck>>(json, Options) ?? new(); }
        catch (JsonException) { return new(); }
    }
}

public static class ScreeningRules
{
    private static readonly HashSet<string> EmployerStopWords = new(StringComparer.OrdinalIgnoreCase)
        { "LIMITED", "LTD", "COMPANY", "PLC", "THE", "AND", "GROUP", "HOLDINGS" };

    /// <summary>
    /// Classifies by the requirement's name (free text today). A DocumentKind column on
    /// LoanProductRequiredDocument would be more robust than keyword matching.
    /// </summary>
    public static DocumentKind Classify(string? documentName)
    {
        var n = (documentName ?? "").ToLowerInvariant();
        if (n.Contains("payslip") || n.Contains("pay slip") || n.Contains("salary")) return DocumentKind.Payslip;
        if (n.Contains("statement")) return DocumentKind.Statement;
        if (n.Contains("national id") || n.Contains("passport") || n.Contains("identity") ||
            Regex.IsMatch(n, @"\bid\b")) return DocumentKind.Identity;
        return DocumentKind.Unknown;
    }

    public static List<ScreeningCheck> Evaluate(DocumentKind kind, Borrower borrower,
        LoanProductRequiredDocument? requirement, string text, DateOnly today)
    {
        var norm = Normalise(text);
        var words = Words(text);
        var checks = new List<ScreeningCheck>();

        switch (kind)
        {
            case DocumentKind.Identity:
                checks.Add(CheckIdNumber(borrower, norm));
                checks.Add(CheckName(borrower, norm, words, required: true));
                checks.Add(CheckDateOfBirth(borrower, DateFinder.FindAll(text)));
                break;

            case DocumentKind.Payslip:
                var amounts = AmountFinder.FindAll(text);
                checks.Add(CheckName(borrower, norm, words, required: true));
                if (!string.IsNullOrWhiteSpace(borrower.EmployerOrBusinessName))
                    checks.Add(CheckEmployer(borrower.EmployerOrBusinessName!, norm, words));
                if (borrower.NetMonthlySalary is > 0)
                    checks.Add(CheckAmount("Net salary", borrower.NetMonthlySalary.Value, amounts, required: true));
                if (borrower.GrossMonthlySalary is > 0)
                    checks.Add(CheckAmount("Gross salary", borrower.GrossMonthlySalary.Value, amounts, required: false));
                break;

            case DocumentKind.Statement:
                checks.Add(CheckName(borrower, norm, words, required: true));
                checks.Add(CheckPhone(borrower, text));
                checks.AddRange(CheckStatementPeriod(DateFinder.FindAll(text),
                    requirement?.RequiredPeriodMonths, today));
                break;

            default:
                // No content rules for this type: name is informational only.
                checks.Add(CheckName(borrower, norm, words, required: false));
                break;
        }

        return checks;
    }

    // ------------------------------------------------------------------ checks

    private static ScreeningCheck CheckIdNumber(Borrower b, string norm)
    {
        var id = Normalise(b.NationalIdNumber);
        if (id.Length < 5)
            return new("ID number", CheckOutcome.NotFound, "The borrower profile has no usable ID number to compare.");

        return norm.Contains(id)
            ? new("ID number", CheckOutcome.Match, "The profile ID number appears on the document.")
            : new("ID number", CheckOutcome.NotFound, "The profile ID number was not found on the document.");
    }

    private static ScreeningCheck CheckName(Borrower b, string norm, HashSet<string> words, bool required)
    {
        var parts = SplitName(b.FirstName).Concat(SplitName(b.LastName)).ToList();
        if (parts.Count == 0)
            return new("Name", CheckOutcome.NotFound, "The borrower profile has no name to compare.", required);

        var found = parts.Count(p => TokenFound(p, norm, words));
        return found == parts.Count
            ? new("Name", CheckOutcome.Match, $"All {parts.Count} name parts from the profile were found.", required)
            : new("Name", CheckOutcome.NotFound, $"Found {found} of {parts.Count} name parts from the profile.", required);
    }

    private static ScreeningCheck CheckDateOfBirth(Borrower b, List<DateOnly> dates)
        => dates.Contains(b.DateOfBirth)
            ? new("Date of birth", CheckOutcome.Match, "The profile date of birth appears on the document.", false)
            : new("Date of birth", CheckOutcome.NotFound, "The profile date of birth was not found (OCR may have misread it).", false);

    private static ScreeningCheck CheckEmployer(string employer, string norm, HashSet<string> words)
    {
        var tokens = SplitName(employer)
            .Where(t => t.Length >= 4 && !EmployerStopWords.Contains(t))
            .ToList();
        if (tokens.Count == 0)
            return new("Employer", CheckOutcome.NotFound, "The employer name on the profile is too short to compare.", false);

        var found = tokens.Count(t => TokenFound(t, norm, words));
        var needed = (int)Math.Ceiling(tokens.Count / 2.0);
        return found >= needed
            ? new("Employer", CheckOutcome.Match, $"Employer name matched ({found} of {tokens.Count} key words).")
            : new("Employer", CheckOutcome.NotFound, $"Employer name not matched ({found} of {tokens.Count} key words).");
    }

    private static ScreeningCheck CheckAmount(string label, decimal declared, List<decimal> amounts, bool required)
        => amounts.Any(a => Math.Abs(a - declared) <= 1m)
            ? new(label, CheckOutcome.Match, $"The declared {label.ToLowerInvariant()} appears on the document.", required)
            : new(label, CheckOutcome.Mismatch, $"The declared {label.ToLowerInvariant()} was not found among the amounts on the document.", required);

    private static ScreeningCheck CheckPhone(Borrower b, string text)
    {
        var digits = new string((b.PhoneNumber ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length < 9)
            return new("Phone number", CheckOutcome.NotFound, "The profile has no usable phone number.", false);

        var last9 = digits[^9..];
        var textDigits = new string(text.Where(char.IsDigit).ToArray());
        return textDigits.Contains(last9)
            ? new("Phone number", CheckOutcome.Match, "The profile phone number appears on the statement.", false)
            : new("Phone number", CheckOutcome.NotFound, "The profile phone number was not found (it may be masked).", false);
    }

    private static IEnumerable<ScreeningCheck> CheckStatementPeriod(List<DateOnly> dates, int? requiredMonths, DateOnly today)
    {
        if (dates.Count < 2)
        {
            yield return new("Statement period", CheckOutcome.NotFound, "Dates could not be read from the statement.");
            yield break;
        }

        var first = dates.Min();
        var last = dates.Max();

        if (requiredMonths is > 0)
        {
            var coveredDays = last.DayNumber - first.DayNumber;
            var neededDays = requiredMonths.Value * 30 - 10;
            yield return coveredDays >= neededDays
                ? new("Statement period", CheckOutcome.Match, $"Covers about {coveredDays} days (needs about {requiredMonths * 30}).")
                : new("Statement period", CheckOutcome.Mismatch, $"Covers about {coveredDays} days but {requiredMonths} months are required.");
        }

        var ageDays = today.DayNumber - last.DayNumber;
        yield return ageDays <= 62
            ? new("Statement recency", CheckOutcome.Match, "The latest date on the statement is recent.")
            : new("Statement recency", CheckOutcome.Mismatch, $"The latest date on the statement is about {ageDays} days old.");
    }

    // ----------------------------------------------------------------- helpers

    private static string Normalise(string? s)
        => new((s ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static HashSet<string> Words(string text)
        => Regex.Matches(text, @"\p{L}{2,}").Select(m => m.Value.ToUpperInvariant()).ToHashSet();

    private static List<string> SplitName(string? name)
        => Regex.Matches(name ?? "", @"\p{L}+").Select(m => m.Value).ToList();

    private static bool TokenFound(string token, string norm, HashSet<string> words)
    {
        var t = token.ToUpperInvariant();
        if (t.Length <= 3) return words.Contains(t);              // short names: whole-word only
        if (norm.Contains(Normalise(t))) return true;             // tolerant of odd spacing
        if (t.Length >= 6)                                         // allow one OCR typo
            return words.Any(w => Math.Abs(w.Length - t.Length) <= 1 && Levenshtein(w, t) <= 1);
        return false;
    }

    private static int Levenshtein(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }
}

internal static class AmountFinder
{
    public static List<decimal> FindAll(string text)
    {
        var list = new List<decimal>();
        foreach (Match m in Regex.Matches(text, @"\d[\d,]*(?:\.\d+)?"))
        {
            if (decimal.TryParse(m.Value.Replace(",", ""), NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var v))
                list.Add(v);
        }
        return list;
    }
}

internal static class DateFinder
{
    private static readonly Regex Numeric = new(@"(?<!\d)(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{4})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Iso = new(@"(?<!\d)(\d{4})[/\-.](\d{1,2})[/\-.](\d{1,2})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Named = new(
        @"(?<!\d)(\d{1,2})[\s\-]*(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)[A-Z]*[\s,\-]*(\d{4})(?!\d)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] Months =
        { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

    public static List<DateOnly> FindAll(string text)
    {
        var result = new List<DateOnly>();

        foreach (Match m in Numeric.Matches(text))
        {
            int a = int.Parse(m.Groups[1].Value), b = int.Parse(m.Groups[2].Value), y = int.Parse(m.Groups[3].Value);
            // Kenya writes day first; fall back to month first only if day-first is impossible.
            if (TryMake(y, b, a, out var d) || TryMake(y, a, b, out d)) result.Add(d);
        }

        foreach (Match m in Iso.Matches(text))
            if (TryMake(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value),
                    int.Parse(m.Groups[3].Value), out var d)) result.Add(d);

        foreach (Match m in Named.Matches(text))
        {
            var month = Array.IndexOf(Months, m.Groups[2].Value.ToUpperInvariant()) + 1;
            if (TryMake(int.Parse(m.Groups[3].Value), month, int.Parse(m.Groups[1].Value), out var d)) result.Add(d);
        }

        return result;
    }

    private static bool TryMake(int year, int month, int day, out DateOnly date)
    {
        date = default;
        if (year < 1900 || year > 2100 || month < 1 || month > 12) return false;
        if (day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        date = new DateOnly(year, month, day);
        return true;
    }
}