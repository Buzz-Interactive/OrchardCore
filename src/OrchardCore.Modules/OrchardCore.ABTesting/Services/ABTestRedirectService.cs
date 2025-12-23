using OrchardCore.ABTesting.Indexes;
using OrchardCore.ABTesting.Models;
using OrchardCore.ContentManagement.Routing;
using YesSql;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for looking up A/B test redirect rules.
/// Redirects direct visits to Variant B's URL back to Variant A during active tests.
/// </summary>
public class ABTestRedirectService : IABTestRedirectService
{
    private readonly ISession _session;
    private readonly IAutorouteEntries _autorouteEntries;

    public ABTestRedirectService(ISession session, IAutorouteEntries autorouteEntries)
    {
        _session = session;
        _autorouteEntries = autorouteEntries;
    }

    /// <inheritdoc/>
    public async Task<string> GetRedirectPathAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        // Normalize path (remove leading/trailing slashes for comparison)
        var normalizedPath = path.Trim('/');

        // Query for active tests where the path matches Variant B's original path
        var test = await _session.Query<ABTest, ABTestIndex>(collection: ABTest.Collection)
            .Where(i => i.IsActive && i.VariantBOriginalPath == normalizedPath)
            .FirstOrDefaultAsync();

        if (test == null)
        {
            return null;
        }

        // Get Variant A's current path
        var (found, entry) = await _autorouteEntries.TryGetEntryByContentItemIdAsync(test.VariantAContentItemId);
        if (found && !string.IsNullOrEmpty(entry.Path))
        {
            return "/" + entry.Path.TrimStart('/');
        }

        return null;
    }
}
