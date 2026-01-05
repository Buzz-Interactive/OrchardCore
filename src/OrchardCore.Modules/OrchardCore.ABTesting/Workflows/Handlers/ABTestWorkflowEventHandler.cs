using OrchardCore.ABTesting.Workflows.Activities;
using OrchardCore.ABTesting.Workflows.Indexes;
using OrchardCore.ABTesting.Workflows.Models;
using OrchardCore.Workflows.Services;
using YesSql;

namespace OrchardCore.ABTesting.Workflows.Handlers;

/// <summary>
/// Handler for triggering A/B testing workflow events.
/// </summary>
public class ABTestWorkflowEventHandler : IABTestWorkflowEventHandler
{
    private readonly IWorkflowManager _workflowManager;
    private readonly ISession _session;

    public ABTestWorkflowEventHandler(
        IWorkflowManager workflowManager,
        ISession session)
    {
        _workflowManager = workflowManager;
        _session = session;
    }

    /// <inheritdoc />
    public async Task<bool> TriggerWinnerDetectedAsync(ABTestEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check if we've already triggered an event for this test
        var existingTrigger = await _session
            .QueryIndex<ABTestWinnerTriggeredIndex>(ABTestWinnerTriggeredRecord.Collection)
            .Where(x => x.TestId == context.TestId)
            .FirstOrDefaultAsync();

        if (existingTrigger != null)
        {
            // Already triggered for this test
            return false;
        }

        // Record that we're triggering for this test
        var triggeredRecord = new ABTestWinnerTriggeredRecord
        {
            TestId = context.TestId,
            TriggeredUtc = DateTime.UtcNow,
            ConfidenceLevel = context.ConfidenceLevel,
            WinningVariant = context.WinningVariant,
        };

        await _session.SaveAsync(triggeredRecord, ABTestWinnerTriggeredRecord.Collection);

        // Trigger the workflow event
        var input = new Dictionary<string, object>
        {
            ["TestId"] = context.TestId,
            ["TestName"] = context.TestName,
            ["WinningVariant"] = context.WinningVariant.ToString(),
            ["ConfidenceLevel"] = context.ConfidenceLevel,
            ["Lift"] = context.Lift,
            ["ImpressionsA"] = context.ImpressionsA,
            ["ImpressionsB"] = context.ImpressionsB,
            ["ConversionsA"] = context.ConversionsA,
            ["ConversionsB"] = context.ConversionsB,
            ["ProbabilityToBeBestA"] = context.ProbabilityToBeBestA,
            ["ProbabilityToBeBestB"] = context.ProbabilityToBeBestB,
            ["VariantAContentItemId"] = context.VariantAContentItemId,
            ["VariantBContentItemId"] = context.VariantBContentItemId,
        };

        await _workflowManager.TriggerEventAsync(
            ABTestWinnerDetectedEvent.EventName,
            input,
            correlationId: context.TestId);

        return true;
    }

    /// <inheritdoc />
    public async Task ClearTriggeredRecordAsync(string testId)
    {
        var triggeredRecord = await _session
            .Query<ABTestWinnerTriggeredRecord, ABTestWinnerTriggeredIndex>(ABTestWinnerTriggeredRecord.Collection)
            .Where(x => x.TestId == testId)
            .FirstOrDefaultAsync();

        if (triggeredRecord != null)
        {
            _session.Delete(triggeredRecord, ABTestWinnerTriggeredRecord.Collection);
        }
    }

    /// <inheritdoc />
    public async Task TriggerWinnerDeclaredAsync(ABTestWinnerDeclaredContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var input = new Dictionary<string, object>
        {
            ["TestId"] = context.TestId,
            ["TestName"] = context.TestName,
            ["WinningVariant"] = context.WinningVariant.ToString(),
            ["VariantAContentItemId"] = context.VariantAContentItemId,
            ["VariantBContentItemId"] = context.VariantBContentItemId,
            ["WinnerContentItemId"] = context.WinnerContentItemId,
            ["LoserContentItemId"] = context.LoserContentItemId,
        };

        await _workflowManager.TriggerEventAsync(
            ABTestWinnerDeclaredEvent.EventName,
            input,
            correlationId: context.TestId);
    }
}
