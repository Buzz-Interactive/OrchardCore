using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.ABTesting.Settings;
using OrchardCore.Navigation;

namespace OrchardCore.ABTesting;

public sealed class AdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary _contentRouteValues = new()
    {
        { "contentTypeId", "ABTest" },
        { "Area", "OrchardCore.Contents" },
        { "Options.SelectedContentType", "ABTest" },
        { "Options.CanCreateSelectedContentType", true },
    };

    private static readonly RouteValueDictionary _settingsRouteValues = new()
    {
        { "area", "OrchardCore.Settings" },
        { "groupId", ABTestingSettingsGroup.Id },
    };

    internal readonly IStringLocalizer S;

    public AdminMenu(IStringLocalizer<AdminMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        builder
            .Add(S["A/B Tests"], S["A/B Tests"].PrefixPosition(), abTests => abTests
                .AddClass("abtests")
                .Id("abtests")
                .Permission(Permissions.ManageABTests)
                .Action("List", "Admin", _contentRouteValues)
                .LocalNav()
            );

        builder
            .Add(S["Settings"], settings => settings
                .Add(S["A/B Testing"], S["A/B Testing"].PrefixPosition(), abTesting => abTesting
                    .Action("Index", "Admin", _settingsRouteValues)
                    .AddClass("abtestingsettings")
                    .Id("abtestingsettings")
                    .Permission(Permissions.ManageABTests)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}
