namespace OrchardCore.ABTesting.ViewModels;

/// <summary>
/// View model for the A/B Testing site settings page.
/// </summary>
public class ABTestingSettingsViewModel
{
    /// <summary>
    /// When true, all content types can be selected for A/B tests.
    /// </summary>
    public bool DisplayAllContentTypes { get; set; } = true;

    /// <summary>
    /// Array of selected content type names that are allowed for A/B tests.
    /// </summary>
    public string[] AllowedContentTypes { get; set; } = [];

    /// <summary>
    /// The minimum allowed value for sample size per variant that tests can be configured with.
    /// Valid range is 10-100. Default is 30.
    /// </summary>
    public int MinimumSampleSizeLimit { get; set; } = 30;

    /// <summary>
    /// When true, bot detection is enabled.
    /// </summary>
    public bool EnableBotDetection { get; set; } = true;

    /// <summary>
    /// Newline-separated list of bot User-Agent patterns.
    /// Displayed as a textarea for easier editing.
    /// </summary>
    public string BotUserAgentPatternsText { get; set; } = string.Empty;
}
