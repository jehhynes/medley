using Hangfire;
using Hangfire.Console;
using Hangfire.MissionControl;
using Hangfire.Server;
using Hangfire.Storage;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for generating embeddings for fragments that don't have embeddings yet
/// Uses configurable embedding provider (Ollama or OpenAI) via Microsoft.Extensions.AI
/// </summary>
[MissionLauncher]
public class EmbeddingGenerationJob : BaseHangfireJob<EmbeddingGenerationJob>
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IKnowledgeUnitRepository _knowledgeUnitRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IEmbeddingHelper _embeddingHelper;
    private readonly EmbeddingSettings _embeddingSettings;
    private readonly AiCallContext _aiCallContext;

    private const int BatchSize = 100; // Process up to 100 fragments per run

    public EmbeddingGenerationJob(
        IFragmentRepository fragmentRepository,
        IKnowledgeUnitRepository knowledgeUnitRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IBackgroundJobClient backgroundJobClient,
        IEmbeddingHelper embeddingHelper,
        IOptions<EmbeddingSettings> embeddingSettings,
        IUnitOfWork unitOfWork,
        ILogger<EmbeddingGenerationJob> logger,
        AiCallContext aiCallContext) : base(unitOfWork, logger)
    {
        _fragmentRepository = fragmentRepository;
        _knowledgeUnitRepository = knowledgeUnitRepository;
        _embeddingGenerator = embeddingGenerator;
        _backgroundJobClient = backgroundJobClient;
        _embeddingHelper = embeddingHelper;
        _embeddingSettings = embeddingSettings.Value;
        _aiCallContext = aiCallContext;
    }

    [Mission]
    public async Task GenerateFragmentEmbeddings(PerformContext context, CancellationToken cancellationToken)
    {
        await GenerateFragmentEmbeddings(context, cancellationToken, null, null);
    }

    /// <summary>
    /// Processes fragments that don't have embeddings and generates embeddings for them
    /// </summary>
    /// <param name="sourceId">Optional source ID to filter fragments by specific source</param>
    /// <param name="fragmentId">Optional fragment ID to process a specific fragment</param>
    public async Task GenerateFragmentEmbeddings(PerformContext context, CancellationToken cancellationToken, Guid? sourceId = null, Guid? fragmentId = null)
    {
        bool shouldRequeue = false;

        await ExecuteWithTransactionAsync(async () =>
        {
            var logMessage = fragmentId.HasValue
                ? $"Starting embedding generation job for fragment {fragmentId.Value}"
                : sourceId.HasValue
                    ? $"Starting embedding generation job for source {sourceId.Value}"
                    : "Starting embedding generation job for fragments without embeddings";
            LogInfo(context, logMessage);

            // Query fragments that don't have embeddings
            var query = _fragmentRepository.Query()
                .Where(f => f.Embedding == null);

            // Filter by fragment ID if specified (highest priority)
            if (fragmentId.HasValue)
            {
                query = query.Where(f => f.Id == fragmentId.Value);
            }
            // Filter by source if specified
            else if (sourceId.HasValue)
            {
                query = query.Where(f => f.SourceId == sourceId.Value);
            }

            var fragmentsWithoutEmbeddings = await query
                .OrderBy(f => f.CreatedAt) // Process oldest first
                .Take(BatchSize) // Limit to BatchSize fragments per run to avoid long-running jobs
                .ToListAsync(cancellationToken);

            shouldRequeue = fragmentsWithoutEmbeddings.Count == BatchSize;

            if (fragmentsWithoutEmbeddings.Count == 0)
            {
                LogInfo(context, "No fragments found without embeddings");
                return;
            }

            LogInfo(context, $"Found {fragmentsWithoutEmbeddings.Count} fragments without embeddings. Processing all in a single batch");

            int processedCount = 0;
            int errorCount = 0;

            try
            {
                // Generate embeddings for all fragments at once
                var textsToEmbed = fragmentsWithoutEmbeddings.Select(f => BuildTextForEmbedding(f)).ToList();
                var options = new EmbeddingGenerationOptions
                {
                    Dimensions = _embeddingSettings.Dimensions
                };

                // Set AI call context for token tracking - use first fragment as representative
                using (_aiCallContext.SetContext(nameof(EmbeddingGenerationJob), nameof(GenerateFragmentEmbeddings), nameof(Fragment), fragmentsWithoutEmbeddings.First().Id))
                {
                    var embeddings = await _embeddingGenerator.GenerateAsync(textsToEmbed, options, cancellationToken: cancellationToken);

                    // Update fragments with their embeddings (normalized for cosine similarity)
                    var embeddingList = embeddings.ToList();
                    for (int j = 0; j < fragmentsWithoutEmbeddings.Count && j < embeddingList.Count; j++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var fragment = fragmentsWithoutEmbeddings[j];
                        var embedding = embeddingList[j];

                        // Process embedding (conditionally normalize based on model)
                        var processedVector = _embeddingHelper.ProcessEmbedding(embedding.Vector.ToArray());

                        fragment.Embedding = new Vector(processedVector);

                        
                        processedCount++;

                        LogDebug($"Generated embedding for fragment {fragment.Id} (Title: {fragment.Title ?? "Untitled"}) with {embedding.Vector.Length} dimensions");
                    }
                }
            }
            catch (Exception ex)
            {
                errorCount = fragmentsWithoutEmbeddings.Count;
                LogError(context, ex, "Failed to generate embeddings for fragments");
                throw;
            }

            LogInfo(context, $"Embedding generation job completed. Processed: {processedCount}, Errors: {errorCount}");
        });

        // If we processed exactly BatchSize fragments, there might be more - requeue the job
        // This happens outside the transaction to ensure it only runs after successful commit
        // Don't requeue if processing a specific fragment
        if (shouldRequeue && !fragmentId.HasValue)
        {
            var requeueMessage = sourceId.HasValue
                ? $"Processed exactly {BatchSize} fragments for source {sourceId.Value}. Continuing with next batch"
                : $"Processed exactly {BatchSize} fragments. Continuing with next batch";
            LogInfo(context, requeueMessage);

            var currentJobId = context.BackgroundJob.Id;
            _backgroundJobClient.ContinueJobWith<EmbeddingGenerationJob>(
                currentJobId,
                j => j.GenerateFragmentEmbeddings(default!, default, sourceId, null));
        }
    }

    /// <summary>
    /// Builds the text to embed from fragment properties
    /// </summary>
    private static string BuildTextForEmbedding(Fragment fragment)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(fragment.Title))
        {
            parts.Add(fragment.Title);
        }

        if (!string.IsNullOrWhiteSpace(fragment.Summary))
        {
            parts.Add(fragment.Summary);
        }

        if (!string.IsNullOrWhiteSpace(fragment.Content))
        {
            parts.Add(fragment.Content);
        }

        return string.Join("\n\n", parts);
    }

    [Mission]
    public async Task GenerateKnowledgeUnitEmbeddings(PerformContext context, CancellationToken cancellationToken)
    {
        await GenerateKnowledgeUnitEmbeddings(context, cancellationToken, null);
    }

    /// <summary>
    /// Processes knowledge units that don't have embeddings and generates embeddings for them
    /// </summary>
    /// <param name="context">Hangfire perform context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="knowledgeUnitId">Optional knowledge unit ID to process a specific knowledge unit</param>
    public async Task GenerateKnowledgeUnitEmbeddings(PerformContext context, CancellationToken cancellationToken, Guid? knowledgeUnitId = null)
    {
        bool shouldRequeue = false;

        await ExecuteWithTransactionAsync(async () =>
        {
            var logMessage = knowledgeUnitId.HasValue
                ? $"Starting embedding generation job for knowledge unit {knowledgeUnitId.Value}"
                : "Starting embedding generation job for knowledge units without embeddings";
            LogInfo(context, logMessage);

            // Query knowledge units that don't have embeddings
            var query = _knowledgeUnitRepository.Query()
                .Where(ku => ku.Embedding == null);

            // Filter by knowledge unit ID if specified
            if (knowledgeUnitId.HasValue)
            {
                query = query.Where(ku => ku.Id == knowledgeUnitId.Value);
            }

            var knowledgeUnitsWithoutEmbeddings = await query
                .OrderBy(ku => ku.CreatedAt) // Process oldest first
                .Take(BatchSize) // Limit to BatchSize knowledge units per run to avoid long-running jobs
                .ToListAsync(cancellationToken);

            shouldRequeue = knowledgeUnitsWithoutEmbeddings.Count == BatchSize;

            if (knowledgeUnitsWithoutEmbeddings.Count == 0)
            {
                LogInfo(context, "No knowledge units found without embeddings");
                return;
            }

            LogInfo(context, $"Found {knowledgeUnitsWithoutEmbeddings.Count} knowledge units without embeddings. Processing all in a single batch");

            int processedCount = 0;
            int errorCount = 0;

            try
            {
                // Generate embeddings for all knowledge units at once
                var textsToEmbed = knowledgeUnitsWithoutEmbeddings.Select(ku => BuildTextForEmbedding(ku)).ToList();
                var options = new EmbeddingGenerationOptions
                {
                    Dimensions = _embeddingSettings.Dimensions
                };

                // Set AI call context for token tracking - use first knowledge unit as representative
                using (_aiCallContext.SetContext(nameof(EmbeddingGenerationJob), nameof(GenerateKnowledgeUnitEmbeddings), nameof(KnowledgeUnit), knowledgeUnitsWithoutEmbeddings.First().Id))
                {
                    var embeddings = await _embeddingGenerator.GenerateAsync(textsToEmbed, options, cancellationToken: cancellationToken);

                    // Update knowledge units with their embeddings (normalized for cosine similarity)
                    var embeddingList = embeddings.ToList();
                    for (int j = 0; j < knowledgeUnitsWithoutEmbeddings.Count && j < embeddingList.Count; j++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var knowledgeUnit = knowledgeUnitsWithoutEmbeddings[j];
                        var embedding = embeddingList[j];

                        // Process embedding (conditionally normalize based on model)
                        var processedVector = _embeddingHelper.ProcessEmbedding(embedding.Vector.ToArray());

                        knowledgeUnit.Embedding = new Vector(processedVector);
                        knowledgeUnit.UpdatedAt = DateTimeOffset.UtcNow;

                        processedCount++;

                        LogDebug($"Generated embedding for knowledge unit {knowledgeUnit.Id} (Title: {knowledgeUnit.Title}) with {embedding.Vector.Length} dimensions");
                    }
                }
            }
            catch (Exception ex)
            {
                errorCount = knowledgeUnitsWithoutEmbeddings.Count;
                LogError(context, ex, "Failed to generate embeddings for knowledge units");
                throw;
            }

            LogInfo(context, $"Embedding generation job completed. Processed: {processedCount}, Errors: {errorCount}");
        });

        // If we processed exactly BatchSize knowledge units, there might be more - requeue the job
        // This happens outside the transaction to ensure it only runs after successful commit
        // Don't requeue if processing a specific knowledge unit
        if (shouldRequeue && !knowledgeUnitId.HasValue)
        {
            LogInfo(context, $"Processed exactly {BatchSize} knowledge units. Continuing with next batch");

            var currentJobId = context.BackgroundJob.Id;
            _backgroundJobClient.ContinueJobWith<EmbeddingGenerationJob>(
                currentJobId,
                j => j.GenerateKnowledgeUnitEmbeddings(default!, default, null));
        }
    }

    /// <summary>
    /// Builds the text to embed from knowledge unit properties
    /// </summary>
    private static string BuildTextForEmbedding(KnowledgeUnit knowledgeUnit)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(knowledgeUnit.Title))
        {
            parts.Add(knowledgeUnit.Title);
        }

        if (!string.IsNullOrWhiteSpace(knowledgeUnit.Summary))
        {
            parts.Add(knowledgeUnit.Summary);
        }

        if (!string.IsNullOrWhiteSpace(knowledgeUnit.Content))
        {
            parts.Add(knowledgeUnit.Content);
        }

        return string.Join("\n\n", parts);
    }
}