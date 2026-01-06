using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Application.Services;

/// <summary>
/// Tools providing fragment search capabilities for the article assistant
/// </summary>
public class ArticleChatTools
{
    private readonly Guid _articleId;
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<PlanFragment> _planFragmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArticleChatTools> _logger;
    private readonly EmbeddingSettings _embeddingSettings;
    private readonly AiCallContext _aiCallContext;
    private readonly Guid _currentUserId;

    public ArticleChatTools(
        Guid articleId,
        Guid userId,
        IFragmentRepository fragmentRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IRepository<Article> articleRepository,
        IRepository<Plan> planRepository,
        IRepository<PlanFragment> planFragmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<ArticleChatTools> logger,
        IOptions<EmbeddingSettings> embeddingSettings,
        AiCallContext aiCallContext)
    {
        _articleId = articleId;
        _currentUserId = userId;
        _fragmentRepository = fragmentRepository;
        _embeddingGenerator = embeddingGenerator;
        _articleRepository = articleRepository;
        _planRepository = planRepository;
        _planFragmentRepository = planFragmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _embeddingSettings = embeddingSettings.Value;
        _aiCallContext = aiCallContext;
    }



    /// <summary>
    /// Search for fragments semantically similar to a query string
    /// </summary>
    [Description("Search for fragments semantically similar to a query string. Returns fragments with similarity scores.")]
    public virtual async Task<string> SearchFragmentsAsync(
        [Description("The search query text to find similar fragments")] string query,
        [Description("Maximum number of results to return (default: 10)")] int limit = 10,
        [Description("Minimum similarity threshold from 0.0 to 1.0 (default: 0.7)")] double? minSimilarity = 0.7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching fragments with query: {Query}, limit: {Limit}, minSimilarity: {MinSimilarity}", 
                query, limit, minSimilarity);

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
            GeneratedEmbeddings<Embedding<float>> embeddingResult;
            using (_aiCallContext.SetContext(nameof(ArticleChatTools), nameof(SearchFragmentsAsync), nameof(Article), _articleId))
            {
                embeddingResult = await _embeddingGenerator.GenerateAsync(
                    new[] { query }, 
                    embeddingGenerationOptions, 
                    cancellationToken);
            }
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
                minSimilarity,
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
    [Description("Find fragments semantically similar to the current article content. " +
        "Useful for finding related content to enhance or expand the article.")]
    public virtual async Task<string> FindSimilarFragmentsAsync(
        [Description("Maximum number of results to return (default: 10)")] int limit = 10,
        [Description("Minimum similarity threshold from 0.0 to 1.0 (default: 0.7)")] double? minSimilarity = 0.7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Finding similar fragments for article: {ArticleId}, limit: {Limit}, minSimilarity: {MinSimilarity}", 
                _articleId, limit, minSimilarity);

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
            GeneratedEmbeddings<Embedding<float>> embeddingResult;
            using (_aiCallContext.SetContext(nameof(ArticleChatTools), nameof(FindSimilarFragmentsAsync), nameof(Article), _articleId))
            {
                embeddingResult = await _embeddingGenerator.GenerateAsync(
                    new[] { textForEmbedding }, 
                    embeddingGenerationOptions, 
                    cancellationToken);
            }
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
                minSimilarity,
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
    [Description("Get the full content and details of a specific fragment by its ID. " +
        "Use this to review fragments in detail before recommending them.")]
    public virtual async Task<string> GetFragmentContentAsync(
        [Description("The unique identifier of the fragment to retrieve")] Guid fragmentId,
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
    [Description("Create/update a structured improvement plan for the article with fragment recommendations.")]
    public virtual async Task<string> CreatePlanAsync(
        [Description("Overall instructions for how to improve the article using the recommended fragments")] string instructions,
        [Description("Array of fragment recommendations")] PlanFragmentRecommendation[] recommendations,
        [Description("Optional summary of changes if this is a modification of an existing plan")] string? changesSummary = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating plan for article: {ArticleId} with {Count} recommendations", 
                _articleId, recommendations.Length);



            // Get existing draft plan (if any) to determine version and parent
            var existingDraftPlan = await _planRepository.Query()
                .Where(p => p.ArticleId == _articleId && p.Status == PlanStatus.Draft)
                .OrderByDescending(p => p.Version)
                .FirstOrDefaultAsync(cancellationToken);

            int newVersion = 1;
            Guid? parentPlanId = null;

            if (existingDraftPlan != null)
            {
                // This is a modification - archive the current draft
                existingDraftPlan.Status = PlanStatus.Archived;
                await _planRepository.SaveAsync(existingDraftPlan);
                
                newVersion = existingDraftPlan.Version + 1;
                parentPlanId = existingDraftPlan.Id;
            }
            else
            {
                // Check if there are any archived plans to determine version
                var maxVersion = await _planRepository.Query()
                    .Where(p => p.ArticleId == _articleId)
                    .MaxAsync(p => (int?)p.Version, cancellationToken);
                
                if (maxVersion.HasValue)
                {
                    newVersion = maxVersion.Value + 1;
                }
            }

            // Create new plan
            var plan = new Plan
            {
                ArticleId = _articleId,
                Instructions = instructions,
                Status = PlanStatus.Draft,
                CreatedByUserId = _currentUserId,
                CreatedAt = DateTimeOffset.UtcNow,
                Version = newVersion,
                ParentPlanId = parentPlanId,
                ChangesSummary = changesSummary
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
    [Description("The unique identifier of the fragment being recommended")]
    public Guid FragmentId { get; set; }
    
    [Description("The similarity score between the fragment and the article (0.0 to 1.0)")]
    public double SimilarityScore { get; set; }
    
    [Description("Whether to include this fragment in the article improvement (true) or exclude it (false)")]
    public bool Include { get; set; }
    
    [Description("Explanation of why this fragment should not be included in the article. Omit if Include=true")]
    [MaxLength(200)]
    public required string Reasoning { get; set; }
    
    [Description("Optional instructions on how to incorporate this fragment into the article")]
    [MaxLength(200)]
    public required string Instructions { get; set; }
}
