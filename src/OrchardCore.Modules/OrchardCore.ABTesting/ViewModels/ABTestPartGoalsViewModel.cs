using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.ViewModels;

public class ABTestPartGoalsViewModel
{
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
    /// Indicates whether goal fields are locked and cannot be edited.
    /// Goals are locked once the test has been published AND has recorded impressions.
    /// </summary>
    [BindNever]
    public bool AreGoalsLocked { get; set; }
}
