using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
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
    private readonly Guid _articleId;
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<PlanFragment> _planFragmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArticleAssistantPlugins> _logger;
    private readonly EmbeddingSettings _embeddingSettings;
    private Guid? _currentUserId; // Set by chat service before running agent

    public ArticleAssistantPlugins(
        Guid articleId,
        IFragmentRepository fragmentRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IRepository<Article> articleRepository,
        IRepository<Plan> planRepository,
        IRepository<PlanFragment> planFragmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<ArticleAssistantPlugins> logger,
        IOptions<EmbeddingSettings> embeddingSettings)
    {
        _articleId = articleId;
        _fragmentRepository = fragmentRepository;
        _embeddingGenerator = embeddingGenerator;
        _articleRepository = articleRepository;
        _planRepository = planRepository;
        _planFragmentRepository = planFragmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _embeddingSettings = embeddingSettings.Value;
    }

    /// <summary>
    /// Set the current user ID for plan creation
    /// </summary>
    public virtual void SetCurrentUserId(Guid userId)
    {
        _currentUserId = userId;
    }

    /// <summary>
    /// Search for fragments semantically similar to a query string
    /// </summary>
    public virtual async Task<string> SearchFragmentsAsync(
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
    public virtual async Task<string> FindSimilarFragmentsAsync(
        int limit = 10,
        double? threshold = 0.7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Finding similar fragments for article: {ArticleId}, limit: {Limit}, threshold: {Threshold}", 
                _articleId, limit, threshold);

            // Load the article
            var article = await _articleRepository.Query()
                .Where(a => a.Id == _articleId)
                .FirstOrDefaultAsync(cancellationToken);

            if (article == null)
            {
                _logger.LogWarning("Article not found: {ArticleId}", _articleId);
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
                _logger.LogWarning("Article has no content to search with: {ArticleId}", _articleId);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Article has no content to search with",
                    articleId = _articleId,
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
                _logger.LogWarning("Failed to generate embedding for article: {ArticleId}", _articleId);
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
                articleId = _articleId,
                articleTitle = article.Title,
                resultsCount = fragments.Count,
                fragments = fragments
            };

            _logger.LogInformation("Found {Count} similar fragments for article: {ArticleId}", fragments.Count, _articleId);

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar fragments for article: {ArticleId}", _articleId);
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

    /// <summary>
    /// Get full content and details for a specific fragment
    /// </summary>
    [Description("Get the full content and details of a specific fragment by its ID")]
    public virtual async Task<string> GetFragmentContentAsync(
        Guid fragmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting fragment content for fragment: {FragmentId}", fragmentId);

            var fragment = await _fragmentRepository.Query()
                .Include(f => f.Source)
                .FirstOrDefaultAsync(f => f.Id == fragmentId, cancellationToken);

            if (fragment == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Fragment not found"
                });
            }

            var response = new
            {
                success = true,
                fragment = new
                {
                    id = fragment.Id,
                    title = fragment.Title,
                    summary = fragment.Summary,
                    category = fragment.Category,
                    content = fragment.Content,
                    confidence = fragment.Confidence?.ToString(),
                    confidenceComment = fragment.ConfidenceComment,
                    source = fragment.Source != null ? new
                    {
                        id = fragment.Source.Id,
                        name = fragment.Source.Name,
                        type = fragment.Source.Type.ToString(),
                        date = fragment.Source.Date,
                        externalId = fragment.Source.ExternalId
                    } : null
                }
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fragment content: {FragmentId}", fragmentId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error getting fragment: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Create an article improvement plan with fragment recommendations
    /// </summary>
    [Description("Create a structured improvement plan for an article with fragment recommendations")]
    public virtual async Task<string> CreatePlanAsync(
        string instructions,
        PlanFragmentRecommendation[] recommendations,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating plan for article: {ArticleId} with {Count} recommendations", 
                _articleId, recommendations.Length);

            if (!_currentUserId.HasValue)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "User ID not set for plan creation"
                });
            }

            // Archive any existing draft plans for this article
            var existingPlans = await _planRepository.Query()
                .Where(p => p.ArticleId == _articleId && p.Status == PlanStatus.Draft)
                .ToListAsync(cancellationToken);

            foreach (var existingPlan in existingPlans)
            {
                existingPlan.Status = PlanStatus.Archived;
                await _planRepository.SaveAsync(existingPlan);
            }

            // Create new plan
            var plan = new Plan
            {
                ArticleId = _articleId,
                Instructions = instructions,
                Status = PlanStatus.Draft,
                CreatedByUserId = _currentUserId.Value,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _planRepository.SaveAsync(plan);

            // Create plan fragments
            foreach (var rec in recommendations)
            {
                var planFragment = new PlanFragment
                {
                    PlanId = plan.Id,
                    FragmentId = rec.FragmentId,
                    SimilarityScore = rec.SimilarityScore,
                    Include = rec.Include,
                    Reasoning = rec.Reasoning,
                    Instructions = rec.Instructions
                };

                await _planFragmentRepository.SaveAsync(planFragment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created plan {PlanId} for article {ArticleId}", plan.Id, _articleId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                planId = plan.Id,
                message = $"Created plan with {recommendations.Length} fragment recommendations"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan for article: {ArticleId}", _articleId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error creating plan: {ex.Message}"
            });
        }
    }
}

/// <summary>
/// Represents a fragment recommendation for a plan
/// </summary>
public class PlanFragmentRecommendation
{
    public Guid FragmentId { get; set; }
    public double SimilarityScore { get; set; }
    public bool Include { get; set; }
    public required string Reasoning { get; set; }
    public required string Instructions { get; set; }
}
