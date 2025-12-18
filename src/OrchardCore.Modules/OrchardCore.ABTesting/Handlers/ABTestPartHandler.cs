using Microsoft.Extensions.Localization;
using OrchardCore.ABTesting.Models;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;

namespace OrchardCore.ABTesting.Handlers;

/// <summary>
/// Content handler for ABTestPart that handles lifecycle events.
/// </summary>
public class ABTestPartHandler : ContentPartHandler<ABTestPart>
{
    private readonly IStringLocalizer S;

    public ABTestPartHandler(IStringLocalizer<ABTestPartHandler> stringLocalizer)
    {
        S = stringLocalizer;
    }

    /// <summary>
    /// Sets default values when initializing a new ABTestPart.
    /// </summary>
    public override Task InitializingAsync(InitializingContentContext context, ABTestPart part)
    {
        part.PercentageA = 50;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the part before publishing.
    /// </summary>
    public override Task ValidatingAsync(ValidateContentContext context, ABTestPart part)
    {
        // Get the ContentPickerFields from the part
        var variantAField = part.Get<ContentPickerField>("VariantA");
        var variantBField = part.Get<ContentPickerField>("VariantB");

        var variantAId = variantAField?.ContentItemIds?.FirstOrDefault();
        var variantBId = variantBField?.ContentItemIds?.FirstOrDefault();

        // Validate that both variants are selected
        if (string.IsNullOrEmpty(variantAId))
        {
            context.Fail(S["Variant A must be selected."], "ABTestPart.VariantA");
        }

        if (string.IsNullOrEmpty(variantBId))
        {
            context.Fail(S["Variant B must be selected."], "ABTestPart.VariantB");
        }

        // Validate that variants are different
        if (!string.IsNullOrEmpty(variantAId) && variantAId == variantBId)
        {
            context.Fail(S["Variant A and Variant B must be different content items."], "ABTestPart.VariantB");
        }

        // Validate percentage range
        if (part.PercentageA < 0 || part.PercentageA > 100)
        {
            context.Fail(S["Percentage must be between 0 and 100."], "ABTestPart.PercentageA");
        }

        return Task.CompletedTask;
    }
}
