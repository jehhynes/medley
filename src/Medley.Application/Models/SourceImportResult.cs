namespace Medley.Application.Models;

/// <summary>
/// Result of a Source import operation
/// </summary>
public class SourceImportResult
{
    public bool Success { get; set; }
    public int TotalSourcesProcessed { get; set; }
    public int SourcesImported { get; set; }
    public int SourcesSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan Duration { get; set; }

    public static SourceImportResult SuccessResult(int imported, int skipped, TimeSpan duration)
    {
        return new SourceImportResult
        {
            Success = true,
            SourcesImported = imported,
            SourcesSkipped = skipped,
            TotalSourcesProcessed = imported + skipped,
            Duration = duration
        };
    }

    public static SourceImportResult FailureResult(string error)
    {
        return new SourceImportResult
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}

