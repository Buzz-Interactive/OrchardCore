using OrchardCore.ABTesting.Models;
using YesSql.Indexes;

namespace OrchardCore.ABTesting.Indexes;

/// <summary>
/// YesSql index for efficient lookup of A/B tests stored in the ABTest collection.
/// </summary>
public class ABTestIndex : MapIndex
{
    /// <summary>
    /// The unique identifier of the ABTest entity.
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// The ContentItemId of Variant A.
    /// </summary>
    public string VariantAContentItemId { get; set; }

    /// <summary>
    /// The ContentItemId of Variant B.
    /// </summary>
    public string VariantBContentItemId { get; set; }

    /// <summary>
    /// Whether the test is currently active (running).
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the test was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// State of the Variant A content item.
    /// </summary>
    public VariantState VariantAState { get; set; }

    /// <summary>
    /// State of the Variant B content item.
    /// </summary>
    public VariantState VariantBState { get; set; }

    /// <summary>
    /// Whether the test has been concluded (winner declared).
    /// </summary>
    public bool IsConcluded { get; set; }

    /// <summary>
    /// The original URL path of Variant B (for active test redirects).
    /// </summary>
    public string VariantBOriginalPath { get; set; }
}
