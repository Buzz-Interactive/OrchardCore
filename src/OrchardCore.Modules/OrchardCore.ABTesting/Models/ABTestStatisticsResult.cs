namespace OrchardCore.ABTesting.Models;

/// <summary>
/// Contains the statistical analysis results for an A/B test.
/// </summary>
public class ABTestStatisticsResult
{
    /// <summary>
    /// The winning variant, or null if there is no statistically significant winner.
    /// </summary>
    public ABVariant? WinningVariant { get; set; }

    /// <summary>
    /// The confidence level at which the winner was determined (90, 95, or 99).
    /// Only meaningful when IsSignificant is true.
    /// </summary>
    public int ConfidenceLevel { get; set; }

    /// <summary>
    /// Whether the difference between variants is statistically significant.
    /// </summary>
    public bool IsSignificant { get; set; }

    /// <summary>
    /// The percentage improvement (lift) of the winning variant over the losing variant.
    /// Positive value indicates the winner's conversion rate is higher.
    /// </summary>
    public double Lift { get; set; }

    /// <summary>
    /// Whether there is sufficient data to perform statistical analysis.
    /// Requires minimum impressions per variant.
    /// </summary>
    public bool HasSufficientData { get; set; }

    /// <summary>
    /// A human-readable summary of the statistical analysis.
    /// </summary>
    public string SummaryText { get; set; }

    /// <summary>
    /// The Bayesian probability (0-100) that Variant A has the higher true conversion rate.
    /// Calculated using Monte Carlo simulation with Beta distributions.
    /// </summary>
    public double ProbabilityToBeBestA { get; set; }

    /// <summary>
    /// The Bayesian probability (0-100) that Variant B has the higher true conversion rate.
    /// Calculated using Monte Carlo simulation with Beta distributions.
    /// </summary>
    public double ProbabilityToBeBestB { get; set; }

    /// <summary>
    /// The percentage uplift of Variant B relative to Variant A (the control).
    /// Calculated as (RateB - RateA) / RateA * 100.
    /// Positive values indicate B is performing better, negative values indicate worse.
    /// Null when Variant A has zero conversions (uplift cannot be calculated).
    /// </summary>
    public double? UpliftB { get; set; }
}
