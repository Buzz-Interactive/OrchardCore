using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ABTesting.Filters;
using OrchardCore.ABTesting.Indexes;
using OrchardCore.ABTesting.Middleware;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
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
        // Register services
        services.AddScoped<IABTestManager, ABTestManager>();
        services.AddScoped<IABTestLookupService, ABTestLookupService>();
        services.AddScoped<IImpressionService, ImpressionService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IVisitorAssignmentService, VisitorAssignmentService>();
        services.AddScoped<IABTestContentResolver, ABTestContentResolver>();
        services.AddSingleton<IStatisticalAnalysisService, StatisticalAnalysisService>();

        // Register the tracking filter
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<ABTestTrackingFilter>();
        });

        // Register the ABTest collection and index provider
        services.Configure<StoreCollectionOptions>(o => o.Collections.Add(ABTest.Collection));
        services.AddIndexProvider<ABTestIndexProvider>();

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
