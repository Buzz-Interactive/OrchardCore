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
}
