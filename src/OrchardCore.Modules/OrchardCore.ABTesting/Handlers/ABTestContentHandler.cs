using OrchardCore.ABTesting.Indexes;
using OrchardCore.ABTesting.Models;
using OrchardCore.ContentManagement.Handlers;
using YesSql;

namespace OrchardCore.ABTesting.Handlers;

/// <summary>
/// Content handler that responds to content item deletion and unpublishing
/// to update affected A/B tests.
/// </summary>
public class ABTestContentHandler : ContentHandlerBase
{
    private readonly ISession _session;

    public ABTestContentHandler(ISession session)
    {
        _session = session;
    }

    /// <summary>
    /// When a content item is unpublished, mark affected variants and deactivate the test.
    /// </summary>
    public override async Task UnpublishedAsync(PublishContentContext context)
    {
        var contentItemId = context.ContentItem.ContentItemId;
        var displayName = context.ContentItem.DisplayText ?? contentItemId;

        await UpdateAffectedTestsAsync(
            contentItemId,
            displayName,
            VariantState.Unpublished);
    }

    /// <summary>
    /// When a content item is republished, restore variant state if previously unpublished.
    /// </summary>
    public override async Task PublishedAsync(PublishContentContext context)
    {
        var contentItemId = context.ContentItem.ContentItemId;
        var displayName = context.ContentItem.DisplayText ?? contentItemId;

        await RestoreUnpublishedVariantsAsync(contentItemId, displayName);
    }

    /// <summary>
    /// When a content item is permanently removed, mark affected variants as deleted.
    /// </summary>
    public override async Task RemovedAsync(RemoveContentContext context)
    {
        // Only handle when all versions are removed (permanent deletion)
        if (!context.NoActiveVersionLeft)
        {
            return;
        }

        var contentItemId = context.ContentItem.ContentItemId;
        var displayName = context.ContentItem.DisplayText ?? contentItemId;

        await UpdateAffectedTestsAsync(
            contentItemId,
            displayName,
            VariantState.Deleted);
    }

    private async Task UpdateAffectedTestsAsync(
        string contentItemId,
        string displayName,
        VariantState newState)
    {
        // Find all tests that reference this content item as either variant
        var affectedTests = await _session.Query<ABTest, ABTestIndex>(collection: ABTest.Collection)
            .Where(i => i.VariantAContentItemId == contentItemId ||
                       i.VariantBContentItemId == contentItemId)
            .ListAsync();

        foreach (var test in affectedTests)
        {
            var wasModified = false;
            var now = DateTime.UtcNow;

            // Update Variant A if it matches
            if (test.VariantAContentItemId == contentItemId)
            {
                test.VariantADisplayName = displayName;
                test.VariantAState = newState;
                test.VariantAUnavailableSinceUtc = now;
                wasModified = true;
            }

            // Update Variant B if it matches
            if (test.VariantBContentItemId == contentItemId)
            {
                test.VariantBDisplayName = displayName;
                test.VariantBState = newState;
                test.VariantBUnavailableSinceUtc = now;
                wasModified = true;
            }

            // Deactivate the test if it was active
            if (wasModified && test.IsActive)
            {
                test.IsActive = false;
            }

            if (wasModified)
            {
                test.ModifiedUtc = now;
                await _session.SaveAsync(test, collection: ABTest.Collection);
            }
        }
    }

    private async Task RestoreUnpublishedVariantsAsync(
        string contentItemId,
        string displayName)
    {
        // Find tests where this content item was unpublished (not deleted)
        var affectedTests = await _session.Query<ABTest, ABTestIndex>(collection: ABTest.Collection)
            .Where(i => i.VariantAContentItemId == contentItemId ||
                       i.VariantBContentItemId == contentItemId)
            .ListAsync();

        foreach (var test in affectedTests)
        {
            var wasModified = false;

            // Restore Variant A if it was unpublished (not deleted)
            if (test.VariantAContentItemId == contentItemId &&
                test.VariantAState == VariantState.Unpublished)
            {
                test.VariantADisplayName = displayName;
                test.VariantAState = VariantState.Available;
                test.VariantAUnavailableSinceUtc = null;
                wasModified = true;
            }

            // Restore Variant B if it was unpublished (not deleted)
            if (test.VariantBContentItemId == contentItemId &&
                test.VariantBState == VariantState.Unpublished)
            {
                test.VariantBDisplayName = displayName;
                test.VariantBState = VariantState.Available;
                test.VariantBUnavailableSinceUtc = null;
                wasModified = true;
            }

            if (wasModified)
            {
                test.ModifiedUtc = DateTime.UtcNow;
                await _session.SaveAsync(test, collection: ABTest.Collection);
            }
        }
    }
}
