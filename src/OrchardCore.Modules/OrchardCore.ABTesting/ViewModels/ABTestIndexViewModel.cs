using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.ViewModels;

/// <summary>
/// View model for the ABTest list/index page.
/// </summary>
public class ABTestIndexViewModel
{
    /// <summary>
    /// The list of ABTest entries to display.
    /// </summary>
    public IEnumerable<ABTestEntry> Tests { get; set; } = [];
}

/// <summary>
/// Represents a single ABTest entry in the list.
/// </summary>
public class ABTestEntry
{
    /// <summary>
    /// The unique identifier of the ABTest.
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// The display name of the test.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Whether the test is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Display name of Variant A.
    /// </summary>
    public string VariantADisplayText { get; set; }

    /// <summary>
    /// Display name of Variant B.
    /// </summary>
    public string VariantBDisplayText { get; set; }

    /// <summary>
    /// Traffic percentage for Variant A.
    /// </summary>
    public int PercentageA { get; set; }

    /// <summary>
    /// Total impressions across both variants.
    /// </summary>
    public long TotalImpressions { get; set; }

    /// <summary>
    /// Total conversions across both variants.
    /// </summary>
    public long TotalConversions { get; set; }

    /// <summary>
    /// When the test was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// State of Variant A content item.
    /// </summary>
    public VariantState VariantAState { get; set; }

    /// <summary>
    /// State of Variant B content item.
    /// </summary>
    public VariantState VariantBState { get; set; }

    /// <summary>
    /// Whether either variant has been deleted.
    /// </summary>
    public bool HasDeletedVariant { get; set; }

    /// <summary>
    /// Whether either variant is unavailable (deleted or unpublished).
    /// </summary>
    public bool HasUnavailableVariant { get; set; }
}
