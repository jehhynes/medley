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
    private readonly Guid? _implementingPlanId;
    private readonly Guid? _conversationId;
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<PlanFragment> _planFragmentRepository;
    private readonly IArticleVersionService _articleVersionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArticleChatTools> _logger;
    private readonly EmbeddingSettings _embeddingSettings;
    private readonly AiCallContext _aiCallContext;
    private readonly Guid _currentUserId;
    private readonly ICursorService? _cursorService;
    private readonly IOptions<CursorSettings>? _cursorSettings;

    public ArticleChatTools(
        Guid articleId,
        Guid userId,
        Guid? implementingPlanId,
        Guid? conversationId,
        IFragmentRepository fragmentRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IRepository<Article> articleRepository,
        IRepository<Plan> planRepository,
        IRepository<PlanFragment> planFragmentRepository,
        IArticleVersionService articleVersionService,
        IUnitOfWork unitOfWork,
        ILogger<ArticleChatTools> logger,
        IOptions<EmbeddingSettings> embeddingSettings,
        AiCallContext aiCallContext,
        ICursorService? cursorService = null,
        IOptions<CursorSettings>? cursorSettings = null)
    {
        _articleId = articleId;
        _currentUserId = userId;
        _implementingPlanId = implementingPlanId;
        _conversationId = conversationId;
        _fragmentRepository = fragmentRepository;
        _embeddingGenerator = embeddingGenerator;
        _articleRepository = articleRepository;
        _planRepository = planRepository;
        _planFragmentRepository = planFragmentRepository;
        _articleVersionService = articleVersionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _embeddingSettings = embeddingSettings.Value;
        _aiCallContext = aiCallContext;
        _cursorService = cursorService;
        _cursorSettings = cursorSettings;
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
                        date = fragment.Source.Date
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
        [Description("Request containing plan instructions, fragment recommendations, and optional change summary")] CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating plan for article: {ArticleId} with {Count} recommendations", 
                _articleId, request.Recommendations.Length);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Instructions))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Instructions cannot be empty"
                });
            }

            if (request.Recommendations == null || request.Recommendations.Length == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "At least one fragment recommendation is required"
                });
            }

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
                // Entity is already tracked, changes will be saved on SaveChangesAsync
                
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
                Instructions = request.Instructions,
                Status = PlanStatus.Draft,
                CreatedByUserId = _currentUserId,
                CreatedAt = DateTimeOffset.UtcNow,
                Version = newVersion,
                ParentPlanId = parentPlanId,
                ChangesSummary = request.ChangesSummary,
                ConversationId = _conversationId
            };

            await _planRepository.AddAsync(plan);

            // Create plan fragments
            foreach (var rec in request.Recommendations)
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

                await _planFragmentRepository.AddAsync(planFragment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created plan {PlanId} for article {ArticleId}", plan.Id, _articleId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                planId = plan.Id,
                message = $"Created plan with {request.Recommendations.Length} fragment recommendations"
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

    /// <summary>
    /// Create a new AI-generated version of the article with improved content
    /// </summary>
    [Description("Create a new AI-generated version of the article with improved content. " +
        "This creates a new ArticleVersion record with the improved content.")]
    public virtual async Task<string> CreateArticleVersionAsync(
        [Description("Request containing the improved article content and change description")] CreateArticleVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating AI article version for article: {ArticleId}", _articleId);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Content cannot be empty"
                });
            }

            if (string.IsNullOrWhiteSpace(request.ChangeMessage))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Change message cannot be empty"
                });
            }

            if (request.ChangeMessage.Length > 200)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Change message cannot exceed 200 characters"
                });
            }

            // Create AI version using the service
            var versionDto = await _articleVersionService.CreateAiVersionAsync(
                _articleId,
                request.Content,
                request.ChangeMessage,
                _currentUserId,
                _conversationId,
                cancellationToken);
            
            // If this is implementing a plan, mark it as Applied
            if (_implementingPlanId.HasValue)
            {
                var plan = await _planRepository.GetByIdAsync(_implementingPlanId.Value, cancellationToken);
                if (plan != null)
                {
                    plan.Status = PlanStatus.Applied;
                    plan.AppliedAt = DateTimeOffset.UtcNow;
                    // Entity is already tracked, changes will be saved on SaveChangesAsync
                    _logger.LogInformation("Marked plan {PlanId} as Applied", _implementingPlanId.Value);
                }
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                versionId = versionDto.Id,
                versionNumber = versionDto.VersionNumber,
                message = $"Created AI version {versionDto.VersionNumber} of the article"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating article version for article: {ArticleId}", _articleId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article version for article: {ArticleId}", _articleId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error creating article version: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Review and improve article content using Cursor Agent
    /// </summary>
    [Description("Ask Cursor Agent to improve the article. The Agent has access to the application source code and is the authority on existing functionality. Creates a new article version and returns a summary of changes.")]
    public virtual async Task<string> ReviewArticleWithCursorAsync(
        [Description("Instructions for how to improve the article")] string instructions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Cursor review for article: {ArticleId}", _articleId);

            // Check if Cursor is enabled
            if (_cursorSettings?.Value?.Enabled != true)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Cursor integration is not enabled"
                });
            }

            if (_cursorService == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Cursor service is not available"
                });
            }

            // Load article
            var article = await _articleRepository.Query()
                .Where(a => a.Id == _articleId)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (article == null)
            {
                _logger.LogWarning("Article not found: {ArticleId}", _articleId);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Article not found"
                });
            }

            // Call CursorService to review the article
            var result = await _cursorService.ReviewArticleAsync(
                article.Content ?? string.Empty,
                instructions,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Cursor review failed: {Error}", result.Error);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = result.Error
                });
            }

            // Create new article version with improved content
            var changeMessage = result.ChangesSummary.Length > 200 
                ? result.ChangesSummary.Substring(0, 200)
                : result.ChangesSummary;

            var versionDto = await _articleVersionService.CreateAiVersionAsync(
                _articleId,
                result.ImprovedContent,
                changeMessage,
                _currentUserId,
                _conversationId,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created version {VersionNumber} for article {ArticleId} with Cursor improvements", 
                versionDto.VersionNumber, _articleId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                versionId = versionDto.Id,
                versionNumber = versionDto.VersionNumber,
                improvedContent = result.ImprovedContent,
                changesSummary = result.ChangesSummary,
                message = $"Created version {versionDto.VersionNumber} with Cursor improvements"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Cursor review for article: {ArticleId}", _articleId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error during Cursor review: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Ask Cursor Agent a question without modifying the article
    /// </summary>
    [Description("Ask Cursor Agent a question. The Agent has access to the application source code and is the authority on existing functionality. Use for questions about application features.")]
    public virtual async Task<string> AskQuestionWithCursorAsync(
        [Description("The question to ask the Cursor Agent")] string question,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Asking Cursor question for article: {ArticleId}", _articleId);

            // Check if Cursor is enabled
            if (_cursorSettings?.Value?.Enabled != true)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Cursor integration is not enabled"
                });
            }

            if (_cursorService == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Cursor service is not available"
                });
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Question cannot be empty"
                });
            }

            // Call CursorService to ask the question
            var result = await _cursorService.AskQuestionAsync(question, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Cursor question failed: {Error}", result.Error);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = result.Error
                });
            }

            _logger.LogInformation("Cursor question completed successfully for article {ArticleId}", _articleId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                response = result.Response,
                question = question
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Cursor question for article: {ArticleId}", _articleId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error asking Cursor question: {ex.Message}"
            });
        }
    }
}

/// <summary>
/// Request for creating an article improvement plan
/// </summary>
public class CreatePlanRequest
{
    /// <summary>
    /// Overall instructions for how to improve the article using the recommended fragments
    /// </summary>
    [Description("Overall instructions for how to improve the article using the recommended fragments")]
    [Required]
    public required string Instructions { get; set; }

    /// <summary>
    /// Array of fragment recommendations
    /// </summary>
    [Description("Array of fragment recommendations")]
    [Required]
    [MinLength(1)]
    public required PlanFragmentRecommendation[] Recommendations { get; set; }

    /// <summary>
    /// Optional summary of changes if this is a modification of an existing plan
    /// </summary>
    [Description("Optional summary of changes if this is a modification of an existing plan")]
    [MaxLength(500)]
    public string? ChangesSummary { get; set; }
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

public class CreateArticleVersionRequest
{
    /// <summary>
    /// The complete improved article content
    /// </summary>
    [Description("The complete improved article content")]
    [Required]
    public required string Content { get; set; }

    /// <summary>
    /// Description of changes made in this version
    /// </summary>
    [Description("Description of changes made in this version")]
    [Required]
    [MaxLength(200)]
    public required string ChangeMessage { get; set; }
}
