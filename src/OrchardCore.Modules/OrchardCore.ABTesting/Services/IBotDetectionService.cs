namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for detecting bot traffic based on User-Agent patterns.
/// </summary>
public interface IBotDetectionService
{
    /// <summary>
    /// Determines if the given User-Agent string matches known bot patterns.
    /// </summary>
    /// <param name="userAgent">The User-Agent string to check.</param>
    /// <returns>True if the User-Agent appears to be a bot; otherwise, false.</returns>
    bool IsBot(string userAgent);
}
