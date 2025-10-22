using System.ComponentModel.DataAnnotations;

namespace Medley.Web.Areas.Integrations.Models;

/// <summary>
/// Unified view model for creating and editing Fellow integrations
/// </summary>
public class FellowViewModel
{
    public Guid? Id { get; set; }
    public FormFields Form { get; set; } = new() { DisplayName = string.Empty, ApiKey = string.Empty, BaseUrl = "https://mycompany.fellow.app" };

    /// <summary>
    /// Indicates if this is an edit operation (has an ID)
    /// </summary>
    public bool IsEdit => Id.HasValue;

    /// <summary>
    /// Gets the page title based on operation type
    /// </summary>
    public string PageTitle => IsEdit ? "Edit Fellow Integration" : "Add Fellow Integration";

    /// <summary>
    /// Gets the submit button text based on operation type
    /// </summary>
    public string SubmitButtonText => IsEdit ? "Update Fellow Integration" : "Create Fellow Integration";

    /// <summary>
    /// Gets the submit button icon based on operation type
    /// </summary>
    public string SubmitButtonIcon => IsEdit ? "bi bi-check-circle" : "bi bi-plus";

    /// <summary>
    /// Form fields for Fellow integration
    /// </summary>
    public class FormFields
    {
        [Required(ErrorMessage = "Display name is required")]
        [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters")]
        [MinLength(3, ErrorMessage = "Display name must be at least 3 characters")]
        public required string DisplayName { get; set; }

        [Required(ErrorMessage = "API key is required")]
        [MaxLength(500, ErrorMessage = "API key cannot exceed 500 characters")]
        [MinLength(20, ErrorMessage = "Fellow API key must be at least 20 characters")]
        public required string ApiKey { get; set; }

        [Url(ErrorMessage = "Base URL must be a valid URL")]
        [RegularExpression(@"^https?://.*\.fellow\.app.*", ErrorMessage = "Base URL must be a Fellow.ai workspace URL starting with http:// or https://")]
        public required string BaseUrl { get; set; }
    }
}
