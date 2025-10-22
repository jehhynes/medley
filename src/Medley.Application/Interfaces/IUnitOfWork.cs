using System.Data;

namespace Medley.Application.Interfaces;

/// <summary>
/// Unit of Work interface for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all changes made in this unit of work to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Begins a new database transaction with the specified isolation level
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction</param>
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync();
}