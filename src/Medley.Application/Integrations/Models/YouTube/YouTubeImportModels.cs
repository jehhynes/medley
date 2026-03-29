using System.Text.Json.Serialization;

namespace Medley.Application.Integrations.Models.YouTube;

/// <summary>
/// Transcript segment from the SocialKit YouTube Transcript API
/// </summary>
public class SocialKitTranscriptSegment
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}

/// <summary>
/// Video result from the SocialKit YouTube Search API
/// </summary>
public class SocialKitSearchResult
{
    [JsonPropertyName("videoId")]
    public string? VideoId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("channelName")]
    public string? ChannelName { get; set; }

    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("channelUrl")]
    public string? ChannelUrl { get; set; }

    [JsonPropertyName("publishedTime")]
    public string? PublishedTime { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("views")]
    public long? Views { get; set; }

    [JsonPropertyName("viewsFormatted")]
    public string? ViewsFormatted { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }
}

/// <summary>
/// Combined metadata model stored as MetadataJson for YouTube sources
/// </summary>
public class YouTubeVideoMetadata
{
    public string VideoId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Thumbnail { get; set; }
    public string? ChannelName { get; set; }
    public string? ChannelId { get; set; }
    public string? ChannelUrl { get; set; }
    public string? PublishedTime { get; set; }
    public string? Duration { get; set; }
    public long? Views { get; set; }
    public string? ViewsFormatted { get; set; }
    public string? Url { get; set; }
    public string? Transcript { get; set; }
    public List<SocialKitTranscriptSegment>? TranscriptSegments { get; set; }
    public int? WordCount { get; set; }
    public int? Segments { get; set; }
}
