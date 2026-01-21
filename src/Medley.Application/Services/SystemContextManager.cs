using System.Text.Json;
using System.Text.Json.Serialization;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Services;

/// <summary>
/// Service for composing structured system prompts from various context sources as JSON
/// </summary>
public class SystemContextManager
{
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<AiPrompt> _promptRepository;
    private readonly ILogger<SystemContextManager> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SystemContextManager(
        IRepository<Article> articleRepository,
        IRepository<Plan> planRepository,
        IRepository<AiPrompt> promptRepository,
        ILogger<SystemContextManager> logger)
    {
        _articleRepository = articleRepository;
        _planRepository = planRepository;
        _promptRepository = promptRepository;
        _logger = logger;
    }

    /// <summary>
    /// Build a complete system prompt from article, plan, and prompt context
    /// </summary>
    /// <param name="articleId">Required: The article being worked on</param>
    /// <param name="promptType">Required: The prompt type to use</param>
    /// <param name="conversationMode">Required: The conversation mode (Agent or Plan)</param>
    /// <param name="userName">Required: The name of the user interacting with the system</param>
    /// <param name="planId">Optional: Include plan-specific context if provided</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete system prompt as JSON string</returns>
    public async Task<string> BuildPromptAsync(
        Guid articleId,
        PromptType promptType,
        ConversationMode conversationMode,
        string userName,
        Guid? planId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building system prompt for article {ArticleId}, plan {PlanId}, prompt {PromptType}, mode {ConversationMode}",
            articleId, planId, promptType, conversationMode);

        var promptData = new SystemPromptData
        {
            ConversationMode = conversationMode.ToString(),
            UserName = userName
        };

        // 1. Start with prompt (required)
        var prompt = await _promptRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == promptType, cancellationToken);

        if (prompt != null)
        {
            promptData.PrimaryGuidance = prompt.Content;
        }
        else
        {
            _logger.LogWarning("Prompt type {PromptType} not found", promptType);
        }

        // 2. Always append article context (required)
        var article = await _articleRepository.Query()
            .Include(a => a.ArticleType)
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article == null)
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        promptData.Article = new ArticleData
        {
            Id = article.Id,
            Title = article.Title,
            Type = article.ArticleType?.Name,
            Summary = article.Summary,
            Status = article.Status.ToString(),
            Content = article.Content ?? "(No content yet)"
        };

        // 2a. Add article type guidance (if article has a type)
        if (article.ArticleType != null)
        {
            var articleTypePromptType = promptType == PromptType.ArticlePlanCreation
                ? PromptType.ArticleTypePlanMode
                : PromptType.ArticleTypeAgentMode;

            var articleTypePrompt = await _promptRepository.Query()
                .FirstOrDefaultAsync(t => t.Type == articleTypePromptType && t.ArticleTypeId == article.ArticleTypeId, cancellationToken);

            if (articleTypePrompt != null)
            {
                promptData.ArticleTypeGuidance = articleTypePrompt.Content;
            }
            else
            {
                _logger.LogDebug("No article type guidance found for type {ArticleTypeId} and prompt type {PromptType}",
                    article.ArticleTypeId, articleTypePromptType);
            }
        }

        // 3. Conditionally append plan context (if provided)
        if (planId.HasValue)
        {
            var plan = await _planRepository.Query()
                .Include(p => p.PlanFragments.Where(pf => pf.Include))
                    .ThenInclude(pf => pf.Fragment)
                        .ThenInclude(f => f.Source)
                            .ThenInclude(s => s!.Tags)
                                .ThenInclude(t => t.TagType)
                .FirstOrDefaultAsync(p => p.Id == planId.Value, cancellationToken);

            if (plan != null)
            {
                var includedFragments = plan.PlanFragments.Where(pf => pf.Include).ToList();

                promptData.Plan = new PlanData
                {
                    Id = plan.Id,
                    Instructions = plan.Instructions,
                    Fragments = includedFragments
                        .OrderByDescending(x => x.SimilarityScore)
                        .Select(pf => new FragmentData
                        {
                            Id = pf.Fragment.Id,
                            Title = pf.Fragment.Title,
                            Summary = pf.Fragment.Summary,
                            Content = pf.Fragment.Content,
                            Instructions = pf.Instructions,
                            Source = pf.Fragment.Source != null
                                ? new SourceData
                                {
                                    Date = pf.Fragment.Source.Date.Date,
                                    IsInternal = pf.Fragment.Source.IsInternal,
                                    Tags = pf.Fragment.Source.Tags.Select(t => new TagData
                                    {
                                        Type = t.TagType.Name,
                                        Value = t.Value
                                    }).ToList()
                                }
                                : null
                        })
                        .ToList()
                };
            }
            else
            {
                _logger.LogWarning("Plan {PlanId} not found", planId);
            }
        }

        var json = JsonSerializer.Serialize(promptData, _jsonOptions);

        _logger.LogInformation("Built system prompt JSON of {Length} characters for article {ArticleId}",
            json.Length, articleId);

        return json;
    }

    // Data models for JSON serialization
    private class SystemPromptData
    {
        public string ConversationMode { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? PrimaryGuidance { get; set; }
        public string? ArticleTypeGuidance { get; set; }
        public ArticleData? Article { get; set; } = null!;
        public PlanData? Plan { get; set; }
    }

    private class ArticleData
    {
        public required Guid Id { get; set; }
        public required string Title { get; set; } = null!;
        public required string? Type { get; set; }
        public required string? Summary { get; set; }
        public required string Status { get; set; } = null!;
        public required string Content { get; set; } = null!;
    }

    private class PlanData
    {
        public Guid Id { get; set; }
        public string Instructions { get; set; } = null!;
        public List<FragmentData> Fragments { get; set; } = new();
    }

    private class FragmentData
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Summary { get; set; }
        public string Content { get; set; } = null!;
        public string? Instructions { get; set; }
        public SourceData? Source { get; set; }
    }

    private class SourceData
    {
        public DateTimeOffset Date { get; set; }
        public bool? IsInternal { get; set; }
        public List<TagData> Tags { get; set; } = new();
    }

    private class TagData
    {
        public string Type { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
