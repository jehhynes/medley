using Hangfire;
using Medley.Application.Configuration;
using Medley.Application.Integrations.Interfaces;
using Medley.Application.Integrations.Models.YouTube;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Models;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Medley.Application.Integrations.Services;

/// <summary>
/// Imports YouTube videos via the SocialKit API (transcript + search metadata)
/// </summary>
public partial class YouTubeImportService : IYouTubeImportService
{
    private readonly IRepository<Source> _sourceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<YouTubeImportService> _logger;
    private readonly HttpClient _httpClient;
    private readonly SocialKitSettings _settings;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string BaseUrl = "https://api.socialkit.dev";
    private const int MinContentLength = 100;

    public YouTubeImportService(
        IRepository<Source> sourceRepository,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        IHttpClientFactory httpClientFactory,
        IOptions<SocialKitSettings> settings,
        ILogger<YouTubeImportService> logger)
    {
        _sourceRepository = sourceRepository;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _httpClient = httpClientFactory.CreateClient();
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SourceImportResult> ImportAsync(IEnumerable<string> urls, Integration integration)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SourceImportResult();

        foreach (var url in urls)
        {
            result.TotalSourcesProcessed++;
            try
            {
                var source = await ImportSingleAsync(url, integration);
                if (source is null)
                {
                    result.SourcesSkipped++;
                }
                else
                {
                    result.SourcesImported++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing YouTube URL {Url}", url);
                result.Errors.Add($"{url}: {ex.Message}");
            }
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.Success = result.Errors.Count == 0;
        return result;
    }

    public async Task<Source?> ImportSingleAsync(string url, Integration integration)
    {
        var videoId = ExtractVideoId(url);
        if (string.IsNullOrEmpty(videoId))
        {
            _logger.LogWarning("Could not extract video ID from URL: {Url}", url);
            throw new ArgumentException($"Could not extract YouTube video ID from URL: {url}");
        }

        // Check if source already exists
        var existing = await _sourceRepository.Query()
            .FirstOrDefaultAsync(s => s.ExternalId == videoId);

        if (existing is not null)
        {
            _logger.LogDebug("Source already exists for YouTube video {VideoId}. Skipping.", videoId);
            return existing;
        }

        // Fetch transcript and search metadata in parallel
        var transcriptTask = FetchTranscriptAsync(url);
        var searchTask = FetchSearchResultAsync(videoId);
        await Task.WhenAll(transcriptTask, searchTask);

        var transcriptData = await transcriptTask;
        var searchResult = await searchTask;

        // Skip if no usable transcript
        if (string.IsNullOrWhiteSpace(transcriptData?.Transcript) ||
            transcriptData.Transcript.Length < MinContentLength)
        {
            _logger.LogDebug("Skipping YouTube video {VideoId}: transcript is missing or too short", videoId);
            return null;
        }

        var metadata = new YouTubeVideoMetadata
        {
            VideoId = videoId,
            Url = url,
            Title = searchResult?.Title,
            Description = searchResult?.Description,
            Thumbnail = searchResult?.Thumbnail,
            ChannelName = searchResult?.ChannelName,
            ChannelId = searchResult?.ChannelId,
            ChannelUrl = searchResult?.ChannelUrl,
            PublishedTime = searchResult?.PublishedTime,
            Duration = searchResult?.Duration,
            Views = searchResult?.Views,
            ViewsFormatted = searchResult?.ViewsFormatted,
            Transcript = transcriptData.Transcript,
            TranscriptSegments = transcriptData.TranscriptSegments,
            WordCount = transcriptData.WordCount,
            Segments = transcriptData.Segments
        };

        var source = new Source
        {
            Type = SourceType.YouTube,
            MetadataType = SourceMetadataType.Youtube_SocialKit,
            ExternalId = videoId,
            Name = metadata.Title ?? videoId,
            Content = transcriptData.Transcript,
            MetadataJson = JsonSerializer.Serialize(metadata, _jsonOptions),
            Date = DateTimeOffset.UtcNow,
            Integration = integration
        };

        await _sourceRepository.Add(source);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created source for YouTube video {VideoId} ({Title})", videoId, source.Name);

        _backgroundJobClient.Schedule<SmartTagProcessorJob>(
            j => j.ExecuteAsync(default!, default, source.Id), TimeSpan.FromMinutes(10));

        return source;
    }

    private async Task<TranscriptApiData?> FetchTranscriptAsync(string url)
    {
        var requestUrl = $"{BaseUrl}/youtube/transcript?url={Uri.EscapeDataString(url)}&access_key={Uri.EscapeDataString(_settings.ApiKey)}";
        var response = await _httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<SocialKitEnvelope<TranscriptApiData>>(json, _jsonOptions);

        if (envelope?.Success != true || envelope.Data is null)
        {
            _logger.LogWarning("SocialKit transcript API returned unsuccessful response for URL: {Url}", url);
            return null;
        }

        return envelope.Data;
    }

    public async Task<SourceImportResult> SearchAndImportAsync(string query, int maxResults, Integration integration)
    {
        _logger.LogInformation("Searching YouTube for '{Query}' (max {Max} results)", query, maxResults);

        var searchResults = await FetchSearchResultsAsync(query, maxResults);

        if (searchResults.Count == 0)
        {
            return new SourceImportResult { Success = true, Duration = TimeSpan.Zero };
        }

        // Deduplicate within the search results by video ID before hitting the DB
        var urls = searchResults
            .Where(r => !string.IsNullOrWhiteSpace(r.Url))
            .DistinctBy(r => r.VideoId)
            .Select(r => r.Url!)
            .ToList();

        return await ImportAsync(urls, integration);
    }

    private async Task<SocialKitSearchResult?> FetchSearchResultAsync(string videoId)
    {
        var results = await FetchSearchResultsAsync(videoId, limit: 1);
        if (results.Count == 0)
        {
            _logger.LogWarning("SocialKit search API returned no results for video ID: {VideoId}", videoId);
            return null;
        }
        return results[0];
    }

    private async Task<List<SocialKitSearchResult>> FetchSearchResultsAsync(string query, int limit)
    {
        var requestUrl = $"{BaseUrl}/youtube/search?query={Uri.EscapeDataString(query)}&limit={limit}&access_key={Uri.EscapeDataString(_settings.ApiKey)}";
        var response = await _httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<SocialKitEnvelope<SearchApiData>>(json, _jsonOptions);

        if (envelope?.Success != true || envelope.Data?.Results is null)
        {
            _logger.LogWarning("SocialKit search API returned unsuccessful response for query: {Query}", query);
            return [];
        }

        return envelope.Data.Results;
    }

    /// <summary>
    /// Extracts the YouTube video ID from common URL formats
    /// </summary>
    internal static string? ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // youtu.be/VIDEO_ID
        var shortMatch = ShortUrlPattern().Match(url);
        if (shortMatch.Success)
            return shortMatch.Groups["id"].Value;

        // youtube.com/watch?v=VIDEO_ID
        var watchMatch = WatchUrlPattern().Match(url);
        if (watchMatch.Success)
            return watchMatch.Groups["id"].Value;

        // youtube.com/shorts/VIDEO_ID
        var shortsMatch = ShortsUrlPattern().Match(url);
        if (shortsMatch.Success)
            return shortsMatch.Groups["id"].Value;

        // youtube.com/embed/VIDEO_ID
        var embedMatch = EmbedUrlPattern().Match(url);
        if (embedMatch.Success)
            return embedMatch.Groups["id"].Value;

        return null;
    }

    [GeneratedRegex(@"youtu\.be/(?<id>[A-Za-z0-9_\-]{11})")]
    private static partial Regex ShortUrlPattern();

    [GeneratedRegex(@"[?&]v=(?<id>[A-Za-z0-9_\-]{11})")]
    private static partial Regex WatchUrlPattern();

    [GeneratedRegex(@"youtube\.com/shorts/(?<id>[A-Za-z0-9_\-]{11})")]
    private static partial Regex ShortsUrlPattern();

    [GeneratedRegex(@"youtube\.com/embed/(?<id>[A-Za-z0-9_\-]{11})")]
    private static partial Regex EmbedUrlPattern();

    // Internal deserialization helpers

    private class SocialKitEnvelope<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private class TranscriptApiData
    {
        [JsonPropertyName("transcript")]
        public string? Transcript { get; set; }

        [JsonPropertyName("transcriptSegments")]
        public List<SocialKitTranscriptSegment>? TranscriptSegments { get; set; }

        [JsonPropertyName("wordCount")]
        public int? WordCount { get; set; }

        [JsonPropertyName("segments")]
        public int? Segments { get; set; }
    }

    private class SearchApiData
    {
        [JsonPropertyName("results")]
        public List<SocialKitSearchResult>? Results { get; set; }
    }
}
