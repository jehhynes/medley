using Medley.Collector.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Medley.Collector.Services;

public class FellowApiService
{
    private const string BaseUrl = "https://{0}.fellow.app";
    private const int MinimumDelayMs = 500;
    private static DateTime _lastApiCall = DateTime.MinValue;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    
    private readonly HttpClient _httpClient;
    
    public FellowApiService(string workspace)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(string.Format(BaseUrl, workspace))
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    
    public async Task<FellowRecordingsResponse?> ListRecordingsAsync(
        string apiKey,
        string? cursor = null,
        int pageSize = 50,
        bool includeTranscript = true,
        DateTime? createdAtStart = null)
    {
        await EnforceRateLimitAsync();
        
        var requestBody = new Dictionary<string, object?>
        {
            ["pagination"] = new Dictionary<string, object?>
            {
                ["page_size"] = pageSize
            },
            ["include"] = new Dictionary<string, object?>
            {
                ["transcript"] = includeTranscript
            }
        };
        
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            ((Dictionary<string, object?>)requestBody["pagination"]!)["cursor"] = cursor;
        }
        
        if (createdAtStart.HasValue)
        {
            requestBody["filters"] = new Dictionary<string, object?>
            {
                ["created_at_start"] = createdAtStart.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }
        
        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/recordings")
        {
            Content = content
        };
        request.Headers.Add("X-API-KEY", apiKey);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FellowRecordingsResponse>(responseContent, _jsonOptions);
    }
    
    public async Task<FellowNotesResponse?> ListNotesAsync(
        string apiKey,
        string? cursor = null,
        int pageSize = 50,
        bool includeEventAttendees = true,
        bool includeContentMarkdown = true,
        DateTime? updatedAtStart = null)
    {
        await EnforceRateLimitAsync();
        
        var requestBody = new Dictionary<string, object?>
        {
            ["pagination"] = new Dictionary<string, object?>
            {
                ["page_size"] = pageSize
            }
        };
        
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            ((Dictionary<string, object?>)requestBody["pagination"]!)["cursor"] = cursor;
        }
        
        // Add include options for event_attendees and content_markdown
        var include = new Dictionary<string, object?>
        {
            ["event_attendees"] = includeEventAttendees,
            ["content_markdown"] = includeContentMarkdown
        };
        requestBody["include"] = include;
        
        if (updatedAtStart.HasValue)
        {
            requestBody["filters"] = new Dictionary<string, object?>
            {
                ["updated_at_start"] = updatedAtStart.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }
        
        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/notes")
        {
            Content = content
        };
        request.Headers.Add("X-API-KEY", apiKey);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FellowNotesResponse>(responseContent, _jsonOptions);
    }
    
    public async Task<FellowNoteResponse?> GetNoteAsync(string apiKey, string noteId)
    {
        await EnforceRateLimitAsync();
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/note/{noteId}");
        request.Headers.Add("X-API-KEY", apiKey);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FellowNoteResponse>(responseContent, _jsonOptions);
    }
    
    public async Task<FellowMeResponse?> GetAuthenticatedUserAsync(string apiKey)
    {
        await EnforceRateLimitAsync();
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Add("X-API-KEY", apiKey);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FellowMeResponse>(responseContent, _jsonOptions);
    }
    
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
