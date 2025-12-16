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
}
