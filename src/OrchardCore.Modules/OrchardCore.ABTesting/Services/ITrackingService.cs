using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for tracking and retrieving A/B test impressions and conversions.
/// </summary>
public interface ITrackingService
{
    /// <summary>
    /// Records an impression for a specific test and variant.
    /// </summary>
    /// <param name="testId">The A/B test ID.</param>
    /// <param name="variant">The variant that was shown.</param>
    Task RecordImpressionAsync(string testId, ABVariant variant);

    /// <summary>
    /// Gets the impression counts for both variants of a test.
    /// </summary>
    /// <param name="testId">The A/B test ID.</param>
    /// <returns>A tuple containing the impression counts for Variant A and Variant B.</returns>
    Task<(long VariantA, long VariantB)> GetImpressionsAsync(string testId);

    /// <summary>
    /// Records a goal conversion for a specific test and variant.
    /// </summary>
    /// <param name="testId">The A/B test ID.</param>
    /// <param name="variant">The variant that converted.</param>
    Task RecordConversionAsync(string testId, ABVariant variant);

    /// <summary>
    /// Gets the conversion counts for both variants of a test.
    /// </summary>
    /// <param name="testId">The A/B test ID.</param>
    /// <returns>A tuple containing the conversion counts for Variant A and Variant B.</returns>
    Task<(long VariantA, long VariantB)> GetConversionsAsync(string testId);
}
