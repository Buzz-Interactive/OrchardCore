namespace OrchardCore.ContentFields.Settings;

/// <summary>
/// A single button in the Quill toolbar.
/// </summary>
public class ToolbarButton
{
    /// <summary>
    /// Button type: "bold", "italic", "header", "list", etc.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Value for parameterized buttons: "1" for header 1, "ordered" for ordered list, null for simple buttons.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Position within the group (ascending order).
    /// </summary>
    public int Order { get; set; }

    public ToolbarButton()
    {
    }

    public ToolbarButton(string type, string value = null, int order = 0)
    {
        Type = type;
        Value = value;
        Order = order;
    }
}
