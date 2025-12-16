using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Enums;

/// <summary>
/// Determines how a tag type affects the source's IsInternal property when scope is unknown.
/// </summary>
public enum ScopeUpdateMode
{
    /// <summary>
    /// Do not update the source's scope (IsInternal remains unchanged).
    /// </summary>
    [Display(Name = "None (Do not update scope)")]
    None = 0,

    /// <summary>
    /// Mark the source as internal (IsInternal = true) if IsInternal is currently null.
    /// </summary>
    [Display(Name = "Mark as Internal")]
    MarkInternalIfUnknown = 1,

    /// <summary>
    /// Mark the source as external (IsInternal = false) if IsInternal is currently null.
    /// </summary>
    [Display(Name = "Mark as External")]
    MarkExternalIfUnknown = 2
}

