using Medley.Application.Integrations.Models.Fellow;
using Medley.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Medley.Application.Integrations.Services;

/// <summary>
/// Service for Fellow.ai integration connection management
/// </summary>
public class FellowIntegrationService : BaseIntegrationConnectionService
{
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastApiCall = DateTime.MinValue;
    private const int MinimumDelayMs = 500;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IHttpClientFactory _httpClientFactory;

    public FellowIntegrationService(
        ILogger<FellowIntegrationService> logger,
        IHttpClientFactory httpClientFactory) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override IntegrationType IntegrationType => IntegrationType.Fellow;

    /// <summary>
    /// Creates and configures an authenticated HttpClient for Fellow.ai API
    /// </summary>
    private HttpClient CreateAuthenticatedClient(string apiKey, string baseUrl)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
        httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient;
    }

    protected override async Task<bool> TestConnectionInternalAsync(string apiKey, string baseUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl))
            {
                Logger.LogWarning("Fellow integration test failed: missing required configuration");
                return false;
            }

            // Test connection using the /me endpoint
            var meResponse = await GetMeAsync(apiKey, baseUrl);

            if (meResponse?.User == null || meResponse.Workspace == null)
            {
                Logger.LogWarning("Fellow integration test failed: invalid response from /me endpoint");
                return false;
            }

            Logger.LogInformation("Fellow integration connection test successful for user {Email} in workspace {Workspace}",
                meResponse.User.Email, meResponse.Workspace.Name);
            return true;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Fellow integration connection test failed: HTTP error");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fellow integration connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Gets current user and workspace information from Fellow.ai API
    /// </summary>
    /// <param name="apiKey">Fellow.ai API key</param>
    /// <param name="baseUrl">Base URL for Fellow.ai API</param>
    /// <returns>User and workspace information</returns>
    public async Task<FellowMeResponse?> GetMeAsync(string apiKey, string baseUrl)
    {
        await EnforceRateLimitAsync();

        using var httpClient = CreateAuthenticatedClient(apiKey, baseUrl);
        var requestUri = "/api/v1/me";

        Logger.LogDebug("Fetching Fellow user info: {RequestUri}", requestUri);

        var response = await httpClient.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var meResponse = JsonSerializer.Deserialize<FellowMeResponse>(content, _jsonOptions);

        Logger.LogInformation("Successfully fetched user info from Fellow.ai");

        return meResponse;
    }

    /// <summary>
    /// Gets a specific recording by ID from Fellow.ai API
    /// </summary>
    /// <param name="apiKey">Fellow.ai API key</param>
    /// <param name="baseUrl">Base URL for Fellow.ai API</param>
    /// <param name="recordingId">The recording ID to retrieve</param>
    /// <returns>Recording details</returns>
    public async Task<FellowRecordingResponse?> GetRecordingAsync(
        string apiKey,
        string baseUrl,
        string recordingId)
    {
        await EnforceRateLimitAsync();

        using var httpClient = CreateAuthenticatedClient(apiKey, baseUrl);
        var requestUri = $"/api/v1/recording/{recordingId}";

        Logger.LogDebug("Fetching Fellow recording: {RequestUri}", requestUri);

        var response = await httpClient.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var recordingResponse = JsonSerializer.Deserialize<FellowRecordingResponse>(content, _jsonOptions);

        Logger.LogInformation("Successfully fetched recording {RecordingId} from Fellow.ai", recordingId);

        return recordingResponse;
    }

    /// <summary>
    /// Lists all recordings from Fellow.ai API with pagination and filters
    /// </summary>
    /// <param name="apiKey">Fellow.ai API key</param>
    /// <param name="baseUrl">Base URL for Fellow.ai API</param>
    /// <param name="options">Optional request options for pagination, includes, and filters</param>
    /// <returns>Paginated list of recordings</returns>
    public async Task<FellowRecordingsResponse?> ListRecordingsAsync(
        string apiKey,
        string baseUrl,
        FellowRecordingsRequestOptions? options = null)
    {
        await EnforceRateLimitAsync();

        using var httpClient = CreateAuthenticatedClient(apiKey, baseUrl);
        var requestUri = "/api/v1/recordings";

        Logger.LogDebug("Fetching Fellow recordings: {RequestUri}", requestUri);

        var requestBody = new Dictionary<string, object?>();

        // Pagination
        if (options?.Pagination != null)
        {
            var pagination = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(options.Pagination.Cursor))
            {
                pagination["cursor"] = options.Pagination.Cursor;
            }
            if (options.Pagination.PageSize.HasValue)
            {
                pagination["page_size"] = options.Pagination.PageSize.Value;
            }
            if (pagination.Count > 0)
            {
                requestBody["pagination"] = pagination;
            }
        }

        // Include options
        if (options?.Include != null)
        {
            var include = new Dictionary<string, object?>();
            if (options.Include.Transcript.HasValue)
            {
                include["transcript"] = options.Include.Transcript.Value;
            }
            if (include.Count > 0)
            {
                requestBody["include"] = include;
            }
        }

        // Filters
        if (options?.Filters != null)
        {
            var filters = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(options.Filters.EventGuid))
            {
                filters["event_guid"] = options.Filters.EventGuid;
            }
            if (options.Filters.CreatedAtStart != null)
            {
                filters["created_at_start"] = options.Filters.CreatedAtStart;//.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            if (options.Filters.CreatedAtEnd != null)
            {
                filters["created_at_end"] = options.Filters.CreatedAtEnd;//.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            if (options.Filters.UpdatedAtStart != null)
            {
                filters["updated_at_start"] = options.Filters.UpdatedAtStart;//.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            if (options.Filters.UpdatedAtEnd != null)
            {
                filters["updated_at_end"] = options.Filters.UpdatedAtEnd;//.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            if (!string.IsNullOrWhiteSpace(options.Filters.ChannelId))
            {
                filters["channel_id"] = options.Filters.ChannelId;
            }
            if (!string.IsNullOrWhiteSpace(options.Filters.Title))
            {
                filters["title"] = options.Filters.Title;
            }
            if (filters.Count > 0)
            {
                requestBody["filters"] = filters;
            }
        }

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var recordingsResponse = JsonSerializer.Deserialize<FellowRecordingsResponse>(responseContent, _jsonOptions);

        Logger.LogInformation("Successfully fetched {Count} recordings from Fellow.ai",
            recordingsResponse?.Recordings?.Data?.Count ?? 0);

        return recordingsResponse;
    }

    /// <summary>
    /// Enforces 500ms minimum delay between API calls
    /// </summary>
    private static async Task EnforceRateLimitAsync()
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
            var remainingDelay = MinimumDelayMs - (int)timeSinceLastCall.TotalMilliseconds;

            if (remainingDelay > 0)
            {
                await Task.Delay(remainingDelay);
            }

            _lastApiCall = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}
