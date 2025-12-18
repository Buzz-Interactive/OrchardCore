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
        return Initialize<ABTestPartViewModel>(GetEditorShapeType(context), model =>
            BuildViewModelAsync(model, part));
    }

    public override async Task<IDisplayResult> UpdateAsync(ABTestPart part, UpdatePartEditorContext context)
    {
        var viewModel = new ABTestPartViewModel();

        await context.Updater.TryUpdateModelAsync(viewModel, Prefix,
            m => m.PercentageA,
            m => m.GoalType,
            m => m.GoalSelector,
            m => m.GoalScrollPercentage,
            m => m.GoalEventName,
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
            if (viewModel.GoalType != part.GoalType)
            {
                context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.GoalType),
                    S["Goal type cannot be changed after the test has started tracking impressions."]);
            }

            if (viewModel.GoalSelector != part.GoalSelector)
            {
                context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.GoalSelector),
                    S["Goal selector cannot be changed after the test has started tracking impressions."]);
            }

            if (viewModel.GoalScrollPercentage != part.GoalScrollPercentage)
            {
                context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.GoalScrollPercentage),
                    S["Scroll percentage cannot be changed after the test has started tracking impressions."]);
            }

            if (viewModel.GoalEventName != part.GoalEventName)
            {
                context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.GoalEventName),
                    S["Event name cannot be changed after the test has started tracking impressions."]);
            }
        }

        // Validate percentage range
        if (viewModel.PercentageA < 0 || viewModel.PercentageA > 100)
        {
            context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.PercentageA),
                S["Percentage must be between 0 and 100."]);
        }

        // Validate MinimumSampleSize range
        if (viewModel.MinimumSampleSize < 30 || viewModel.MinimumSampleSize > 500)
        {
            context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.MinimumSampleSize),
                S["Minimum sample size must be between 30 and 500."]);
        }

        // Validate ConfidenceThreshold values
        if (viewModel.ConfidenceThreshold != 90 && viewModel.ConfidenceThreshold != 95 && viewModel.ConfidenceThreshold != 99)
        {
            context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.ConfidenceThreshold),
                S["Confidence threshold must be 90, 95, or 99."]);
        }

        // Validate goal configuration only if goals are not locked
        if (!areGoalsLocked)
        {
            if (viewModel.GoalType == GoalType.ButtonLinkClick || viewModel.GoalType == GoalType.FormSubmission)
            {
                if (string.IsNullOrWhiteSpace(viewModel.GoalSelector))
                {
                    context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.GoalSelector),
                        S["CSS selector is required for this goal type."]);
                }
            }

            if (viewModel.GoalType == GoalType.ScrollPercentage)
            {
                if (viewModel.GoalScrollPercentage < 0 || viewModel.GoalScrollPercentage > 100)
                {
                    context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.GoalScrollPercentage),
                        S["Scroll percentage must be between 0 and 100."]);
                }
            }

            if (viewModel.GoalType == GoalType.CustomEvent)
            {
                if (string.IsNullOrWhiteSpace(viewModel.GoalEventName))
                {
                    context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.GoalEventName),
                        S["Event name is required for custom event goals."]);
                }
            }
        }

        part.PercentageA = Math.Clamp(viewModel.PercentageA, 0, 100);

        // Only update goal fields if not locked
        if (!areGoalsLocked)
        {
            part.GoalType = viewModel.GoalType;
            part.GoalSelector = viewModel.GoalSelector;
            part.GoalScrollPercentage = viewModel.GoalScrollPercentage;
            part.GoalEventName = viewModel.GoalEventName;
        }

        part.MinimumSampleSize = Math.Clamp(viewModel.MinimumSampleSize, 30, 500);
        part.ConfidenceThreshold = viewModel.ConfidenceThreshold;

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
}
