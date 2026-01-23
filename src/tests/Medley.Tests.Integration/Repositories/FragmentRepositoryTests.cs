using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Infrastructure.Data;
using Medley.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Pgvector;
using Xunit;

namespace Medley.Tests.Integration.Repositories;

public class FragmentRepositoryTests : DatabaseTestBase
{
    protected IFragmentRepository _repository = null!;
    protected FragmentCategory _defaultCategory = null!;

    public FragmentRepositoryTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _repository = new FragmentRepository(_dbContext);

        // Create default fragment category for tests
        _defaultCategory = new FragmentCategory
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            Icon = "bi-test",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _dbContext.Set<FragmentCategory>().AddAsync(_defaultCategory);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFragment_WhenExists()
    {
        // Arrange
        var integration = new Domain.Entities.Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = Domain.Enums.IntegrationType.Fellow };
        await _dbContext.Integrations.AddAsync(integration);
        var source = new Source { Id = Guid.NewGuid(), Type = Domain.Enums.SourceType.Meeting, MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow, Name = "Meeting", Content = "Test content", MetadataJson = "{}", Date = DateTimeOffset.UtcNow, Integration = integration };
        await _dbContext.Sources.AddAsync(source);
        await _dbContext.SaveChangesAsync();

        var fragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Test Title",
            Summary = "Test Summary",
            FragmentCategory = _defaultCategory,
            Content = "Test content",
            CreatedAt = DateTimeOffset.UtcNow,
            Source = source
        };
        await _dbContext.Fragments.AddAsync(fragment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(fragment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fragment.Id, result.Id);
        Assert.Equal(fragment.Content, result.Content);
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
    public async Task AddAsync_ShouldAddNewFragment()
    {
        // Arrange
        var integration = new Domain.Entities.Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = Domain.Enums.IntegrationType.Fellow };
        await _dbContext.Integrations.AddAsync(integration);
        var fragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "New Fragment Title",
            Summary = "New Fragment Summary",
            FragmentCategory = _defaultCategory,
            Content = "New fragment",
            CreatedAt = DateTimeOffset.UtcNow,
            Source = new Source { Id = Guid.NewGuid(), Type = Domain.Enums.SourceType.Meeting, MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow, Name = "Meeting", Content = "Test content", MetadataJson = "{}", Date = DateTimeOffset.UtcNow, Integration = integration }
        };

        // Act
        await _repository.AddAsync(fragment);

        // Assert
        var saved = await _dbContext.Fragments.FindAsync(fragment.Id);
        Assert.NotNull(saved);
        Assert.Equal(fragment.Content, saved.Content);
    }

    [Fact]
    public async Task Query_ShouldReturnAllFragments()
    {
        // Arrange
        var integration = new Domain.Entities.Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = Domain.Enums.IntegrationType.Fellow };
        await _dbContext.Integrations.AddAsync(integration);
        var source = new Source { Id = Guid.NewGuid(), Type = Domain.Enums.SourceType.Meeting, MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow, Name = "Meeting", Content = "Test content", MetadataJson = "{}", Date = DateTimeOffset.UtcNow, Integration = integration };
        await _dbContext.Sources.AddAsync(source);
        await _dbContext.SaveChangesAsync();

        var fragments = new[]
        {
            new Fragment { Id = Guid.NewGuid(), Title = "Title 1", Summary = "Summary 1", FragmentCategory = _defaultCategory, Content = "Fragment 1", CreatedAt = DateTimeOffset.UtcNow, Source = source },
            new Fragment { Id = Guid.NewGuid(), Title = "Title 2", Summary = "Summary 2", FragmentCategory = _defaultCategory, Content = "Fragment 2", CreatedAt = DateTimeOffset.UtcNow, Source = source },
            new Fragment { Id = Guid.NewGuid(), Title = "Title 3", Summary = "Summary 3", FragmentCategory = _defaultCategory, Content = "Fragment 3", CreatedAt = DateTimeOffset.UtcNow, Source = source }
        };
        await _dbContext.Fragments.AddRangeAsync(fragments);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = _repository.Query().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => f.Content == "Fragment 1");
        Assert.Contains(result, f => f.Content == "Fragment 2");
        Assert.Contains(result, f => f.Content == "Fragment 3");
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldReturnSimilarFragments_OrderedByDistance()
    {
        // Arrange - Create test fragments with embeddings
        var baseEmbedding = CreateTestEmbedding(1.0f, 0.0f);
        var similarEmbedding = CreateTestEmbedding(0.9f, 0.1f);
        var differentEmbedding = CreateTestEmbedding(0.0f, 1.0f);

        var integration = new Domain.Entities.Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = Domain.Enums.IntegrationType.Fellow };
        await _dbContext.Integrations.AddAsync(integration);
        var source = new Source { Id = Guid.NewGuid(), Type = Domain.Enums.SourceType.Meeting, MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow, Name = "Meeting", Content = "Test content", MetadataJson = "{}", Date = DateTimeOffset.UtcNow, Integration = integration };
        await _dbContext.Sources.AddAsync(source);
        await _dbContext.SaveChangesAsync();

        var fragments = new[]
        {
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Base Fragment",
                Summary = "Base Summary",
                FragmentCategory = _defaultCategory,
                Content = "Base fragment",
                Embedding = new Vector(baseEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                Source = source
            },
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Similar Fragment",
                Summary = "Similar Summary",
                FragmentCategory = _defaultCategory,
                Content = "Similar fragment",
                Embedding = new Vector(similarEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                Source = source
            },
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Different Fragment",
                Summary = "Different Summary",
                FragmentCategory = _defaultCategory,
                Content = "Different fragment",
                Embedding = new Vector(differentEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                Source = source
            }
        };

        await _dbContext.Fragments.AddRangeAsync(fragments);
        await _dbContext.SaveChangesAsync();

        // Act - Find similar fragments to base embedding
        var results = await _repository.FindSimilarAsync(baseEmbedding, 2);
        var resultList = results.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.Equal("Base fragment", resultList[0].Fragment.Content); // Most similar (identical)
        Assert.Equal("Similar fragment", resultList[1].Fragment.Content); // Second most similar
        Assert.True(resultList[0].Distance <= resultList[1].Distance); // Ordered by distance
    }

    [Fact]
    public async Task FindSimilarAsync_WithMinSimilarity_ShouldFilterBySimilarityScore()
    {
        // Arrange
        var queryEmbedding = CreateTestEmbedding(1.0f, 0.0f);
        var similarEmbedding = CreateTestEmbedding(0.95f, 0.05f);
        var differentEmbedding = CreateTestEmbedding(0.0f, 1.0f);

        var integration = new Domain.Entities.Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = Domain.Enums.IntegrationType.Fellow };
        await _dbContext.Integrations.AddAsync(integration);
        var source = new Source { Id = Guid.NewGuid(), Type = Domain.Enums.SourceType.Meeting, MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow, Name = "Meeting", Content = "Test content", MetadataJson = "{}", Date = DateTimeOffset.UtcNow, Integration = integration };
        await _dbContext.Sources.AddAsync(source);
        await _dbContext.SaveChangesAsync();

        var fragments = new[]
        {
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Similar Fragment",
                Summary = "Similar Summary",
                FragmentCategory = _defaultCategory,
                Content = "Similar fragment",
                Embedding = new Vector(similarEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                Source = source
            },
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Different Fragment",
                Summary = "Different Summary",
                FragmentCategory = _defaultCategory,
                Content = "Different fragment",
                Embedding = new Vector(differentEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                Source = source
            }
        };

        await _dbContext.Fragments.AddRangeAsync(fragments);
        await _dbContext.SaveChangesAsync();

        // Act - Find similar with minimum similarity threshold
        // Similarity score ranges from 0 (opposite) to 1 (identical)
        // The similar embedding should have similarity > 0.75, different should be ~0.3
        var results = await _repository.FindSimilarAsync(queryEmbedding, 10, 0.75);
        var resultList = results.ToList();

        // Assert - Should only return the similar fragment, not the different one
        Assert.Single(resultList);
        Assert.Equal("Similar fragment", resultList[0].Fragment.Content);
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldHandleEmptyDatabase()
    {
        // Arrange
        var queryEmbedding = CreateTestEmbedding(1.0f, 0.0f);

        // Act
        var results = await _repository.FindSimilarAsync(queryEmbedding, 10);
        var resultList = results.ToList();

        // Assert
        Assert.Empty(resultList);
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldIgnoreFragmentsWithoutEmbeddings()
    {
        // Arrange
        var queryEmbedding = CreateTestEmbedding(1.0f, 0.0f);
        var integration = new Domain.Entities.Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = Domain.Enums.IntegrationType.Fellow };
        await _dbContext.Integrations.AddAsync(integration);
        var source = new Source { Id = Guid.NewGuid(), Type = Domain.Enums.SourceType.Meeting, MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow, Name = "Meeting", Content = "Test content", MetadataJson = "{}", Date = DateTimeOffset.UtcNow, Integration = integration };
        await _dbContext.Sources.AddAsync(source);

        var fragments = new[]
        {
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Fragment With Embedding",
                Summary = "Fragment Summary",
                FragmentCategory = _defaultCategory,
                Content = "Fragment with embedding",
                Embedding = new Vector(queryEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                Source = source
            },
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Fragment Without Embedding",
                Summary = "Fragment Summary",
                FragmentCategory = _defaultCategory,
                Content = "Fragment without embedding",
                Embedding = null,
                CreatedAt = DateTimeOffset.UtcNow,
                Source = source
            }
        };

        await _dbContext.Fragments.AddRangeAsync(fragments);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _repository.FindSimilarAsync(queryEmbedding, 10);
        var resultList = results.ToList();

        // Assert
        Assert.Single(resultList);
        Assert.Equal("Fragment with embedding", resultList[0].Fragment.Content);
    }

    /// <summary>
    /// Creates a test embedding vector with specified values
    /// </summary>
    private static float[] CreateTestEmbedding(float primaryValue, float secondaryValue)
    {
        var embedding = new float[2000];
        for (int i = 0; i < 2000; i++)
        {
            embedding[i] = i % 2 == 0 ? primaryValue : secondaryValue;
        }
        return embedding;
    }
}
