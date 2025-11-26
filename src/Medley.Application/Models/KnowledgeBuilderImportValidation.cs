namespace Medley.Application.Models;

/// <summary>
/// Validation result for Knowledge Builder import data
/// </summary>
public class KnowledgeBuilderImportValidation
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int ArticleCount { get; set; }

    public static KnowledgeBuilderImportValidation Valid(int articleCount)
    {
        return new KnowledgeBuilderImportValidation
        {
            IsValid = true,
            ArticleCount = articleCount
        };
    }

    public static KnowledgeBuilderImportValidation Invalid(params string[] errors)
    {
        return new KnowledgeBuilderImportValidation
        {
            IsValid = false,
            Errors = new List<string>(errors)
        };
    }
}

