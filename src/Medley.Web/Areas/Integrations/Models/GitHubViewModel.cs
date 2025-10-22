using System.ComponentModel.DataAnnotations;

namespace Medley.Web.Areas.Integrations.Models;

/// <summary>
/// Unified view model for creating and editing GitHub integrations
/// </summary>
public class GitHubViewModel
{
    public Guid? Id { get; set; }
    public FormFields Form { get; set; } = new() { ApiKey = string.Empty, BaseUrl = "https://api.github.com", DisplayName = string.Empty };

    /// <summary>
    /// Indicates if this is an edit operation (has an ID)
    /// </summary>
    public bool IsEdit => Id.HasValue;

    /// <summary>
    /// Gets the page title based on operation type
    /// </summary>
    public string PageTitle => IsEdit ? "Edit GitHub Integration" : "Add GitHub Integration";

    /// <summary>
    /// Gets the submit button text based on operation type
    /// </summary>
    public string SubmitButtonText => IsEdit ? "Update GitHub Integration" : "Create GitHub Integration";

    /// <summary>
    /// Gets the submit button icon based on operation type
    /// </summary>
    public string SubmitButtonIcon => IsEdit ? "bi bi-check-circle" : "bi bi-plus";

    /// <summary>
    /// Form fields for GitHub integration
    /// </summary>
    public class FormFields
    {
        [Required(ErrorMessage = "Display name is required")]
        [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters")]
        [MinLength(3, ErrorMessage = "Display name must be at least 3 characters")]
        public required string DisplayName { get; set; }

        [Required(ErrorMessage = "Personal access token is required")]
        [MaxLength(500, ErrorMessage = "Personal access token cannot exceed 500 characters")]
        [MinLength(40, ErrorMessage = "GitHub personal access token must be at least 40 characters")]
        [RegularExpression(@"^ghp_[a-zA-Z0-9]{36}$|^gho_[a-zA-Z0-9]{36}$|^ghu_[a-zA-Z0-9]{36}$|^ghs_[a-zA-Z0-9]{36}$|^ghr_[a-zA-Z0-9]{36}$", 
            ErrorMessage = "GitHub personal access token must start with ghp_, gho_, ghu_, ghs_, or ghr_ followed by 36 characters")]
        public required string ApiKey { get; set; } 

        [Url(ErrorMessage = "Base URL must be a valid URL")]
        [RegularExpression(@"^https?://.*", ErrorMessage = "Base URL must start with http:// or https://")]
        public required string BaseUrl { get; set; }
    }
}
