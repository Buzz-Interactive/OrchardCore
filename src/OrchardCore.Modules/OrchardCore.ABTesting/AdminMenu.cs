using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.ABTesting.Drivers;
using OrchardCore.Navigation;

namespace OrchardCore.ABTesting;

public sealed class AdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary _routeValues = new()
    {
        { "Area", "OrchardCore.ABTesting" },
    };

    private static readonly RouteValueDictionary _settingsRouteValues = new()
    {
        { "area", "OrchardCore.Settings" },
        { "groupId", ABTestingSiteSettingsDisplayDriver.GroupId },
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
                .Action("Index", "Admin", _routeValues)
                .LocalNav()
            )
            .Add(S["Settings"], settings => settings
                .Add(S["A/B Testing"], S["A/B Testing"].PrefixPosition(), abTestingSettings => abTestingSettings
                    .Action("Index", "Admin", _settingsRouteValues)
                    .Permission(Permissions.ManageABTests)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}
