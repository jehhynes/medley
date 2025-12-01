using Medley.Application.Models;
using Medley.Domain.Enums;

namespace Medley.Web.Areas.Admin.Models;

/// <summary>
/// View model for Source file import
/// </summary>
public class SourceImportViewModel
{
    public string[] AllowedFileTypes { get; set; } = new[] { ".json", ".zip" };
    public int MaxFileSizeMB { get; set; } = 50;
    public SourceMetadataType SelectedMetadataType { get; set; } = SourceMetadataType.Collector_GoogleDrive;
    public SourceImportResult? ImportResult { get; set; }
    
    /// <summary>
    /// Available metadata types for selection (excludes Unknown)
    /// </summary>
    public IEnumerable<SourceMetadataType> AvailableMetadataTypes { get; set; } = new[]
    {
        SourceMetadataType.Collector_GoogleDrive,
        SourceMetadataType.Collector_Fellow
    };
}

