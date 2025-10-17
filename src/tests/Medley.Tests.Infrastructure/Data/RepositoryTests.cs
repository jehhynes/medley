using Medley.Application.Interfaces;
using Medley.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Medley.Tests.Infrastructure.Data;

public class RepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private ApplicationDbContext _dbContext = null!;
    private IDbContextTransaction _transaction = null!;
    private IRepository<IdentityUser> _repository = null!;

    public RepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        _repository = new Repository<IdentityUser>(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task Query_ReturnsQueryableCollection()
    {
        // Arrange - Transaction isolation handles cleanup automatically
        var user1 = new IdentityUser { Id = Guid.NewGuid().ToString(), UserName = "user1", Email = "user1@test.com" };
        var user2 = new IdentityUser { Id = Guid.NewGuid().ToString(), UserName = "user2", Email = "user2@test.com" };
        
        _dbContext.Users.AddRange(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        var query = _repository.Query();
        var users = query.ToList();

        // Assert
        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.UserName == "user1");
        Assert.Contains(users, u => u.UserName == "user2");
    }

    [Fact]
    public async Task SaveAsync_WithNewEntity_AddsToDatabase()
    {
        // Arrange - Transaction isolation handles cleanup automatically
        var userId = Guid.NewGuid().ToString();
        var user = new IdentityUser { Id = userId, UserName = "newuser", Email = "newuser@test.com" };

        // Act
        await _repository.SaveAsync(user);

        // Assert
        var savedUser = await _dbContext.Users.FindAsync(userId);
        Assert.NotNull(savedUser);
        Assert.Equal("newuser", savedUser.UserName);
        Assert.Equal("newuser@test.com", savedUser.Email);
    }

    [Fact]
    public async Task SaveAsync_WithExistingEntity_UpdatesDatabase()
    {
        // Arrange - Transaction isolation handles cleanup automatically
        var userId = Guid.NewGuid().ToString();
        var user = new IdentityUser { Id = userId, UserName = "originaluser", Email = "original@test.com" };
        
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act - Update the user through the repository
        user.UserName = "updateduser";
        user.Email = "updated@test.com";
        await _repository.SaveAsync(user);

        // Assert
        var savedUser = await _dbContext.Users.FindAsync(userId);
        Assert.NotNull(savedUser);
        Assert.Equal("updateduser", savedUser.UserName);
        Assert.Equal("updated@test.com", savedUser.Email);
    }
}