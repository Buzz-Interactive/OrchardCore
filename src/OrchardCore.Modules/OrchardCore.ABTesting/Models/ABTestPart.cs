using OrchardCore.ContentManagement;

namespace OrchardCore.ABTesting.Models;

/// <summary>
/// Content part that provides A/B testing configuration.
/// This part is attached to the ABTest content type.
/// </summary>
public class ABTestPart : ContentPart
{
    /// <summary>
    /// The percentage of traffic that should be directed to Variant A.
    /// Variant B receives (100 - PercentageA)% of traffic.
    /// </summary>
    public int PercentageA { get; set; } = 50;

    /// <summary>
    /// The type of goal to track for this A/B test.
    /// </summary>
    public GoalType GoalType { get; set; } = GoalType.None;

    /// <summary>
    /// CSS selector for ButtonLinkClick or FormSubmission goals.
    /// Example: "#signup-btn" or ".cta-button"
    /// </summary>
    public string GoalSelector { get; set; }

    /// <summary>
    /// Scroll percentage threshold (0-100) for ScrollPercentage goals.
    /// </summary>
    public int GoalScrollPercentage { get; set; } = 50;

    /// <summary>
    /// Custom JavaScript event name for CustomEvent goals.
    /// Example: "purchase-completed"
    /// </summary>
    public string GoalEventName { get; set; }
}
