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

    public List<string> AvailableRoles { get; set; } = new();
    public List<string> UserRoles { get; set; } = new();
}
