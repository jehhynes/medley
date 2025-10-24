using System.ComponentModel.DataAnnotations;
using UoN.ExpressiveAnnotations.Net8.Attributes;

namespace Medley.Web.Areas.Integrations.Models;

/// <summary>
/// Unified view model for creating and editing GitHub integrations
/// </summary>
public class GitHubViewModel
{
    public Guid? Id { get; set; }

    /// <summary>
    /// Indicates if this is an edit operation (has an ID)
    /// </summary>
    public bool IsEdit => Id.HasValue;

    [Required(ErrorMessage = "Display name is required")]
    [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters")]
    [MinLength(3, ErrorMessage = "Display name must be at least 3 characters")]
    public required string DisplayName { get; set; }

    [RequiredIf("Id == null")]
    [MaxLength(500, ErrorMessage = "Personal access token cannot exceed 500 characters")]
    [MinLength(40, ErrorMessage = "GitHub personal access token must be at least 40 characters")]
    [RegularExpression(@"^ghp_[a-zA-Z0-9]{36}$|^gho_[a-zA-Z0-9]{36}$|^ghu_[a-zA-Z0-9]{36}$|^ghs_[a-zA-Z0-9]{36}$|^ghr_[a-zA-Z0-9]{36}$", 
        ErrorMessage = "GitHub personal access token must start with ghp_, gho_, ghu_, ghs_, or ghr_ followed by 36 characters")]
    public string? ApiKey { get; set; }

    [Url(ErrorMessage = "Base URL must be a valid URL")]
    [RegularExpression(@"^https?://.*", ErrorMessage = "Base URL must start with http:// or https://")]
    public string BaseUrl { get; set; } = "https://api.github.com";
}
