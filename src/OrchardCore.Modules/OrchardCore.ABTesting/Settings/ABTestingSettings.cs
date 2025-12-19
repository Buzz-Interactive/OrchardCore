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
}
