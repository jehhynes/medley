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
        bool includeTranscript = true)
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
