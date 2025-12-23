using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ABTesting.Workflows.Activities;
using OrchardCore.ABTesting.Workflows.BackgroundTasks;
using OrchardCore.ABTesting.Workflows.Drivers;
using OrchardCore.ABTesting.Workflows.Handlers;
using OrchardCore.ABTesting.Workflows.Indexes;
using OrchardCore.ABTesting.Workflows.Models;
using OrchardCore.BackgroundTasks;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Workflows.Helpers;

namespace OrchardCore.ABTesting.Workflows;

[RequireFeatures("OrchardCore.Workflows")]
public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Events
        services.AddActivity<ABTestWinnerDetectedEvent, ABTestWinnerDetectedEventDisplayDriver>();

        // Tasks
        services.AddActivity<DeclareABTestWinnerTask, DeclareABTestWinnerTaskDisplayDriver>();

        // Event handler for triggering workflow events
        services.AddScoped<IABTestWorkflowEventHandler, ABTestWorkflowEventHandler>();

        // Data persistence for tracking triggered events
        services.Configure<StoreCollectionOptions>(o => o.Collections.Add(ABTestWinnerTriggeredRecord.Collection));
        services.AddIndexProvider<ABTestWinnerTriggeredIndexProvider>();
        services.AddDataMigration<Migrations>();

        // Background task for winner detection (runs every 5 minutes)
        services.AddSingleton<IBackgroundTask, ABTestWinnerCheckBackgroundTask>();
    }
}
