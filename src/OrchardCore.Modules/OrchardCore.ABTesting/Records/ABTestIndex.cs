using YesSql.Indexes;

namespace OrchardCore.ABTesting.Records;

/// <summary>
/// YesSql index for efficient lookup of A/B tests.
/// Allows fast queries to check if a content item is part of an active test.
/// </summary>
public class ABTestIndex : MapIndex
{
    /// <summary>
    /// The ContentItemId of the ABTest content item itself.
    /// </summary>
    public string ABTestContentItemId { get; set; }

    /// <summary>
    /// The ContentItemId of Variant A.
    /// </summary>
    public string VariantAContentItemId { get; set; }

    /// <summary>
    /// The ContentItemId of Variant B.
    /// </summary>
    public string VariantBContentItemId { get; set; }

    /// <summary>
    /// The percentage of traffic for Variant A (0-100).
    /// </summary>
    public int PercentageA { get; set; }

    /// <summary>
    /// Whether the test is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the ABTest content item is published.
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    /// Whether this is the latest version of the ABTest content item.
    /// </summary>
    public bool Latest { get; set; }

    /// <summary>
    /// The scheduled start date/time in UTC. Null means no start restriction.
    /// </summary>
    public DateTime? ScheduledStartUtc { get; set; }

    /// <summary>
    /// The scheduled end date/time in UTC. Null means no end restriction.
    /// </summary>
    public DateTime? ScheduledEndUtc { get; set; }
}
