using OrchardCore.ABTesting.Models;
using YesSql.Indexes;

namespace OrchardCore.ABTesting.Workflows.Indexes;

/// <summary>
/// YesSql index for tracking A/B test workflow event triggers.
/// </summary>
public class ABTestWinnerTriggeredIndex : MapIndex
{
    /// <summary>
    /// The unique identifier of the A/B test.
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// When the workflow event was triggered.
    /// </summary>
    public DateTime TriggeredUtc { get; set; }

    /// <summary>
    /// The confidence level at which the winner was detected.
    /// </summary>
    public int ConfidenceLevel { get; set; }

    /// <summary>
    /// The winning variant that was detected.
    /// </summary>
    public ABVariant WinningVariant { get; set; }
}
