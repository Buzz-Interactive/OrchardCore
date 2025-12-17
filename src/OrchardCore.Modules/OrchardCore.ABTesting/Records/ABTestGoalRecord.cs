namespace OrchardCore.ABTesting.Records;

/// <summary>
/// Database record for storing A/B test goal conversion counts.
/// </summary>
public class ABTestGoalRecord
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
    /// The number of conversions for Variant A.
    /// </summary>
    public long VariantAConversions { get; set; }

    /// <summary>
    /// The number of conversions for Variant B.
    /// </summary>
    public long VariantBConversions { get; set; }
}
