using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Records;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace OrchardCore.ABTesting;

public sealed class Migrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public Migrations(IContentDefinitionManager contentDefinitionManager)
    {
        _contentDefinitionManager = contentDefinitionManager;
    }

    public async Task<int> CreateAsync()
    {
        // Define the ABTestPart
        await _contentDefinitionManager.AlterPartDefinitionAsync(nameof(ABTestPart), builder => builder
            .WithDescription("Provides A/B testing configuration including traffic split and activation settings.")
            .WithField("VariantA", field => field
                .OfType("ContentPickerField")
                .WithDisplayName("Variant A")
                .WithDescription("The first variant (control). Select the content item to use as Variant A.")
                .WithSettings(new ContentPickerFieldSettings
                {
                    Required = true,
                    Multiple = false,
                    DisplayAllContentTypes = true,
                }))
            .WithField("VariantB", field => field
                .OfType("ContentPickerField")
                .WithDisplayName("Variant B")
                .WithDescription("The second variant (challenger). Select the content item to use as Variant B.")
                .WithSettings(new ContentPickerFieldSettings
                {
                    Required = true,
                    Multiple = false,
                    DisplayAllContentTypes = true,
                }))
        );

        // Create the ABTest content type
        await _contentDefinitionManager.AlterTypeDefinitionAsync("ABTest", type => type
            .DisplayedAs("AB Test")
            .WithDescription("An A/B test that compares two content variants.")
            .WithPart("TitlePart", part => part
                .WithPosition("0")
                .WithDescription("The name of the A/B test."))
            .WithPart(nameof(ABTestPart), part => part
                .WithPosition("1"))
        );

        // Create the index table for efficient lookups
        await SchemaBuilder.CreateMapIndexTableAsync<ABTestIndex>(table => table
            .Column<string>("ABTestContentItemId", col => col.WithLength(26))
            .Column<string>("VariantAContentItemId", col => col.WithLength(26))
            .Column<string>("VariantBContentItemId", col => col.WithLength(26))
            .Column<int>("PercentageA")
            .Column<bool>("IsActive")
            .Column<bool>("Published", col => col.WithDefault(true))
            .Column<bool>("Latest", col => col.WithDefault(false))
        );

        // Create index for looking up tests by variant content item IDs
        await SchemaBuilder.AlterIndexTableAsync<ABTestIndex>(table => table
            .CreateIndex("IDX_ABTestIndex_DocumentId",
                "DocumentId",
                "ABTestContentItemId",
                "VariantAContentItemId",
                "VariantBContentItemId",
                "IsActive",
                "Published",
                "Latest")
        );

        // Create index for finding tests containing a specific content item
        await SchemaBuilder.AlterIndexTableAsync<ABTestIndex>(table => table
            .CreateIndex("IDX_ABTestIndex_VariantA",
                "DocumentId",
                "VariantAContentItemId",
                "IsActive",
                "Published")
        );

        await SchemaBuilder.AlterIndexTableAsync<ABTestIndex>(table => table
            .CreateIndex("IDX_ABTestIndex_VariantB",
                "DocumentId",
                "VariantBContentItemId",
                "IsActive",
                "Published")
        );

        // Create the impression tracking table
        await SchemaBuilder.CreateTableAsync("ABTestImpressionRecord", table => table
            .Column<int>("Id", col => col.PrimaryKey().Identity())
            .Column<string>("TestContentItemId", col => col.WithLength(26))
            .Column<long>("VariantAImpressions", col => col.WithDefault(0))
            .Column<long>("VariantBImpressions", col => col.WithDefault(0))
        );

        await SchemaBuilder.AlterTableAsync("ABTestImpressionRecord", table => table
            .CreateIndex("IDX_ABTestImpressionRecord_TestContentItemId",
                "TestContentItemId")
        );

        // Create the goal tracking table
        await SchemaBuilder.CreateTableAsync("ABTestGoalRecord", table => table
            .Column<int>("Id", col => col.PrimaryKey().Identity())
            .Column<string>("TestContentItemId", col => col.WithLength(26))
            .Column<long>("VariantAConversions", col => col.WithDefault(0))
            .Column<long>("VariantBConversions", col => col.WithDefault(0))
        );

        await SchemaBuilder.AlterTableAsync("ABTestGoalRecord", table => table
            .CreateIndex("IDX_ABTestGoalRecord_TestContentItemId",
                "TestContentItemId")
        );

        return 5;
    }
}
