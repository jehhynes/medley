using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Medley.Application.Configuration;
using Medley.Application.Hubs;
using Medley.Application.Hubs.Clients;
using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs;
using Medley.Application.Models.DTOs.Llm;
using Medley.Application.Models.DTOs.Llm.Tools;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Application.Services;

/// <summary>
/// Tools providing knowledge unit search capabilities for the article assistant
/// </summary>
public class ArticleChatTools
{
    private readonly Guid _articleId;
    private readonly Guid? _implementingPlanId;
    private readonly Guid _conversationId;
    private readonly IKnowledgeUnitRepository _knowledgeUnitRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<PlanKnowledgeUnit> _planKnowledgeUnitRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IArticleVersionService _articleVersionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArticleChatTools> _logger;
    private readonly EmbeddingSettings _embeddingSettings;
    private readonly AiCallContext _aiCallContext;
    private readonly Guid _currentUserId;
    private readonly ICursorService? _cursorService;
    private readonly IOptions<CursorSettings>? _cursorSettings;
    private readonly IHubContext<ArticleHub, IArticleClient> _hubContext;
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ArticleChatTools(
        Guid articleId,
        Guid userId,
        Guid? implementingPlanId,
        Guid conversationId,
        IKnowledgeUnitRepository knowledgeUnitRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IRepository<Article> articleRepository,
        IRepository<ChatConversation> conversationRepository,
        IRepository<Plan> planRepository,
        IRepository<PlanKnowledgeUnit> planKnowledgeUnitRepository,
        IRepository<User> userRepository,
        IArticleVersionService articleVersionService,
        IUnitOfWork unitOfWork,
        ILogger<ArticleChatTools> logger,
        IOptions<EmbeddingSettings> embeddingSettings,
        AiCallContext aiCallContext,
        IHubContext<ArticleHub, IArticleClient> hubContext,
        ICursorService? cursorService = null,
        IOptions<CursorSettings>? cursorSettings = null)
    {
        _articleId = articleId;
        _currentUserId = userId;
        _implementingPlanId = implementingPlanId;
        _conversationId = conversationId;
        _knowledgeUnitRepository = knowledgeUnitRepository;
        _embeddingGenerator = embeddingGenerator;
        _articleRepository = articleRepository;
        _conversationRepository = conversationRepository;
        _planRepository = planRepository;
        _planKnowledgeUnitRepository = planKnowledgeUnitRepository;
        _userRepository = userRepository;
        _articleVersionService = articleVersionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _embeddingSettings = embeddingSettings.Value;
        _aiCallContext = aiCallContext;
        _hubContext = hubContext;
        _cursorService = cursorService;
        _cursorSettings = cursorSettings;
    }



    /// <summary>
    /// Search for knowledge units semantically similar to a query string
    /// </summary>
    [Description("Search for knowledge units semantically similar to a query string. Returns knowledge units with similarity scores.")]
    public virtual async Task<string> SearchKnowledgeUnitsAsync(
        [Description("The search query text to find similar knowledge units")] string query,
        [Description("Maximum number of results to return (default: 10)")] int limit = 10,
        [Description("Minimum similarity threshold from 0.0 to 1.0 (default: 0.7)")] double? minSimilarity = 0.7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching knowledge units with query: {Query}, limit: {Limit}, minSimilarity: {MinSimilarity}", 
                query, limit, minSimilarity);

            if (string.IsNullOrWhiteSpace(query))
            {
                var errorResponse = new SearchKnowledgeUnitsResponse
                {
                    Success = false,
                    Error = "Query cannot be empty",
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Generate embedding for the query
            var embeddingGenerationOptions = new EmbeddingGenerationOptions
            {
                Dimensions = _embeddingSettings.Dimensions
            };
            GeneratedEmbeddings<Embedding<float>> embeddingResult;
            using (_aiCallContext.SetContext(nameof(ArticleChatTools), nameof(SearchKnowledgeUnitsAsync), nameof(Article), _articleId))
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
                var errorResponse = new SearchKnowledgeUnitsResponse
                {
                    Success = false,
                    Error = "Failed to generate embedding for query",
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Search for similar knowledge units
            var results = await _knowledgeUnitRepository.FindSimilarAsync(
                embedding.Vector.ToArray(),
                limit,
                minSimilarity,
                cancellationToken: cancellationToken);

            var knowledgeUnits = new List<ToolKnowledgeUnitSearchResult>();
            foreach (var result in results)
            {
                var knowledgeUnit = result.KnowledgeUnit;

                knowledgeUnits.Add(new ToolKnowledgeUnitSearchResult
                {
                    Id = knowledgeUnit.Id,
                    Title = knowledgeUnit.Title,
                    Summary = knowledgeUnit.Summary,
                    Category = knowledgeUnit.Category.Name,
                    SimilarityScore = result.Similarity,
                    Confidence = knowledgeUnit.Confidence,
                    ConfidenceComment = knowledgeUnit.ConfidenceComment
                });
            }

            var response = new SearchKnowledgeUnitsResponse
            {
                Success = true,
                KnowledgeUnits = knowledgeUnits
            };

            _logger.LogInformation("Found {Count} knowledge units for query: {Query}", knowledgeUnits.Count, query);

            return JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching knowledge units with query: {Query}", query);
            var errorResponse = new SearchKnowledgeUnitsResponse
            {
                Success = false,
                Error = $"Error searching knowledge units: {ex.Message}",
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
        }
    }

    /// <summary>
    /// Find knowledge units semantically similar to the current article content
    /// </summary>
    [Description("Find knowledge units semantically similar to the current article content. " +
        "Useful for finding related content to enhance or expand the article.")]
    public virtual async Task<string> FindSimilarKnowledgeUnitsAsync(
        [Description("Maximum number of results to return (default: 10)")] int limit = 10,
        [Description("Minimum similarity threshold from 0.0 to 1.0 (default: 0.7)")] double? minSimilarity = 0.7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Finding similar knowledge units for article: {ArticleId}, limit: {Limit}, minSimilarity: {MinSimilarity}", 
                _articleId, limit, minSimilarity);

            // Load the article
            var article = await _articleRepository.Query()
                .Where(a => a.Id == _articleId)
                .FirstOrDefaultAsync(cancellationToken);

            if (article == null)
            {
                _logger.LogWarning("Article not found: {ArticleId}", _articleId);
                var errorResponse = new FindSimilarKnowledgeUnitsResponse
                {
                    Success = false,
                    Error = "Article not found",
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Build text for embedding from article content
            var textForEmbedding = BuildTextForEmbedding(article);

            if (string.IsNullOrWhiteSpace(textForEmbedding))
            {
                _logger.LogWarning("Article has no content to search with: {ArticleId}", _articleId);
                var errorResponse = new FindSimilarKnowledgeUnitsResponse
                {
                    Success = false,
                    Error = "Article has no content to search with"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Generate embedding for the article content
            var embeddingGenerationOptions = new EmbeddingGenerationOptions
            {
                Dimensions = _embeddingSettings.Dimensions
            };
            GeneratedEmbeddings<Embedding<float>> embeddingResult;
            using (_aiCallContext.SetContext(nameof(ArticleChatTools), nameof(FindSimilarKnowledgeUnitsAsync), nameof(Article), _articleId))
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
                var errorResponse = new FindSimilarKnowledgeUnitsResponse
                {
                    Success = false,
                    Error = "Failed to generate embedding for article content",
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Search for similar knowledge units
            var results = await _knowledgeUnitRepository.FindSimilarAsync(
                embedding.Vector.ToArray(),
                limit,
                minSimilarity,
                cancellationToken: cancellationToken);

            var knowledgeUnits = new List<ToolKnowledgeUnitSearchResult>();
            foreach (var result in results)
            {
                var knowledgeUnit = result.KnowledgeUnit;

                knowledgeUnits.Add(new ToolKnowledgeUnitSearchResult
                {
                    Id = knowledgeUnit.Id,
                    Title = knowledgeUnit.Title,
                    Summary = knowledgeUnit.Summary,
                    Category = knowledgeUnit.Category.Name,
                    SimilarityScore = result.Similarity,
                    Confidence = knowledgeUnit.Confidence,
                    ConfidenceComment = knowledgeUnit.ConfidenceComment
                });
            }

            var response = new FindSimilarKnowledgeUnitsResponse
            {
                Success = true,
                KnowledgeUnits = knowledgeUnits
            };

            _logger.LogInformation("Found {Count} similar knowledge units for article: {ArticleId}", knowledgeUnits.Count, _articleId);

            return JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar knowledge units for article: {ArticleId}", _articleId);
            var errorResponse = new FindSimilarKnowledgeUnitsResponse
            {
                Success = false,
                Error = $"Error finding similar knowledge units: {ex.Message}",
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
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
    /// Get full content and details for a specific knowledge unit
    /// </summary>
    [Description("Get the full content and details of a specific knowledge unit by its ID. " +
        "Use this to review knowledge units in detail before recommending them.")]
    public virtual async Task<string> GetKnowledgeUnitContentAsync(
        [Description("The unique identifier of the knowledge unit to retrieve")] Guid knowledgeUnitId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting knowledge unit content for knowledge unit: {KnowledgeUnitId}", knowledgeUnitId);

            var knowledgeUnit = await _knowledgeUnitRepository.Query()
                .Include(ku => ku.Category)
                .FirstOrDefaultAsync(ku => ku.Id == knowledgeUnitId, cancellationToken);

            if (knowledgeUnit == null)
            {
                var errorResponse = new GetKnowledgeUnitResponse
                {
                    Success = false,
                    Error = "Knowledge unit not found"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            var response = new GetKnowledgeUnitResponse
            {
                Success = true,
                KnowledgeUnit = new KnowledgeUnitWithContentData
                {
                    Id = knowledgeUnit.Id,
                    Title = knowledgeUnit.Title,
                    Summary = knowledgeUnit.Summary,
                    Category = knowledgeUnit.Category.Name,
                    Content = knowledgeUnit.Content,
                    Confidence = knowledgeUnit.Confidence,
                    ConfidenceComment = knowledgeUnit.ConfidenceComment,
                    Source = null // Knowledge units don't expose source data
                }
            };

            return JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge unit content: {KnowledgeUnitId}", knowledgeUnitId);
            var errorResponse = new GetKnowledgeUnitResponse
            {
                Success = false,
                Error = $"Error getting knowledge unit: {ex.Message}"
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
        }
    }

    /// <summary>
    /// Create an article improvement plan with knowledge unit recommendations
    /// </summary>
    [Description("Create/update a structured improvement plan for the article with knowledge unit recommendations.")]
    public virtual async Task<string> CreatePlanAsync(
        [Description("Request containing plan instructions, knowledge unit recommendations, and optional change summary")] CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating plan for article: {ArticleId} with {Count} recommendations", 
                _articleId, request.Recommendations.Length);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Instructions))
            {
                var errorResponse = new CreatePlanResponse
                {
                    Success = false,
                    Error = "Instructions cannot be empty"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            if (request.Recommendations == null || request.Recommendations.Length == 0)
            {
                var errorResponse = new CreatePlanResponse
                {
                    Success = false,
                    Error = "At least one knowledge unit recommendation is required"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
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

            // Load required navigation properties for Plan
            var article = await _articleRepository.GetByIdAsync(_articleId);
            if (article == null)
            {
                throw new InvalidOperationException($"Article {_articleId} not found");
            }

            var user = await _userRepository.GetByIdAsync(_currentUserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"User {_currentUserId} not found");
            }

            var conversation = await _conversationRepository.GetByIdAsync(_conversationId);
            if(conversation == null)
            {
                throw new InvalidOperationException($"Conversation {_conversationId} not found");
            }

            // Create new plan
            var plan = new Plan
            {
                Article = article,
                Instructions = request.Instructions,
                Status = PlanStatus.Draft,
                CreatedBy = user,
                CreatedAt = DateTimeOffset.UtcNow,
                Version = newVersion,
                ParentPlanId = parentPlanId,
                ChangesSummary = request.ChangesSummary,
                Conversation = conversation
            };

            await _planRepository.AddAsync(plan);

            // Set as current plan on the article
            article.CurrentPlanId = plan.Id;
            article.CurrentPlan = plan;

            // Create plan knowledge units
            foreach (var rec in request.Recommendations)
            {
                // Load knowledge unit for required navigation property
                var knowledgeUnit = await _knowledgeUnitRepository.GetByIdAsync(rec.KnowledgeUnitId);
                if (knowledgeUnit == null)
                {
                    _logger.LogWarning("Knowledge unit {KnowledgeUnitId} not found, skipping plan knowledge unit", rec.KnowledgeUnitId);
                    continue;
                }

                var planKnowledgeUnit = new PlanKnowledgeUnit
                {
                    Plan = plan,
                    KnowledgeUnit = knowledgeUnit,
                    SimilarityScore = rec.SimilarityScore,
                    Include = rec.Include,
                    Reasoning = rec.Reasoning,
                    Instructions = rec.Instructions
                };

                await _planKnowledgeUnitRepository.AddAsync(planKnowledgeUnit);
            }

            // Register post-commit action to send SignalR notification
            var payload = new PlanGeneratedPayload
            {
                ArticleId = _articleId,
                PlanId = plan.Id,
                Timestamp = DateTimeOffset.UtcNow
            };
            _unitOfWork.RegisterPostCommitAction(async () =>
            {
                await _hubContext.Clients.Group($"Article_{_articleId}")
                    .PlanGenerated(payload);
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created plan {PlanId} for article {ArticleId}", plan.Id, _articleId);

            var response = new CreatePlanResponse
            {
                Success = true,
                PlanId = plan.Id,
                Message = $"Created plan with {request.Recommendations.Length} knowledge unit recommendations"
            };
            return JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan for article: {ArticleId}", _articleId);
            var errorResponse = new CreatePlanResponse
            {
                Success = false,
                Error = $"Error creating plan: {ex.Message}"
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
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
                var errorResponse = new CreateArticleVersionResponse
                {
                    Success = false,
                    Error = "Content cannot be empty"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Create AI version using the service
            var versionDto = await _articleVersionService.CreateAiVersionAsync(
                _articleId,
                request.Content,
                request.ChangeMessage,
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
                    _logger.LogInformation("Marked plan {PlanId} as Applied", _implementingPlanId.Value);
                }
            }
            
            // Register post-commit action to send SignalR notification
            var payload = new VersionCreatedPayload
            {
                ArticleId = _articleId,
                VersionId = versionDto.Id,
                VersionNumber = versionDto.VersionNumber,
                VersionType = VersionType.AI,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _unitOfWork.RegisterPostCommitAction(async () =>
            {
                await _hubContext.Clients.Group($"Article_{_articleId}")
                    .VersionCreated(payload);
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new CreateArticleVersionResponse
            {
                Success = true,
                VersionId = versionDto.Id,
                VersionNumber = versionDto.VersionNumber,
                Message = $"Created AI version {versionDto.VersionNumber} of the article"
            };
            return JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating article version for article: {ArticleId}", _articleId);
            var errorResponse = new CreateArticleVersionResponse
            {
                Success = false,
                Error = ex.Message
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article version for article: {ArticleId}", _articleId);
            var errorResponse = new CreateArticleVersionResponse
            {
                Success = false,
                Error = $"Error creating article version: {ex.Message}"
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
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
                var errorResponse = new ReviewArticleWithCursorResponse
                {
                    Success = false,
                    Error = "Cursor integration is not enabled"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            if (_cursorService == null)
            {
                var errorResponse = new ReviewArticleWithCursorResponse
                {
                    Success = false,
                    Error = "Cursor service is not available"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Load article
            var article = await _articleRepository.Query()
                .Where(a => a.Id == _articleId)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (article == null)
            {
                _logger.LogWarning("Article not found: {ArticleId}", _articleId);
                var errorResponse = new ReviewArticleWithCursorResponse
                {
                    Success = false,
                    Error = "Article not found"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Call CursorService to review the article
            var result = await _cursorService.ReviewArticleAsync(
                articleContent: article.Content ?? string.Empty,
                instructions: instructions,
                articleTypeId: article.ArticleTypeId,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Cursor review failed: {Error}", result.Error);
                var errorResponse = new ReviewArticleWithCursorResponse
                {
                    Success = false,
                    Error = result.Error
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Create new article version with improved content
            var changeMessage = result.ChangesSummary.Length > 200 
                ? result.ChangesSummary.Substring(0, 200)
                : result.ChangesSummary;

            var versionDto = await _articleVersionService.CreateAiVersionAsync(
                _articleId,
                result.ImprovedContent,
                changeMessage,
                _conversationId,
                cancellationToken);

            // Register post-commit action to send SignalR notification
            var payload = new VersionCreatedPayload
            {
                ArticleId = _articleId,
                VersionId = versionDto.Id,
                VersionNumber = versionDto.VersionNumber,
                VersionType = VersionType.AI,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _unitOfWork.RegisterPostCommitAction(async () =>
            {
                await _hubContext.Clients.Group($"Article_{_articleId}")
                    .VersionCreated(payload);
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created version {VersionNumber} for article {ArticleId} with Cursor improvements", 
                versionDto.VersionNumber, _articleId);

            var response = new ReviewArticleWithCursorResponse
            {
                Success = true,
                VersionId = versionDto.Id,
                VersionNumber = versionDto.VersionNumber,
                ImprovedContent = result.ImprovedContent,
                ChangesSummary = result.ChangesSummary,
                Message = $"Created version {versionDto.VersionNumber} with Cursor improvements"
            };
            return JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Cursor review for article: {ArticleId}", _articleId);
            var errorResponse = new ReviewArticleWithCursorResponse
            {
                Success = false,
                Error = $"Error during Cursor review: {ex.Message}"
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
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
                var errorResponse = new AskQuestionWithCursorResponse
                {
                    Success = false,
                    Error = "Cursor integration is not enabled"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            if (_cursorService == null)
            {
                var errorResponse = new AskQuestionWithCursorResponse
                {
                    Success = false,
                    Error = "Cursor service is not available"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                var errorResponse = new AskQuestionWithCursorResponse
                {
                    Success = false,
                    Error = "Question cannot be empty"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Call CursorService to ask the question
            var result = await _cursorService.AskQuestionAsync(question, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Cursor question failed: {Error}", result.Error);
                var errorResponse = new AskQuestionWithCursorResponse
                {
                    Success = false,
                    Error = result.Error
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            _logger.LogInformation("Cursor question completed successfully for article {ArticleId}", _articleId);

            var response = new AskQuestionWithCursorResponse
            {
                Success = true,
                Response = result.Response
            };
            return JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Cursor question for article: {ArticleId}", _articleId);
            var errorResponse = new AskQuestionWithCursorResponse
            {
                Success = false,
                Error = $"Error asking Cursor question: {ex.Message}"
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
        }
    }

    /// <summary>
    /// Add knowledge units to an existing plan
    /// </summary>
    [Description("Add additional knowledge unit recommendations to an existing draft plan. " +
        "Use this to incrementally build a plan by adding more knowledge units without creating a new plan version.")]
    public virtual async Task<string> AddKnowledgeUnitsToPlanAsync(
        [Description("Request containing the plan ID and knowledge unit recommendations to add")] AddKnowledgeUnitsToPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding knowledge units to plan: {PlanId} for article: {ArticleId}", 
                request.PlanId, _articleId);

            // Validate request
            if (request.Recommendations == null || request.Recommendations.Length == 0)
            {
                var errorResponse = new AddKnowledgeUnitsToPlanResponse
                {
                    Success = false,
                    Error = "At least one knowledge unit recommendation is required"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Load the plan with existing knowledge units
            var plan = await _planRepository.Query()
                .Include(p => p.PlanKnowledgeUnits)
                .Where(p => p.Id == request.PlanId)
                .FirstOrDefaultAsync(cancellationToken);

            if (plan == null)
            {
                _logger.LogWarning("Plan not found: {PlanId}", request.PlanId);
                var errorResponse = new AddKnowledgeUnitsToPlanResponse
                {
                    Success = false,
                    Error = "Plan not found"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Validate plan belongs to the current article
            if (plan.ArticleId != _articleId)
            {
                _logger.LogWarning("Plan {PlanId} does not belong to article {ArticleId}", 
                    request.PlanId, _articleId);
                var errorResponse = new AddKnowledgeUnitsToPlanResponse
                {
                    Success = false,
                    Error = "Plan does not belong to this article"
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Validate plan is in Draft status
            if (plan.Status != PlanStatus.Draft)
            {
                _logger.LogWarning("Plan {PlanId} is not in Draft status (current: {Status})", 
                    request.PlanId, plan.Status);
                var errorResponse = new AddKnowledgeUnitsToPlanResponse
                {
                    Success = false,
                    Error = $"Cannot add knowledge units to a plan with status: {plan.Status}. Only Draft plans can be modified."
                };
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }

            // Get existing knowledge unit IDs to prevent duplicates
            var existingKnowledgeUnitIds = plan.PlanKnowledgeUnits
                .Select(pku => pku.KnowledgeUnitId)
                .ToHashSet();

            int addedCount = 0;
            int skippedCount = 0;

            // Process each recommendation
            foreach (var rec in request.Recommendations)
            {
                // Check if knowledge unit already exists in plan
                if (existingKnowledgeUnitIds.Contains(rec.KnowledgeUnitId))
                {
                    _logger.LogDebug("Knowledge unit {KnowledgeUnitId} already exists in plan {PlanId}, skipping", 
                        rec.KnowledgeUnitId, request.PlanId);
                    skippedCount++;
                    continue;
                }

                // Load knowledge unit to validate it exists
                var knowledgeUnit = await _knowledgeUnitRepository.GetByIdAsync(rec.KnowledgeUnitId, cancellationToken);
                if (knowledgeUnit == null)
                {
                    _logger.LogWarning("Knowledge unit {KnowledgeUnitId} not found, skipping", rec.KnowledgeUnitId);
                    skippedCount++;
                    continue;
                }

                // Create plan knowledge unit
                var planKnowledgeUnit = new PlanKnowledgeUnit
                {
                    Plan = plan,
                    KnowledgeUnit = knowledgeUnit,
                    SimilarityScore = rec.SimilarityScore,
                    Include = rec.Include,
                    Reasoning = rec.Reasoning,
                    Instructions = rec.Instructions
                };

                await _planKnowledgeUnitRepository.AddAsync(planKnowledgeUnit);
                addedCount++;
                
                _logger.LogDebug("Added knowledge unit {KnowledgeUnitId} to plan {PlanId}", 
                    rec.KnowledgeUnitId, request.PlanId);
            }

            // Register post-commit action to send SignalR notification
            if (addedCount > 0)
            {
                var payload = new PlanUpdatedPayload
                {
                    ArticleId = _articleId,
                    PlanId = request.PlanId,
                    KnowledgeUnitsAdded = addedCount,
                    Timestamp = DateTimeOffset.UtcNow
                };
                _unitOfWork.RegisterPostCommitAction(async () =>
                {
                    await _hubContext.Clients.Group($"Article_{_articleId}")
                        .PlanUpdated(payload);
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added {AddedCount} knowledge units to plan {PlanId}, skipped {SkippedCount}", 
                addedCount, request.PlanId, skippedCount);

            var response = new AddKnowledgeUnitsToPlanResponse
            {
                Success = true,
                AddedCount = addedCount,
                SkippedCount = skippedCount,
                Message = $"Added {addedCount} knowledge unit(s) to plan. {skippedCount} knowledge unit(s) skipped (duplicates or not found)."
            };
            return JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding knowledge units to plan: {PlanId}", request.PlanId);
            var errorResponse = new AddKnowledgeUnitsToPlanResponse
            {
                Success = false,
                Error = $"Error adding knowledge units to plan: {ex.Message}"
            };
            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
        }
    }
}