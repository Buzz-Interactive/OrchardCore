using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Records;
using OrchardCore.ContentManagement;
using OrchardCore.Modules;
using YesSql;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for looking up A/B tests using the ABTestIndex.
/// </summary>
public class ABTestLookupService : IABTestLookupService
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;
    private readonly IClock _clock;

    public ABTestLookupService(
        ISession session,
        IContentManager contentManager,
        IClock clock)
    {
        _session = session;
        _contentManager = contentManager;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ABTestInfo> GetActiveTestForContentAsync(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        var utcNow = _clock.UtcNow;

        // Find any active, published test that contains this content item as either variant
        // and is within its scheduled date range
        var index = await _session.QueryIndex<ABTestIndex>(i =>
            i.Published &&
            i.IsActive &&
            (i.VariantAContentItemId == contentItemId || i.VariantBContentItemId == contentItemId) &&
            (i.ScheduledStartUtc == null || i.ScheduledStartUtc <= utcNow) &&
            (i.ScheduledEndUtc == null || i.ScheduledEndUtc > utcNow))
            .FirstOrDefaultAsync();

        if (index == null)
        {
            return null;
        }

        // Get the test content item and extract goal properties
        var testContentItem = await _contentManager.GetAsync(index.ABTestContentItemId, VersionOptions.Published);
        var testName = testContentItem?.DisplayText ?? "Unnamed Test";
        var abTestPart = testContentItem?.As<ABTestPart>();

        return new ABTestInfo
        {
            TestContentItemId = index.ABTestContentItemId,
            TestName = testName,
            VariantAContentItemId = index.VariantAContentItemId,
            VariantBContentItemId = index.VariantBContentItemId,
            PercentageA = index.PercentageA,
            IsVariantA = index.VariantAContentItemId == contentItemId,
            GoalType = abTestPart?.GoalType ?? GoalType.None,
            GoalSelector = abTestPart?.GoalSelector,
            GoalScrollPercentage = abTestPart?.GoalScrollPercentage ?? 50,
            GoalEventName = abTestPart?.GoalEventName,
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ABTestInfo>> GetActiveTestsAsync()
    {
        var utcNow = _clock.UtcNow;

        var indexes = await _session.QueryIndex<ABTestIndex>(i =>
            i.Published &&
            i.IsActive &&
            (i.ScheduledStartUtc == null || i.ScheduledStartUtc <= utcNow) &&
            (i.ScheduledEndUtc == null || i.ScheduledEndUtc > utcNow))
            .ListAsync();

        var results = new List<ABTestInfo>();

        foreach (var index in indexes)
        {
            var testContentItem = await _contentManager.GetAsync(index.ABTestContentItemId, VersionOptions.Published);
            var testName = testContentItem?.DisplayText ?? "Unnamed Test";
            var abTestPart = testContentItem?.As<ABTestPart>();

            results.Add(new ABTestInfo
            {
                TestContentItemId = index.ABTestContentItemId,
                TestName = testName,
                VariantAContentItemId = index.VariantAContentItemId,
                VariantBContentItemId = index.VariantBContentItemId,
                PercentageA = index.PercentageA,
                IsVariantA = true, // Default, not relevant for listing
                GoalType = abTestPart?.GoalType ?? GoalType.None,
                GoalSelector = abTestPart?.GoalSelector,
                GoalScrollPercentage = abTestPart?.GoalScrollPercentage ?? 50,
                GoalEventName = abTestPart?.GoalEventName,
            });
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<ABTestInfo> GetTestByIdAsync(string testContentItemId)
    {
        if (string.IsNullOrEmpty(testContentItemId))
        {
            return null;
        }

        var index = await _session.QueryIndex<ABTestIndex>(i =>
            i.ABTestContentItemId == testContentItemId && i.Published)
            .FirstOrDefaultAsync();

        if (index == null)
        {
            return null;
        }

        var testContentItem = await _contentManager.GetAsync(index.ABTestContentItemId, VersionOptions.Published);
        var testName = testContentItem?.DisplayText ?? "Unnamed Test";
        var abTestPart = testContentItem?.As<ABTestPart>();

        return new ABTestInfo
        {
            TestContentItemId = index.ABTestContentItemId,
            TestName = testName,
            VariantAContentItemId = index.VariantAContentItemId,
            VariantBContentItemId = index.VariantBContentItemId,
            PercentageA = index.PercentageA,
            IsVariantA = true,
            GoalType = abTestPart?.GoalType ?? GoalType.None,
            GoalSelector = abTestPart?.GoalSelector,
            GoalScrollPercentage = abTestPart?.GoalScrollPercentage ?? 50,
            GoalEventName = abTestPart?.GoalEventName,
        };
    }
}
