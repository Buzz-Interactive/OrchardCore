using Microsoft.Extensions.Localization;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
using OrchardCore.ABTesting.ViewModels;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace OrchardCore.ABTesting.Drivers;

public sealed class ABTestPartDisplayDriver : ContentPartDisplayDriver<ABTestPart>
{
    private readonly IContentManager _contentManager;
    private readonly IImpressionService _impressionService;
    private readonly IGoalService _goalService;

    internal readonly IStringLocalizer S;

    public ABTestPartDisplayDriver(
        IContentManager contentManager,
        IImpressionService impressionService,
        IGoalService goalService,
        IStringLocalizer<ABTestPartDisplayDriver> localizer)
    {
        _contentManager = contentManager;
        _impressionService = impressionService;
        _goalService = goalService;
        S = localizer;
    }

    public override IDisplayResult Display(ABTestPart part, BuildPartDisplayContext context)
    {
        return Combine(
            Initialize<ABTestPartViewModel>("ABTestPart_SummaryAdmin", model =>
                BuildViewModelAsync(model, part))
                .Location("SummaryAdmin", "TestMeta:10"),
            Initialize<ABTestPartViewModel>("ABTestPart_DetailAdmin", model =>
                BuildViewModelAsync(model, part))
                .Location("DetailAdmin", "Content:5")
        );
    }

    public override IDisplayResult Edit(ABTestPart part, BuildPartEditorContext context)
    {
        return Combine(
            // Traffic Split - positioned in Content zone (no tab)
            Initialize<ABTestPartViewModel>("ABTestPart_Edit", model =>
                BuildTrafficViewModelAsync(model, part))
                .Location("Parts:1"),

            // Goals tab
            Initialize<ABTestPartGoalsViewModel>("ABTestPartGoals_Edit", async model =>
                await BuildGoalsViewModelAsync(model, part))
                .Location("Parts#Goals;10"),

            // Statistics tab
            Initialize<ABTestPartStatisticsViewModel>("ABTestPartStatistics_Edit", model =>
                BuildStatisticsViewModel(model, part))
                .Location("Parts#Settings;20")
        );
    }

    public override async Task<IDisplayResult> UpdateAsync(ABTestPart part, UpdatePartEditorContext context)
    {
        // Bind traffic view model
        var trafficViewModel = new ABTestPartViewModel();
        await context.Updater.TryUpdateModelAsync(trafficViewModel, Prefix,
            m => m.PercentageA);

        // Bind goals view model
        var goalsViewModel = new ABTestPartGoalsViewModel();
        await context.Updater.TryUpdateModelAsync(goalsViewModel, Prefix,
            m => m.GoalType,
            m => m.GoalSelector,
            m => m.GoalScrollPercentage,
            m => m.GoalEventName);

        // Bind statistics view model
        var statisticsViewModel = new ABTestPartStatisticsViewModel();
        await context.Updater.TryUpdateModelAsync(statisticsViewModel, Prefix,
            m => m.MinimumSampleSize,
            m => m.ConfidenceThreshold);

        // Check if goals are locked (published + has impressions)
        var contentItemId = part.ContentItem.ContentItemId;
        var (variantAImpressions, variantBImpressions) = await _impressionService.GetImpressionsAsync(contentItemId);
        var totalImpressions = variantAImpressions + variantBImpressions;
        var areGoalsLocked = part.ContentItem.Published && totalImpressions > 0;

        if (areGoalsLocked)
        {
            // Check if any goal field was changed and add validation errors
            if (goalsViewModel.GoalType != part.GoalType)
            {
                context.Updater.ModelState.AddModelError(Prefix + "." + nameof(goalsViewModel.GoalType),
                    S["Goal type cannot be changed after the test has started tracking impressions."]);
            }

            if (goalsViewModel.GoalSelector != part.GoalSelector)
            {
                context.Updater.ModelState.AddModelError(Prefix + "." + nameof(goalsViewModel.GoalSelector),
                    S["Goal selector cannot be changed after the test has started tracking impressions."]);
            }

            if (goalsViewModel.GoalScrollPercentage != part.GoalScrollPercentage)
            {
                context.Updater.ModelState.AddModelError(Prefix + "." + nameof(goalsViewModel.GoalScrollPercentage),
                    S["Scroll percentage cannot be changed after the test has started tracking impressions."]);
            }

            if (goalsViewModel.GoalEventName != part.GoalEventName)
            {
                context.Updater.ModelState.AddModelError(Prefix + "." + nameof(goalsViewModel.GoalEventName),
                    S["Event name cannot be changed after the test has started tracking impressions."]);
            }
        }

        // Validate percentage range
        if (trafficViewModel.PercentageA < 0 || trafficViewModel.PercentageA > 100)
        {
            context.Updater.ModelState.AddModelError(Prefix + "." + nameof(trafficViewModel.PercentageA),
                S["Percentage must be between 0 and 100."]);
        }

        // Validate MinimumSampleSize range
        if (statisticsViewModel.MinimumSampleSize < 30 || statisticsViewModel.MinimumSampleSize > 500)
        {
            context.Updater.ModelState.AddModelError(Prefix + "." + nameof(statisticsViewModel.MinimumSampleSize),
                S["Minimum sample size must be between 30 and 500."]);
        }

        // Validate ConfidenceThreshold values
        if (statisticsViewModel.ConfidenceThreshold != 90 && statisticsViewModel.ConfidenceThreshold != 95 && statisticsViewModel.ConfidenceThreshold != 99)
        {
            context.Updater.ModelState.AddModelError(Prefix + "." + nameof(statisticsViewModel.ConfidenceThreshold),
                S["Confidence threshold must be 90, 95, or 99."]);
        }

        // Validate goal configuration only if goals are not locked
        if (!areGoalsLocked)
        {
            if (goalsViewModel.GoalType == GoalType.ButtonLinkClick || goalsViewModel.GoalType == GoalType.FormSubmission)
            {
                if (string.IsNullOrWhiteSpace(goalsViewModel.GoalSelector))
                {
                    context.Updater.ModelState.AddModelError(Prefix + "." + nameof(goalsViewModel.GoalSelector),
                        S["CSS selector is required for this goal type."]);
                }
            }

            if (goalsViewModel.GoalType == GoalType.ScrollPercentage)
            {
                if (goalsViewModel.GoalScrollPercentage < 0 || goalsViewModel.GoalScrollPercentage > 100)
                {
                    context.Updater.ModelState.AddModelError(Prefix + "." + nameof(goalsViewModel.GoalScrollPercentage),
                        S["Scroll percentage must be between 0 and 100."]);
                }
            }

            if (goalsViewModel.GoalType == GoalType.CustomEvent)
            {
                if (string.IsNullOrWhiteSpace(goalsViewModel.GoalEventName))
                {
                    context.Updater.ModelState.AddModelError(Prefix + "." + nameof(goalsViewModel.GoalEventName),
                        S["Event name is required for custom event goals."]);
                }
            }
        }

        part.PercentageA = Math.Clamp(trafficViewModel.PercentageA, 0, 100);

        // Only update goal fields if not locked
        if (!areGoalsLocked)
        {
            part.GoalType = goalsViewModel.GoalType;
            part.GoalSelector = goalsViewModel.GoalSelector;
            part.GoalScrollPercentage = goalsViewModel.GoalScrollPercentage;
            part.GoalEventName = goalsViewModel.GoalEventName;
        }

        part.MinimumSampleSize = Math.Clamp(statisticsViewModel.MinimumSampleSize, 30, 500);
        part.ConfidenceThreshold = statisticsViewModel.ConfidenceThreshold;

        return Edit(part, context);
    }

    private async ValueTask BuildViewModelAsync(ABTestPartViewModel model, ABTestPart part)
    {
        model.PercentageA = part.PercentageA;
        model.ABTestPart = part;

        // Calculate the current status based on published state
        model.Status = part.ContentItem.Published ? ABTestStatus.Running : ABTestStatus.Inactive;

        // Get total impressions and conversions
        var contentItemId = part.ContentItem.ContentItemId;
        var (variantAImpressions, variantBImpressions) = await _impressionService.GetImpressionsAsync(contentItemId);
        model.TotalImpressions = variantAImpressions + variantBImpressions;

        var (variantAConversions, variantBConversions) = await _goalService.GetConversionsAsync(contentItemId);
        model.TotalConversions = variantAConversions + variantBConversions;

        // Determine if goal fields should be locked
        // Goals are locked when: Published AND has impressions
        model.AreGoalsLocked = part.ContentItem.Published && model.TotalImpressions > 0;

        // Populate goal properties
        model.GoalType = part.GoalType;
        model.GoalSelector = part.GoalSelector;
        model.GoalScrollPercentage = part.GoalScrollPercentage;
        model.GoalEventName = part.GoalEventName;

        // Populate statistical settings
        model.MinimumSampleSize = part.MinimumSampleSize;
        model.ConfidenceThreshold = part.ConfidenceThreshold;

        // Get display text for variants
        var variantAField = part.Get<ContentPickerField>("VariantA");
        var variantBField = part.Get<ContentPickerField>("VariantB");

        if (variantAField?.ContentItemIds?.Length > 0)
        {
            var variantA = await _contentManager.GetAsync(variantAField.ContentItemIds[0], VersionOptions.Latest);
            model.VariantADisplayText = variantA?.DisplayText ?? S["(Not found)"];
        }
        else
        {
            model.VariantADisplayText = S["(Not selected)"];
        }

        if (variantBField?.ContentItemIds?.Length > 0)
        {
            var variantB = await _contentManager.GetAsync(variantBField.ContentItemIds[0], VersionOptions.Latest);
            model.VariantBDisplayText = variantB?.DisplayText ?? S["(Not found)"];
        }
        else
        {
            model.VariantBDisplayText = S["(Not selected)"];
        }
    }

    private static void BuildTrafficViewModelAsync(ABTestPartViewModel model, ABTestPart part)
    {
        model.PercentageA = part.PercentageA;
    }

    private async ValueTask BuildGoalsViewModelAsync(ABTestPartGoalsViewModel model, ABTestPart part)
    {
        model.GoalType = part.GoalType;
        model.GoalSelector = part.GoalSelector;
        model.GoalScrollPercentage = part.GoalScrollPercentage;
        model.GoalEventName = part.GoalEventName;

        // Determine if goal fields should be locked
        // Goals are locked when: Published AND has impressions
        var contentItemId = part.ContentItem.ContentItemId;
        var (variantAImpressions, variantBImpressions) = await _impressionService.GetImpressionsAsync(contentItemId);
        var totalImpressions = variantAImpressions + variantBImpressions;
        model.AreGoalsLocked = part.ContentItem.Published && totalImpressions > 0;
    }

    private static void BuildStatisticsViewModel(ABTestPartStatisticsViewModel model, ABTestPart part)
    {
        model.MinimumSampleSize = part.MinimumSampleSize;
        model.ConfidenceThreshold = part.ConfidenceThreshold;
    }
}
