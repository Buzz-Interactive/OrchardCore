using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for looking up A/B tests using the ABTestManager.
/// </summary>
public class ABTestLookupService : IABTestLookupService
{
    private readonly IABTestManager _abTestManager;

    public ABTestLookupService(IABTestManager abTestManager)
    {
        _abTestManager = abTestManager;
    }

    /// <inheritdoc />
    public async Task<ABTestInfo> GetActiveTestForContentAsync(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        // Find any active test that contains this content item as either variant
        var test = await _abTestManager.GetByVariantAsync(contentItemId);
        if (test == null)
        {
            return null;
        }

        return MapToInfo(test, contentItemId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ABTestInfo>> GetActiveTestsAsync()
    {
        var tests = await _abTestManager.GetActiveAsync();

        return tests.Select(test => MapToInfo(test, null));
    }

    /// <inheritdoc />
    public async Task<ABTestInfo> GetTestByIdAsync(string testId)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return null;
        }

        var test = await _abTestManager.GetAsync(testId);
        if (test == null || !test.IsActive)
        {
            return null;
        }

        return MapToInfo(test, null);
    }

    private static ABTestInfo MapToInfo(ABTest test, string requestedContentItemId)
    {
        return new ABTestInfo
        {
            TestId = test.TestId,
            TestName = test.Name ?? "Unnamed Test",
            VariantAContentItemId = test.VariantAContentItemId,
            VariantBContentItemId = test.VariantBContentItemId,
            PercentageA = test.PercentageA,
            IsVariantA = requestedContentItemId == null || test.VariantAContentItemId == requestedContentItemId,
            GoalType = test.GoalType,
            GoalSelector = test.GoalSelector,
            GoalScrollPercentage = test.GoalScrollPercentage,
            GoalEventName = test.GoalEventName,
            GoalTimeOnPageSeconds = test.GoalTimeOnPageSeconds,
        };
    }
}
