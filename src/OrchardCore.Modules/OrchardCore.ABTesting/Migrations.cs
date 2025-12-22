using OrchardCore.ABTesting.Indexes;
using OrchardCore.ABTesting.Models;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace OrchardCore.ABTesting;

public sealed class Migrations : DataMigration
{
    public async Task<int> CreateAsync()
    {
        // Create the ABTestIndex table in the ABTest collection
        await SchemaBuilder.CreateMapIndexTableAsync<ABTestIndex>(table => table
            .Column<string>("TestId", col => col.WithLength(26))
            .Column<string>("VariantAContentItemId", col => col.WithLength(26))
            .Column<string>("VariantBContentItemId", col => col.WithLength(26))
            .Column<bool>("IsActive")
            .Column<DateTime>("CreatedUtc"),
            collection: ABTest.Collection
        );

        await SchemaBuilder.AlterIndexTableAsync<ABTestIndex>(table => table
            .CreateIndex("IDX_ABTestIndex_DocumentId",
                "DocumentId",
                "TestId",
                "IsActive",
                "CreatedUtc"),
            collection: ABTest.Collection
        );

        await SchemaBuilder.AlterIndexTableAsync<ABTestIndex>(table => table
            .CreateIndex("IDX_ABTestIndex_VariantA",
                "DocumentId",
                "VariantAContentItemId",
                "IsActive"),
            collection: ABTest.Collection
        );

        await SchemaBuilder.AlterIndexTableAsync<ABTestIndex>(table => table
            .CreateIndex("IDX_ABTestIndex_VariantB",
                "DocumentId",
                "VariantBContentItemId",
                "IsActive"),
            collection: ABTest.Collection
        );

        // Create the impression tracking table
        await SchemaBuilder.CreateTableAsync("ABTestImpressionRecord", table => table
            .Column<int>("Id", col => col.PrimaryKey().Identity())
            .Column<string>("TestId", col => col.WithLength(26))
            .Column<long>("VariantAImpressions", col => col.WithDefault(0))
            .Column<long>("VariantBImpressions", col => col.WithDefault(0))
        );

        await SchemaBuilder.AlterTableAsync("ABTestImpressionRecord", table => table
            .CreateIndex("IDX_ABTestImpressionRecord_TestId", "TestId")
        );

        // Create the goal tracking table
        await SchemaBuilder.CreateTableAsync("ABTestGoalRecord", table => table
            .Column<int>("Id", col => col.PrimaryKey().Identity())
            .Column<string>("TestId", col => col.WithLength(26))
            .Column<long>("VariantAConversions", col => col.WithDefault(0))
            .Column<long>("VariantBConversions", col => col.WithDefault(0))
        );

        await SchemaBuilder.AlterTableAsync("ABTestGoalRecord", table => table
            .CreateIndex("IDX_ABTestGoalRecord_TestId", "TestId")
        );

        return 1;
    }

    public async Task<int> UpdateFrom1Async()
    {
        // Add variant state columns to track deleted/unpublished variants
        await SchemaBuilder.AlterIndexTableAsync<ABTestIndex>(table => table
            .AddColumn<int>("VariantAState", col => col.WithDefault(0)),
            collection: ABTest.Collection
        );

        await SchemaBuilder.AlterIndexTableAsync<ABTestIndex>(table => table
            .AddColumn<int>("VariantBState", col => col.WithDefault(0)),
            collection: ABTest.Collection
        );

        return 2;
    }
}
