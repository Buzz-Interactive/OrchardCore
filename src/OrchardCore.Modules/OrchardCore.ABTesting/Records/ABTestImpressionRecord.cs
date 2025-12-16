namespace OrchardCore.ABTesting.Records;

/// <summary>
/// Database record for storing A/B test impression counts.
/// </summary>
public class ABTestImpressionRecord
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The ContentItemId of the ABTest content item.
    /// </summary>
    public string TestContentItemId { get; set; }

    /// <summary>
    /// The number of impressions for Variant A.
    /// </summary>
    public long VariantAImpressions { get; set; }

    /// <summary>
    /// The number of impressions for Variant B.
    /// </summary>
    public long VariantBImpressions { get; set; }
}
