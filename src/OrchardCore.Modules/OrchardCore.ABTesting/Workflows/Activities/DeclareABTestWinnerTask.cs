using Microsoft.Extensions.Localization;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.ABTesting.Workflows.Activities;

public class DeclareABTestWinnerTask : TaskActivity<DeclareABTestWinnerTask>
{
    private readonly IABTestWinnerService _winnerService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;

    public DeclareABTestWinnerTask(
        IABTestWinnerService winnerService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<DeclareABTestWinnerTask> localizer)
    {
        _winnerService = winnerService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    private IStringLocalizer S { get; }

    public override string Name => nameof(DeclareABTestWinnerTask);

    public override LocalizedString DisplayText => S["Declare A/B Test Winner"];

    public override LocalizedString Category => S["A/B Testing"];

    /// <summary>
    /// A JavaScript expression that evaluates to the Test ID.
    /// If not specified, uses the "TestId" input from the workflow context.
    /// </summary>
    public WorkflowExpression<string> TestId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    /// <summary>
    /// The winner to declare: "A", "B", or "Auto".
    /// "Auto" uses the WinningVariant from the workflow context input.
    /// </summary>
    public string Winner
    {
        get => GetProperty(() => "Auto");
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
    {
        return Outcomes(S["Done"], S["Failed"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
    {
        // Get Test ID from expression or workflow input
        string testId;
        if (!string.IsNullOrWhiteSpace(TestId.Expression))
        {
            testId = await _expressionEvaluator.EvaluateAsync(TestId, workflowContext, null);
        }
        else
        {
            testId = workflowContext.Input.TryGetValue("TestId", out var testIdValue)
                ? testIdValue?.ToString()
                : null;
        }

        if (string.IsNullOrWhiteSpace(testId))
        {
            workflowContext.LastResult = "Test ID is required but was not provided.";
            return Outcomes("Failed");
        }

        // Determine the winner
        ABVariant? winner = Winner?.ToUpperInvariant() switch
        {
            "A" => ABVariant.A,
            "B" => ABVariant.B,
            "AUTO" or "" or null => GetWinnerFromContext(workflowContext),
            _ => null
        };

        if (!winner.HasValue)
        {
            workflowContext.LastResult = $"Invalid winner value: '{Winner}'. Must be 'A', 'B', or 'Auto'.";
            return Outcomes("Failed");
        }

        // Declare the winner
        var success = await _winnerService.DeclareWinnerAsync(testId, winner.Value);

        if (!success)
        {
            workflowContext.LastResult = $"Failed to declare winner for test '{testId}'. The test may not exist or may already be concluded.";
            return Outcomes("Failed");
        }

        workflowContext.LastResult = new
        {
            TestId = testId,
            Winner = winner.Value.ToString(),
            Success = true,
        };

        return Outcomes("Done");
    }

    private static ABVariant? GetWinnerFromContext(WorkflowExecutionContext workflowContext)
    {
        if (!workflowContext.Input.TryGetValue("WinningVariant", out var winningVariantValue))
        {
            return null;
        }

        // Handle both string and enum values
        if (winningVariantValue is ABVariant variant)
        {
            return variant;
        }

        var variantString = winningVariantValue?.ToString()?.ToUpperInvariant();
        return variantString switch
        {
            "A" => ABVariant.A,
            "B" => ABVariant.B,
            _ => null
        };
    }
}
