namespace Kredvanta.ViewModels.Staff;

public record WorkCard(int Stage, string Title, string Hint, int Count, string Url, bool ShowAllClear = true);

public class StaffHomeViewModel
{
    public List<WorkCard> Cards { get; set; } = new();
}