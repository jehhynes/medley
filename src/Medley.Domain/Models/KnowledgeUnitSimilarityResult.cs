using Medley.Domain.Entities;

namespace Medley.Domain.Models;

/// <summary>
/// Represents a knowledge unit with its similarity score
/// </summary>
public class KnowledgeUnitSimilarityResult : BaseSimilarityResult<KnowledgeUnit>
{
    /// <summary>
    /// The knowledge unit
    /// </summary>
    public KnowledgeUnit KnowledgeUnit => RelatedEntity;
}
