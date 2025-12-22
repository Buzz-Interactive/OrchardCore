namespace OrchardCore.ABTesting.Models;

/// <summary>
/// Represents the state of a variant's content item.
/// </summary>
public enum VariantState
{
    /// <summary>
    /// Content item exists and is published.
    /// </summary>
    Available = 0,

    /// <summary>
    /// Content item exists but is unpublished.
    /// </summary>
    Unpublished = 1,

    /// <summary>
    /// Content item has been permanently deleted.
    /// </summary>
    Deleted = 2,
}
