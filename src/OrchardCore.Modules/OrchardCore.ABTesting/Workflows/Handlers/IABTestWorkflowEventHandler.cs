using OrchardCore.ABTesting.Workflows.Models;

namespace OrchardCore.ABTesting.Workflows.Handlers;

/// <summary>
/// Interface for triggering A/B testing workflow events.
/// </summary>
public interface IABTestWorkflowEventHandler
{
    /// <summary>
    /// Triggers the "A/B Test Winner Detected" workflow event.
    /// Only triggers if an event hasn't already been triggered for this test.
    /// </summary>
    /// <param name="context">The event context containing test and statistical data.</param>
    /// <returns>True if the event was triggered, false if already triggered for this test.</returns>
    Task<bool> TriggerWinnerDetectedAsync(ABTestEventContext context);

    /// <summary>
    /// Clears the triggered record for a test, allowing the event to be triggered again.
    /// Call this when a test is reset or deactivated.
    /// </summary>
    /// <param name="testId">The test ID to clear.</param>
    Task ClearTriggeredRecordAsync(string testId);

    /// <summary>
    /// Triggers the "A/B Test Winner Declared" workflow event.
    /// Called when a winner is declared for a test (manually or via workflow).
    /// </summary>
    /// <param name="context">The event context containing test data.</param>
    Task TriggerWinnerDeclaredAsync(ABTestWinnerDeclaredContext context);
}
