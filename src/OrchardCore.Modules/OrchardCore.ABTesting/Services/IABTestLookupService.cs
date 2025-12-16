using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for looking up A/B tests.
/// </summary>
public interface IABTestLookupService
{
    /// <summary>
    /// Checks if a content item is part of any active A/B test.
    /// </summary>
    /// <param name="contentItemId">The ContentItemId to check.</param>
    /// <returns>
    /// ABTestInfo if the content item is part of an active test, null otherwise.
    /// </returns>
    Task<ABTestInfo> GetActiveTestForContentAsync(string contentItemId);

    /// <summary>
    /// Gets all active A/B tests.
    /// </summary>
    /// <returns>A collection of all active tests.</returns>
    Task<IEnumerable<ABTestInfo>> GetActiveTestsAsync();

    /// <summary>
    /// Gets a specific A/B test by its ContentItemId.
    /// </summary>
    /// <param name="testContentItemId">The ContentItemId of the ABTest.</param>
    /// <returns>ABTestInfo if found, null otherwise.</returns>
    Task<ABTestInfo> GetTestByIdAsync(string testContentItemId);
}
