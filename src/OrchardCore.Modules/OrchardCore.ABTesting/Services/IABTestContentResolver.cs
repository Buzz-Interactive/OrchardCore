namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for resolving which content item should be displayed
/// when a visitor requests a content item that is part of an A/B test.
/// </summary>
public interface IABTestContentResolver
{
    /// <summary>
    /// Resolves the content item ID that should be displayed to the current visitor.
    /// If the content item is part of an active A/B test, returns the assigned variant's ID.
    /// Otherwise, returns the original content item ID.
    /// </summary>
    /// <param name="requestedContentItemId">The originally requested content item ID.</param>
    /// <returns>The content item ID that should be displayed.</returns>
    Task<string> ResolveContentItemIdAsync(string requestedContentItemId);

    /// <summary>
    /// Checks if the content item is part of an active A/B test.
    /// </summary>
    /// <param name="contentItemId">The content item ID to check.</param>
    /// <returns>True if the content item is part of an active test.</returns>
    Task<bool> IsPartOfActiveTestAsync(string contentItemId);
}
