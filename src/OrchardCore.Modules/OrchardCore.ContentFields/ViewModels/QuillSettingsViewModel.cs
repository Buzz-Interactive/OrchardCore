using System.Collections.Generic;
using System.Linq;
using OrchardCore.ContentFields.Settings;

namespace OrchardCore.ContentFields.ViewModels;

/// <summary>
/// View model for Quill editor settings form binding.
/// </summary>
public class QuillSettingsViewModel
{
    public QuillTheme Theme { get; set; }
    public List<string> CustomColors { get; set; } = new();
    public List<ToolbarGroupViewModel> Groups { get; set; } = new();

    /// <summary>
    /// Converts form values to QuillToolbarConfig.
    /// </summary>
    public QuillToolbarConfig ToToolbarConfig()
    {
        var config = new QuillToolbarConfig
        {
            CustomColors = CustomColors ?? new List<string>(),
            Groups = Groups?.Select(g => new ToolbarGroup
            {
                Id = g.Id,
                Name = g.Name,
                Order = g.Order,
                Buttons = g.Buttons?.Select(b => new ToolbarButton
                {
                    Type = b.Type,
                    Value = b.Value,
                    Order = b.Order
                }).ToList() ?? new List<ToolbarButton>()
            }).ToList() ?? new List<ToolbarGroup>()
        };

        return config;
    }

    /// <summary>
    /// Creates ViewModel from QuillToolbarConfig.
    /// </summary>
    public static QuillSettingsViewModel FromToolbarConfig(QuillToolbarConfig config, QuillTheme theme)
    {
        return new QuillSettingsViewModel
        {
            Theme = theme,
            CustomColors = config?.CustomColors ?? new List<string>(),
            Groups = config?.Groups?.Select(g => new ToolbarGroupViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Order = g.Order,
                Buttons = g.Buttons?.Select(b => new ToolbarButtonViewModel
                {
                    Type = b.Type,
                    Value = b.Value,
                    Order = b.Order
                }).ToList() ?? new List<ToolbarButtonViewModel>()
            }).ToList() ?? new List<ToolbarGroupViewModel>()
        };
    }
}

/// <summary>
/// View model for a toolbar group.
/// </summary>
public class ToolbarGroupViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Order { get; set; }
    public List<ToolbarButtonViewModel> Buttons { get; set; } = new();
}

/// <summary>
/// View model for a toolbar button.
/// </summary>
public class ToolbarButtonViewModel
{
    public string Type { get; set; }
    public string Value { get; set; }
    public int Order { get; set; }
}
