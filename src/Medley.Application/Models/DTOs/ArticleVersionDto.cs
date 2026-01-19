using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for ArticleVersion entity
/// </summary>
public class ArticleVersionDto
{
    /// <summary>
    /// Unique identifier for the version
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Article ID this version belongs to
    /// </summary>
    public required Guid ArticleId { get; set; }

    /// <summary>
    /// Conversation ID that created this version (optional)
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    public required int VersionNumber { get; set; }

    /// <summary>
    /// User who created this version
    /// </summary>
    public UserRef? CreatedBy { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this version was last modified (for draft versions)
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// Type of version (User or AI draft)
    /// </summary>
    public required VersionType VersionType { get; set; }

    /// <summary>
    /// Parent version ID (for AI versions)
    /// </summary>
    public Guid? ParentVersionId { get; set; }

    /// <summary>
    /// Commit-style message describing changes
    /// </summary>
    public string? ChangeMessage { get; set; }

    /// <summary>
    /// Review action taken on this AI version
    /// </summary>
    public required ReviewAction ReviewAction { get; set; }

    /// <summary>
    /// When this AI version was reviewed
    /// </summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>
    /// User who reviewed this version
    /// </summary>
    public UserRef? ReviewedBy { get; set; }

    /// <summary>
    /// Computed status of this version (CurrentVersion, OldVersion, PendingAiVersion, etc.)
    /// </summary>
    public VersionStatus? Status { get; set; }
}

/// <summary>
/// Comparison between two versions showing before/after content
/// </summary>
public class ArticleVersionComparisonDto
{
    /// <summary>
    /// Version ID being compared
    /// </summary>
    public required Guid VersionId { get; set; }

    /// <summary>
    /// Version number (formatted as string, e.g., "1" or "1.2" for AI versions)
    /// </summary>
    public required string VersionNumber { get; set; }

    /// <summary>
    /// Content before this version
    /// </summary>
    public required string BeforeContent { get; set; }

    /// <summary>
    /// Content after this version
    /// </summary>
    public required string AfterContent { get; set; }
}

/// <summary>
/// Response from version capture operation
/// </summary>
public class VersionCaptureResponse
{
    /// <summary>
    /// Indicates if a new version was created
    /// </summary>
    public required bool IsNewVersion { get; set; }

    /// <summary>
    /// Version ID (new or existing)
    /// </summary>
    public required Guid VersionId { get; set; }

    /// <summary>
    /// Version number (formatted as string, e.g., "1" or "1.2" for AI versions)
    /// </summary>
    public required string VersionNumber { get; set; }
}
