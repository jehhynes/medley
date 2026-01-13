using System.Diagnostics;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Medley.Application.Services;

/// <summary>
/// Delegating EmbeddingGenerator that tracks token usage for embedding generation calls
/// </summary>
public class TokenTrackingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly AiCallContext _aiCallContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TokenTrackingEmbeddingGenerator> _logger;
    private readonly string _modelName;

    public TokenTrackingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        AiCallContext aiCallContext,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TokenTrackingEmbeddingGenerator> logger,
        string modelName) : base(innerGenerator)
    {
        _aiCallContext = aiCallContext;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _modelName = modelName;
    }

    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        GeneratedEmbeddings<Embedding<float>>? result = null;
        Exception? exception = null;

        try
        {
            result = await base.GenerateAsync(values, options, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await TrackUsageAsync(result, stopwatch.ElapsedMilliseconds, exception);
        }
    }

    private async Task TrackUsageAsync(
        GeneratedEmbeddings<Embedding<float>>? result,
        long durationMs,
        Exception? exception)
    {
        // Create a separate service scope for isolated unit of work
        using var scope = _serviceScopeFactory.CreateScope();
        var tokenUsageRepository = scope.ServiceProvider.GetRequiredService<IRepository<AiTokenUsage>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // Validate context is set (throws in debug, warns in release)
            #if DEBUG
            _aiCallContext.EnsureContextSet();
            #else
            if (!_aiCallContext.HasContext)
            {
                _logger.LogWarning("Embedding generation call made without context set. Service will be logged as 'Unknown'");
            }
            #endif

            // Calculate embedding tokens from the result
            // For embeddings, token count is typically based on input text length
            // or available in usage metadata if provided by the generator
            int? embeddingTokens = null;

            if (result?.Usage != null)
            {
                // Some providers include total token count in usage
                embeddingTokens = result.Usage.InputTokenCount != null ? (int?)result.Usage.InputTokenCount.Value : 
                                 (result.Usage.TotalTokenCount != null ? (int?)result.Usage.TotalTokenCount.Value : null);
            }

            var usage = new AiTokenUsage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                DurationMs = durationMs,
                EmbeddingTokens = embeddingTokens,
                ModelName = _modelName,
                ServiceName = _aiCallContext.ServiceName ?? "Unknown",
                OperationName = _aiCallContext.OperationName,
                CallStack = _aiCallContext.CallStack,
                RelatedEntityType = _aiCallContext.EntityType,
                RelatedEntityId = _aiCallContext.EntityId,
                IsSuccess = exception == null,
                ErrorMessage = exception?.Message
            };

            // Begin transaction, save, commit immediately
            await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await tokenUsageRepository.AddAsync(usage);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            _logger.LogDebug(
                "Tracked embedding token usage: Service={Service}, Operation={Operation}, CallStack={CallStack}, Entity={EntityType}:{EntityId}, Tokens={EmbeddingTokens}, Duration={Duration}ms",
                usage.ServiceName, usage.OperationName, usage.CallStack, usage.RelatedEntityType, usage.RelatedEntityId, embeddingTokens, durationMs);
        }
        catch (Exception ex)
        {
            // Rollback transaction if it was started
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch
            {
                // Ignore rollback errors
            }

            // Don't let tracking failures break the embedding call
            _logger.LogError(ex, "Failed to track embedding token usage");
        }
    }
}
