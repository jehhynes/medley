using Hangfire;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for generating embeddings for fragments that don't have embeddings yet
/// Uses Ollama (qwen3-embedding:4b) via Microsoft.Extensions.AI
/// </summary>
public class EmbeddingGenerationJob : BaseHangfireJob<EmbeddingGenerationJob>
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IBackgroundJobService _backgroundJobService;
    
    private const int BatchSize = 100; // Process up to 100 fragments per run

    public EmbeddingGenerationJob(
        IFragmentRepository fragmentRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IBackgroundJobService backgroundJobService,
        IUnitOfWork unitOfWork,
        ILogger<EmbeddingGenerationJob> logger) : base(unitOfWork, logger)
    {
        _fragmentRepository = fragmentRepository;
        _embeddingGenerator = embeddingGenerator;
        _backgroundJobService = backgroundJobService;
    }

    /// <summary>
    /// Processes fragments that don't have embeddings and generates embeddings for them
    /// </summary>
    [DisableMultipleQueuedItemsFilter]
    //[DisableConcurrentExecution(timeoutInSeconds: 1)]
    public async Task GenerateFragmentEmbeddings(CancellationToken cancellationToken)
    {
        int fragmentsProcessed = 0;
        
        await ExecuteWithTransactionAsync(async () =>
        {
            _logger.LogInformation("Starting embedding generation job for fragments without embeddings");

            // Query fragments that don't have embeddings
            var fragmentsWithoutEmbeddings = await _fragmentRepository.Query()
                .Where(f => f.Embedding == null)
                .OrderBy(f => f.CreatedAt) // Process oldest first
                .Take(BatchSize) // Limit to BatchSize fragments per run to avoid long-running jobs
                .ToListAsync();

            if (fragmentsWithoutEmbeddings.Count == 0)
            {
                _logger.LogInformation("No fragments found without embeddings");
                fragmentsProcessed = 0;
                return;
            }

            _logger.LogInformation("Found {Count} fragments without embeddings. Processing all in a single batch",
                fragmentsWithoutEmbeddings.Count);

            int processedCount = 0;
            int errorCount = 0;

            try
            {
                // Generate embeddings for all fragments at once
                var textsToEmbed = fragmentsWithoutEmbeddings.Select(f => BuildTextForEmbedding(f)).ToList();
                var options = new EmbeddingGenerationOptions()
                {
                    Dimensions = 2000
                };
                
                var embeddings = await _embeddingGenerator.GenerateAsync(textsToEmbed, options, cancellationToken: cancellationToken);

                // Update fragments with their embeddings
                var embeddingList = embeddings.ToList();
                for (int j = 0; j < fragmentsWithoutEmbeddings.Count && j < embeddingList.Count; j++)
                {
                    var fragment = fragmentsWithoutEmbeddings[j];
                    var embedding = embeddingList[j];
                    fragment.Embedding = new Vector(embedding.Vector);
                    await _fragmentRepository.SaveAsync(fragment);
                    processedCount++;
                    
                    _logger.LogDebug("Generated embedding for fragment {FragmentId} (Title: {Title}) with {Dimensions} dimensions",
                        fragment.Id, fragment.Title ?? "Untitled", embedding.Vector.Length);
                }

                // Save all changes
                //await _unitOfWork.SaveChangesAsync();
                
                fragmentsProcessed = fragmentsWithoutEmbeddings.Count;
            }
            catch (Exception ex)
            {
                errorCount = fragmentsWithoutEmbeddings.Count;
                _logger.LogError(ex, "Failed to generate embeddings for fragments");
                throw;
            }

            _logger.LogInformation("Embedding generation job completed. Processed: {ProcessedCount}, Errors: {ErrorCount}",
                processedCount, errorCount);
        });

        // If we processed exactly BatchSize fragments, there might be more - requeue the job
        // This happens outside the transaction to ensure it only runs after successful commit
        if (fragmentsProcessed == BatchSize)
        {
            _logger.LogInformation("Processed exactly {BatchSize} fragments. Requeuing job to process remaining fragments", BatchSize);
            _backgroundJobService.Schedule<EmbeddingGenerationJob>(j => j.GenerateFragmentEmbeddings(default), TimeSpan.FromSeconds(5));
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
}

