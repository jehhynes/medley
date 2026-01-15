using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Infrastructure.Data;
using Xunit;

namespace Medley.Tests.Integration.Data;

public class UnitOfWorkTests : IClassFixture<UnitOfWorkDatabaseFixture>
{
    private readonly UnitOfWorkDatabaseFixture _fixture;

    public UnitOfWorkTests(UnitOfWorkDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChangesAsync_WithChanges_ReturnsNumberOfChanges()
    {
        // Arrange - Each test gets its own isolated database
        using var dbContext = await _fixture.CreateIsolatedDbContextAsync();
        using var unitOfWork = new UnitOfWork(dbContext);
        
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            UserName = "testuser",
            Email = "testuser@test.com",
            FullName = "Test User"
        };
        dbContext.Users.Add(user);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
        
        // Verify the user was actually saved
        var savedUser = await dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("testuser", savedUser.UserName);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        // Arrange - Each test gets its own isolated database
        using var dbContext = await _fixture.CreateIsolatedDbContextAsync();
        using var unitOfWork = new UnitOfWork(dbContext);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleChanges_ReturnsCorrectCount()
    {
        // Arrange - Each test gets its own isolated database
        using var dbContext = await _fixture.CreateIsolatedDbContextAsync();
        using var unitOfWork = new UnitOfWork(dbContext);
        
        var user1 = new User 
        { 
            Id = Guid.NewGuid(), 
            UserName = "user1",
            Email = "user1@test.com",
            FullName = "User One"
        };
        var user2 = new User 
        { 
            Id = Guid.NewGuid(), 
            UserName = "user2",
            Email = "user2@test.com",
            FullName = "User Two"
        };
        
        dbContext.Users.AddRange(user1, user2);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(2, result);
        
        // Verify both users were saved
        var savedUser1 = await dbContext.Users.FindAsync(user1.Id);
        var savedUser2 = await dbContext.Users.FindAsync(user2.Id);
        Assert.NotNull(savedUser1);
        Assert.NotNull(savedUser2);
    }

    [Fact]
    public async Task Dispose_DoesNotThrow()
    {
        // Arrange - Each test gets its own isolated database
        using var dbContext = await _fixture.CreateIsolatedDbContextAsync();
        using var unitOfWork = new UnitOfWork(dbContext);

        // Act & Assert
        var exception = Record.Exception(() => unitOfWork.Dispose());
        Assert.Null(exception);
    }
}