using Medley.Domain.Entities;

namespace Medley.Domain.Models;

public class FragmentSimilarityResult : BaseSimilarityResult<Fragment>
{
	public Fragment Fragment => RelatedEntity;
}
