using OrchardCore.ABTesting.Models;
using YesSql.Indexes;

namespace OrchardCore.ABTesting.Indexes;

/// <summary>
/// Index provider for ABTest entities stored in the ABTest collection.
/// </summary>
public class ABTestIndexProvider : IndexProvider<ABTest>
{
    public ABTestIndexProvider() => CollectionName = ABTest.Collection;

    public override void Describe(DescribeContext<ABTest> context) =>
        context.For<ABTestIndex>()
            .Map(abTest => new ABTestIndex
            {
                TestId = abTest.TestId,
                VariantAContentItemId = abTest.VariantAContentItemId,
                VariantBContentItemId = abTest.VariantBContentItemId,
                IsActive = abTest.IsActive,
                CreatedUtc = abTest.CreatedUtc,
                VariantAState = abTest.VariantAState,
                VariantBState = abTest.VariantBState,
            });
}
