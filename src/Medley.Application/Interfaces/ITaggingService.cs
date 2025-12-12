using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

public interface ITaggingService
{
    /// <summary>
    /// Tag a single source. When force is false, skips sources already tagged.
    /// </summary>
    Task<TaggingResult> GenerateTagsAsync(Guid sourceId, bool force = false, CancellationToken cancellationToken = default);
}

public class TaggingResult
{
    public Guid SourceId { get; set; }
    public bool Processed { get; set; }
    public bool? IsInternal { get; set; }
    public int TagCount { get; set; }
    public string? Message { get; set; }
    public string? SkipReason { get; set; }
}

