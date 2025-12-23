using OrchardCore.ABTesting.Workflows.Indexes;
using OrchardCore.ABTesting.Workflows.Models;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace OrchardCore.ABTesting.Workflows;

/// <summary>
/// Migrations for the A/B Testing Workflows feature.
/// </summary>
public sealed class Migrations : DataMigration
{
    public async Task<int> CreateAsync()
    {
        // Create the ABTestWinnerTriggeredIndex table in its own collection
        await SchemaBuilder.CreateMapIndexTableAsync<ABTestWinnerTriggeredIndex>(table => table
            .Column<string>("TestId", col => col.WithLength(26))
            .Column<DateTime>("TriggeredUtc")
            .Column<int>("ConfidenceLevel")
            .Column<int>("WinningVariant"),
            collection: ABTestWinnerTriggeredRecord.Collection
        );

        await SchemaBuilder.AlterIndexTableAsync<ABTestWinnerTriggeredIndex>(table => table
            .CreateIndex("IDX_ABTestWinnerTriggered_TestId",
                "DocumentId",
                "TestId"),
            collection: ABTestWinnerTriggeredRecord.Collection
        );

        return 1;
    }
}
