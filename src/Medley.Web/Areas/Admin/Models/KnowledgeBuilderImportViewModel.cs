using Medley.Application.Models;

namespace Medley.Web.Areas.Admin.Models;

/// <summary>
/// View model for Knowledge Builder article import
/// </summary>
public class KnowledgeBuilderImportViewModel
{
    public string[] AllowedFileTypes { get; set; } = new[] { ".json", ".zip" };
    public int MaxFileSizeMB { get; set; } = 50;
    public KnowledgeBuilderImportResult? ImportResult { get; set; }
}

