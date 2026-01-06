using System.ComponentModel.DataAnnotations;

namespace Medley.Web.Models;

/// <summary>
/// View model for editing user roles
/// </summary>
public class UserEditViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; }

    [Display(Name = "Locked Out")]
    public bool IsLockedOut { get; set; }

    [Display(Name = "Initials")]
    [StringLength(3, ErrorMessage = "Initials cannot exceed 3 characters")]
    public string? Initials { get; set; }

    [Display(Name = "Avatar Color")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color (e.g., #FF5733)")]
    public string? Color { get; set; }

    public List<string> AvailableRoles { get; set; } = new();
    public List<string> UserRoles { get; set; } = new();
}
