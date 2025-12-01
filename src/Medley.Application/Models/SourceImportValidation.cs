namespace Medley.Application.Models;

/// <summary>
/// Validation result for Source import data
/// </summary>
public class SourceImportValidation
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int SourceCount { get; set; }

    public static SourceImportValidation Valid(int sourceCount)
    {
        return new SourceImportValidation
        {
            IsValid = true,
            SourceCount = sourceCount
        };
    }

    public static SourceImportValidation Invalid(params string[] errors)
    {
        return new SourceImportValidation
        {
            IsValid = false,
            Errors = new List<string>(errors)
        };
    }
}

