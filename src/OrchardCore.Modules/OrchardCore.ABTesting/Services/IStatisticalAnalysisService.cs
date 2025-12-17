using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Provides statistical analysis for A/B test results.
/// </summary>
public interface IStatisticalAnalysisService
{
    /// <summary>
    /// Analyzes the A/B test data and determines if there is a statistically significant winner.
    /// Uses the Z-test for two proportions to compare conversion rates.
    /// </summary>
    /// <param name="impressionsA">Number of impressions for Variant A.</param>
    /// <param name="impressionsB">Number of impressions for Variant B.</param>
    /// <param name="conversionsA">Number of conversions for Variant A.</param>
    /// <param name="conversionsB">Number of conversions for Variant B.</param>
    /// <param name="minimumSampleSize">Minimum impressions required per variant.</param>
    /// <param name="confidenceThreshold">Confidence level required to declare a winner (90, 95, or 99).</param>
    /// <returns>Statistical analysis results including winner determination and confidence level.</returns>
    ABTestStatisticsResult Analyze(
        long impressionsA,
        long impressionsB,
        long conversionsA,
        long conversionsB,
        int minimumSampleSize,
        int confidenceThreshold);
}
