using System.ComponentModel.DataAnnotations;

namespace Kredvanta.ViewModels.Staff;

public class CreateStaffViewModel
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    public List<string> Presets { get; set; } = new();
}

public class EditStaffViewModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public List<string> Presets { get; set; } = new();
}