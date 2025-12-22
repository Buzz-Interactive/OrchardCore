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
    /// Cached display name of Variant A (preserved when content item is deleted).
    /// </summary>
    public string VariantADisplayName { get; set; }

    /// <summary>
    /// Cached display name of Variant B (preserved when content item is deleted).
    /// </summary>
    public string VariantBDisplayName { get; set; }

    /// <summary>
    /// State of the Variant A content item.
    /// </summary>
    public VariantState VariantAState { get; set; } = VariantState.Available;

    /// <summary>
    /// State of the Variant B content item.
    /// </summary>
    public VariantState VariantBState { get; set; } = VariantState.Available;

    /// <summary>
    /// When Variant A became unavailable (unpublished or deleted).
    /// </summary>
    public DateTime? VariantAUnavailableSinceUtc { get; set; }

    /// <summary>
    /// When Variant B became unavailable (unpublished or deleted).
    /// </summary>
    public DateTime? VariantBUnavailableSinceUtc { get; set; }

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
    /// Time in seconds that the user must spend on the page for TimeOnPage goals.
    /// Valid range is 5-300 seconds. Default is 30 seconds.
    /// </summary>
    public int GoalTimeOnPageSeconds { get; set; } = 30;

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

    /// <summary>
    /// The winning variant (A or B), null if test not concluded.
    /// </summary>
    public ABVariant? WinningVariant { get; set; }

    /// <summary>
    /// When the winner was declared.
    /// </summary>
    public DateTime? ConcludedUtc { get; set; }

    /// <summary>
    /// Whether the test has been concluded (winner declared).
    /// </summary>
    public bool IsConcluded { get; set; }
}
