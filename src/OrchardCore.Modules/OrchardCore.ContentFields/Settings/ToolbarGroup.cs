using System;
using System.Collections.Generic;

namespace OrchardCore.ContentFields.Settings;

/// <summary>
/// A group of toolbar buttons. Groups create visual separators in the toolbar.
/// </summary>
public class ToolbarGroup
{
    /// <summary>
    /// Unique identifier for UI binding and drag-and-drop tracking.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Optional display name for administrative reference (not shown to end users).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Position in the toolbar (ascending order).
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Buttons in this group.
    /// </summary>
    public List<ToolbarButton> Buttons { get; set; } = new();

    public ToolbarGroup()
    {
    }

    public ToolbarGroup(string name, int order = 0)
    {
        Name = name;
        Order = order;
    }

    public ToolbarGroup(string name, int order, params ToolbarButton[] buttons)
        : this(name, order)
    {
        Buttons = new List<ToolbarButton>(buttons);
    }
}
