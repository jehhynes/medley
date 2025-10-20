using Medley.Domain.Entities;

namespace Medley.Domain.Models;

public class FindingSimilarityResult : BaseSimilarityResult<Finding>
{
    public Finding Finding => RelatedEntity;
}