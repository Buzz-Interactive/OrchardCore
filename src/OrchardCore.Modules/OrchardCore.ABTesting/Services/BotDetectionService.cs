using System.Text.RegularExpressions;
using OrchardCore.ABTesting.Settings;
using OrchardCore.Settings;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service that detects bots by matching User-Agent strings against configured patterns.
/// </summary>
public sealed class BotDetectionService : IBotDetectionService
{
    private readonly ISiteService _siteService;
    private Regex _botPatternRegex;
    private string _cachedPatternsKey;

    public BotDetectionService(ISiteService siteService)
    {
        _siteService = siteService;
    }

    /// <inheritdoc />
    public bool IsBot(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return false;
        }

        var settings = GetSettings();

        if (!settings.EnableBotDetection)
        {
            return false;
        }

        var patterns = settings.BotUserAgentPatterns;
        if (patterns == null || patterns.Length == 0)
        {
            return false;
        }

        // Build and cache the regex if patterns have changed
        var patternsKey = string.Join("|", patterns);
        if (_botPatternRegex == null || _cachedPatternsKey != patternsKey)
        {
            _cachedPatternsKey = patternsKey;
            _botPatternRegex = new Regex(
                string.Join("|", patterns.Select(Regex.Escape)),
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        return _botPatternRegex.IsMatch(userAgent);
    }

    private ABTestingSettings GetSettings()
    {
        var site = _siteService.GetSiteSettingsAsync().GetAwaiter().GetResult();
        return site.As<ABTestingSettings>();
    }
}
