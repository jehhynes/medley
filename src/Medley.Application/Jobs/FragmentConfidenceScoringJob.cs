using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Hangfire.Server;
using Hangfire.Storage;
using Medley.Application.Interfaces;
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
    private readonly IRepository<Template> _templateRepository;

    public FragmentConfidenceScoringJob(
        IAiProcessingService aiProcessingService,
        IRepository<Fragment> fragmentRepository,
        IRepository<Source> sourceRepository,
        IRepository<Template> templateRepository,
        IUnitOfWork unitOfWork,
        ILogger<FragmentConfidenceScoringJob> logger) : base(unitOfWork, logger)
    {
        _aiProcessingService = aiProcessingService;
        _fragmentRepository = fragmentRepository;
        _sourceRepository = sourceRepository;
        _templateRepository = templateRepository;
    }

    /// <summary>
    /// Scores fragments for a specific source
    /// </summary>
    /// <param name="sourceId">The source ID whose fragments need confidence scoring</param>
    [Mission]
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken, Guid sourceId)
    {
        try
        {
            using var sourceLock = context.Connection.AcquireDistributedLock($"{nameof(FragmentConfidenceScoringJob)}_{sourceId}", TimeSpan.FromSeconds(1));

            await ExecuteWithTransactionAsync(async () =>
            {
                _logger.LogInformation("Starting confidence scoring for source {SourceId}", sourceId);

                var systemPrompt = await BuildSystemPromptAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(systemPrompt))
                {
                    _logger.LogWarning("Confidence scoring template not configured; skipping job.");
                    return;
                }

                var source = await _sourceRepository.Query()
                    .Include(s => s.Fragments)
                    .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

                if (source == null)
                {
                    _logger.LogWarning("Source {SourceId} not found while scoring confidence", sourceId);
                    return;
                }

                if (string.IsNullOrWhiteSpace(source.Content))
                {
                    _logger.LogWarning("Source {SourceId} has no content; skipping confidence scoring", sourceId);
                    return;
                }

                var fragmentsToScore = source.Fragments
                    .Where(f => f.Confidence == null)
                    .ToList();

                if (fragmentsToScore.Count == 0)
                {
                    _logger.LogInformation("Source {SourceId} has no fragments pending confidence scoring", sourceId);
                    return;
                }

                _logger.LogInformation("Scoring {Count} fragments for source {SourceId}", fragmentsToScore.Count, sourceId);

                var userPrompt = BuildUserPrompt(source.Content, fragmentsToScore);

                var response = await _aiProcessingService.ProcessStructuredPromptAsync<ConfidenceScoringResponse>(
                    userPrompt: userPrompt,
                    systemPrompt: systemPrompt,
                    cancellationToken: cancellationToken);

                if (response?.Scores == null || response.Scores.Count == 0)
                {
                    _logger.LogWarning("Confidence scoring returned no scores for source {SourceId}", sourceId);
                    return;
                }

                var scoresById = response.Scores.ToDictionary(s => s.FragmentId, s => s);
                int scoredCount = 0;

                foreach (var fragment in fragmentsToScore)
                {
                    if (!scoresById.TryGetValue(fragment.Id, out var score) || !score.Confidence.HasValue)
                    {
                        _logger.LogWarning("No confidence score returned for fragment {FragmentId}", fragment.Id);
                        continue;
                    }

                    fragment.Confidence = score.Confidence.Value;
                    fragment.ConfidenceComment = TrimConfidenceComment(score.ConfidenceComment);
                    fragment.LastModifiedAt = DateTimeOffset.UtcNow;
                    await _fragmentRepository.SaveAsync(fragment);
                    scoredCount++;

                    _logger.LogDebug("Updated confidence for fragment {FragmentId}: {Confidence}", 
                        fragment.Id, score.Confidence.Value);
                }

                _logger.LogInformation("Successfully scored {ScoredCount} of {TotalCount} fragments for source {SourceId}", 
                    scoredCount, fragmentsToScore.Count, sourceId);
            });
        }
        catch (DistributedLockTimeoutException)
        {
            _logger.LogInformation("Another job is already scoring confidence for source {SourceId}; skipping", sourceId);
        }
    }

    private async Task<string?> BuildSystemPromptAsync(CancellationToken cancellationToken)
    {
        var scoringTemplate = await _templateRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == TemplateType.ConfidenceScoring, cancellationToken);

        if (scoringTemplate == null)
        {
            _logger.LogWarning("Confidence scoring template not found in database.");
            return null;
        }

        var orgContextTemplate = await _templateRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == TemplateType.OrganizationContext, cancellationToken);

        if (orgContextTemplate != null && !string.IsNullOrWhiteSpace(orgContextTemplate.Content))
        {
            return $@"{scoringTemplate.Content}

## Company Context
{orgContextTemplate.Content}";
        }

        return scoringTemplate.Content;
    }

    private static string BuildUserPrompt(string sourceContent, IEnumerable<Fragment> fragments)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("# Source Content");
        sb.AppendLine();
        sb.AppendLine(sourceContent);
        sb.AppendLine();
        sb.AppendLine("# Fragments to Score");
        sb.AppendLine();

        int index = 1;
        foreach (var fragment in fragments)
        {
            sb.AppendLine($"## Fragment {index}");
            sb.AppendLine($"- **ID**: {fragment.Id}");
            sb.AppendLine($"- **Category**: {fragment.Category}");
            sb.AppendLine("- **Content**:");
            sb.AppendLine();
            sb.AppendLine(fragment.Content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            index++;
        }

        return sb.ToString();
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
    public Guid FragmentId { get; set; }
    public ConfidenceLevel? Confidence { get; set; }
    [MaxLength(1000)]
    [Description("Brief explanation of factors affecting the assigned confidence")]
    public string? ConfidenceComment { get; set; }
}

