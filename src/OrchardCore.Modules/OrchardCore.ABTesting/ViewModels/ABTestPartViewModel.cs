using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.ViewModels;

public class ABTestPartViewModel
{
    /// <summary>
    /// The percentage of traffic for Variant A (0-100).
    /// </summary>
    public int PercentageA { get; set; } = 50;

    /// <summary>
    /// The calculated percentage for Variant B.
    /// </summary>
    public int PercentageB => 100 - PercentageA;

    /// <summary>
    /// Whether the test is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The scheduled start date in UTC (for display purposes).
    /// </summary>
    [BindNever]
    public DateTime? ScheduledStartUtc { get; set; }

    /// <summary>
    /// The scheduled start date in local time (for form binding).
    /// </summary>
    public DateTime? ScheduledStartLocalDateTime { get; set; }

    /// <summary>
    /// The scheduled end date in UTC (for display purposes).
    /// </summary>
    [BindNever]
    public DateTime? ScheduledEndUtc { get; set; }

    /// <summary>
    /// The scheduled end date in local time (for form binding).
    /// </summary>
    public DateTime? ScheduledEndLocalDateTime { get; set; }

    /// <summary>
    /// Display name of Variant A (from linked content item).
    /// </summary>
    [BindNever]
    public string VariantADisplayText { get; set; }

    /// <summary>
    /// Display name of Variant B (from linked content item).
    /// </summary>
    [BindNever]
    public string VariantBDisplayText { get; set; }

    /// <summary>
    /// Reference to the content part.
    /// </summary>
    [BindNever]
    public ABTestPart ABTestPart { get; set; }

    /// <summary>
    /// Total impressions across both variants.
    /// </summary>
    [BindNever]
    public long TotalImpressions { get; set; }

    /// <summary>
    /// The current status of the test based on schedule and IsActive flag.
    /// </summary>
    [BindNever]
    public ABTestStatus Status { get; set; }

    /// <summary>
    /// The type of goal to track for this A/B test.
    /// </summary>
    public GoalType GoalType { get; set; } = GoalType.None;

    /// <summary>
    /// CSS selector for ButtonLinkClick or FormSubmission goals.
    /// </summary>
    public string GoalSelector { get; set; }

    /// <summary>
    /// Scroll percentage threshold (0-100) for ScrollPercentage goals.
    /// </summary>
    public int GoalScrollPercentage { get; set; } = 50;

    /// <summary>
    /// Custom JavaScript event name for CustomEvent goals.
    /// </summary>
    public string GoalEventName { get; set; }

    /// <summary>
    /// Total conversions across both variants.
    /// </summary>
    [BindNever]
    public long TotalConversions { get; set; }
}

/// <summary>
/// Represents the current status of an A/B test.
/// </summary>
public enum ABTestStatus
{
    /// <summary>
    /// The test is inactive (IsActive = false).
    /// </summary>
    Inactive,

    /// <summary>
    /// The test is scheduled to start in the future.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The test is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// The test has ended (past the scheduled end date).
    /// </summary>
    Ended,
}
