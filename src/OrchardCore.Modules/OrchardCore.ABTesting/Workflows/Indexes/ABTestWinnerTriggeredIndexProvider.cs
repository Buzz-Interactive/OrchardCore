using OrchardCore.ABTesting.Workflows.Models;
using YesSql.Indexes;

namespace OrchardCore.ABTesting.Workflows.Indexes;

/// <summary>
/// Index provider for ABTestWinnerTriggeredRecord entities.
/// </summary>
public class ABTestWinnerTriggeredIndexProvider : IndexProvider<ABTestWinnerTriggeredRecord>
{
    public ABTestWinnerTriggeredIndexProvider() => CollectionName = ABTestWinnerTriggeredRecord.Collection;

    public override void Describe(DescribeContext<ABTestWinnerTriggeredRecord> context) =>
        context.For<ABTestWinnerTriggeredIndex>()
            .Map(record => new ABTestWinnerTriggeredIndex
            {
                TestId = record.TestId,
                TriggeredUtc = record.TriggeredUtc,
                ConfidenceLevel = record.ConfidenceLevel,
                WinningVariant = record.WinningVariant,
            });
}
