using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Records;
using OrchardCore.ContentManagement;
using YesSql;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for looking up A/B tests using the ABTestIndex.
/// </summary>
public class ABTestLookupService : IABTestLookupService
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;

    public ABTestLookupService(ISession session, IContentManager contentManager)
    {
        _session = session;
        _contentManager = contentManager;
    }

    /// <inheritdoc />
    public async Task<ABTestInfo> GetActiveTestForContentAsync(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        // Find any active, published test that contains this content item as either variant
        var index = await _session.QueryIndex<ABTestIndex>(i =>
            i.Published &&
            i.IsActive &&
            (i.VariantAContentItemId == contentItemId || i.VariantBContentItemId == contentItemId))
            .FirstOrDefaultAsync();

        if (index == null)
        {
            return null;
        }

        // Get the test name from the content item
        var testContentItem = await _contentManager.GetAsync(index.ABTestContentItemId, VersionOptions.Published);
        var testName = testContentItem?.DisplayText ?? "Unnamed Test";

        return new ABTestInfo
        {
            TestContentItemId = index.ABTestContentItemId,
            TestName = testName,
            VariantAContentItemId = index.VariantAContentItemId,
            VariantBContentItemId = index.VariantBContentItemId,
            PercentageA = index.PercentageA,
            IsVariantA = index.VariantAContentItemId == contentItemId,
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ABTestInfo>> GetActiveTestsAsync()
    {
        var indexes = await _session.QueryIndex<ABTestIndex>(i =>
            i.Published && i.IsActive)
            .ListAsync();

        var results = new List<ABTestInfo>();

        foreach (var index in indexes)
        {
            var testContentItem = await _contentManager.GetAsync(index.ABTestContentItemId, VersionOptions.Published);
            var testName = testContentItem?.DisplayText ?? "Unnamed Test";

            results.Add(new ABTestInfo
            {
                TestContentItemId = index.ABTestContentItemId,
                TestName = testName,
                VariantAContentItemId = index.VariantAContentItemId,
                VariantBContentItemId = index.VariantBContentItemId,
                PercentageA = index.PercentageA,
                IsVariantA = true, // Default, not relevant for listing
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

        return new ABTestInfo
        {
            TestContentItemId = index.ABTestContentItemId,
            TestName = testName,
            VariantAContentItemId = index.VariantAContentItemId,
            VariantBContentItemId = index.VariantBContentItemId,
            PercentageA = index.PercentageA,
            IsVariantA = true,
        };
    }
}
