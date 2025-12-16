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

    internal readonly IStringLocalizer S;

    public ABTestPartDisplayDriver(
        IContentManager contentManager,
        IImpressionService impressionService,
        IStringLocalizer<ABTestPartDisplayDriver> localizer)
    {
        _contentManager = contentManager;
        _impressionService = impressionService;
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
            m => m.IsActive);

        // Validate percentage range
        if (viewModel.PercentageA < 0 || viewModel.PercentageA > 100)
        {
            context.Updater.ModelState.AddModelError(Prefix + "." + nameof(viewModel.PercentageA),
                S["Percentage must be between 0 and 100."]);
        }

        part.PercentageA = Math.Clamp(viewModel.PercentageA, 0, 100);
        part.IsActive = viewModel.IsActive;

        return Edit(part, context);
    }

    private async ValueTask BuildViewModelAsync(ABTestPartViewModel model, ABTestPart part)
    {
        model.PercentageA = part.PercentageA;
        model.IsActive = part.IsActive;
        model.ABTestPart = part;

        // Get total impressions
        var contentItemId = part.ContentItem.ContentItemId;
        var (variantAImpressions, variantBImpressions) = await _impressionService.GetImpressionsAsync(contentItemId);
        model.TotalImpressions = variantAImpressions + variantBImpressions;

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
