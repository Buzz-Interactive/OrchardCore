namespace OrchardCore.ABTesting.Models;

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
    /// The unique identifier of the ABTest entity.
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// The number of impressions for Variant A.
    /// </summary>
    public long VariantAImpressions { get; set; }

    /// <summary>
    /// The number of impressions for Variant B.
    /// </summary>
    public long VariantBImpressions { get; set; }
}
