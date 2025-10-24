using System.ComponentModel.DataAnnotations;
using UoN.ExpressiveAnnotations.Net8.Attributes;

namespace Medley.Web.Areas.Integrations.Models;

/// <summary>
/// Unified view model for creating and editing Fellow integrations
/// </summary>
public class FellowViewModel
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
    [MaxLength(500, ErrorMessage = "API key cannot exceed 500 characters")]
    [MinLength(20, ErrorMessage = "Fellow API key must be at least 20 characters")]
    public string? ApiKey { get; set; }

    [Url(ErrorMessage = "Base URL must be a valid URL")]
    [RegularExpression(@"^https?://.*\.fellow\.app.*", ErrorMessage = "Base URL must be a Fellow.ai workspace URL starting with http:// or https://")]
    public required string BaseUrl { get; set; } = "https://mycompany.fellow.app";
}
