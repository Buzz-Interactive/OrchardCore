using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.ABTesting.Models;
using OrchardCore.ContentFields.ViewModels;

namespace OrchardCore.ABTesting.ViewModels;

/// <summary>
/// View model for creating and editing ABTest entities.
/// </summary>
public class ABTestViewModel
{
    /// <summary>
    /// The unique identifier of the ABTest (null for new tests).
    /// </summary>
    [BindNever]
    public string TestId { get; set; }

    /// <summary>
    /// The display name of the test.
    /// </summary>
    [Required(ErrorMessage = "Test name is required.")]
    [StringLength(255, ErrorMessage = "Test name cannot exceed 255 characters.")]
    public string Name { get; set; }

    /// <summary>
    /// The ContentItemId of Variant A.
    /// </summary>
    [Required(ErrorMessage = "Variant A is required.")]
    public string VariantAContentItemId { get; set; }

    /// <summary>
    /// The ContentItemId of Variant B.
    /// </summary>
    [Required(ErrorMessage = "Variant B is required.")]
    public string VariantBContentItemId { get; set; }

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
    /// Selected item data for Variant A content picker (used by vue-multiselect).
    /// </summary>
    [BindNever]
    public VueMultiselectItemViewModel SelectedVariantA { get; set; }

    /// <summary>
    /// Selected item data for Variant B content picker (used by vue-multiselect).
    /// </summary>
    [BindNever]
    public VueMultiselectItemViewModel SelectedVariantB { get; set; }

    /// <summary>
    /// The percentage of traffic for Variant A (0-100).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100.")]
    public int PercentageA { get; set; } = 50;

    /// <summary>
    /// The calculated percentage for Variant B.
    /// </summary>
    public int PercentageB => 100 - PercentageA;

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
    [Range(0, 100, ErrorMessage = "Scroll percentage must be between 0 and 100.")]
    public int GoalScrollPercentage { get; set; } = 50;

    /// <summary>
    /// Custom JavaScript event name for CustomEvent goals.
    /// </summary>
    public string GoalEventName { get; set; }

    /// <summary>
    /// Minimum number of impressions required per variant before statistical analysis is performed.
    /// </summary>
    [Range(30, 500, ErrorMessage = "Minimum sample size must be between 30 and 500.")]
    public int MinimumSampleSize { get; set; } = 50;

    /// <summary>
    /// The confidence level required to declare a winner (90, 95, or 99).
    /// </summary>
    public int ConfidenceThreshold { get; set; } = 90;

    /// <summary>
    /// Whether the test is currently active.
    /// </summary>
    [BindNever]
    public bool IsActive { get; set; }

    /// <summary>
    /// Total impressions across both variants.
    /// </summary>
    [BindNever]
    public long TotalImpressions { get; set; }

    /// <summary>
    /// Total conversions across both variants.
    /// </summary>
    [BindNever]
    public long TotalConversions { get; set; }

    /// <summary>
    /// Indicates whether goal fields are locked and cannot be edited.
    /// Goals are locked once the test is active AND has recorded impressions.
    /// </summary>
    [BindNever]
    public bool AreGoalsLocked { get; set; }

    /// <summary>
    /// When the test was created.
    /// </summary>
    [BindNever]
    public DateTime? CreatedUtc { get; set; }
}
