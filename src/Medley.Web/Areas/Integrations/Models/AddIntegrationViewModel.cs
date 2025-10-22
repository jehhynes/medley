using Medley.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Medley.Web.Areas.Integrations.Models;

/// <summary>
/// View model for adding a new integration (type selection)
/// </summary>
public class AddIntegrationViewModel
{
    [Required(ErrorMessage = "Integration type is required")]
    public IntegrationType SelectedType { get; set; }

    public SelectList IntegrationTypes { get; set; } = new(new List<object>());
}
