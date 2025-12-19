using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.ABTesting;

public sealed class AdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary _routeValues = new()
    {
        { "Area", "OrchardCore.ABTesting" },
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
            );

        return ValueTask.CompletedTask;
    }
}
