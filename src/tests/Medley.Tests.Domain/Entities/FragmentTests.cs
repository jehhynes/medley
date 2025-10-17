using Medley.Domain.Entities;
using Xunit;

namespace Medley.Tests.Domain.Entities;

public class FragmentTests
{
    [Fact]
    public void Fragment_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var fragment = new Fragment();

        // Assert
        Assert.Equal(Guid.Empty, fragment.Id);
        Assert.Equal(string.Empty, fragment.Content);
        Assert.Null(fragment.Embedding);
        Assert.Null(fragment.SourceType);
        Assert.Null(fragment.SourceId);
        Assert.NotEqual(DateTimeOffset.MinValue, fragment.CreatedAt);
        Assert.Null(fragment.LastModifiedAt);
    }

    [Fact]
    public void Fragment_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var content = "Test fragment content";
        var embedding = new float[1536];
        for (int i = 0; i < 1536; i++)
        {
            embedding[i] = (float)i / 1536;
        }
        var sourceType = "Fellow.ai";
        var sourceId = "meeting-123";
        var createdAt = DateTimeOffset.UtcNow;
        var lastModifiedAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var fragment = new Fragment
        {
            Id = id,
            Content = content,
            Embedding = embedding,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedAt = createdAt,
            LastModifiedAt = lastModifiedAt
        };

        // Assert
        Assert.Equal(id, fragment.Id);
        Assert.Equal(content, fragment.Content);
        Assert.Equal(embedding, fragment.Embedding);
        Assert.Equal(1536, fragment.Embedding!.Length);
        Assert.Equal(sourceType, fragment.SourceType);
        Assert.Equal(sourceId, fragment.SourceId);
        Assert.Equal(createdAt, fragment.CreatedAt);
        Assert.Equal(lastModifiedAt, fragment.LastModifiedAt);
    }

    [Fact]
    public void Fragment_EmbeddingCanBeNull()
    {
        // Arrange & Act
        var fragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Content = "Test content without embedding",
            Embedding = null
        };

        // Assert
        Assert.Null(fragment.Embedding);
    }

    [Theory]
    [InlineData(1536)]
    [InlineData(768)]
    [InlineData(384)]
    public void Fragment_ShouldAcceptDifferentEmbeddingDimensions(int dimensions)
    {
        // Arrange
        var embedding = new float[dimensions];

        // Act
        var fragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Content = "Test content",
            Embedding = embedding
        };

        // Assert
        Assert.Equal(dimensions, fragment.Embedding!.Length);
    }
}
