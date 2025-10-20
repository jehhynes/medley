using Medley.Domain.Entities;

namespace Medley.Domain.Models;

public class ObservationSimilarityResult : BaseSimilarityResult<Observation>
{
	public Observation Observation => RelatedEntity;
}
