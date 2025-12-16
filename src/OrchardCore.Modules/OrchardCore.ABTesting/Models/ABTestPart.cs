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
    /// Whether this A/B test is currently active.
    /// Inactive tests will not intercept requests.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
