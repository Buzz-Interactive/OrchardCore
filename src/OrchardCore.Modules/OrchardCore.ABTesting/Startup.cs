using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ABTesting.Drivers;
using OrchardCore.ABTesting.Filters;
using OrchardCore.ABTesting.Handlers;
using OrchardCore.ABTesting.Indexes;
using OrchardCore.ABTesting.Middleware;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;

namespace OrchardCore.ABTesting;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register the content part
        services.AddContentPart<ABTestPart>()
            .UseDisplayDriver<ABTestPartDisplayDriver>()
            .AddHandler<ABTestPartHandler>();

        // Register services
        services.AddScoped<IABTestLookupService, ABTestLookupService>();
        services.AddScoped<IImpressionService, ImpressionService>();
        services.AddScoped<IVisitorAssignmentService, VisitorAssignmentService>();
        services.AddScoped<IABTestContentResolver, ABTestContentResolver>();

        // Register display driver for results action menu
        services.AddScoped<IContentDisplayDriver, ABTestResultsDisplayDriver>();

        // Register the tracking filter
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<ABTestTrackingFilter>();
        });

        // Register the index provider
        services.AddScoped<ABTestIndexProvider>();
        services.AddScoped<IScopedIndexProvider>(sp => sp.GetRequiredService<ABTestIndexProvider>());
        services.AddScoped<IContentHandler>(sp => sp.GetRequiredService<ABTestIndexProvider>());

        // Register migrations
        services.AddDataMigration<Migrations>();

        // Register permissions
        services.AddPermissionProvider<Permissions>();

        // Register admin menu
        services.AddNavigationProvider<AdminMenu>();
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Register the A/B testing middleware
        // This must be called after UseRouting() to have access to route data
        app.UseABTesting();
    }
}
