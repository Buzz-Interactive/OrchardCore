namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for looking up A/B test redirect rules.
/// </summary>
public interface IABTestRedirectService
{
    /// <summary>
    /// Checks if a URL path should be redirected due to an active or concluded A/B test.
    /// </summary>
    /// <param name="path">The URL path to check.</param>
    /// <returns>
    /// The target redirect path if a redirect is needed, null otherwise.
    /// </returns>
    Task<string> GetRedirectPathAsync(string path);
}
