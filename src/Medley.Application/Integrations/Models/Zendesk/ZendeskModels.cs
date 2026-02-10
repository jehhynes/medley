using System.Text.Json.Serialization;

namespace Medley.Application.Integrations.Models.Zendesk;

/// <summary>
/// Request model for creating/updating a Zendesk article
/// </summary>
public class ZendeskArticleRequest
{
    [JsonPropertyName("article")]
    public required ZendeskArticleData Article { get; set; }

    [JsonPropertyName("notify_subscribers")]
    public bool NotifySubscribers { get; set; } = false;
}

/// <summary>
/// Article data for Zendesk request
/// </summary>
public class ZendeskArticleData
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("locale")]
    public required string Locale { get; set; }

    [JsonPropertyName("user_segment_id")]
    public long UserSegmentId { get; set; }

    [JsonPropertyName("permission_group_id")]
    public long PermissionGroupId { get; set; }
}

/// <summary>
/// Request model for updating a Zendesk article translation
/// </summary>
public class ZendeskTranslationUpdateRequest
{
    [JsonPropertyName("translation")]
    public required ZendeskTranslationUpdateData Translation { get; set; }
}

/// <summary>
/// Translation data for Zendesk update request
/// </summary>
public class ZendeskTranslationUpdateData
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }
}

/// <summary>
/// Response model from Zendesk API when creating/updating an article
/// </summary>
public class ZendeskArticleResponse
{
    [JsonPropertyName("article")]
    public required ZendeskArticle Article { get; set; }
}

/// <summary>
/// Zendesk article model
/// </summary>
public class ZendeskArticle
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("source_locale")]
    public string? SourceLocale { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("author_id")]
    public long? AuthorId { get; set; }

    [JsonPropertyName("section_id")]
    public long? SectionId { get; set; }

    [JsonPropertyName("user_segment_id")]
    public long? UserSegmentId { get; set; }

    [JsonPropertyName("permission_group_id")]
    public long? PermissionGroupId { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("promoted")]
    public bool Promoted { get; set; }

    [JsonPropertyName("position")]
    public int? Position { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("edited_at")]
    public DateTimeOffset? EditedAt { get; set; }
}

/// <summary>
/// Error response from Zendesk API
/// </summary>
public class ZendeskErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("details")]
    public Dictionary<string, List<string>>? Details { get; set; }
}
