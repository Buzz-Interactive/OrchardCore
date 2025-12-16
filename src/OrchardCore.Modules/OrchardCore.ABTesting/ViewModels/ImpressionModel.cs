namespace OrchardCore.ABTesting.ViewModels;

/// <summary>
/// Model for recording A/B test impressions via API.
/// </summary>
public class ImpressionModel
{
    /// <summary>
    /// The ContentItemId of the ABTest.
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// The variant that was shown ("A" or "B").
    /// </summary>
    public string Variant { get; set; }
}
