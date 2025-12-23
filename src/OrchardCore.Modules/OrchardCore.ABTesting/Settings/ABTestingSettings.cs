namespace OrchardCore.ABTesting.Settings;

/// <summary>
/// Site settings for A/B Testing module.
/// </summary>
public class ABTestingSettings
{
    /// <summary>
    /// When true, all content types can be selected for A/B tests.
    /// When false, only content types in <see cref="AllowedContentTypes"/> can be selected.
    /// </summary>
    public bool DisplayAllContentTypes { get; set; } = true;

    /// <summary>
    /// The list of content type names that can be selected for A/B tests.
    /// Only used when <see cref="DisplayAllContentTypes"/> is false.
    /// </summary>
    public string[] AllowedContentTypes { get; set; } = [];

    /// <summary>
    /// The minimum allowed value for sample size per variant that tests can be configured with.
    /// This sets the lower bound for the MinimumSampleSize setting on individual tests.
    /// Default is 30. Valid range is 10-100.
    /// </summary>
    public int MinimumSampleSizeLimit { get; set; } = 30;

    /// <summary>
    /// When true, bot detection is enabled and bot traffic will be excluded from A/B test tracking.
    /// </summary>
    public bool EnableBotDetection { get; set; } = true;

    /// <summary>
    /// The list of User-Agent substrings to match when detecting bots.
    /// Matching is case-insensitive.
    /// </summary>
    public string[] BotUserAgentPatterns { get; set; } = DefaultBotPatterns;

    /// <summary>
    /// Default list of common bot User-Agent patterns.
    /// </summary>
    public static readonly string[] DefaultBotPatterns =
    [
        // Major search engine crawlers
        "Googlebot",
        "bingbot",
        "Baiduspider",
        "YandexBot",
        "DuckDuckBot",
        "Slurp",
        "Sogou",
        "Exabot",
        "facebot",
        "ia_archiver",

        // Social media
        "Twitterbot",
        "LinkedInBot",
        "Pinterest",
        "WhatsApp",
        "TelegramBot",
        "Discordbot",
        "Slackbot",

        // SEO/Analytics tools
        "AhrefsBot",
        "SemrushBot",
        "MJ12bot",
        "DotBot",
        "PetalBot",

        // Generic bot indicators
        "bot",
        "crawler",
        "spider",
        "scraper",
        "headless",
        "phantom",
        "selenium",
        "puppeteer",
        "playwright",

        // Monitoring and uptime
        "UptimeRobot",
        "Pingdom",
        "GTmetrix",
        "Site24x7",
        "StatusCake",

        // Feed readers
        "Feedly",
        "Feedspot",

        // HTTP clients
        "curl",
        "wget",
        "python-requests",
        "Go-http-client",
        "Apache-HttpClient",
        "libwww-perl",
    ];
}
