using Microsoft.Extensions.Localization;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;

namespace OrchardCore.ABTesting.Workflows.Activities;

/// <summary>
/// Workflow event that triggers when an A/B test winner is declared (manually or via workflow).
/// This is different from ABTestWinnerDetectedEvent which triggers on automatic statistical detection.
/// </summary>
public class ABTestWinnerDeclaredEvent : EventActivity, IEvent
{
    public static string EventName => nameof(ABTestWinnerDeclaredEvent);

    private readonly IStringLocalizer S;

    public ABTestWinnerDeclaredEvent(IStringLocalizer<ABTestWinnerDeclaredEvent> localizer)
    {
        S = localizer;
    }

    public override string Name => EventName;

    public override LocalizedString DisplayText => S["A/B Test Winner Declared"];

    public override LocalizedString Category => S["A/B Testing"];

    public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
    {
        return Outcomes(S["Done"]);
    }

    public override ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
    {
        return Halt();
    }

    public override ActivityExecutionResult Resume(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
    {
        return Outcomes("Done");
    }
}
