using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Records;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data;
using YesSql.Indexes;

namespace OrchardCore.ABTesting.Indexes;

/// <summary>
/// Index provider that indexes ABTest content items for efficient lookup.
/// </summary>
public class ABTestIndexProvider : ContentHandlerBase, IIndexProvider, IScopedIndexProvider
{
    private readonly HashSet<ContentItem> _itemRemoved = [];

    public override Task RemovedAsync(RemoveContentContext context)
    {
        if (context.NoActiveVersionLeft)
        {
            var part = context.ContentItem.As<ABTestPart>();
            if (part != null)
            {
                _itemRemoved.Add(context.ContentItem);
            }
        }

        return Task.CompletedTask;
    }

    public string CollectionName { get; set; }

    public Type ForType() => typeof(ContentItem);

    public void Describe(IDescriptor context) => Describe((DescribeContext<ContentItem>)context);

    public void Describe(DescribeContext<ContentItem> context)
    {
        context.For<ABTestIndex>()
            .When(contentItem => contentItem.ContentType == "ABTest")
            .Map(contentItem =>
            {
                // Remove index records for soft deleted items
                if (!contentItem.Published && !contentItem.Latest && !_itemRemoved.Contains(contentItem))
                {
                    return null;
                }

                var part = contentItem.As<ABTestPart>();
                if (part == null)
                {
                    return null;
                }

                // Extract VariantA and VariantB ContentItemIds from ContentPickerFields
                var variantAField = part.Get<ContentPickerField>("VariantA");
                var variantBField = part.Get<ContentPickerField>("VariantB");

                var variantAId = variantAField?.ContentItemIds?.FirstOrDefault();
                var variantBId = variantBField?.ContentItemIds?.FirstOrDefault();

                // If either variant is not set, don't index
                if (string.IsNullOrEmpty(variantAId) || string.IsNullOrEmpty(variantBId))
                {
                    return null;
                }

                return new ABTestIndex
                {
                    ABTestContentItemId = contentItem.ContentItemId,
                    VariantAContentItemId = variantAId,
                    VariantBContentItemId = variantBId,
                    PercentageA = part.PercentageA,
                    IsActive = part.IsActive,
                    Published = contentItem.Published,
                    Latest = contentItem.Latest,
                    ScheduledStartUtc = part.ScheduledStartUtc,
                    ScheduledEndUtc = part.ScheduledEndUtc,
                };
            });
    }
}
