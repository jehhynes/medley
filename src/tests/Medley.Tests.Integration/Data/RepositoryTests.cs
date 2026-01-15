using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Medley.Tests.Integration.Data;

public class RepositoryTests : DatabaseTestBase
{
    private IRepository<User> _repository = null!;
    public RepositoryTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _repository = new Repository<User>(_dbContext);
    }

    [Fact]
    public async Task Query_ReturnsQueryableCollection()
    {
        // Arrange - Transaction isolation handles cleanup automatically
        var user1 = new User { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@test.com", FullName = "User One" };
        var user2 = new User { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@test.com", FullName = "User Two" };
        
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
    public async Task AddAsync_WithNewEntity_AddsToDatabase()
    {
        // Arrange - Transaction isolation handles cleanup automatically
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "newuser", Email = "newuser@test.com", FullName = "New User" };

        // Act
        await _repository.AddAsync(user);

        // Assert
        var savedUser = await _dbContext.Users.FindAsync(userId);
        Assert.NotNull(savedUser);
        Assert.Equal("newuser", savedUser.UserName);
        Assert.Equal("newuser@test.com", savedUser.Email);
    }

    [Fact]
    public async Task AddAsync_WithExistingEntity_UpdatesDatabase()
    {
        // Arrange - Transaction isolation handles cleanup automatically
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "originaluser", Email = "original@test.com", FullName = "Original User" };
        
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act - Update the user through the repository
        user.UserName = "updateduser";
        user.Email = "updated@test.com";
        await _repository.AddAsync(user);

        // Assert
        var savedUser = await _dbContext.Users.FindAsync(userId);
        Assert.NotNull(savedUser);
        Assert.Equal("updateduser", savedUser.UserName);
        Assert.Equal("updated@test.com", savedUser.Email);
    }
}