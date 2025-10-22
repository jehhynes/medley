using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Medley.Web.Areas.Integrations.Models;

/// <summary>
/// View model for the main integrations index page
/// </summary>
public class ManageViewModel
{
    public List<Integration> Integrations { get; set; } = new();
    public string? SearchTerm { get; set; }
    public IntegrationType? SelectedType { get; set; }
    public ConnectionStatus? SelectedStatus { get; set; }
    public SelectList IntegrationTypes { get; set; } = new(new List<object>());
    public SelectList ConnectionStatuses { get; set; } = new(new List<object>());
}
