using Microsoft.Extensions.Localization;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;

namespace OrchardCore.ABTesting.Workflows.Activities;

/// <summary>
/// Workflow event that triggers when an A/B test reaches statistical significance.
/// The confidence level is determined by each test's configuration.
/// </summary>
public class ABTestWinnerDetectedEvent : EventActivity, IEvent
{
    public static string EventName => nameof(ABTestWinnerDetectedEvent);

    private readonly IStringLocalizer S;

    public ABTestWinnerDetectedEvent(IStringLocalizer<ABTestWinnerDetectedEvent> localizer)
    {
        S = localizer;
    }

    public override string Name => EventName;

    public override LocalizedString DisplayText => S["A/B Test Winner Detected"];

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
