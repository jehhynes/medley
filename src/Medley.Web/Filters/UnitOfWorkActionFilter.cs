using Medley.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data;

namespace Medley.Web.Filters;

/// <summary>
/// Action filter that manages database transactions for each request
/// </summary>
public class UnitOfWorkActionFilter : IAsyncActionFilter
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnitOfWorkActionFilter> _logger;

    public UnitOfWorkActionFilter(IUnitOfWork unitOfWork, ILogger<UnitOfWorkActionFilter> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var isModifyingRequest = IsModifyingHttpMethod(context.HttpContext.Request.Method);
        
        // Begin transaction with appropriate isolation level
        var isolationLevel = isModifyingRequest 
            ? IsolationLevel.ReadCommitted 
            : IsolationLevel.Snapshot;
            
        await _unitOfWork.BeginTransactionAsync(isolationLevel);
        
        _logger.LogDebug("Transaction started with {IsolationLevel} isolation for action {ActionName}", 
            isolationLevel, context.ActionDescriptor.DisplayName);

        try
        {
            // Execute the action
            var result = await next();

            if (result.Exception == null)
            {
                if (isModifyingRequest)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                    
                    _logger.LogDebug("Transaction committed successfully for action {ActionName}", 
                        context.ActionDescriptor.DisplayName);
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    
                    _logger.LogDebug("Transaction rolled back for read-only action {ActionName}", 
                        context.ActionDescriptor.DisplayName);
                }
            }
            else
            {
                // If there was an exception, rollback the transaction
                await _unitOfWork.RollbackTransactionAsync();
                
                _logger.LogWarning(result.Exception, 
                    "Transaction rolled back due to exception in action {ActionName}", 
                    context.ActionDescriptor.DisplayName);
            }
        }
        catch (Exception ex)
        {
            // If an exception occurs during transaction management, rollback
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
                
                _logger.LogError(ex, 
                    "Transaction rolled back due to exception during transaction management for action {ActionName}", 
                    context.ActionDescriptor.DisplayName);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, 
                    "Failed to rollback transaction for action {ActionName}", 
                    context.ActionDescriptor.DisplayName);
            }

            // Re-throw the original exception so error handling works correctly
            throw;
        }
    }

    private static bool IsModifyingHttpMethod(string method)
    {
        return method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("DELETE", StringComparison.OrdinalIgnoreCase);
    }
}
