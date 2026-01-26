using System.Text.Json;
using System.Text.Json.Serialization;
using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs.Llm;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.Agents.AI;
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
    private readonly IRepository<ArticleVersion> _versionRepository;
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
        IRepository<ArticleVersion> versionRepository,
        ILogger<SystemContextManager> logger)
    {
        _articleRepository = articleRepository;
        _planRepository = planRepository;
        _promptRepository = promptRepository;
        _versionRepository = versionRepository;
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
    /// <param name="messageStore">Optional: Message store to check for existing tool results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete system prompt as JSON string</returns>
    public async Task<string> BuildPromptAsync(
        Guid articleId,
        PromptType promptType,
        ConversationMode conversationMode,
        string userName,
        Guid? planId = null,
        ChatMessageStore? messageStore = null,
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

        // 1a. Always append organization context
        var organizationPrompt = await _promptRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == PromptType.OrganizationContext, cancellationToken);

        if (organizationPrompt != null)
        {
            promptData.OrganizationContext = organizationPrompt.Content;
        }
        else
        {
            _logger.LogDebug("Organization context prompt not found");
        }

        // 1b. Always append fragment weighting guidance if available
        var fragmentWeightingPrompt = await _promptRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == PromptType.FragmentWeighting, cancellationToken);

        if (fragmentWeightingPrompt != null)
        {
            promptData.FragmentWeightingGuidance = fragmentWeightingPrompt.Content;
        }
        else
        {
            _logger.LogDebug("Fragment weighting guidance not found");
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

        // 2b. Include latest AI draft version if it exists and isn't already included as a tool result
        var pendingAiDraft = await _versionRepository.Query()
            .Include(v => v.ParentVersion)
            .Where(v => v.ArticleId == articleId && v.VersionType == VersionType.AI)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pendingAiDraft != null && pendingAiDraft.ReviewAction == ReviewAction.None) //Only include if it's a Pending AI draft 
        {
            if (!await IsAiDraftAlreadyIncludedInToolResultAsync(pendingAiDraft, messageStore, cancellationToken))
            {
                var versionNumber = pendingAiDraft.ParentVersion != null
                    ? $"{pendingAiDraft.ParentVersion.VersionNumber}.{pendingAiDraft.VersionNumber}"
                    : pendingAiDraft.VersionNumber.ToString();

                promptData.PendingAiDraft = new AiDraftData
                {
                    VersionNumber = versionNumber,
                    Content = pendingAiDraft.ContentSnapshot,
                    ChangeMessage = pendingAiDraft.ChangeMessage,
                    CreatedAt = pendingAiDraft.CreatedAt,
                };

                _logger.LogDebug("Included latest AI draft version {VersionNumber} for article {ArticleId}",
                    versionNumber, articleId);
            }
            else
            {
                _logger.LogDebug("Skipped AI draft version {VersionId} - already included in tool result",
                    pendingAiDraft.Id);
            }
        }

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
                .Include(p => p.PlanFragments.Where(pf => pf.Include))
                    .ThenInclude(pf => pf.Fragment)
                        .ThenInclude(f => f.Source)
                            .ThenInclude(s => s!.PrimarySpeaker)
                .Include(p => p.PlanFragments.Where(pf => pf.Include))
                    .ThenInclude(pf => pf.Fragment)
                        .ThenInclude(f => f.FragmentCategory)
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
                        .Select(pf => new PlanFragmentData
                        {
                            Id = pf.Fragment.Id,
                            Title = pf.Fragment.Title,
                            Summary = pf.Fragment.Summary,
                            Category = pf.Fragment.FragmentCategory.Name,
                            Content = pf.Fragment.Content,
                            Instructions = pf.Instructions,
                            Confidence = pf.Fragment.Confidence,
                            Source = pf.Fragment.Source != null
                                ? new SourceData
                                {
                                    Date = pf.Fragment.Source.Date.Date,
                                    SourceType = pf.Fragment.Source.Type.ToString(),
                                    Scope = pf.Fragment.Source.IsInternal == true ? "Internal" : "External",
                                    PrimarySpeaker = pf.Fragment.Source.PrimarySpeaker?.Name,
                                    PrimarySpeakerTrustLevel = pf.Fragment.Source.PrimarySpeaker?.TrustLevel?.ToString(),
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

    /// <summary>
    /// Check if the AI draft version is already included in a CreateArticleVersion or ReviewArticleWithCursor tool result
    /// </summary>
    private async Task<bool> IsAiDraftAlreadyIncludedInToolResultAsync(
        ArticleVersion pendingAiDraft,
        ChatMessageStore? messageStore,
        CancellationToken cancellationToken = default)
    {
        if (messageStore == null)
        {
            return false;
        }

        try
        {
            // Get all messages from the conversation
            var messages = await messageStore.GetMessagesAsync(cancellationToken);
            
            // Check CreateArticleVersion tool result
            if (await CheckToolResultForVersionIdAsync(messages, "CreateArticleVersion", pendingAiDraft.Id, cancellationToken))
            {
                return true;
            }
            
            // Check ReviewArticleWithCursor tool result
            if (await CheckToolResultForVersionIdAsync(messages, "ReviewArticleWithCursor", pendingAiDraft.Id, cancellationToken))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if AI draft is in tool result");
            return false;
        }
    }

    /// <summary>
    /// Check if a specific tool result contains a matching version ID
    /// </summary>
    private async Task<bool> CheckToolResultForVersionIdAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        string toolName,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var toolResult = await FindToolResultByFunctionNameAsync(messages, toolName, cancellationToken);
        
        if (toolResult == null)
        {
            return false;
        }

        // Parse the result to check if it contains this version ID
        var resultString = toolResult.Result?.ToString();
        if (string.IsNullOrEmpty(resultString))
        {
            return false;
        }

        try
        {
            var resultData = JsonSerializer.Deserialize<JsonElement>(resultString);
            
            // Check if the result contains a versionId that matches
            if (resultData.TryGetProperty("versionId", out var versionIdProperty))
            {
                var versionIdString = versionIdProperty.GetString();
                if (Guid.TryParse(versionIdString, out var parsedVersionId))
                {
                    return parsedVersionId == versionId;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse {ToolName} tool result", toolName);
        }

        return false;
    }

    /// <summary>
    /// Find a tool result by the tool call name (e.g., "CreateArticleVersion")
    /// </summary>
    private async Task<Microsoft.Extensions.AI.FunctionResultContent?> FindToolResultByFunctionNameAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        string toolName,
        CancellationToken cancellationToken = default)
    {
        // First pass: find the call ID for the specified tool name
        string? targetCallId = null;
        
        foreach (var message in messages)
        {
            if (message.Contents == null) continue;

            foreach (var callContent in message.Contents.OfType<Microsoft.Extensions.AI.FunctionCallContent>())
            {
                if (callContent.Name == toolName && !string.IsNullOrEmpty(callContent.CallId))
                {
                    targetCallId = callContent.CallId;
                    break;
                }
            }
            
            if (targetCallId != null) break;
        }

        if (targetCallId == null)
        {
            return null;
        }

        // Second pass: find the result for this call ID
        foreach (var message in messages)
        {
            if (message.Contents == null) continue;

            foreach (var resultContent in message.Contents.OfType<Microsoft.Extensions.AI.FunctionResultContent>())
            {
                if (resultContent.CallId == targetCallId)
                {
                    return resultContent;
                }
            }
        }

        return null;
    }

    // Data models for JSON serialization
    private class SystemPromptData
    {
        public required string ConversationMode { get; set; }
        public required string UserName { get; set; }
        public string? PrimaryGuidance { get; set; }
        public string? OrganizationContext { get; set; }
        public string? FragmentWeightingGuidance { get; set; }
        public string? ArticleTypeGuidance { get; set; }
        public ArticleData? Article { get; set; }
        public AiDraftData? PendingAiDraft { get; set; }
        public PlanData? Plan { get; set; }
    }

    private class ArticleData
    {
        public required Guid Id { get; set; }
        public required string Title { get; set; }
        public required string? Type { get; set; }
        public required string? Summary { get; set; }
        public required string Status { get; set; }
        public required string Content { get; set; }
    }

    private class AiDraftData
    {
        public required string VersionNumber { get; set; }
        public required string Content { get; set; }
        public string? ChangeMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    private class PlanData
    {
        public Guid Id { get; set; }
        public required string Instructions { get; set; }
        public List<PlanFragmentData> Fragments { get; set; } = new();
    }
}
