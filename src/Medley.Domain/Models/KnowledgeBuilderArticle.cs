namespace Medley.Domain.Models;

/// <summary>
/// Represents an article exported from Knowledge Builder
/// </summary>
public class KnowledgeBuilderArticle
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Type { get; set; } = string.Empty;
    
    public List<KnowledgeBuilderArticle> Children { get; set; } = new();
    public KnowledgeBuilderMetadata? Metadata { get; set; }
}

public class KnowledgeBuilderMetadata
{
    public string? Navigation { get; set; }
    public string? Controller { get; set; }
    public List<string>? Actions { get; set; }
    public string? Category { get; set; }
    public string? Module { get; set; }
    public List<string>? RelatedClasses { get; set; }
}
