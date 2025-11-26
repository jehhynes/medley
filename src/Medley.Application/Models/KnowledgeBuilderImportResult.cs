namespace Medley.Application.Models;

/// <summary>
/// Result of a Knowledge Builder article import operation
/// </summary>
public class KnowledgeBuilderImportResult
{
    public bool Success { get; set; }
    public int TotalArticlesProcessed { get; set; }
    public int ArticlesImported { get; set; }
    public int ArticlesSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan Duration { get; set; }

    public static KnowledgeBuilderImportResult SuccessResult(int imported, int skipped, TimeSpan duration)
    {
        return new KnowledgeBuilderImportResult
        {
            Success = true,
            ArticlesImported = imported,
            ArticlesSkipped = skipped,
            TotalArticlesProcessed = imported + skipped,
            Duration = duration
        };
    }

    public static KnowledgeBuilderImportResult FailureResult(string error)
    {
        return new KnowledgeBuilderImportResult
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}

