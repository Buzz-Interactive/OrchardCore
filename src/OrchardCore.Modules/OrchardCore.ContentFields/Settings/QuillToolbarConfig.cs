using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OrchardCore.ContentFields.Settings;

/// <summary>
/// Configuration for Quill.js toolbar with groups and buttons.
/// </summary>
public class QuillToolbarConfig
{
    /// <summary>
    /// Toolbar button groups. Groups create visual separators in the toolbar.
    /// </summary>
    public List<ToolbarGroup> Groups { get; set; } = new();

    /// <summary>
    /// Custom color palette for color/background pickers (hex codes like "#84BD00").
    /// </summary>
    public List<string> CustomColors { get; set; } = new();

    /// <summary>
    /// Validates the toolbar configuration.
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (Groups == null || Groups.Count == 0)
        {
            errors.Add("Toolbar must have at least one group.");
            return false;
        }

        var hasButtons = Groups.Any(g => g.Buttons != null && g.Buttons.Count > 0);
        if (!hasButtons)
        {
            errors.Add("Toolbar must have at least one button.");
            return false;
        }

        // Validate button types
        foreach (var group in Groups)
        {
            if (group.Buttons == null) continue;

            foreach (var button in group.Buttons)
            {
                if (string.IsNullOrEmpty(button.Type))
                {
                    errors.Add($"Button in group '{group.Name}' has no type.");
                    continue;
                }

                if (!ButtonRegistry.IsValid(button.Type))
                {
                    errors.Add($"Invalid button type: '{button.Type}'.");
                }

                var metadata = ButtonRegistry.Get(button.Type);
                if (metadata.RequiresValue && string.IsNullOrEmpty(button.Value))
                {
                    errors.Add($"Button '{button.Type}' requires a value.");
                }
            }
        }

        // Validate custom colors
        if (CustomColors != null)
        {
            foreach (var color in CustomColors)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$"))
                {
                    errors.Add($"Invalid hex color: '{color}'.");
                }
            }
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Generates Quill-compatible toolbar configuration JSON.
    /// </summary>
    public string GenerateQuillJson()
    {
        var toolbarGroups = new List<object>();

        foreach (var group in Groups.OrderBy(g => g.Order))
        {
            var groupArray = new List<object>();

            foreach (var button in group.Buttons.OrderBy(b => b.Order))
            {
                object buttonConfig = button.Type switch
                {
                    // Simple string buttons
                    "bold" or "italic" or "underline" or "strike" or "code"
                    or "blockquote" or "code-block" or "link" or "image"
                    or "video" or "formula" or "clean" => button.Type,

                    // Parameterized buttons (object notation) - with null-safe defaults
                    "header" => new { header = !string.IsNullOrEmpty(button.Value) ? int.Parse(button.Value) : 2 },
                    "list" => new { list = !string.IsNullOrEmpty(button.Value) ? button.Value : "bullet" },
                    "script" => new { script = !string.IsNullOrEmpty(button.Value) ? button.Value : "sub" },
                    "indent" => new { indent = !string.IsNullOrEmpty(button.Value) ? button.Value : "+1" },
                    "direction" => new { direction = !string.IsNullOrEmpty(button.Value) ? button.Value : "rtl" },

                    // Buttons with arrays
                    "color" => new { color = CustomColors.Count > 0 ? CustomColors.ToArray() : System.Array.Empty<string>() },
                    "background" => new { background = CustomColors.Count > 0 ? CustomColors.ToArray() : System.Array.Empty<string>() },
                    "font" => new { font = System.Array.Empty<string>() },
                    "size" => new { size = new object[] { "small", false, "large", "huge" } },
                    "align" => new { align = System.Array.Empty<string>() },

                    _ => button.Type
                };

                groupArray.Add(buttonConfig);
            }

            if (groupArray.Count > 0)
            {
                toolbarGroups.Add(groupArray);
            }
        }

        return JsonSerializer.Serialize(toolbarGroups, new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Creates a standard toolbar configuration.
    /// </summary>
    public static QuillToolbarConfig CreateStandard()
    {
        return new QuillToolbarConfig
        {
            Groups = new List<ToolbarGroup>
            {
                new("Formatting", 0,
                    new ToolbarButton("bold", null, 0),
                    new ToolbarButton("italic", null, 1)
                ),
                new("Blocks", 1,
                    new ToolbarButton("blockquote", null, 0),
                    new ToolbarButton("header", "1", 1),
                    new ToolbarButton("header", "2", 2)
                ),
                new("Lists", 2,
                    new ToolbarButton("list", "ordered", 0),
                    new ToolbarButton("list", "bullet", 1)
                ),
                new("Media", 3,
                    new ToolbarButton("link", null, 0)
                ),
                new("Advanced", 4,
                    new ToolbarButton("clean", null, 0)
                )
            }
        };
    }

    /// <summary>
    /// Creates a minimal toolbar configuration.
    /// </summary>
    public static QuillToolbarConfig CreateMinimal()
    {
        return new QuillToolbarConfig
        {
            Groups = new List<ToolbarGroup>
            {
                new("Formatting", 0,
                    new ToolbarButton("bold", null, 0),
                    new ToolbarButton("italic", null, 1)
                ),
                new("Advanced", 1,
                    new ToolbarButton("clean", null, 0)
                )
            }
        };
    }

    /// <summary>
    /// Creates a full-featured toolbar configuration.
    /// </summary>
    public static QuillToolbarConfig CreateFull()
    {
        return new QuillToolbarConfig
        {
            Groups = new List<ToolbarGroup>
            {
                new("Formatting", 0,
                    new ToolbarButton("bold", null, 0),
                    new ToolbarButton("italic", null, 1),
                    new ToolbarButton("underline", null, 2),
                    new ToolbarButton("strike", null, 3)
                ),
                new("Blocks", 1,
                    new ToolbarButton("blockquote", null, 0),
                    new ToolbarButton("code-block", null, 1)
                ),
                new("Headers", 2,
                    new ToolbarButton("header", "1", 0),
                    new ToolbarButton("header", "2", 1)
                ),
                new("Lists", 3,
                    new ToolbarButton("list", "ordered", 0),
                    new ToolbarButton("list", "bullet", 1),
                    new ToolbarButton("list", "check", 2)
                ),
                new("Media", 4,
                    new ToolbarButton("link", null, 0),
                    new ToolbarButton("image", null, 1),
                    new ToolbarButton("video", null, 2)
                ),
                new("Styles", 5,
                    new ToolbarButton("color", null, 0),
                    new ToolbarButton("background", null, 1),
                    new ToolbarButton("align", null, 2)
                ),
                new("Advanced", 6,
                    new ToolbarButton("script", "sub", 0),
                    new ToolbarButton("script", "super", 1),
                    new ToolbarButton("indent", "-1", 2),
                    new ToolbarButton("indent", "+1", 3),
                    new ToolbarButton("clean", null, 4)
                )
            }
        };
    }
}
