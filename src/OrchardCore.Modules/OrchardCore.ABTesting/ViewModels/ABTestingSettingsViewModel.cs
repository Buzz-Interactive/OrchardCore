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
    /// When true, bot detection is enabled.
    /// </summary>
    public bool EnableBotDetection { get; set; } = true;

    /// <summary>
    /// Newline-separated list of bot User-Agent patterns.
    /// Displayed as a textarea for easier editing.
    /// </summary>
    public string BotUserAgentPatternsText { get; set; } = string.Empty;
}
