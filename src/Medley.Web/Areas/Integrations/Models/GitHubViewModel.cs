using System.ComponentModel.DataAnnotations;

namespace Medley.Web.Areas.Integrations.Models;

/// <summary>
/// Unified view model for creating and editing GitHub integrations
/// </summary>
public class GitHubViewModel
{
    public Guid? Id { get; set; }
    public FormFields Form { get; set; } = new();

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
        [StringLength(200, ErrorMessage = "Display name cannot exceed 200 characters")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "API key is required")]
        [StringLength(500, ErrorMessage = "API key cannot exceed 500 characters")]
        public string ApiKey { get; set; } = string.Empty;

        [Url(ErrorMessage = "Base URL must be a valid URL")]
        public string BaseUrl { get; set; } = "https://api.github.com";
    }
}
