using OrchardCore.Entities;

namespace OrchardCore.ABTesting.Models;

/// <summary>
/// Standalone entity representing an A/B test.
/// Stored in a custom YesSql collection (not as a content item).
/// </summary>
public class ABTest : Entity
{
    /// <summary>
    /// The collection name for storing ABTest entities.
    /// </summary>
    public const string Collection = "ABTest";

    /// <summary>
    /// Unique identifier for the test (26-character format).
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// Display name of the A/B test.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Whether the test is currently active (running).
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The ContentItemId of Variant A.
    /// </summary>
    public string VariantAContentItemId { get; set; }

    /// <summary>
    /// The ContentItemId of Variant B.
    /// </summary>
    public string VariantBContentItemId { get; set; }

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

    /// <summary>
    /// When the test was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// When the test was last modified.
    /// </summary>
    public DateTime? ModifiedUtc { get; set; }
}
