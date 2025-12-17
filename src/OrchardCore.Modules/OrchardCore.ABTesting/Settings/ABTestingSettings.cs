namespace OrchardCore.ABTesting.Settings;

/// <summary>
/// Site-wide settings for A/B Testing statistical analysis.
/// </summary>
public class ABTestingSettings
{
    /// <summary>
    /// Minimum number of impressions required per variant before statistical analysis is performed.
    /// Default is 50. Valid range is 30-500.
    /// </summary>
    public int MinimumSampleSize { get; set; } = 50;

    /// <summary>
    /// The confidence level required to declare a winner (90, 95, or 99).
    /// Default is 90 (90% confidence).
    /// </summary>
    public int ConfidenceThreshold { get; set; } = 90;
}
