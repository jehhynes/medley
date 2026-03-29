using Medley.Application.Models;

namespace Medley.Web.Areas.Admin.Models;

public class YouTubeImportViewModel
{
    // URL import
    public string? UrlsText { get; set; }
    public SourceImportResult? ImportResult { get; set; }

    // Search import
    public string? SearchQuery { get; set; }
    public int SearchMaxResults { get; set; } = 10;
    public SourceImportResult? SearchResult { get; set; }
}
