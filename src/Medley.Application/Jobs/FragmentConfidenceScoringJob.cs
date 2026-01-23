using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire.Console;
using Hangfire.Server;
using Hangfire.Storage;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hangfire.MissionControl;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job that assigns confidence scores to extracted fragments
/// </summary>
[MissionLauncher]
public class FragmentConfidenceScoringJob : BaseHangfireJob<FragmentConfidenceScoringJob>
{
    private readonly IAiProcessingService _aiProcessingService;
    private readonly IRepository<Fragment> _fragmentRepository;
    private readonly IRepository<Source> _sourceRepository;
    private readonly IRepository<AiPrompt> _promptRepository;
    private readonly AiCallContext _aiCallContext;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FragmentConfidenceScoringJob(
        IAiProcessingService aiProcessingService,
        IRepository<Fragment> fragmentRepository,
        IRepository<Source> sourceRepository,
        IRepository<AiPrompt> promptRepository,
        IUnitOfWork unitOfWork,
        ILogger<FragmentConfidenceScoringJob> logger,
        AiCallContext aiCallContext) : base(unitOfWork, logger)
    {
        _aiProcessingService = aiProcessingService;
        _fragmentRepository = fragmentRepository;
        _sourceRepository = sourceRepository;
        _promptRepository = promptRepository;
        _aiCallContext = aiCallContext;
    }

    /// <summary>
    /// Scores fragments for a specific source
    /// </summary>
    /// <param name="sourceId">The source ID whose fragments need confidence scoring</param>
    [DisableMultipleQueuedItemsFilter]
    [Mission]
    public async Task ExecuteAsync(Guid sourceId, PerformContext context, CancellationToken cancellationToken)
    {
        await ExecuteWithTransactionAsync(async () =>
        {
            LogInfo(context, $"Starting confidence scoring for source {sourceId}");

            var systemPrompt = await BuildSystemPromptAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                LogWarning(context, "Confidence scoring template not configured; skipping job.");
                return;
            }

            var source = await _sourceRepository.Query()
                .Include(s => s.Fragments).ThenInclude(x => x.FragmentCategory)
                .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

            if (source == null)
            {
                LogWarning(context, $"Source {sourceId} not found while scoring confidence");
                return;
            }

            if (string.IsNullOrWhiteSpace(source.Content))
            {
                LogWarning(context, $"Source {sourceId} has no content; skipping confidence scoring");
                return;
            }

            var fragmentsToScore = source.Fragments
                .Where(f => f.Confidence == null)
                .ToList();

            if (fragmentsToScore.Count == 0)
            {
                LogInfo(context, $"Source {sourceId} has no fragments pending confidence scoring");
                return;
            }

            LogInfo(context, $"Scoring {fragmentsToScore.Count} fragments for source {sourceId}");

            var fragmentsList = fragmentsToScore.ToList();
            var userPrompt = BuildJsonUserPrompt(source.Content, fragmentsList);

            // Set AI call context for token tracking
            using (_aiCallContext.SetContext(nameof(FragmentConfidenceScoringJob), nameof(ExecuteAsync), nameof(Source), sourceId))
            {
                var response = await _aiProcessingService.ProcessStructuredPromptAsync<ConfidenceScoringResponse>(
                    userPrompt: userPrompt,
                    systemPrompt: systemPrompt,
                    cancellationToken: cancellationToken);

                if (response?.Scores == null || response.Scores.Count == 0)
                {
                    LogWarning(context, $"Confidence scoring returned no scores for source {sourceId}");
                    return;
                }
                try
                {
                    var scoresById = response.Scores.ToDictionary(s => s.FragmentId, s => s);
                    int scoredCount = 0;

                    for (int i = 0; i < fragmentsList.Count; i++)
                    {
                        var fragment = fragmentsList[i];
                        var fragmentIndex = i + 1; // 1-based index to match prompt

                        if (!scoresById.TryGetValue(fragmentIndex, out var score) || !score.Confidence.HasValue)
                        {
                            LogWarning(context, $"No confidence score returned for fragment {fragment.Id} (index {fragmentIndex})");
                            continue;
                        }

                        fragment.Confidence = score.Confidence.Value;
                        fragment.ConfidenceComment = TrimConfidenceComment(score.ConfidenceComment);
                        fragment.LastModifiedAt = DateTimeOffset.UtcNow;
                        
                        scoredCount++;

                        LogSuccess(context, $"✓ Fragment {fragmentIndex}: {score.Confidence.Value}");

                        LogDebug($"Updated confidence for fragment {fragment.Id} (index {fragmentIndex}): {score.Confidence.Value}");
                    }

                    LogSuccess(context, $"Successfully scored {scoredCount} of {fragmentsToScore.Count} fragments");
                }
            catch (Exception)
            {
                throw;
            }
            }
        });
    }

    private async Task<string?> BuildSystemPromptAsync(CancellationToken cancellationToken)
    {
        var scoringTemplate = await _promptRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == PromptType.ConfidenceScoring, cancellationToken);

        if (scoringTemplate == null)
        {
            LogDebug("Confidence scoring template not found in database.");
            return null;
        }

        var orgContextTemplate = await _promptRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == PromptType.OrganizationContext, cancellationToken);

        var promptObject = new ConfidenceScoringSystemPrompt
        {
            Instructions = scoringTemplate.Content,
            OrganizationContext = orgContextTemplate?.Content
        };

        return JsonSerializer.Serialize(promptObject, JsonOptions);
    }

    private static string BuildJsonUserPrompt(string sourceContent, IEnumerable<Fragment> fragments)
    {
        var fragmentsList = fragments.ToList();
        
        var promptObject = new ConfidenceScoringUserPrompt
        {
            SourceContent = sourceContent,
            Fragments = fragmentsList.Select((f, index) => new FragmentToScore
            {
                Id = index + 1,
                Category = f.FragmentCategory.Name,
                Content = f.Content
            }).ToList()
        };

        return JsonSerializer.Serialize(promptObject, JsonOptions);
    }

    private static string? TrimConfidenceComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var trimmed = comment.Trim();
        return trimmed.Length > 1000 ? trimmed[..1000] : trimmed;
    }
}

public class ConfidenceScoringSystemPrompt
{
    public required string Instructions { get; set; }
    
    public string? OrganizationContext { get; set; }
}

public class ConfidenceScoringUserPrompt
{
    public required string SourceContent { get; set; }
    
    public required List<FragmentToScore> Fragments { get; set; }
}

public class FragmentToScore
{
    public int Id { get; set; }
    
    public required string Category { get; set; }

    public required string Content { get; set; }
}

/// <summary>
/// Response DTO for confidence scoring
/// </summary>
public class ConfidenceScoringResponse
{
    public List<FragmentConfidenceScore> Scores { get; set; } = new();
    
    [Description("Any comments or caveats about the scoring process")]
    public string? Message { get; set; }
}

/// <summary>
/// DTO representing the confidence score for a fragment
/// </summary>
public class FragmentConfidenceScore
{
    [Description("The fragment Id")]
    public int FragmentId { get; set; }
    
    public ConfidenceLevel? Confidence { get; set; }
    
    [MaxLength(1000)]
    [Description("Brief explanation of factors affecting the assigned confidence")]
    public string? ConfidenceComment { get; set; }
}

