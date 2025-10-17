using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Infrastructure.Data;
using Medley.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Medley.Tests.Integration;

[Collection("Database")]
public abstract class DatabaseTestBase : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    protected readonly DatabaseFixture _fixture;
    protected ApplicationDbContext _dbContext = null!;
    protected IDbContextTransaction _transaction = null!;

    public DatabaseTestBase(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        
        // Start a transaction for test isolation
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        // Rollback transaction to undo all changes made during the test
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}
