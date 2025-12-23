using OrchardCore.ABTesting.Models;
using OrchardCore.Entities;

namespace OrchardCore.ABTesting.Workflows.Models;

/// <summary>
/// Document that tracks when a workflow event has been triggered for an A/B test winner.
/// Used to prevent duplicate event triggers.
/// </summary>
public class ABTestWinnerTriggeredRecord : Entity
{
    /// <summary>
    /// The collection name for storing triggered records.
    /// </summary>
    public const string Collection = "ABTestWinnerTriggered";

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
