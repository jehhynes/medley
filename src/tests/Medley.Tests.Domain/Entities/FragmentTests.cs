using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Pgvector;
using Xunit;

namespace Medley.Tests.Domain.Entities;

public class FragmentTests
{
    [Fact]
    public void Fragment_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var fragment = new Fragment
        {
            Title = "Test Title",
            Summary = "Test Summary",
            Category = "Test Category",
            Content = string.Empty,
            Source = new Source 
            { 
                Type = SourceType.Meeting,
                MetadataType = SourceMetadataType.Collector_Fellow,
                Name = "Test Source",
                Content = "Test content",
                MetadataJson = "{}",
                Date = DateTimeOffset.UtcNow,
                Integration = new Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = IntegrationType.Fellow }
            }
        };

        // Assert
        Assert.Equal(Guid.Empty, fragment.Id);
        Assert.Equal(string.Empty, fragment.Content);
        Assert.Null(fragment.Embedding);
        Assert.NotEqual(DateTimeOffset.MinValue, fragment.CreatedAt);
        Assert.Null(fragment.LastModifiedAt);
    }

    [Fact]
    public void Fragment_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var content = "Test fragment content";
        var embeddingArray = new float[2000];
        for (int i = 0; i < 2000; i++)
        {
            embeddingArray[i] = (float)i / 2000;
        }
        var embedding = new Vector(embeddingArray);
        var source = new Source
        {
            Id = Guid.NewGuid(),
            Type = SourceType.Meeting,
            MetadataType = SourceMetadataType.Collector_Fellow,
            Name = "Daily Standup",
            Content = "Meeting transcript content",
            MetadataJson = "{}",
            Date = DateTimeOffset.UtcNow,
            ExternalId = "meeting-123",
            Integration = new Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = IntegrationType.Fellow }
        };
        var createdAt = DateTimeOffset.UtcNow;
        var lastModifiedAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var fragment = new Fragment
        {
            Id = id,
            Title = "Test Title",
            Summary = "Test Summary",
            Category = "Test Category",
            Content = content,
            Embedding = embedding,
            Source = source,
            CreatedAt = createdAt,
            LastModifiedAt = lastModifiedAt
        };

        // Assert
        Assert.Equal(id, fragment.Id);
        Assert.Equal(content, fragment.Content);
        Assert.Equal(embedding, fragment.Embedding);
        Assert.Equal(2000, fragment.Embedding!.ToArray().Length);
        Assert.Equal(source, fragment.Source);
        Assert.Equal(SourceType.Meeting, fragment.Source.Type);
        Assert.Equal("Daily Standup", fragment.Source.Name);
        Assert.Equal("meeting-123", fragment.Source.ExternalId);
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
            Title = "Test Title",
            Summary = "Test Summary",
            Category = "Test Category",
            Content = "Test content without embedding",
            Embedding = null,
            Source = new Source 
            { 
                Type = SourceType.Meeting,
                MetadataType = SourceMetadataType.Collector_Fellow,
                Name = "Test Source",
                Content = "Test content",
                MetadataJson = "{}",
                Date = DateTimeOffset.UtcNow,
                Integration = new Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = IntegrationType.Fellow }
            }
        };

        // Assert
        Assert.Null(fragment.Embedding);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(1536)]
    [InlineData(768)]
    [InlineData(384)]
    public void Fragment_ShouldAcceptDifferentEmbeddingDimensions(int dimensions)
    {
        // Arrange
        var embedding = new Vector(new float[dimensions]);

        // Act
        var fragment = new Fragment
        {
            Id = Guid.NewGuid(),
            Title = "Test Title",
            Summary = "Test Summary",
            Category = "Test Category",
            Content = "Test content",
            Embedding = embedding,
            Source = new Source 
            { 
                Type = SourceType.Meeting,
                MetadataType = SourceMetadataType.Collector_Fellow,
                Name = "Test Source",
                Content = "Test content",
                MetadataJson = "{}",
                Date = DateTimeOffset.UtcNow,
                Integration = new Integration { Id = Guid.NewGuid(), Name = "Test Integration", Type = IntegrationType.Fellow }
            }
        };

        // Assert
        Assert.Equal(dimensions, fragment.Embedding!.ToArray().Length);
    }
}
