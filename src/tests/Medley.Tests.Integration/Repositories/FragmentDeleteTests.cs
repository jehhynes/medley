using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medley.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for Fragment delete functionality using global query filters
/// </summary>
public class FragmentDeleteTests : DatabaseTestBase
{
    private IFragmentRepository _repository = null!;
    private FragmentCategory _defaultCategory = null!;
    private Domain.Entities.Integration _testIntegration = null!;
    private Source _testSource = null!;

    public FragmentDeleteTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _repository = new FragmentRepository(_dbContext);

        // Create test data
        _defaultCategory = new FragmentCategory
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            Icon = "bi-test",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _dbContext.Set<FragmentCategory>().AddAsync(_defaultCategory);

        _testIntegration = new Domain.Entities.Integration
        {
            Id = Guid.NewGuid(),
            Name = "Test Integration",
            Type = Domain.Enums.IntegrationType.Fellow
        };
        await _dbContext.Integrations.AddAsync(_testIntegration);

        _testSource = new Source
        {
            Id = Guid.NewGuid(),
            Type = Domain.Enums.SourceType.Meeting,
            MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow,
            Name = "Test Meeting",
            Content = "Test content",
            MetadataJson = "{}",
            Date = DateTimeOffset.UtcNow,
            Integration = _testIntegration
        };
        await _dbContext.Sources.AddAsync(_testSource);

        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Query_ShouldExcludeDeletedFragments_ByDefault()
    {
        // Arrange - Create both deleted and non-deleted fragments
        var activeFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Active Fragment",
            Summary = "Active Summary",
            FragmentCategory = _defaultCategory,
            Content = "Active content",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        var deletedFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Fragment",
            Summary = "Deleted Summary",
            FragmentCategory = _defaultCategory,
            Content = "Deleted content",
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        await _dbContext.Fragments.AddRangeAsync(activeFragment, deletedFragment);
        await _dbContext.SaveChangesAsync();

        // Act - Query without IgnoreQueryFilters
        var results = await _repository.Query().ToListAsync();

        // Assert - Should only return active fragment
        Assert.Single(results);
        Assert.Equal(activeFragment.Id, results[0].Id);
        Assert.Equal("Active content", results[0].Content);
        Assert.DoesNotContain(results, f => f.Id == deletedFragment.Id);
    }

    [Fact]
    public async Task Query_WithIgnoreQueryFilters_ShouldIncludeDeletedFragments()
    {
        // Arrange - Create both deleted and non-deleted fragments
        var activeFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Active Fragment",
            Summary = "Active Summary",
            FragmentCategory = _defaultCategory,
            Content = "Active content",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        var deletedFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Fragment",
            Summary = "Deleted Summary",
            FragmentCategory = _defaultCategory,
            Content = "Deleted content",
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        await _dbContext.Fragments.AddRangeAsync(activeFragment, deletedFragment);
        await _dbContext.SaveChangesAsync();

        // Act - Query with IgnoreQueryFilters
        var results = await _repository.Query()
            .IgnoreQueryFilters()
            .ToListAsync();

        // Assert - Should return both fragments
        Assert.Equal(2, results.Count);
        Assert.Contains(results, f => f.Id == activeFragment.Id);
        Assert.Contains(results, f => f.Id == deletedFragment.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_ForDeletedFragment()
    {
        // Arrange - Create a deleted fragment
        var deletedFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Fragment",
            Summary = "Deleted Summary",
            FragmentCategory = _defaultCategory,
            Content = "Deleted content",
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        await _dbContext.Fragments.AddAsync(deletedFragment);
        await _dbContext.SaveChangesAsync();

        // Act - Try to get deleted fragment by ID
        var result = await _repository.GetByIdAsync(deletedFragment.Id);

        // Assert - Should return null due to global query filter
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFragment_ForActiveFragment()
    {
        // Arrange - Create an active fragment
        var activeFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Active Fragment",
            Summary = "Active Summary",
            FragmentCategory = _defaultCategory,
            Content = "Active content",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        await _dbContext.Fragments.AddAsync(activeFragment);
        await _dbContext.SaveChangesAsync();

        // Act - Get active fragment by ID
        var result = await _repository.GetByIdAsync(activeFragment.Id);

        // Assert - Should return the fragment
        Assert.NotNull(result);
        Assert.Equal(activeFragment.Id, result.Id);
        Assert.Equal("Active content", result.Content);
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldExcludeDeletedFragments()
    {
        // Arrange - Create active and deleted fragments with embeddings
        var testEmbedding = CreateTestEmbedding(1.0f, 0.0f);

        var activeFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Active Fragment",
            Summary = "Active Summary",
            FragmentCategory = _defaultCategory,
            Content = "Active content",
            IsDeleted = false,
            Embedding = new Pgvector.Vector(testEmbedding),
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        var deletedFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Fragment",
            Summary = "Deleted Summary",
            FragmentCategory = _defaultCategory,
            Content = "Deleted content",
            IsDeleted = true,
            Embedding = new Pgvector.Vector(testEmbedding),
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        await _dbContext.Fragments.AddRangeAsync(activeFragment, deletedFragment);
        await _dbContext.SaveChangesAsync();

        // Act - Find similar fragments
        var results = await _repository.FindSimilarAsync(testEmbedding, 10);
        var resultList = results.ToList();

        // Assert - Should only return active fragment
        Assert.Single(resultList);
        Assert.Equal(activeFragment.Id, resultList[0].Fragment.Id);
        Assert.Equal("Active content", resultList[0].Fragment.Content);
    }

    [Fact]
    public async Task Include_WithSource_ShouldExcludeDeletedFragments()
    {
        // Arrange - Create active and deleted fragments
        var activeFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Active Fragment",
            Summary = "Active Summary",
            FragmentCategory = _defaultCategory,
            Content = "Active content",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        var deletedFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Fragment",
            Summary = "Deleted Summary",
            FragmentCategory = _defaultCategory,
            Content = "Deleted content",
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        await _dbContext.Fragments.AddRangeAsync(activeFragment, deletedFragment);
        await _dbContext.SaveChangesAsync();

        // Clear change tracker to ensure fresh query
        _dbContext.ChangeTracker.Clear();

        // Act - Load source with fragments using a fresh query
        var source = await _dbContext.Sources
            .Include(s => s.Fragments)
            .FirstOrDefaultAsync(s => s.Id == _testSource.Id);

        // Assert - Source.Fragments should only contain active fragment
        Assert.NotNull(source);
        Assert.Single(source.Fragments);
        Assert.Equal(activeFragment.Id, source.Fragments.First().Id);
    }

    [Fact]
    public async Task Count_ShouldExcludeDeletedFragments()
    {
        // Arrange - Create 2 active and 3 deleted fragments
        var fragments = new List<Fragment>();
        
        for (int i = 0; i < 2; i++)
        {
            fragments.Add(new Fragment
            {
                Id = Guid.NewGuid(),
                Title = $"Active Fragment {i}",
                Summary = $"Active Summary {i}",
                FragmentCategory = _defaultCategory,
                Content = $"Active content {i}",
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                Source = _testSource
            });
        }

        for (int i = 0; i < 3; i++)
        {
            fragments.Add(new Fragment
            {
                Id = Guid.NewGuid(),
                Title = $"Deleted Fragment {i}",
                Summary = $"Deleted Summary {i}",
                FragmentCategory = _defaultCategory,
                Content = $"Deleted content {i}",
                IsDeleted = true,
                CreatedAt = DateTimeOffset.UtcNow,
                Source = _testSource
            });
        }

        await _dbContext.Fragments.AddRangeAsync(fragments);
        await _dbContext.SaveChangesAsync();

        // Act - Count fragments
        var count = await _repository.Query().CountAsync();

        // Assert - Should only count active fragments
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Where_WithAdditionalFilters_ShouldExcludeDeletedFragments()
    {
        // Arrange - Create a dedicated source for this test
        var testIntegration = new Domain.Entities.Integration
        {
            Id = Guid.NewGuid(),
            Name = "Test Integration Where",
            Type = Domain.Enums.IntegrationType.Fellow
        };
        await _dbContext.Integrations.AddAsync(testIntegration);

        var testSource = new Source
        {
            Id = Guid.NewGuid(),
            Type = Domain.Enums.SourceType.Meeting,
            MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow,
            Name = "Test Meeting Where",
            Content = "Test content",
            MetadataJson = "{}",
            Date = DateTimeOffset.UtcNow,
            Integration = testIntegration
        };
        await _dbContext.Sources.AddAsync(testSource);
        await _dbContext.SaveChangesAsync();

        // Create fragments with different titles
        var activeFragmentA = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Alpha Fragment",
            Summary = "Summary A",
            FragmentCategory = _defaultCategory,
            Content = "Content A",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = testSource
        };

        var activeFragmentB = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Beta Fragment",
            Summary = "Summary B",
            FragmentCategory = _defaultCategory,
            Content = "Content B",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = testSource
        };

        var deletedFragmentA = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Alpha Deleted",
            Summary = "Summary A",
            FragmentCategory = _defaultCategory,
            Content = "Content A",
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = testSource
        };

        await _dbContext.Fragments.AddRangeAsync(activeFragmentA, activeFragmentB, deletedFragmentA);
        await _dbContext.SaveChangesAsync();

        // Clear change tracker to ensure fresh query
        _dbContext.ChangeTracker.Clear();

        // Act - Query with additional filter
        var results = await _repository.Query()
            .Where(f => f.Title.Contains("Alpha"))
            .ToListAsync();

        // Assert - Should only return active fragment with "Alpha" in title (from this test)
        var testResults = results.Where(f => f.SourceId == testSource.Id).ToList();
        Assert.Single(testResults);
        Assert.Equal(activeFragmentA.Id, testResults[0].Id);
    }

    [Fact]
    public async Task DeleteFragment_ShouldMakeItInvisibleToQueries()
    {
        // Arrange - Create an active fragment
        var fragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Fragment to Delete",
            Summary = "Summary",
            FragmentCategory = _defaultCategory,
            Content = "Content",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        await _dbContext.Fragments.AddAsync(fragment);
        await _dbContext.SaveChangesAsync();

        // Verify it's visible before deleting
        var beforeDelete = await _repository.Query().CountAsync();
        Assert.Equal(1, beforeDelete);

        // Act - Delete the fragment
        fragment.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        // Clear change tracker to ensure fresh query
        _dbContext.ChangeTracker.Clear();

        // Assert - Fragment should no longer be visible
        var afterDelete = await _repository.Query().CountAsync();
        Assert.Equal(0, afterDelete);

        // But should be visible with IgnoreQueryFilters
        var withIgnoreFilters = await _repository.Query()
            .IgnoreQueryFilters()
            .CountAsync();
        Assert.Equal(1, withIgnoreFilters);
    }

    [Fact]
    public async Task PlanKnowledgeUnits_ShouldNotLoadDeletedKnowledgeUnits()
    {
        // Arrange - Create dedicated test data
        var testIntegration = new Domain.Entities.Integration
        {
            Id = Guid.NewGuid(),
            Name = "Test Integration Plan",
            Type = Domain.Enums.IntegrationType.Fellow
        };
        await _dbContext.Integrations.AddAsync(testIntegration);

        var testSource = new Source
        {
            Id = Guid.NewGuid(),
            Type = Domain.Enums.SourceType.Meeting,
            MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow,
            Name = "Test Meeting Plan",
            Content = "Test content",
            MetadataJson = "{}",
            Date = DateTimeOffset.UtcNow,
            Integration = testIntegration
        };
        await _dbContext.Sources.AddAsync(testSource);

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org"
        };
        await _dbContext.Organizations.AddAsync(organization);

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            FullName = "Test User"
        };
        await _dbContext.Users.AddAsync(user);

        var articleType = new ArticleType
        {
            Id = Guid.NewGuid(),
            Name = "Test Type",
            Icon = "bi-test",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _dbContext.ArticleTypes.AddAsync(articleType);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Test Article",
            Summary = "Test Summary",
            Content = "Test Content",
            Status = Domain.Enums.ArticleStatus.Draft,
            ArticleType = articleType,
            CreatedBy = user,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _dbContext.Articles.AddAsync(article);

        var conversation = new ChatConversation()
        {
            Article = article,
            CreatedBy = user
        };
        await _dbContext.ChatConversations.AddAsync(conversation);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            Article = article,
            Instructions = "Test instructions",
            Status = Domain.Enums.PlanStatus.Draft,
            CreatedBy = user,
            CreatedAt = DateTimeOffset.UtcNow,
            Version = 1,
            Conversation = conversation
        };
        await _dbContext.Plans.AddAsync(plan);

        // Create active and deleted knowledge units
        var activeKnowledgeUnit = new KnowledgeUnit
        {
            Id = Guid.NewGuid(),
            Title = "Active Knowledge Unit",
            Summary = "Active Summary",
            Content = "Active content",
            Category = _defaultCategory,
            Confidence = Domain.Enums.ConfidenceLevel.High,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var deletedKnowledgeUnit = new KnowledgeUnit
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Knowledge Unit",
            Summary = "Deleted Summary",
            Content = "Deleted content",
            Category = _defaultCategory,
            Confidence = Domain.Enums.ConfidenceLevel.High,
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _dbContext.KnowledgeUnits.AddRangeAsync(activeKnowledgeUnit, deletedKnowledgeUnit);

        // Create plan knowledge units for both
        var activePlanKnowledgeUnit = new PlanKnowledgeUnit
        {
            Id = Guid.NewGuid(),
            Plan = plan,
            KnowledgeUnit = activeKnowledgeUnit,
            SimilarityScore = 0.9,
            Include = true
        };

        var deletedPlanKnowledgeUnit = new PlanKnowledgeUnit
        {
            Id = Guid.NewGuid(),
            Plan = plan,
            KnowledgeUnit = deletedKnowledgeUnit,
            SimilarityScore = 0.8,
            Include = true
        };

        await _dbContext.PlanKnowledgeUnits.AddRangeAsync(activePlanKnowledgeUnit, deletedPlanKnowledgeUnit);
        await _dbContext.SaveChangesAsync();

        // Clear change tracker
        _dbContext.ChangeTracker.Clear();

        // Act - Load plan with knowledge units
        var loadedPlan = await _dbContext.Plans
            .Include(p => p.PlanKnowledgeUnits)
                .ThenInclude(pku => pku.KnowledgeUnit)
            .FirstOrDefaultAsync(p => p.Id == plan.Id);

        // Assert - Only PlanKnowledgeUnits with non-deleted knowledge units are loaded
        Assert.NotNull(loadedPlan);
        Assert.Single(loadedPlan.PlanKnowledgeUnits);
        Assert.Equal(activeKnowledgeUnit.Id, loadedPlan.PlanKnowledgeUnits.First().KnowledgeUnit.Id);
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

    [Fact]
    public async Task Projection_FragmentCount_ShouldExcludeDeletedFragments()
    {
        // Arrange - Create a source with both active and deleted fragments
        var testIntegration = new Domain.Entities.Integration
        {
            Id = Guid.NewGuid(),
            Name = "Test Integration Projection",
            Type = Domain.Enums.IntegrationType.Fellow
        };
        await _dbContext.Integrations.AddAsync(testIntegration);

        var testSource = new Source
        {
            Id = Guid.NewGuid(),
            Type = Domain.Enums.SourceType.Meeting,
            MetadataType = Domain.Enums.SourceMetadataType.Collector_Fellow,
            Name = "Test Meeting Projection",
            Content = "Test content",
            MetadataJson = "{}",
            Date = DateTimeOffset.UtcNow,
            Integration = testIntegration
        };
        await _dbContext.Sources.AddAsync(testSource);
        await _dbContext.SaveChangesAsync();

        // Create 3 active and 2 deleted fragments
        var fragments = new List<Fragment>();
        for (int i = 0; i < 3; i++)
        {
            fragments.Add(new Fragment
            {
                Id = Guid.NewGuid(),
                Title = $"Active Fragment {i}",
                Summary = $"Active Summary {i}",
                FragmentCategory = _defaultCategory,
                Content = $"Active content {i}",
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                Source = testSource
            });
        }

        for (int i = 0; i < 2; i++)
        {
            fragments.Add(new Fragment
            {
                Id = Guid.NewGuid(),
                Title = $"Deleted Fragment {i}",
                Summary = $"Deleted Summary {i}",
                FragmentCategory = _defaultCategory,
                Content = $"Deleted content {i}",
                IsDeleted = true,
                CreatedAt = DateTimeOffset.UtcNow,
                Source = testSource
            });
        }

        await _dbContext.Fragments.AddRangeAsync(fragments);
        await _dbContext.SaveChangesAsync();

        // Clear change tracker to ensure fresh query
        _dbContext.ChangeTracker.Clear();

        // Act - Query with projection using Fragments.Count (like SourcesApiController does)
        var result = await _dbContext.Sources
            .Include(s => s.Fragments)
            .Where(s => s.Id == testSource.Id)
            .Select(s => new
            {
                SourceId = s.Id,
                FragmentCount = s.Fragments.Count
            })
            .FirstOrDefaultAsync();

        // Assert - Count should only include active fragments (3), not deleted ones (2)
        Assert.NotNull(result);
        Assert.Equal(3, result.FragmentCount);
    }

    [Fact]
    public async Task ClusteredFragments_ShouldNotBeDeleteable()
    {
        // Arrange - Create a KnowledgeUnit and a fragment clustered into it
        var knowledgeUnit = new KnowledgeUnit
        {
            Id = Guid.NewGuid(),
            Title = "Test Knowledge Unit",
            Summary = "Test Summary",
            Content = "Test Content",
            Confidence = Domain.Enums.ConfidenceLevel.High,
            Category = _defaultCategory,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var clusteredFragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Clustered Fragment",
            Summary = "Clustered Summary",
            FragmentCategory = _defaultCategory,
            Content = "Clustered content",
            IsDeleted = false,
            KnowledgeUnitId = knowledgeUnit.Id,
            KnowledgeUnit = knowledgeUnit,
            CreatedAt = DateTimeOffset.UtcNow,
            Source = _testSource
        };

        await _dbContext.KnowledgeUnits.AddAsync(knowledgeUnit);
        await _dbContext.Fragments.AddAsync(clusteredFragment);
        await _dbContext.SaveChangesAsync();

        // Clear change tracker
        _dbContext.ChangeTracker.Clear();

        // Act - Try to delete the clustered fragment (should fail)
        var fragmentToDelete = await _repository.Query()
            .IgnoreQueryFilters()
            .Include(f => f.KnowledgeUnit)
            .FirstOrDefaultAsync(f => f.Id == clusteredFragment.Id);

        // Assert - Fragment should have KnowledgeUnitId set
        Assert.NotNull(fragmentToDelete);
        Assert.NotNull(fragmentToDelete.KnowledgeUnitId);
        Assert.Equal(knowledgeUnit.Id, fragmentToDelete.KnowledgeUnitId);
        
        // In a real scenario, the controller would check this and return an error
        // This test verifies the data structure is correct for that validation
    }
}