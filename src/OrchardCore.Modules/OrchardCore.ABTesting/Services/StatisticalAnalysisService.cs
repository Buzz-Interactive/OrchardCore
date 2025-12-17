using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Implements statistical analysis for A/B test results using the Z-test for two proportions.
/// </summary>
public sealed class StatisticalAnalysisService : IStatisticalAnalysisService
{
    /// <summary>
    /// Z-score thresholds for different confidence levels (two-tailed test).
    /// </summary>
    private const double ZScore90 = 1.645;  // 90% confidence (p < 0.10)
    private const double ZScore95 = 1.96;   // 95% confidence (p < 0.05)
    private const double ZScore99 = 2.576;  // 99% confidence (p < 0.01)

    public ABTestStatisticsResult Analyze(
        long impressionsA,
        long impressionsB,
        long conversionsA,
        long conversionsB,
        int minimumSampleSize,
        int confidenceThreshold)
    {
        var result = new ABTestStatisticsResult();

        // Check for sufficient sample size
        if (impressionsA < minimumSampleSize || impressionsB < minimumSampleSize)
        {
            result.HasSufficientData = false;
            result.IsSignificant = false;
            result.SummaryText = $"Not enough data yet. Need at least {minimumSampleSize} impressions per variant for statistical analysis.";
            return result;
        }

        result.HasSufficientData = true;

        // Calculate conversion rates
        var rateA = (double)conversionsA / impressionsA;
        var rateB = (double)conversionsB / impressionsB;

        // Handle edge case where both rates are 0 or both are 1
        if ((rateA == 0 && rateB == 0) || (rateA == 1 && rateB == 1))
        {
            result.IsSignificant = false;
            result.SummaryText = "No clear winner yet. Both variants have identical conversion rates.";
            return result;
        }

        // Calculate pooled proportion
        var totalConversions = conversionsA + conversionsB;
        var totalImpressions = impressionsA + impressionsB;
        var pooledProportion = (double)totalConversions / totalImpressions;

        // Handle edge case where pooled proportion is 0 or 1
        if (pooledProportion == 0 || pooledProportion == 1)
        {
            result.IsSignificant = false;
            result.SummaryText = "No clear winner yet. Results are not statistically significant.";
            return result;
        }

        // Calculate standard error
        var standardError = Math.Sqrt(
            pooledProportion * (1 - pooledProportion) * (1.0 / impressionsA + 1.0 / impressionsB)
        );

        // Handle edge case where standard error is 0
        if (standardError == 0)
        {
            result.IsSignificant = false;
            result.SummaryText = "No clear winner yet. Results are not statistically significant.";
            return result;
        }

        // Calculate Z-score
        var zScore = (rateA - rateB) / standardError;
        var absZScore = Math.Abs(zScore);

        // Get the required Z-score threshold based on confidence threshold setting
        var requiredZScore = GetZScoreForConfidence(confidenceThreshold);

        // Determine actual achieved confidence level
        int achievedConfidence;
        if (absZScore >= ZScore99)
        {
            achievedConfidence = 99;
        }
        else if (absZScore >= ZScore95)
        {
            achievedConfidence = 95;
        }
        else if (absZScore >= ZScore90)
        {
            achievedConfidence = 90;
        }
        else
        {
            achievedConfidence = 0;
        }

        // Check if we meet the required confidence threshold
        if (absZScore < requiredZScore)
        {
            result.IsSignificant = false;
            result.SummaryText = "No clear winner yet. Results are not statistically significant.";
            return result;
        }

        result.IsSignificant = true;
        result.ConfidenceLevel = achievedConfidence;

        // Determine winner and calculate lift
        if (rateA > rateB)
        {
            result.WinningVariant = ABVariant.A;
            result.Lift = rateB > 0 ? Math.Round((rateA - rateB) / rateB * 100, 1) : 0;
            result.SummaryText = FormatWinnerSummary("A", result.ConfidenceLevel, result.Lift);
        }
        else
        {
            result.WinningVariant = ABVariant.B;
            result.Lift = rateA > 0 ? Math.Round((rateB - rateA) / rateA * 100, 1) : 0;
            result.SummaryText = FormatWinnerSummary("B", result.ConfidenceLevel, result.Lift);
        }

        return result;
    }

    private static double GetZScoreForConfidence(int confidence)
    {
        return confidence switch
        {
            99 => ZScore99,
            95 => ZScore95,
            _ => ZScore90,
        };
    }

    private static string FormatWinnerSummary(string variant, int confidence, double lift)
    {
        var liftText = lift > 0 ? $" (+{lift}% lift)" : "";
        return $"Variant {variant} is winning with {confidence}% confidence{liftText}";
    }
}
