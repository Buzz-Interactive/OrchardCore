namespace OrchardCore.ABTesting.ViewModels;

/// <summary>
/// Model for recording A/B test goal conversions via API.
/// </summary>
public class ConversionModel
{
    /// <summary>
    /// The ContentItemId of the ABTest.
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// The variant that achieved the conversion ("A" or "B").
    /// </summary>
    public string Variant { get; set; }
}
