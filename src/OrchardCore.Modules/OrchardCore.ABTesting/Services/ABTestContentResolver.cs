namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for resolving which content item should be displayed
/// based on the visitor's A/B test assignment.
/// </summary>
public class ABTestContentResolver : IABTestContentResolver
{
    private readonly IABTestLookupService _lookupService;
    private readonly IVisitorAssignmentService _assignmentService;

    public ABTestContentResolver(
        IABTestLookupService lookupService,
        IVisitorAssignmentService assignmentService)
    {
        _lookupService = lookupService;
        _assignmentService = assignmentService;
    }

    /// <inheritdoc />
    public async Task<string> ResolveContentItemIdAsync(string requestedContentItemId)
    {
        if (string.IsNullOrEmpty(requestedContentItemId))
        {
            return requestedContentItemId;
        }

        // Check if this content item is part of an active test
        var testInfo = await _lookupService.GetActiveTestForContentAsync(requestedContentItemId);

        if (testInfo == null)
        {
            // Not part of any active test, return original
            return requestedContentItemId;
        }

        // Get or assign a variant for this visitor
        var assignedVariant = await _assignmentService.GetOrAssignVariantAsync(
            testInfo.TestId,
            testInfo.PercentageA);

        // Return the content item ID of the assigned variant
        return testInfo.GetVariantContentItemId(assignedVariant);
    }

    /// <inheritdoc />
    public async Task<bool> IsPartOfActiveTestAsync(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return false;
        }

        var testInfo = await _lookupService.GetActiveTestForContentAsync(contentItemId);
        return testInfo != null;
    }
}
