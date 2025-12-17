using Microsoft.Extensions.Localization;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
using OrchardCore.ABTesting.ViewModels;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Modules;

namespace OrchardCore.ABTesting.Drivers;

public sealed class ABTestPartDisplayDriver : ContentPartDisplayDriver<ABTestPart>
{
    private readonly IContentManager _contentManager;
    private readonly IImpressionService _impressionService;
    private readonly IGoalService _goalService;
    private readonly ILocalClock _localClock;
    private readonly IClock _clock;

    internal readonly IStringLocalizer S;

    public ABTestPartDisplayDriver(
        IContentManager contentManager,
        IImpressionService impressionService,
        IGoalService goalService,
        ILocalClock localClock,
        IClock clock,
        IStringLocalizer<ABTestPartDisplayDriver> localizer)
    {
        _contentManager = contentManager;
        _impressionService = impressionService;
        _goalService = goalService;
        _localClock = localClock;
        _clock = clock;
        S = localizer;
    }

    public override IDisplayResult Display(ABTestPart part, BuildPartDisplayContext context)
    {
        return Combine(
            Initialize<ABTestPartViewModel>("ABTestPart_SummaryAdmin", model =>
                BuildViewModelAsync(model, part))
                .Location("SummaryAdmin", "Meta:10"),
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
            m => m.IsActive,
            m => m.ScheduledStartLocalDateTime,
            m => m.ScheduledEndLocalDateTime,
            m => m.GoalType,
            m => m.GoalSelector,
            m => m.GoalScrollPercentage,
            m => m.GoalEventName);

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

        // Convert local times to UTC
        DateTime? scheduledStartUtc = null;
        DateTime? scheduledEndUtc = null;

        if (viewModel.ScheduledStartLocalDateTime.HasValue)
        {
            scheduledStartUtc = await _localClock.ConvertToUtcAsync(viewModel.ScheduledStartLocalDateTime.Value);
        }

        if (viewModel.ScheduledEndLocalDateTime.HasValue)
        {
            scheduledEndUtc = await _localClock.ConvertToUtcAsync(viewModel.ScheduledEndLocalDateTime.Value);
        }

        // Validate that end date is after start date (if both are provided)
        if (scheduledStartUtc.HasValue && scheduledEndUtc.HasValue && scheduledEndUtc <= scheduledStartUtc)
        {
            context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.ScheduledEndLocalDateTime),
                S["End date must be after start date."]);
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
        part.IsActive = viewModel.IsActive;
        part.ScheduledStartUtc = scheduledStartUtc;
        part.ScheduledEndUtc = scheduledEndUtc;

        // Only update goal fields if not locked
        if (!areGoalsLocked)
        {
            part.GoalType = viewModel.GoalType;
            part.GoalSelector = viewModel.GoalSelector;
            part.GoalScrollPercentage = viewModel.GoalScrollPercentage;
            part.GoalEventName = viewModel.GoalEventName;
        }

        return Edit(part, context);
    }

    private async ValueTask BuildViewModelAsync(ABTestPartViewModel model, ABTestPart part)
    {
        model.PercentageA = part.PercentageA;
        model.IsActive = part.IsActive;
        model.ABTestPart = part;

        // Convert UTC to local for display
        model.ScheduledStartUtc = part.ScheduledStartUtc;
        model.ScheduledEndUtc = part.ScheduledEndUtc;

        model.ScheduledStartLocalDateTime = part.ScheduledStartUtc.HasValue
            ? (await _localClock.ConvertToLocalAsync(part.ScheduledStartUtc.Value)).DateTime
            : null;

        model.ScheduledEndLocalDateTime = part.ScheduledEndUtc.HasValue
            ? (await _localClock.ConvertToLocalAsync(part.ScheduledEndUtc.Value)).DateTime
            : null;

        // Calculate the current status
        model.Status = CalculateStatus(part, _clock.UtcNow);

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

    private static ABTestStatus CalculateStatus(ABTestPart part, DateTime utcNow)
    {
        if (!part.IsActive)
        {
            return ABTestStatus.Inactive;
        }

        // If there's a start date and we haven't reached it yet
        if (part.ScheduledStartUtc.HasValue && utcNow < part.ScheduledStartUtc.Value)
        {
            return ABTestStatus.Scheduled;
        }

        // If there's an end date and we've passed it
        if (part.ScheduledEndUtc.HasValue && utcNow >= part.ScheduledEndUtc.Value)
        {
            return ABTestStatus.Ended;
        }

        return ABTestStatus.Running;
    }
}
