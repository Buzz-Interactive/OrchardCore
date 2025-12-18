using System.ComponentModel.DataAnnotations;
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
    /// The current status of the test based on published state.
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

    /// <summary>
    /// Indicates whether goal fields are locked and cannot be edited.
    /// Goals are locked once the test has been published AND has recorded impressions.
    /// </summary>
    [BindNever]
    public bool AreGoalsLocked { get; set; }

    /// <summary>
    /// Minimum number of impressions required per variant before statistical analysis is performed.
    /// </summary>
    [Range(30, 500, ErrorMessage = "Minimum sample size must be between 30 and 500.")]
    public int MinimumSampleSize { get; set; } = 50;

    /// <summary>
    /// The confidence level required to declare a winner (90, 95, or 99).
    /// </summary>
    public int ConfidenceThreshold { get; set; } = 90;
}

/// <summary>
/// Represents the current status of an A/B test.
/// </summary>
public enum ABTestStatus
{
    /// <summary>
    /// The test is not published (draft or unpublished).
    /// </summary>
    Inactive,

    /// <summary>
    /// The test is published and currently running.
    /// </summary>
    Running,
}
