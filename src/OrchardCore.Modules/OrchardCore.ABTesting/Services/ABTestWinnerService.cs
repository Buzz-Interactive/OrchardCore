using OrchardCore.ABTesting.Models;
using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement;
using OrchardCore.Settings;
using OrchardCore.Title.Models;

#nullable enable

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for declaring winners in A/B tests.
/// </summary>
public class ABTestWinnerService : IABTestWinnerService
{
    // The key used in HomeRoute to identify the content item ID
    // This is configured in OrchardCore.Contents as "contentItemId"
    private const string ContentItemIdKey = "contentItemId";

    private readonly IABTestManager _abTestManager;
    private readonly IContentManager _contentManager;
    private readonly ISiteService _siteService;

    public ABTestWinnerService(
        IABTestManager abTestManager,
        IContentManager contentManager,
        ISiteService siteService)
    {
        _abTestManager = abTestManager;
        _contentManager = contentManager;
        _siteService = siteService;
    }

    /// <inheritdoc/>
    public async Task<bool> DeclareWinnerAsync(string testId, ABVariant winner)
    {
        var test = await _abTestManager.GetAsync(testId);
        if (test == null)
        {
            throw new InvalidOperationException($"Test with ID '{testId}' not found.");
        }

        if (test.IsConcluded)
        {
            throw new InvalidOperationException("This test has already been concluded.");
        }

        // Determine winner and loser content item IDs
        var winnerContentItemId = winner == ABVariant.A
            ? test.VariantAContentItemId
            : test.VariantBContentItemId;

        var loserContentItemId = winner == ABVariant.A
            ? test.VariantBContentItemId
            : test.VariantAContentItemId;

        // Get content items
        var winnerItem = await _contentManager.GetAsync(winnerContentItemId, VersionOptions.Latest);
        var loserItem = await _contentManager.GetAsync(loserContentItemId, VersionOptions.Latest);

        if (winnerItem == null)
        {
            throw new InvalidOperationException($"Winner content item '{winnerContentItemId}' not found.");
        }

        if (loserItem == null)
        {
            throw new InvalidOperationException($"Loser content item '{loserContentItemId}' not found.");
        }

        // If B wins, transfer route from A to B
        if (winner == ABVariant.B)
        {
            await TransferRouteAsync(loserItem, winnerItem);
            // Re-fetch the loser item after route transfer as it was published
            loserItem = await _contentManager.GetAsync(loserContentItemId, VersionOptions.Latest);
        }

        // Process the loser: append "[Variant – Not Selected]" to title and unpublish
        await ProcessLoserAsync(loserItem, routeAlreadyCleared: winner == ABVariant.B);

        // Update the ABTest
        test.WinningVariant = winner;
        test.IsConcluded = true;
        test.ConcludedUtc = DateTime.UtcNow;
        test.IsActive = false;
        await _abTestManager.UpdateAsync(test);

        return true;
    }

    private async Task TransferRouteAsync(ContentItem sourceItem, ContentItem targetItem)
    {
        // Get the PUBLISHED version of source to read the actual path in use
        var publishedSource = await _contentManager.GetAsync(sourceItem.ContentItemId, VersionOptions.Published);
        if (publishedSource == null)
        {
            // Source is not published, nothing to transfer
            return;
        }

        var publishedSourceAutoroute = publishedSource.As<AutoroutePart>();
        if (publishedSourceAutoroute == null)
        {
            // Source doesn't have AutoroutePart
            return;
        }

        var sourcePath = publishedSourceAutoroute.Path;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            // No path to transfer
            return;
        }

        // Check if source is the homepage (using same pattern as AutoroutePartDisplayDriver)
        var site = await _siteService.GetSiteSettingsAsync();
        var homeRoute = site.HomeRoute;
        var isSourceHomepage = homeRoute != null &&
            string.Equals(
                homeRoute[ContentItemIdKey]?.ToString(),
                sourceItem.ContentItemId,
                StringComparison.OrdinalIgnoreCase);

        // Step 1: Clear the source path first (to avoid uniqueness conflict)
        // Get a draft version to ensure PublishAsync triggers the handlers
        var sourceDraft = await _contentManager.GetAsync(sourceItem.ContentItemId, VersionOptions.DraftRequired);
        if (sourceDraft != null)
        {
            var sourceAutoroute = sourceDraft.As<AutoroutePart>();
            if (sourceAutoroute != null)
            {
                sourceAutoroute.Path = $"/archived/{sourceDraft.ContentItemId}";
                sourceAutoroute.SetHomepage = false;
                sourceDraft.Apply(sourceAutoroute);
                await _contentManager.PublishAsync(sourceDraft);
            }
        }

        // Step 2: Update target with the source's original path
        // Get a draft version to ensure PublishAsync triggers the handlers (including SetHomepage)
        var targetDraft = await _contentManager.GetAsync(targetItem.ContentItemId, VersionOptions.DraftRequired);
        if (targetDraft != null)
        {
            var targetAutoroute = targetDraft.As<AutoroutePart>();
            if (targetAutoroute != null)
            {
                targetAutoroute.Path = sourcePath;
                targetAutoroute.SetHomepage = isSourceHomepage;
                targetDraft.Apply(targetAutoroute);
                await _contentManager.PublishAsync(targetDraft);
            }
        }
    }

    private async Task ProcessLoserAsync(ContentItem loserItem, bool routeAlreadyCleared = false)
    {
        // Append "[Variant – Not Selected]" to title
        var titlePart = loserItem.As<TitlePart>();
        if (titlePart != null)
        {
            var newTitle = titlePart.Title;
            if (!newTitle.EndsWith("[Variant – Not Selected]"))
            {
                newTitle = $"{newTitle} [Variant – Not Selected]";
            }

            titlePart.Title = newTitle;
            loserItem.Apply(titlePart);
            loserItem.DisplayText = newTitle;
        }
        else
        {
            // If no TitlePart, just update DisplayText
            if (!loserItem.DisplayText.EndsWith("[Variant – Not Selected]"))
            {
                loserItem.DisplayText = $"{loserItem.DisplayText} [Variant – Not Selected]";
            }
        }

        // Clear the autoroute path if it hasn't been cleared already (skip if route was already handled)
        if (!routeAlreadyCleared)
        {
            var loserAutoroute = loserItem.As<AutoroutePart>();
            if (loserAutoroute != null && !string.IsNullOrWhiteSpace(loserAutoroute.Path))
            {
                loserAutoroute.Path = $"/archived/{loserItem.ContentItemId}";
                loserAutoroute.SetHomepage = false;
                loserItem.Apply(loserAutoroute);
            }
        }

        // Update and unpublish
        await _contentManager.UpdateAsync(loserItem);
        await _contentManager.UnpublishAsync(loserItem);
    }
}
