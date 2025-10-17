using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Infrastructure.Data;
using Medley.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medley.Tests.Infrastructure.Repositories;

[Collection("Database")]
public class FragmentRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ApplicationDbContext _context;
    private readonly IFragmentRepository _repository;

    public FragmentRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _context = _fixture.CreateDbContext();
        _repository = new FragmentRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFragment_WhenExists()
    {
        // Arrange
        var fragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Content = "Test content",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _context.Fragments.AddAsync(fragment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(fragment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fragment.Id, result.Id);
        Assert.Equal(fragment.Content, result.Content);

        // Cleanup
        _context.Fragments.Remove(fragment);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ShouldAddNewFragment()
    {
        // Arrange
        var fragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Content = "New fragment",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _repository.SaveAsync(fragment);

        // Assert
        var saved = await _context.Fragments.FindAsync(fragment.Id);
        Assert.NotNull(saved);
        Assert.Equal(fragment.Content, saved.Content);

        // Cleanup
        _context.Fragments.Remove(fragment);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Query_ShouldReturnAllFragments()
    {
        // Arrange
        var fragments = new[]
        {
            new Fragment { Id = Guid.NewGuid(), Content = "Fragment 1", CreatedAt = DateTimeOffset.UtcNow },
            new Fragment { Id = Guid.NewGuid(), Content = "Fragment 2", CreatedAt = DateTimeOffset.UtcNow },
            new Fragment { Id = Guid.NewGuid(), Content = "Fragment 3", CreatedAt = DateTimeOffset.UtcNow }
        };
        await _context.Fragments.AddRangeAsync(fragments);
        await _context.SaveChangesAsync();

        // Act
        var result = _repository.Query().ToList();

        // Assert
        Assert.True(result.Count >= 3, $"Expected at least 3 fragments, but found {result.Count}");
        Assert.Contains(result, f => f.Content == "Fragment 1");
        Assert.Contains(result, f => f.Content == "Fragment 2");
        Assert.Contains(result, f => f.Content == "Fragment 3");

        // Cleanup
        _context.Fragments.RemoveRange(fragments);
        await _context.SaveChangesAsync();
    }
}
