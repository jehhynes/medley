namespace Medley.Domain.Enums;

/// <summary>
/// Status of an article improvement plan
/// </summary>
public enum PlanStatus
{
    /// <summary>
    /// Plan has been created and is ready for review
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Plan is currently being applied to the article
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Plan has been successfully applied to the article
    /// </summary>
    Applied = 3,

    /// <summary>
    /// Plan has been archived (superseded by a new plan)
    /// </summary>
    Archived = 4
}
