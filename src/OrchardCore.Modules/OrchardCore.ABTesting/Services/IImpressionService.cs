using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for tracking and retrieving A/B test impressions.
/// </summary>
public interface IImpressionService
{
    /// <summary>
    /// Records an impression for the specified test and variant.
    /// </summary>
    /// <param name="testId">The unique identifier of the ABTest.</param>
    /// <param name="variant">The variant that was shown.</param>
    Task RecordImpressionAsync(string testId, ABVariant variant);

    /// <summary>
    /// Gets the impression counts for the specified test.
    /// </summary>
    /// <param name="testId">The unique identifier of the ABTest.</param>
    /// <returns>A tuple containing (VariantA impressions, VariantB impressions).</returns>
    Task<(long VariantA, long VariantB)> GetImpressionsAsync(string testId);
}
