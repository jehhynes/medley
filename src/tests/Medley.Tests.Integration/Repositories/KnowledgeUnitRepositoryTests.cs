using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Xunit;

namespace Medley.Tests.Integration.Repositories;

public class KnowledgeUnitRepositoryTests : DatabaseTestBase
{
    protected IKnowledgeUnitRepository _repository = null!;
    protected FragmentCategory _defaultCategory = null!;

    public KnowledgeUnitRepositoryTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _repository = new KnowledgeUnitRepository(_dbContext);

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
    public async Task GetByIdAsync_ShouldReturnKnowledgeUnit_WhenExists()
    {
        // Arrange
        var knowledgeUnit = new KnowledgeUnit
        {
            Id = Guid.NewGuid(),
            Title = "Test Knowledge Unit",
            Summary = "Test Summary",
            Content = "Test content for knowledge unit",
            Confidence = ConfidenceLevel.High,
            Category = _defaultCategory,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _dbContext.KnowledgeUnits.AddAsync(knowledgeUnit);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(knowledgeUnit.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(knowledgeUnit.Id, result.Id);
        Assert.Equal(knowledgeUnit.Title, result.Title);
        Assert.Equal(knowledgeUnit.Content, result.Content);
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
    public async Task AddAsync_ShouldAddNewKnowledgeUnit()
    {
        // Arrange
        var knowledgeUnit = new KnowledgeUnit
        {
            Id = Guid.NewGuid(),
            Title = "New Knowledge Unit",
            Summary = "New Summary",
            Content = "New content",
            Confidence = ConfidenceLevel.Medium,
            Category = _defaultCategory,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _repository.AddAsync(knowledgeUnit);
        await _dbContext.SaveChangesAsync();

        // Assert
        var saved = await _dbContext.KnowledgeUnits.FindAsync(knowledgeUnit.Id);
        Assert.NotNull(saved);
        Assert.Equal(knowledgeUnit.Title, saved.Title);
        Assert.Equal(knowledgeUnit.Content, saved.Content);
    }

    [Fact]
    public async Task Query_ShouldReturnAllKnowledgeUnits()
    {
        // Arrange
        var knowledgeUnits = new[]
        {
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "KU 1",
                Summary = "Summary 1",
                Content = "Content 1",
                Confidence = ConfidenceLevel.High,
                Category = _defaultCategory,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "KU 2",
                Summary = "Summary 2",
                Content = "Content 2",
                Confidence = ConfidenceLevel.Medium,
                Category = _defaultCategory,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "KU 3",
                Summary = "Summary 3",
                Content = "Content 3",
                Confidence = ConfidenceLevel.Low,
                Category = _defaultCategory,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };
        await _dbContext.KnowledgeUnits.AddRangeAsync(knowledgeUnits);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = _repository.Query().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, ku => ku.Title == "KU 1");
        Assert.Contains(result, ku => ku.Title == "KU 2");
        Assert.Contains(result, ku => ku.Title == "KU 3");
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldReturnSimilarKnowledgeUnits_OrderedByDistance()
    {
        // Arrange - Create test knowledge units with embeddings
        var baseEmbedding = CreateTestEmbedding(1.0f, 0.0f);
        var similarEmbedding = CreateTestEmbedding(0.9f, 0.1f);
        var differentEmbedding = CreateTestEmbedding(0.0f, 1.0f);

        var knowledgeUnits = new[]
        {
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "Base KU",
                Summary = "Base Summary",
                Content = "Base content",
                Confidence = ConfidenceLevel.High,
                Category = _defaultCategory,
                Embedding = new Vector(baseEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "Similar KU",
                Summary = "Similar Summary",
                Content = "Similar content",
                Confidence = ConfidenceLevel.High,
                Category = _defaultCategory,
                Embedding = new Vector(similarEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "Different KU",
                Summary = "Different Summary",
                Content = "Different content",
                Confidence = ConfidenceLevel.Medium,
                Category = _defaultCategory,
                Embedding = new Vector(differentEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        await _dbContext.KnowledgeUnits.AddRangeAsync(knowledgeUnits);
        await _dbContext.SaveChangesAsync();

        // Act - Find similar knowledge units to base embedding
        var results = await _repository.FindSimilarAsync(baseEmbedding, 2);
        var resultList = results.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.Equal("Base content", resultList[0].KnowledgeUnit.Content); // Most similar (identical)
        Assert.Equal("Similar content", resultList[1].KnowledgeUnit.Content); // Second most similar
        Assert.True(resultList[0].Distance <= resultList[1].Distance); // Ordered by distance
    }

    [Fact]
    public async Task FindSimilarAsync_WithMinSimilarity_ShouldFilterBySimilarityScore()
    {
        // Arrange
        var queryEmbedding = CreateTestEmbedding(1.0f, 0.0f);
        var similarEmbedding = CreateTestEmbedding(0.95f, 0.05f);
        var differentEmbedding = CreateTestEmbedding(0.0f, 1.0f);

        var knowledgeUnits = new[]
        {
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "Similar KU",
                Summary = "Similar Summary",
                Content = "Similar content",
                Confidence = ConfidenceLevel.High,
                Category = _defaultCategory,
                Embedding = new Vector(similarEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "Different KU",
                Summary = "Different Summary",
                Content = "Different content",
                Confidence = ConfidenceLevel.Medium,
                Category = _defaultCategory,
                Embedding = new Vector(differentEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        await _dbContext.KnowledgeUnits.AddRangeAsync(knowledgeUnits);
        await _dbContext.SaveChangesAsync();

        // Act - Find similar with minimum similarity threshold
        var results = await _repository.FindSimilarAsync(queryEmbedding, 10, 0.75);
        var resultList = results.ToList();

        // Assert - Should only return the similar knowledge unit, not the different one
        Assert.Single(resultList);
        Assert.Equal("Similar content", resultList[0].KnowledgeUnit.Content);
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
    public async Task FindSimilarAsync_ShouldIgnoreKnowledgeUnitsWithoutEmbeddings()
    {
        // Arrange
        var queryEmbedding = CreateTestEmbedding(1.0f, 0.0f);

        var knowledgeUnits = new[]
        {
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "KU With Embedding",
                Summary = "Summary",
                Content = "Content with embedding",
                Confidence = ConfidenceLevel.High,
                Category = _defaultCategory,
                Embedding = new Vector(queryEmbedding),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new KnowledgeUnit
            {
                Id = Guid.NewGuid(),
                Title = "KU Without Embedding",
                Summary = "Summary",
                Content = "Content without embedding",
                Confidence = ConfidenceLevel.Medium,
                Category = _defaultCategory,
                Embedding = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        await _dbContext.KnowledgeUnits.AddRangeAsync(knowledgeUnits);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _repository.FindSimilarAsync(queryEmbedding, 10);
        var resultList = results.ToList();

        // Assert
        Assert.Single(resultList);
        Assert.Equal("Content with embedding", resultList[0].KnowledgeUnit.Content);
    }

    [Fact]
    public async Task GetWithFragmentsAsync_ShouldReturnKnowledgeUnitWithFragments()
    {
        // Arrange
        var integration = new Domain.Entities.Integration
        {
            Id = Guid.NewGuid(),
            Name = "Test Integration",
            Type = Domain.Enums.IntegrationType.Fellow
        };
        await _dbContext.Integrations.AddAsync(integration);

        var source = new Source
        {
            Id = Guid.NewGuid(),
            Type = Domain.Enums.SourceType.Meeting,
            MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow,
            Name = "Meeting",
            Content = "Test content",
            MetadataJson = "{}",
            Date = DateTimeOffset.UtcNow,
            Integration = integration
        };
        await _dbContext.Sources.AddAsync(source);

        var knowledgeUnit = new KnowledgeUnit
        {
            Id = Guid.NewGuid(),
            Title = "Test KU",
            Summary = "Test Summary",
            Content = "Test content",
            Confidence = ConfidenceLevel.High,
            Category = _defaultCategory,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _dbContext.KnowledgeUnits.AddAsync(knowledgeUnit);

        var fragments = new[]
        {
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Fragment 1",
                Summary = "Summary 1",
                Content = "Content 1",
                FragmentCategory = _defaultCategory,
                Source = source,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Fragment
            {
                Id = Guid.NewGuid(),
                Title = "Fragment 2",
                Summary = "Summary 2",
                Content = "Content 2",
                FragmentCategory = _defaultCategory,
                Source = source,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Note: We can't set KnowledgeUnit navigation property yet because Fragment entity
        // doesn't have KnowledgeUnit property yet (that's in task 3)
        // This test will need to be updated after task 3 is complete
        await _dbContext.Fragments.AddRangeAsync(fragments);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithFragmentsAsync(knowledgeUnit.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(knowledgeUnit.Id, result.Id);
        Assert.NotNull(result.Fragments);
        Assert.NotNull(result.Category);
    }

    [Fact]
    public async Task GetWithFragmentsAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetWithFragmentsAsync(nonExistentId);

        // Assert
        Assert.Null(result);
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
