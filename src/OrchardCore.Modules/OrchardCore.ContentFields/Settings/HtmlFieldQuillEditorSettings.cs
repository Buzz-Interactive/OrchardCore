namespace OrchardCore.ContentFields.Settings;

/// <summary>
/// Settings for configuring the Quill editor on HTML fields.
/// Stored in content type field definitions.
/// </summary>
public class HtmlFieldQuillEditorSettings
{
    /// <summary>
    /// The Quill theme to use (Snow or Bubble)
    /// </summary>
    public QuillTheme Theme { get; set; } = QuillTheme.Snow;

    /// <summary>
    /// Toolbar configuration with enabled buttons and custom colors
    /// </summary>
    public QuillToolbarConfig ToolbarConfig { get; set; } = new QuillToolbarConfig();

    /// <summary>
    /// Generates Quill-compatible toolbar configuration JSON.
    /// Delegates to ToolbarConfig.GenerateQuillJson().
    /// </summary>
    public string GenerateQuillJson()
    {
        return ToolbarConfig?.GenerateQuillJson() ?? "[]";
    }
}
