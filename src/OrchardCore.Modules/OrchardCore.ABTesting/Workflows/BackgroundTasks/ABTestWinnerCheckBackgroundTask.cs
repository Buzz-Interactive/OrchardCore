using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
using OrchardCore.ABTesting.Workflows.Handlers;
using OrchardCore.ABTesting.Workflows.Models;
using OrchardCore.BackgroundTasks;

namespace OrchardCore.ABTesting.Workflows.BackgroundTasks;

/// <summary>
/// Background task that periodically checks active A/B tests for statistical significance
/// and triggers workflow events when a winner is detected.
/// </summary>
[BackgroundTask(
    Title = "A/B Test Winner Detection",
    Schedule = "*/5 * * * *",
    Description = "Checks active A/B tests for statistical significance and triggers workflow events when a winner is detected.")]
public sealed class ABTestWinnerCheckBackgroundTask : IBackgroundTask
{
    public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ABTestWinnerCheckBackgroundTask>>();
        var abTestManager = serviceProvider.GetRequiredService<IABTestManager>();
        var trackingService = serviceProvider.GetRequiredService<ITrackingService>();
        var statisticsService = serviceProvider.GetRequiredService<IStatisticalAnalysisService>();
        var workflowEventHandler = serviceProvider.GetRequiredService<IABTestWorkflowEventHandler>();

        try
        {
            // Get all active tests
            var activeTests = await abTestManager.GetActiveAsync();

            foreach (var test in activeTests)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Skip tests without goal tracking
                if (test.GoalType == GoalType.None)
                {
                    continue;
                }

                // Skip already concluded tests
                if (test.IsConcluded)
                {
                    continue;
                }

                await CheckTestForWinnerAsync(
                    test,
                    trackingService,
                    statisticsService,
                    workflowEventHandler,
                    logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking A/B tests for winners.");
        }
    }

    private static async Task CheckTestForWinnerAsync(
        ABTest test,
        ITrackingService trackingService,
        IStatisticalAnalysisService statisticsService,
        IABTestWorkflowEventHandler workflowEventHandler,
        ILogger logger)
    {
        try
        {
            // Get impressions and conversions
            var impressions = await trackingService.GetImpressionsAsync(test.TestId);
            var conversions = await trackingService.GetConversionsAsync(test.TestId);

            // Analyze the data
            var result = statisticsService.Analyze(
                impressions.VariantA,
                impressions.VariantB,
                conversions.VariantA,
                conversions.VariantB,
                test.MinimumSampleSize,
                test.ConfidenceThreshold);

            // Only trigger if we have a statistically significant winner
            if (!result.IsSignificant || !result.WinningVariant.HasValue)
            {
                return;
            }

            // Build the event context
            var context = new ABTestEventContext
            {
                TestId = test.TestId,
                TestName = test.Name,
                WinningVariant = result.WinningVariant.Value,
                ConfidenceLevel = result.ConfidenceLevel,
                Lift = result.Lift,
                ImpressionsA = impressions.VariantA,
                ImpressionsB = impressions.VariantB,
                ConversionsA = conversions.VariantA,
                ConversionsB = conversions.VariantB,
                ProbabilityToBeBestA = result.ProbabilityToBeBestA,
                ProbabilityToBeBestB = result.ProbabilityToBeBestB,
                VariantAContentItemId = test.VariantAContentItemId,
                VariantBContentItemId = test.VariantBContentItemId,
            };

            // Trigger the workflow event (handler will prevent duplicates)
            var triggered = await workflowEventHandler.TriggerWinnerDetectedAsync(context);

            if (triggered)
            {
                logger.LogInformation(
                    "Triggered workflow event for A/B test '{TestName}' ({TestId}). Winner: Variant {Winner} at {Confidence}% confidence.",
                    test.Name,
                    test.TestId,
                    result.WinningVariant.Value,
                    result.ConfidenceLevel);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking A/B test '{TestId}' for winner.", test.TestId);
        }
    }
}
