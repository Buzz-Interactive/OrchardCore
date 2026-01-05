using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Workflows.Models;

/// <summary>
/// Context data passed to workflow events when an A/B test winner is declared.
/// </summary>
public class ABTestWinnerDeclaredContext
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
    /// The declared winning variant (A or B).
    /// </summary>
    public ABVariant WinningVariant { get; set; }

    /// <summary>
    /// ContentItemId of Variant A.
    /// </summary>
    public string VariantAContentItemId { get; set; }

    /// <summary>
    /// ContentItemId of Variant B.
    /// </summary>
    public string VariantBContentItemId { get; set; }

    /// <summary>
    /// ContentItemId of the winning variant.
    /// </summary>
    public string WinnerContentItemId { get; set; }

    /// <summary>
    /// ContentItemId of the losing variant.
    /// </summary>
    public string LoserContentItemId { get; set; }
}
