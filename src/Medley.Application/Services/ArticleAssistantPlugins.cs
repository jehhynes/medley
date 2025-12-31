using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Application.Services;

/// <summary>
/// Plugin providing fragment search capabilities for the article assistant
/// </summary>
public class ArticleAssistantPlugins
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IRepository<Article> _articleRepository;
    private readonly ILogger<ArticleAssistantPlugins> _logger;
    private readonly EmbeddingSettings _embeddingSettings;

    public ArticleAssistantPlugins(
        IFragmentRepository fragmentRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IRepository<Article> articleRepository,
        ILogger<ArticleAssistantPlugins> logger,
        IOptions<EmbeddingSettings> embeddingSettings)
    {
        _fragmentRepository = fragmentRepository;
        _embeddingGenerator = embeddingGenerator;
        _articleRepository = articleRepository;
        _logger = logger;
        _embeddingSettings = embeddingSettings.Value;
    }

    /// <summary>
    /// Search for fragments semantically similar to a query string
    /// </summary>
    public async Task<string> SearchFragmentsAsync(
        string query,
        int limit = 10,
        double? threshold = 0.7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching fragments with query: {Query}, limit: {Limit}, threshold: {Threshold}", 
                query, limit, threshold);

            if (string.IsNullOrWhiteSpace(query))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Query cannot be empty",
                    fragments = Array.Empty<object>()
                });
            }

            // Generate embedding for the query
            var embeddingGenerationOptions = new EmbeddingGenerationOptions
            {
                Dimensions = _embeddingSettings.Dimensions
            };
            var embeddingResult = await _embeddingGenerator.GenerateAsync(
                new[] { query }, 
                embeddingGenerationOptions, 
                cancellationToken);
            var embedding = embeddingResult.FirstOrDefault();

            if (embedding == null)
            {
                _logger.LogWarning("Failed to generate embedding for query: {Query}", query);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Failed to generate embedding for query",
                    fragments = Array.Empty<object>()
                });
            }

            // Search for similar fragments
            var results = await _fragmentRepository.FindSimilarAsync(
                embedding.Vector.ToArray(),
                limit,
                threshold,
                cancellationToken);

            var fragments = new List<object>();
            foreach (var result in results)
            {
                var fragment = result.RelatedEntity;
                
                // Load source information if available
                string? sourceName = null;
                string? sourceType = null;
                DateTimeOffset? sourceDate = null;

                if (fragment.SourceId.HasValue)
                {
                    var source = await _fragmentRepository.Query()
                        .Where(f => f.Id == fragment.Id)
                        .Select(f => new { f.Source!.Name, f.Source.Type, f.Source.Date })
                        .FirstOrDefaultAsync(cancellationToken);
                    
                    if (source != null)
                    {
                        sourceName = source.Name;
                        sourceType = source.Type.ToString();
                        sourceDate = source.Date;
                    }
                }

                fragments.Add(new
                {
                    id = fragment.Id,
                    title = fragment.Title,
                    summary = fragment.Summary,
                    category = fragment.Category,
                    content = fragment.Content,
                    similarityScore = Math.Round(1.0 - (result.Distance / 2.0), 3), // Convert distance to similarity (0-1 scale)
                    distance = Math.Round(result.Distance, 3),
                    source = new
                    {
                        name = sourceName,
                        type = sourceType,
                        date = sourceDate
                    }
                });
            }

            var response = new
            {
                success = true,
                query = query,
                resultsCount = fragments.Count,
                fragments = fragments
            };

            _logger.LogInformation("Found {Count} fragments for query: {Query}", fragments.Count, query);

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching fragments with query: {Query}", query);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error searching fragments: {ex.Message}",
                fragments = Array.Empty<object>()
            });
        }
    }

    /// <summary>
    /// Find fragments semantically similar to the current article content
    /// </summary>
    public async Task<string> FindSimilarFragmentsAsync(
        Guid articleId,
        int limit = 10,
        double? threshold = 0.7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Finding similar fragments for article: {ArticleId}, limit: {Limit}, threshold: {Threshold}", 
                articleId, limit, threshold);

            // Load the article
            var article = await _articleRepository.Query()
                .Where(a => a.Id == articleId)
                .FirstOrDefaultAsync(cancellationToken);

            if (article == null)
            {
                _logger.LogWarning("Article not found: {ArticleId}", articleId);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Article not found",
                    fragments = Array.Empty<object>()
                });
            }

            // Build text for embedding from article content
            var textForEmbedding = BuildTextForEmbedding(article);

            if (string.IsNullOrWhiteSpace(textForEmbedding))
            {
                _logger.LogWarning("Article has no content to search with: {ArticleId}", articleId);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Article has no content to search with",
                    articleId = articleId,
                    articleTitle = article.Title,
                    fragments = Array.Empty<object>()
                });
            }

            // Generate embedding for the article content
            var embeddingGenerationOptions = new EmbeddingGenerationOptions
            {
                Dimensions = _embeddingSettings.Dimensions
            };
            var embeddingResult = await _embeddingGenerator.GenerateAsync(
                new[] { textForEmbedding }, 
                embeddingGenerationOptions, 
                cancellationToken);
            var embedding = embeddingResult.FirstOrDefault();

            if (embedding == null)
            {
                _logger.LogWarning("Failed to generate embedding for article: {ArticleId}", articleId);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Failed to generate embedding for article content",
                    fragments = Array.Empty<object>()
                });
            }

            // Search for similar fragments
            var results = await _fragmentRepository.FindSimilarAsync(
                embedding.Vector.ToArray(),
                limit,
                threshold,
                cancellationToken);

            var fragments = new List<object>();
            foreach (var result in results)
            {
                var fragment = result.RelatedEntity;
                
                // Load source information if available
                string? sourceName = null;
                string? sourceType = null;
                DateTimeOffset? sourceDate = null;

                if (fragment.SourceId.HasValue)
                {
                    var source = await _fragmentRepository.Query()
                        .Where(f => f.Id == fragment.Id)
                        .Select(f => new { f.Source!.Name, f.Source.Type, f.Source.Date })
                        .FirstOrDefaultAsync(cancellationToken);
                    
                    if (source != null)
                    {
                        sourceName = source.Name;
                        sourceType = source.Type.ToString();
                        sourceDate = source.Date;
                    }
                }

                fragments.Add(new
                {
                    id = fragment.Id,
                    title = fragment.Title,
                    summary = fragment.Summary,
                    category = fragment.Category,
                    content = fragment.Content,
                    similarityScore = Math.Round(1.0 - (result.Distance / 2.0), 3), // Convert distance to similarity (0-1 scale)
                    distance = Math.Round(result.Distance, 3),
                    source = new
                    {
                        name = sourceName,
                        type = sourceType,
                        date = sourceDate
                    }
                });
            }

            var response = new
            {
                success = true,
                articleId = articleId,
                articleTitle = article.Title,
                resultsCount = fragments.Count,
                fragments = fragments
            };

            _logger.LogInformation("Found {Count} similar fragments for article: {ArticleId}", fragments.Count, articleId);

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar fragments for article: {ArticleId}", articleId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error finding similar fragments: {ex.Message}",
                fragments = Array.Empty<object>()
            });
        }
    }

    /// <summary>
    /// Builds text for embedding from article properties
    /// </summary>
    private static string BuildTextForEmbedding(Article article)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(article.Title))
        {
            sb.AppendLine(article.Title);
        }

        if (!string.IsNullOrWhiteSpace(article.Summary))
        {
            sb.AppendLine(article.Summary);
        }

        if (!string.IsNullOrWhiteSpace(article.Content))
        {
            // Limit content length to avoid token limits
            var content = article.Content;
            if (content.Length > 5000)
            {
                content = content.Substring(0, 5000);
            }
            sb.AppendLine(content);
        }

        return sb.ToString();
    }
}
