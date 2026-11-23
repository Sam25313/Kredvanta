namespace Kredvanta.Services.Documents;

public static class DocumentRejectionReasons
{
    public const string Illegible = "ILLEGIBLE";
    public const string Expired = "EXPIRED";
    public const string WrongDocument = "WRONG_DOCUMENT";
    public const string Incomplete = "INCOMPLETE";
    public const string DetailsMismatch = "DETAILS_MISMATCH";
    public const string SuspectedAlteration = "SUSPECTED_ALTERATION";   // staff only
    public const string Other = "OTHER";

    public static readonly string[] All =
    {
        Illegible, Expired, WrongDocument, Incomplete,
        DetailsMismatch, SuspectedAlteration, Other
    };

    public static string Label(string code) => code switch
    {
        Illegible => "Illegible",
        Expired => "Expired",
        WrongDocument => "Wrong document",
        Incomplete => "Incomplete",
        DetailsMismatch => "Details do not match profile",
        SuspectedAlteration => "Suspected alteration (internal)",
        _ => "Other"
    };
}