using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Dashboard;

public enum StepState { Done, Current, Pending }

public class JourneyStep
{
    public string Title { get; set; } = "";
    public StepState State { get; set; }
}

public class JourneyNextAction
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ButtonText { get; set; }
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public Guid? RouteId { get; set; }
    public string? Anchor { get; set; }
    public string Css { get; set; } = "info";
}

public class JourneyProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int MinimumTerm { get; set; }
    public int MaximumTerm { get; set; }
    public string Frequency { get; set; } = "";
    public string InterestMethod { get; set; } = "";
}

public class JourneyDocument
{
    public Guid RequirementId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsUploaded { get; set; }
    public Guid? DocumentId { get; set; }
    public int Version { get; set; }
    public DocumentReviewStatus? ReviewStatus { get; set; }
    public string? Message { get; set; }
}

public class JourneyApplication
{
    public Guid Id { get; set; }
    public string Number { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Amount { get; set; }
    public int Term { get; set; }
    public LoanApplicationStatus Status { get; set; }
    public bool CanUpload { get; set; }
    public bool CanSubmit { get; set; }
    public int MissingMandatory { get; set; }
    public List<JourneyDocument> Documents { get; set; } = new();
}

public class BorrowerJourneyViewModel
{
    public bool ProfileComplete { get; set; }
    public int ProfileStep { get; set; } = 1;
    public bool Eligible { get; set; }
    public List<string> EligibilityReasons { get; set; } = new();
    public List<JourneyStep> Steps { get; set; } = new();
    public JourneyNextAction NextAction { get; set; } = new();
    public List<JourneyProduct> Products { get; set; } = new();
    public bool CanStartApplication { get; set; }
    public JourneyApplication? OpenApplication { get; set; }
}