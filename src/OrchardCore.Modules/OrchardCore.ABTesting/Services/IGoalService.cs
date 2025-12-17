using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for tracking and retrieving A/B test goal conversions.
/// </summary>
public interface IGoalService
{
    /// <summary>
    /// Records a goal conversion for the specified test and variant.
    /// </summary>
    /// <param name="testContentItemId">The ContentItemId of the ABTest.</param>
    /// <param name="variant">The variant that achieved the conversion.</param>
    Task RecordConversionAsync(string testContentItemId, ABVariant variant);

    /// <summary>
    /// Gets the conversion counts for the specified test.
    /// </summary>
    /// <param name="testContentItemId">The ContentItemId of the ABTest.</param>
    /// <returns>A tuple containing (VariantA conversions, VariantB conversions).</returns>
    Task<(long VariantA, long VariantB)> GetConversionsAsync(string testContentItemId);
}
