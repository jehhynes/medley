using System.Diagnostics;
using System.Runtime.CompilerServices;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Medley.Application.Middleware;

/// <summary>
/// Delegating ChatClient middleware that tracks token usage for all AI chat calls
/// </summary>
public class TokenTrackingChatClientMiddleware : DelegatingChatClient
{
    private readonly AiCallContext _aiCallContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TokenTrackingChatClientMiddleware> _logger;
    private readonly string _modelName;

    public TokenTrackingChatClientMiddleware(
        IChatClient innerClient,
        AiCallContext aiCallContext,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TokenTrackingChatClientMiddleware> logger,
        string modelName) : base(innerClient)
    {
        _aiCallContext = aiCallContext;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _modelName = modelName;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        ChatResponse? response = null;
        Exception? exception = null;

        try
        {
            response = await base.GetResponseAsync(messages, options, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await TrackUsageAsync(response, stopwatch.ElapsedMilliseconds, exception);
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var inputTokens = 0;
        var outputTokens = 0;
        Exception? exception = null;

        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        try
        {
            enumerator = base.GetStreamingResponseAsync(messages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                ChatResponseUpdate update;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                    update = enumerator.Current;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    throw;
                }

                // Accumulate token counts as they come in
                if (update.Contents != null)
                {
                    foreach (var content in update.Contents)
                    {
                        if (content is UsageContent usageContent)
                        {
                            if (usageContent.Details?.InputTokenCount.HasValue == true)
                            {
                                inputTokens = (int)usageContent.Details.InputTokenCount.Value;
                            }
                            if (usageContent.Details?.OutputTokenCount.HasValue == true)
                            {
                                outputTokens = (int)usageContent.Details.OutputTokenCount.Value;
                            }
                        }
                    }
                }

                yield return update;
            }
        }
        finally
        {
            if (enumerator != null)
            {
                await enumerator.DisposeAsync();
            }

            stopwatch.Stop();

            // Track usage for streaming (we accumulated tokens during streaming)
            await TrackStreamingUsageAsync(inputTokens, outputTokens, stopwatch.ElapsedMilliseconds, exception);
        }
    }

    private async Task TrackUsageAsync(ChatResponse? response, long durationMs, Exception? exception)
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
                _logger.LogWarning("AI call made without context set. Service will be logged as 'Unknown'");
            }
            #endif

            var usage = new AiTokenUsage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                DurationMs = durationMs,
                InputTokens = response?.Usage?.InputTokenCount != null ? (int?)response.Usage.InputTokenCount.Value : null,
                OutputTokens = response?.Usage?.OutputTokenCount != null ? (int?)response.Usage.OutputTokenCount.Value : null,
                ModelName = _modelName,
                ServiceName = _aiCallContext.ServiceName ?? "Unknown",
                OperationName = _aiCallContext.OperationName ?? "Unknown",
                CallStack = _aiCallContext.CallStack,
                RelatedEntityType = _aiCallContext.EntityType,
                RelatedEntityId = _aiCallContext.EntityId,
                IsSuccess = exception == null,
                ErrorMessage = exception?.Message
            };

            // Begin transaction, save, commit immediately
            await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await tokenUsageRepository.SaveAsync(usage);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            _logger.LogDebug(
                "Tracked AI token usage: Service={Service}, Operation={Operation}, CallStack={CallStack}, Entity={EntityType}:{EntityId}, Input={InputTokens}, Output={OutputTokens}, Duration={Duration}ms",
                usage.ServiceName, usage.OperationName, usage.CallStack, usage.RelatedEntityType, usage.RelatedEntityId, usage.InputTokens, usage.OutputTokens, durationMs);
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

            // Don't let tracking failures break the AI call
            _logger.LogError(ex, "Failed to track token usage");
        }
    }

    private async Task TrackStreamingUsageAsync(int inputTokens, int outputTokens, long durationMs, Exception? exception)
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
                _logger.LogWarning("AI streaming call made without context set. Service will be logged as 'Unknown'");
            }
            #endif

            var usage = new AiTokenUsage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                DurationMs = durationMs,
                InputTokens = inputTokens > 0 ? inputTokens : null,
                OutputTokens = outputTokens > 0 ? outputTokens : null,
                ModelName = _modelName,
                ServiceName = _aiCallContext.ServiceName ?? "Unknown",
                OperationName = _aiCallContext.OperationName ?? "Unknown",
                CallStack = _aiCallContext.CallStack,
                RelatedEntityType = _aiCallContext.EntityType,
                RelatedEntityId = _aiCallContext.EntityId,
                IsSuccess = exception == null,
                ErrorMessage = exception?.Message
            };

            // Begin transaction, save, commit immediately
            await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await tokenUsageRepository.SaveAsync(usage);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            _logger.LogDebug(
                "Tracked AI streaming token usage: Service={Service}, Operation={Operation}, CallStack={CallStack}, Entity={EntityType}:{EntityId}, Input={InputTokens}, Output={OutputTokens}, Duration={Duration}ms",
                usage.ServiceName, usage.OperationName, usage.CallStack, usage.RelatedEntityType, usage.RelatedEntityId, inputTokens, outputTokens, durationMs);
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

            // Don't let tracking failures break the AI call
            _logger.LogError(ex, "Failed to track streaming token usage");
        }
    }
}
