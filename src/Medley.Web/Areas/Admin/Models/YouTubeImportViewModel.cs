using Medley.Application.Models;

namespace Medley.Web.Areas.Admin.Models;

public class YouTubeImportViewModel
{
    public string? UrlsText { get; set; }
    public SourceImportResult? ImportResult { get; set; }
}
