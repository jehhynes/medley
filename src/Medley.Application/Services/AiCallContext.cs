namespace Medley.Application.Services;

/// <summary>
/// Scoped service that holds context information for AI calls
/// Used to track which service/operation is making an AI call for token usage tracking
/// Supports nested call tracking with automatic call stack management
/// </summary>
public class AiCallContext
{
    /// <summary>
    /// Name of the current service making the AI call (e.g., "FragmentExtractionService")
    /// </summary>
    public string? ServiceName { get; private set; }

    /// <summary>
    /// Name of the current operation/method making the AI call (e.g., "ExtractFragmentsAsync")
    /// </summary>
    public string? OperationName { get; private set; }

    /// <summary>
    /// Type of the related entity (e.g., "Fragment", "Source", "Conversation")
    /// </summary>
    public string? EntityType { get; private set; }

    /// <summary>
    /// ID of the related entity
    /// </summary>
    public Guid? EntityId { get; private set; }

    /// <summary>
    /// Stack of previous contexts (Service:Operation pairs) for nested calls
    /// </summary>
    private readonly Stack<ContextFrame> _callStack = new();

    /// <summary>
    /// Gets the call stack as a newline-separated string of Service:Operation pairs
    /// Returns null if the stack is empty
    /// </summary>
    public string? CallStack => _callStack.Count > 0 
        ? string.Join("\n", _callStack.Reverse().Select(f => $"{f.ServiceName}:{f.OperationName}"))
        : null;

    /// <summary>
    /// Sets the context for the current AI call and returns a disposable that restores the previous context
    /// Use with 'using' statement for automatic cleanup
    /// </summary>
    /// <param name="serviceName">Name of the service (use nameof)</param>
    /// <param name="operationName">Name of the operation/method (use nameof)</param>
    /// <param name="entityType">Type of the related entity (optional, use nameof)</param>
    /// <param name="entityId">ID of the related entity (optional)</param>
    /// <returns>Disposable that restores the previous context when disposed</returns>
    /// <example>
    /// <code>
    /// using (_aiCallContext.SetContext(nameof(FragmentExtractionService), nameof(ExtractFragmentsAsync), nameof(Source), sourceId))
    /// {
    ///     // AI calls here will be tracked with this context
    ///     await _aiService.ProcessPromptAsync(...);
    /// }
    /// </code>
    /// </example>
    public IDisposable SetContext(string serviceName, string operationName, string? entityType = null, Guid? entityId = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("ServiceName cannot be null or empty", nameof(serviceName));
        
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("OperationName cannot be null or empty", nameof(operationName));

        // Push current context onto the stack (if set)
        if (!string.IsNullOrWhiteSpace(ServiceName))
        {
            _callStack.Push(new ContextFrame(ServiceName, OperationName, EntityType, EntityId));
        }

        // Set new context
        ServiceName = serviceName;
        OperationName = operationName;
        EntityType = entityType;
        EntityId = entityId;

        return new ContextScope(this);
    }

    /// <summary>
    /// Validates that context has been set. Throws if not.
    /// </summary>
    public void EnsureContextSet()
    {
        if (string.IsNullOrWhiteSpace(ServiceName))
        {
            throw new InvalidOperationException(
                "AiCallContext has not been set. " +
                "Use: using (_aiCallContext.SetContext(nameof(MyService), nameof(MyMethod), ...))");
        }
    }

    /// <summary>
    /// Returns true if context has been set
    /// </summary>
    public bool HasContext => !string.IsNullOrWhiteSpace(ServiceName);

    /// <summary>
    /// Restores the previous context from the stack
    /// </summary>
    private void PopContext()
    {
        if (_callStack.Count > 0)
        {
            var previousFrame = _callStack.Pop();
            ServiceName = previousFrame.ServiceName;
            OperationName = previousFrame.OperationName;
            EntityType = previousFrame.EntityType;
            EntityId = previousFrame.EntityId;
        }
        else
        {
            // No previous context, clear everything
            ServiceName = null;
            OperationName = null;
            EntityType = null;
            EntityId = null;
        }
    }

    /// <summary>
    /// Represents a saved context frame in the call stack
    /// </summary>
    private record ContextFrame(string ServiceName, string? OperationName, string? EntityType, Guid? EntityId);

    /// <summary>
    /// Disposable scope that restores the previous context when disposed
    /// </summary>
    private class ContextScope : IDisposable
    {
        private readonly AiCallContext _context;
        private bool _disposed = false;
        
        public ContextScope(AiCallContext context)
        {
            _context = context;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _context.PopContext();
                _disposed = true;
            }
        }
    }
}
