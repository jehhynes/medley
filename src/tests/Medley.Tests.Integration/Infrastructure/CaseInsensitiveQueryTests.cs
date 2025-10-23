using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Medley.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for CaseInsensitiveQueryInterceptor to verify that string comparison operations
/// with StringComparison.OrdinalIgnoreCase are properly transformed to PostgreSQL ILIKE operations.
/// </summary>
[Collection("Database")]
public class CaseInsensitiveQueryTests : DatabaseTestBase
{
    private readonly ITestOutputHelper _output;

    public CaseInsensitiveQueryTests(DatabaseFixture fixture, ITestOutputHelper output) 
        : base(fixture)
    {
        _output = output;
    }

    /// <summary>
    /// Verifies that StartsWith with OrdinalIgnoreCase is transformed to ILIKE 'value%'
    /// </summary>
    [Fact]
    public async Task StartsWith_WithOrdinalIgnoreCase_ShouldUseILike()
    {
        // Arrange
        await SeedTestDataAsync();
        var prefix = "test";

        // Act
        var results = await _dbContext.Fragments
            .Where(f => f.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, f => Assert.StartsWith(prefix, f.Content, StringComparison.OrdinalIgnoreCase));
        
        // Verify case-insensitive matching worked
        var upperCaseResults = await _dbContext.Fragments
            .Where(f => f.Content.StartsWith("TEST", StringComparison.OrdinalIgnoreCase))
            .ToListAsync();
        
        Assert.Equal(results.Count, upperCaseResults.Count);
    }

    /// <summary>
    /// Verifies that EndsWith with OrdinalIgnoreCase is transformed to ILIKE '%value'
    /// </summary>
    [Fact]
    public async Task EndsWith_WithOrdinalIgnoreCase_ShouldUseILike()
    {
        // Arrange
        await SeedTestDataAsync();
        var suffix = "content";

        // Act
        var results = await _dbContext.Fragments
            .Where(f => f.Content.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, f => Assert.EndsWith(suffix, f.Content, StringComparison.OrdinalIgnoreCase));
        
        // Verify case-insensitive matching worked
        var upperCaseResults = await _dbContext.Fragments
            .Where(f => f.Content.EndsWith("CONTENT", StringComparison.OrdinalIgnoreCase))
            .ToListAsync();
        
        Assert.Equal(results.Count, upperCaseResults.Count);
    }

    /// <summary>
    /// Verifies that queries without OrdinalIgnoreCase are NOT transformed (case-sensitive)
    /// </summary>
    [Fact]
    public async Task StringMethods_WithoutOrdinalIgnoreCase_ShouldRemainCaseSensitive()
    {
        // Arrange
        await SeedTestDataAsync();
        var searchTerm = "test";

        // Act - Case-sensitive search
        var caseSensitiveResults = await _dbContext.Fragments
            .Where(f => f.Content.Contains(searchTerm))
            .ToListAsync();

        // Act - Case-insensitive search using EF.Functions.ILike directly
        // This is what our interceptor should transform string.Contains with OrdinalIgnoreCase to
        var caseInsensitiveResults = await _dbContext.Fragments
            .Where(f => EF.Functions.ILike(f.Content, $"%{searchTerm}%"))
            .ToListAsync();

        // Assert
        // Case-sensitive should return fewer results than case-insensitive
        Assert.True(caseInsensitiveResults.Count >= caseSensitiveResults.Count);
        
        // Verify case-sensitive results contain the exact search term (case-sensitive)
        Assert.All(caseSensitiveResults, f => 
            Assert.Contains("test", f.Content, StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies complex queries with multiple case-insensitive conditions
    /// </summary>
    [Fact]
    public async Task ComplexQuery_WithMultipleCaseInsensitiveConditions_ShouldWork()
    {
        // Arrange
        await SeedTestDataAsync();
        var searchTerm = "test";

        // Act
        var results = await _dbContext.Fragments
            .Where(f => f.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       f.Content.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       f.Content.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();

        // Assert
        Assert.NotEmpty(results);
        
        // Verify all results match at least one condition
        Assert.All(results, f => 
        {
            var content = f.Content;
            var matchesCondition = 
                content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                content.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase);
            Assert.True(matchesCondition, $"Fragment content '{content}' should match at least one condition for '{searchTerm}'");
        });
    }

    /// <summary>
    /// Verifies that the interceptor works with parameterized queries
    /// </summary>
    [Fact]
    public async Task ParameterizedQuery_WithOrdinalIgnoreCase_ShouldWork()
    {
        // Arrange
        await SeedTestDataAsync();
        var searchTerms = new[] { "test", "content", "fragment" };

        // Act
        var results = new List<Fragment>();
        foreach (var term in searchTerms)
        {
            var termResults = await _dbContext.Fragments
                .Where(f => f.Content.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();
            results.AddRange(termResults);
        }

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, f => 
            Assert.Contains(searchTerms, term => 
                f.Content.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Verifies that the interceptor handles special ILIKE characters correctly
    /// </summary>
    [Fact]
    public async Task SpecialCharacters_InSearchTerm_ShouldBeHandledCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        
        // Add fragments with special characters
        var specialFragment = new Fragment
        {
            Content = "This contains % and _ special characters",
            Source = await _dbContext.Sources.FirstAsync()
        };
        
        var noSpecialFragment = new Fragment
        {
            Content = "This fragment has no special characters",
            Source = await _dbContext.Sources.FirstAsync()
        };
        
        _dbContext.Fragments.AddRange(specialFragment, noSpecialFragment);
        await _dbContext.SaveChangesAsync();

        // Act - Search for special characters
        var percentResults = await _dbContext.Fragments
            .Where(f => f.Content.Contains("%", StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        var underscoreResults = await _dbContext.Fragments
            .Where(f => f.Content.Contains("_", StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        // Assert - Verify matching results are returned
        Assert.Contains(specialFragment, percentResults);
        Assert.Contains(specialFragment, underscoreResults);
        
        // Assert - Verify non-matching results are NOT returned
        Assert.DoesNotContain(noSpecialFragment, percentResults);
        Assert.DoesNotContain(noSpecialFragment, underscoreResults);
        
        // Assert - Verify only fragments containing the literal special characters are returned
        Assert.All(percentResults, f => Assert.Contains("%", f.Content));
        Assert.All(underscoreResults, f => Assert.Contains("_", f.Content));
    }

    /// <summary>
    /// Verifies that all case-insensitive StringComparison values are translated to ILIKE
    /// </summary>
    [Fact]
    public async Task CaseInsensitiveComparisons_ShouldBeTranslatedToILike()
    {
        // Arrange
        await SeedTestDataAsync();
        var searchTerm = "test";

        // Act - These should all be translated to ILIKE
        var ordinalIgnoreCaseResults = await _dbContext.Fragments
            .Where(f => f.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        var currentCultureIgnoreCaseResults = await _dbContext.Fragments
            .Where(f => f.Content.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase))
            .ToListAsync();

        var invariantCultureIgnoreCaseResults = await _dbContext.Fragments
            .Where(f => f.Content.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
            .ToListAsync();

        // Assert - All should return case-insensitive results
        Assert.NotEmpty(ordinalIgnoreCaseResults);
        Assert.NotEmpty(currentCultureIgnoreCaseResults);
        Assert.NotEmpty(invariantCultureIgnoreCaseResults);
        
        // All should include fragments with different cases
        Assert.Contains(ordinalIgnoreCaseResults, f => f.Content == "This is a test fragment");
        Assert.Contains(ordinalIgnoreCaseResults, f => f.Content == "TEST content with uppercase");
        Assert.Contains(ordinalIgnoreCaseResults, f => f.Content == "Mixed Test Case Content");
        
        Assert.Contains(currentCultureIgnoreCaseResults, f => f.Content == "This is a test fragment");
        Assert.Contains(currentCultureIgnoreCaseResults, f => f.Content == "TEST content with uppercase");
        Assert.Contains(currentCultureIgnoreCaseResults, f => f.Content == "Mixed Test Case Content");
        
        Assert.Contains(invariantCultureIgnoreCaseResults, f => f.Content == "This is a test fragment");
        Assert.Contains(invariantCultureIgnoreCaseResults, f => f.Content == "TEST content with uppercase");
        Assert.Contains(invariantCultureIgnoreCaseResults, f => f.Content == "Mixed Test Case Content");
    }

    /// <summary>
    /// Verifies that case-sensitive comparisons are NOT translatable by EF Core
    /// </summary>
    [Fact]
    public async Task CaseSensitiveComparisons_ShouldNotBeTranslatable()
    {
        // Arrange
        await SeedTestDataAsync();
        var searchTerm = "test";

        // Act & Assert - These should throw translation exceptions because EF Core cannot translate them
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _dbContext.Fragments
                .Where(f => f.Content.Contains(searchTerm, StringComparison.Ordinal))
                .ToListAsync());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _dbContext.Fragments
                .Where(f => f.Content.Contains(searchTerm, StringComparison.CurrentCulture))
                .ToListAsync());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _dbContext.Fragments
                .Where(f => f.Content.Contains(searchTerm, StringComparison.InvariantCulture))
                .ToListAsync());
    }

    /// <summary>
    /// Seeds test data for the interceptor tests
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        // Clear existing data
        _dbContext.Fragments.RemoveRange(_dbContext.Fragments);
        _dbContext.Sources.RemoveRange(_dbContext.Sources);
        await _dbContext.SaveChangesAsync();

        // Create a test source
        var source = new Source
        {
            Name = "Test Source",
            Type = SourceType.Meeting
        };
        _dbContext.Sources.Add(source);
        await _dbContext.SaveChangesAsync();

        // Create test fragments with various case combinations
        var testFragments = new[]
        {
            new Fragment { Content = "This is a test fragment", Source = source },
            new Fragment { Content = "TEST content with uppercase", Source = source },
            new Fragment { Content = "Mixed Test Case Content", Source = source },
            new Fragment { Content = "Another fragment for testing", Source = source },
            new Fragment { Content = "Ends with test", Source = source },
            new Fragment { Content = "Test starts this fragment", Source = source },
            new Fragment { Content = "No matching content here", Source = source },
            new Fragment { Content = "Special % and _ characters", Source = source }
        };

        _dbContext.Fragments.AddRange(testFragments);
        await _dbContext.SaveChangesAsync();
    }
}
