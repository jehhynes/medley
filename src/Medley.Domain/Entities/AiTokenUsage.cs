using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Tracks token usage for AI calls (chat completions and embeddings)
/// </summary>
[Index(nameof(Timestamp), Name = "IX_AiTokenUsage_Timestamp")]
[Index(nameof(ServiceName), Name = "IX_AiTokenUsage_ServiceName")]
[Index(nameof(RelatedEntityType), nameof(RelatedEntityId), Name = "IX_AiTokenUsage_RelatedEntity")]
[Index(nameof(ModelName), Name = "IX_AiTokenUsage_ModelName")]
public class AiTokenUsage : BaseEntity
{
    /// <summary>
    /// When the AI call was made
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Duration of the AI call in milliseconds
    /// </summary>
    public required long DurationMs { get; set; }

    /// <summary>
    /// Input tokens for chat/inference calls (null for embeddings)
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Output tokens for chat/inference calls (null for embeddings)
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Tokens used for embedding generation (null for chat/inference)
    /// </summary>
    public int? EmbeddingTokens { get; set; }

    /// <summary>
    /// The AI model identifier (e.g., "anthropic.claude-3-5-sonnet-20241022-v2:0", "mxbai-embed-large")
    /// </summary>
    [MaxLength(200)]
    public required string ModelName { get; set; }

    /// <summary>
    /// Name of the service making the AI call (e.g., "FragmentExtractionService", "ArticleChatService")
    /// </summary>
    [MaxLength(200)]
    public required string ServiceName { get; set; }

    /// <summary>
    /// Name of the operation/method making the AI call (e.g., "ExtractFragmentsAsync", "ProcessChatMessageAsync")
    /// </summary>
    [MaxLength(200)]
    public string? OperationName { get; set; }

    /// <summary>
    /// Call stack of Service:Operation pairs (newline-separated) showing the chain of calls
    /// Example: "FragmentExtractionService:ExtractFragmentsAsync\nContentChunkingService:ChunkContentAsync"
    /// </summary>
    [MaxLength(2000)]
    public string? CallStack { get; set; }

    /// <summary>
    /// Type of the related entity (e.g., "Fragment", "Source", "Conversation")
    /// </summary>
    [MaxLength(100)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// ID of the related entity
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Whether the AI call succeeded
    /// </summary>
    public required bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the call failed
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
}
