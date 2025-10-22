using Medley.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Medley.Infrastructure.Data;

/// <summary>
/// Unit of Work implementation using Entity Framework Core
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }
        _transaction = await _context.Database.BeginTransactionAsync(isolationLevel);
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is currently active.");
        }

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is currently active.");
        }

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
        _disposed = true;
    }
}