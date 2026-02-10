using Markdig;
using Medley.Application.Configuration;
using Medley.Application.Integrations.Models.Zendesk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Medley.Application.Integrations.Services;

/// <summary>
/// Service for Zendesk Help Center integration
/// </summary>
public class ZendeskService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ZendeskSettings _settings;
    private readonly ILogger<ZendeskService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public ZendeskService(
        IHttpClientFactory httpClientFactory,
        IOptions<ZendeskSettings> settings,
        ILogger<ZendeskService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates and configures an authenticated HttpClient for Zendesk API
    /// </summary>
    private HttpClient CreateAuthenticatedClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri($"https://{_settings.Subdomain}.zendesk.com/");
        
        // Basic authentication: {email}/token:{api_token} base64 encoded
        var credentials = $"{_settings.Email}/token:{_settings.ApiToken}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return httpClient;
    }

    /// <summary>
    /// Converts markdown content to HTML for Zendesk
    /// </summary>
    private static string? ConvertMarkdownToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return markdown;
        }

        try
        {
            return Markdown.ToHtml(markdown, _markdownPipeline);
        }
        catch (Exception)
        {
            // If markdown conversion fails, return the original content
            return markdown;
        }
    }

    /// <summary>
    /// Strips the first H1 heading from markdown content if it matches the title
    /// </summary>
    private static string? StripH1IfMatchesTitle(string? markdown, string title)
    {
        if (string.IsNullOrWhiteSpace(markdown) || string.IsNullOrWhiteSpace(title))
        {
            return markdown;
        }

        try
        {
            // Pattern to match first H1: # Title at the start of content (possibly with leading whitespace)
            var h1Pattern = @"^\s*#\s+(.+?)(\r?\n|$)";
            var match = Regex.Match(markdown, h1Pattern, RegexOptions.Multiline, TimeSpan.FromSeconds(1));

            if (match.Success)
            {
                var h1Text = match.Groups[1].Value.Trim();
                
                // If H1 matches the title, remove it
                if (h1Text.Equals(title, StringComparison.Ordinal))
                {
                    return Regex.Replace(markdown, h1Pattern, string.Empty, RegexOptions.Multiline, TimeSpan.FromSeconds(1)).TrimStart();
                }
            }

            return markdown;
        }
        catch (Exception)
        {
            // If regex fails, return the original content
            return markdown;
        }
    }

    /// <summary>
    /// Creates a new article in Zendesk Help Center
    /// </summary>
    /// <param name="title">Article title</param>
    /// <param name="body">Article body (markdown content)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Zendesk article ID</returns>
    public async Task<string> CreateArticleAsync(
        string title,
        string? body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = CreateAuthenticatedClient();
            var requestUri = $"api/v2/help_center/{_settings.Locale}/sections/{_settings.SectionId}/articles";

            _logger.LogInformation("Zendesk Settings - SectionId: {SectionId}, PermissionGroupId: {PermissionGroupId}, UserSegmentId: {UserSegmentId}, Locale: {Locale}",
                _settings.SectionId, _settings.PermissionGroupId, _settings.UserSegmentId, _settings.Locale);

            // Strip H1 if it matches the title (Zendesk displays title separately)
            var contentWithoutRedundantH1 = StripH1IfMatchesTitle(body, title);
            
            // Convert markdown to HTML for Zendesk
            var htmlBody = ConvertMarkdownToHtml(contentWithoutRedundantH1);

            var request = new ZendeskArticleRequest
            {
                Article = new ZendeskArticleData
                {
                    Title = title,
                    Body = htmlBody,
                    Locale = _settings.Locale,
                    UserSegmentId = _settings.UserSegmentId,
                    PermissionGroupId = _settings.PermissionGroupId
                },
                NotifySubscribers = false // Don't spam subscribers during sync
            };

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating Zendesk article: {Title} at {BaseUrl}{RequestUri}", 
                title, httpClient.BaseAddress, requestUri);
            _logger.LogInformation("Request body: {Body}", jsonContent);

            var response = await httpClient.PostAsync(requestUri, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create Zendesk article. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);
                
                throw new HttpRequestException($"Zendesk API returned {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var articleResponse = JsonSerializer.Deserialize<ZendeskArticleResponse>(responseContent, _jsonOptions);

            if (articleResponse?.Article == null)
            {
                throw new InvalidOperationException("Failed to deserialize Zendesk article response");
            }

            var articleId = articleResponse.Article.Id.ToString();
            _logger.LogInformation("Successfully created Zendesk article {ArticleId}: {Title}", articleId, title);

            return articleId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Zendesk article: {Title}", title);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing article in Zendesk Help Center
    /// </summary>
    /// <param name="zendeskArticleId">Zendesk article ID</param>
    /// <param name="title">Updated article title</param>
    /// <param name="body">Updated article body (markdown content)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateArticleAsync(
        string zendeskArticleId,
        string title,
        string? body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = CreateAuthenticatedClient();
            // Use translations endpoint to update article content (title, body, draft status)
            var requestUri = $"api/v2/help_center/articles/{zendeskArticleId}/translations/{_settings.Locale}";

            // Strip H1 if it matches the title (Zendesk displays title separately)
            var contentWithoutRedundantH1 = StripH1IfMatchesTitle(body, title);
            
            // Convert markdown to HTML for Zendesk
            var htmlBody = ConvertMarkdownToHtml(contentWithoutRedundantH1);

            var request = new ZendeskTranslationUpdateRequest
            {
                Translation = new ZendeskTranslationUpdateData
                {
                    Title = title,
                    Body = htmlBody,
                    Draft = false // Ensure article is published, not in draft
                }
            };

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Updating Zendesk article {ArticleId}: {Title} at {BaseUrl}{RequestUri}", 
                zendeskArticleId, title, httpClient.BaseAddress, requestUri);
            _logger.LogInformation("Request body: {Body}", jsonContent);

            var response = await httpClient.PutAsync(requestUri, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update Zendesk article {ArticleId}. Status: {StatusCode}, Response: {Response}",
                    zendeskArticleId, response.StatusCode, responseContent);
                
                throw new HttpRequestException($"Zendesk API returned {response.StatusCode}: {responseContent}");
            }

            _logger.LogInformation("Successfully updated Zendesk article {ArticleId}: {Title}", zendeskArticleId, title);
            _logger.LogDebug("Response body: {Response}", responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Zendesk article {ArticleId}: {Title}", zendeskArticleId, title);
            throw;
        }
    }
}
