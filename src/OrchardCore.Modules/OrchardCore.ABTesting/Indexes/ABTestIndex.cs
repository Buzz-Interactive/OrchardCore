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
}
