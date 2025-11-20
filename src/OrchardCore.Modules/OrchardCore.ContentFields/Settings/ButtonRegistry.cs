using System.Collections.Generic;
using System.Linq;

namespace OrchardCore.ContentFields.Settings;

/// <summary>
/// Static registry of all available Quill toolbar button types and their metadata.
/// </summary>
public static class ButtonRegistry
{
    private static readonly Dictionary<string, ButtonMetadata> _buttons = new()
    {
        // Formatting buttons
        ["bold"] = new("bold", "Bold", "B", "Formatting", false),
        ["italic"] = new("italic", "Italic", "I", "Formatting", false),
        ["underline"] = new("underline", "Underline", "U", "Formatting", false),
        ["strike"] = new("strike", "Strikethrough", "S", "Formatting", false),
        ["code"] = new("code", "Inline Code", "<>", "Formatting", false),

        // Block buttons
        ["blockquote"] = new("blockquote", "Blockquote", "\"", "Blocks", false),
        ["code-block"] = new("code-block", "Code Block", "{ }", "Blocks", false),
        ["header"] = new("header", "Header", "H", "Blocks", true, new[] { "1", "2" }),

        // List buttons
        ["list"] = new("list", "List", "•", "Lists", true, new[] { "ordered", "bullet", "check" }),

        // Media buttons
        ["link"] = new("link", "Link", "🔗", "Media", false),
        ["image"] = new("image", "Image", "🖼", "Media", false),
        ["video"] = new("video", "Video", "🎥", "Media", false),
        ["formula"] = new("formula", "Formula", "∑", "Media", false),

        // Style buttons
        ["color"] = new("color", "Text Color", "A", "Styles", false),
        ["background"] = new("background", "Background Color", "■", "Styles", false),
        ["font"] = new("font", "Font Family", "Aa", "Styles", false),
        ["size"] = new("size", "Font Size", "T↕", "Styles", false),
        ["align"] = new("align", "Alignment", "≡", "Styles", false),

        // Advanced buttons
        ["script"] = new("script", "Script", "x²", "Advanced", true, new[] { "sub", "super" }),
        ["indent"] = new("indent", "Indent", "→", "Advanced", true, new[] { "-1", "+1" }),
        ["direction"] = new("direction", "Text Direction", "RTL", "Advanced", true, new[] { "rtl" }),
        ["clean"] = new("clean", "Remove Formatting", "⌧", "Advanced", false)
    };

    /// <summary>
    /// Get metadata for a specific button type.
    /// </summary>
    public static ButtonMetadata Get(string type)
    {
        return _buttons.TryGetValue(type, out var metadata)
            ? metadata
            : new ButtonMetadata(type, type, "?", "Unknown", false);
    }

    /// <summary>
    /// Get all available button types.
    /// </summary>
    public static IEnumerable<ButtonMetadata> All => _buttons.Values;

    /// <summary>
    /// Get buttons filtered by category.
    /// </summary>
    public static IEnumerable<ButtonMetadata> GetByCategory(string category)
    {
        return _buttons.Values.Where(b => b.Category == category);
    }

    /// <summary>
    /// Check if a button type is valid.
    /// </summary>
    public static bool IsValid(string type)
    {
        return _buttons.ContainsKey(type);
    }

    /// <summary>
    /// Get all available categories.
    /// </summary>
    public static IEnumerable<string> Categories => _buttons.Values
        .Select(b => b.Category)
        .Distinct()
        .OrderBy(c => c);
}
