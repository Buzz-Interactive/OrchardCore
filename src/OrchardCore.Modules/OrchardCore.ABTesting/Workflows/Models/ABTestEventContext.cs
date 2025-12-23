using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Workflows.Models;

/// <summary>
/// Context data passed to workflow events when an A/B test winner is detected.
/// </summary>
public class ABTestEventContext
{
    /// <summary>
    /// The unique identifier of the A/B test.
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// The display name of the A/B test.
    /// </summary>
    public string TestName { get; set; }

    /// <summary>
    /// The detected winning variant (A or B).
    /// </summary>
    public ABVariant WinningVariant { get; set; }

    /// <summary>
    /// The confidence level at which the winner was determined (90, 95, or 99).
    /// </summary>
    public int ConfidenceLevel { get; set; }

    /// <summary>
    /// The percentage improvement (lift) of the winner over the loser.
    /// </summary>
    public double Lift { get; set; }

    /// <summary>
    /// Total impressions for Variant A.
    /// </summary>
    public long ImpressionsA { get; set; }

    /// <summary>
    /// Total impressions for Variant B.
    /// </summary>
    public long ImpressionsB { get; set; }

    /// <summary>
    /// Total conversions for Variant A.
    /// </summary>
    public long ConversionsA { get; set; }

    /// <summary>
    /// Total conversions for Variant B.
    /// </summary>
    public long ConversionsB { get; set; }

    /// <summary>
    /// Bayesian probability (0-100) that Variant A is the best.
    /// </summary>
    public double ProbabilityToBeBestA { get; set; }

    /// <summary>
    /// Bayesian probability (0-100) that Variant B is the best.
    /// </summary>
    public double ProbabilityToBeBestB { get; set; }

    /// <summary>
    /// ContentItemId of Variant A.
    /// </summary>
    public string VariantAContentItemId { get; set; }

    /// <summary>
    /// ContentItemId of Variant B.
    /// </summary>
    public string VariantBContentItemId { get; set; }
}
