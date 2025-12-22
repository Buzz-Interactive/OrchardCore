namespace OrchardCore.ABTesting.Models;

/// <summary>
/// Information about an active A/B test, used for lookup and variant selection.
/// </summary>
public class ABTestInfo
{
    /// <summary>
    /// The unique identifier of the ABTest entity.
    /// </summary>
    public string TestId { get; set; }

    /// <summary>
    /// The display name of the test.
    /// </summary>
    public string TestName { get; set; }

    /// <summary>
    /// The ContentItemId of Variant A.
    /// </summary>
    public string VariantAContentItemId { get; set; }

    /// <summary>
    /// The ContentItemId of Variant B.
    /// </summary>
    public string VariantBContentItemId { get; set; }

    /// <summary>
    /// The percentage of traffic for Variant A (0-100).
    /// </summary>
    public int PercentageA { get; set; }

    /// <summary>
    /// Whether the requested content item is Variant A.
    /// If false, the requested content item is Variant B.
    /// </summary>
    public bool IsVariantA { get; set; }

    /// <summary>
    /// The type of goal configured for this test.
    /// </summary>
    public GoalType GoalType { get; set; }

    /// <summary>
    /// CSS selector for ButtonLinkClick or FormSubmission goals.
    /// </summary>
    public string GoalSelector { get; set; }

    /// <summary>
    /// Scroll percentage threshold (0-100) for ScrollPercentage goals.
    /// </summary>
    public int GoalScrollPercentage { get; set; }

    /// <summary>
    /// Custom JavaScript event name for CustomEvent goals.
    /// </summary>
    public string GoalEventName { get; set; }

    /// <summary>
    /// Time in seconds for TimeOnPage goals.
    /// </summary>
    public int GoalTimeOnPageSeconds { get; set; }

    /// <summary>
    /// Gets the ContentItemId of the variant that should be shown to the visitor.
    /// </summary>
    /// <param name="assignedVariant">The variant assigned to the visitor.</param>
    /// <returns>The ContentItemId of the variant to show.</returns>
    public string GetVariantContentItemId(ABVariant assignedVariant)
    {
        return assignedVariant == ABVariant.A ? VariantAContentItemId : VariantBContentItemId;
    }
}

/// <summary>
/// Represents a variant in an A/B test.
/// </summary>
public enum ABVariant
{
    /// <summary>
    /// Variant A (typically the control or original).
    /// </summary>
    A,

    /// <summary>
    /// Variant B (typically the challenger or test variant).
    /// </summary>
    B,
}
