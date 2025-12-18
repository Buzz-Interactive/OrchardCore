using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
using OrchardCore.ABTesting.Settings;
using OrchardCore.Admin;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement;
using OrchardCore.Settings;

namespace OrchardCore.ABTesting.Controllers;

[Admin("ABTesting/{action}/{contentItemId?}", "ABTesting.{action}")]
public class AdminController : Controller
{
    private readonly IContentManager _contentManager;
    private readonly IImpressionService _impressionService;
    private readonly IGoalService _goalService;
    private readonly IStatisticalAnalysisService _statisticalAnalysisService;
    private readonly ISiteService _siteService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShapeFactory _shapeFactory;

    public AdminController(
        IContentManager contentManager,
        IImpressionService impressionService,
        IGoalService goalService,
        IStatisticalAnalysisService statisticalAnalysisService,
        ISiteService siteService,
        IAuthorizationService authorizationService,
        IShapeFactory shapeFactory)
    {
        _contentManager = contentManager;
        _impressionService = impressionService;
        _goalService = goalService;
        _statisticalAnalysisService = statisticalAnalysisService;
        _siteService = siteService;
        _authorizationService = authorizationService;
        _shapeFactory = shapeFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Results(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return NotFound();
        }

        // Check permissions
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        // Load the ABTest content item
        var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
        if (contentItem == null || contentItem.ContentType != "ABTest")
        {
            return NotFound();
        }

        var abTestPart = contentItem.As<ABTestPart>();
        if (abTestPart == null)
        {
            return NotFound();
        }

        // Get impression counts
        var (variantAImpressions, variantBImpressions) = await _impressionService.GetImpressionsAsync(contentItemId);

        // Get variant names
        var variantAField = abTestPart.Get<ContentPickerField>("VariantA");
        var variantBField = abTestPart.Get<ContentPickerField>("VariantB");

        string variantAName = "(Not selected)";
        string variantAContentItemId = null;
        string variantBName = "(Not selected)";
        string variantBContentItemId = null;

        if (variantAField?.ContentItemIds?.Length > 0)
        {
            variantAContentItemId = variantAField.ContentItemIds[0];
            var variantA = await _contentManager.GetAsync(variantAField.ContentItemIds[0], VersionOptions.Latest);
            variantAName = variantA?.DisplayText ?? "(Not found)";
        }

        if (variantBField?.ContentItemIds?.Length > 0)
        {
            variantBContentItemId = variantBField.ContentItemIds[0];
            var variantB = await _contentManager.GetAsync(variantBField.ContentItemIds[0], VersionOptions.Latest);
            variantBName = variantB?.DisplayText ?? "(Not found)";
        }

        // Calculate percentages
        var totalImpressions = variantAImpressions + variantBImpressions;
        var variantAPercentage = totalImpressions > 0
            ? Math.Round((double)variantAImpressions / totalImpressions * 100, 1)
            : 0;
        var variantBPercentage = totalImpressions > 0
            ? Math.Round((double)variantBImpressions / totalImpressions * 100, 1)
            : 0;

        // Get conversion counts
        var (variantAConversions, variantBConversions) = await _goalService.GetConversionsAsync(contentItemId);
        var totalConversions = variantAConversions + variantBConversions;

        // Calculate conversion rates (conversions / impressions)
        var variantAConversionRate = variantAImpressions > 0
            ? Math.Round((double)variantAConversions / variantAImpressions * 100, 2)
            : 0;
        var variantBConversionRate = variantBImpressions > 0
            ? Math.Round((double)variantBConversions / variantBImpressions * 100, 2)
            : 0;

        // Get goal display name based on type
        var goalDisplayName = GetDefaultGoalName(abTestPart.GoalType);

        // Get settings for statistical analysis
        var settings = await _siteService.GetSettingsAsync<ABTestingSettings>();

        // Perform statistical analysis (only meaningful when there's a goal)
        var statistics = abTestPart.GoalType != GoalType.None
            ? _statisticalAnalysisService.Analyze(
                variantAImpressions,
                variantBImpressions,
                variantAConversions,
                variantBConversions,
                settings.MinimumSampleSize,
                settings.ConfidenceThreshold)
            : null;

        // Build shape with all the data
        var shape = await _shapeFactory.New.ABTestResults(
            TestName: contentItem.DisplayText ?? "Unnamed Test",
            TestContentItemId: contentItemId,
            TargetPercentageA: abTestPart.PercentageA,
            TargetPercentageB: 100 - abTestPart.PercentageA,
            IsPublished: contentItem.Published,
            VariantAName: variantAName,
            VariantAContentItemId: variantAContentItemId,
            VariantAImpressions: variantAImpressions,
            VariantAPercentage: variantAPercentage,
            VariantBName: variantBName,
            VariantBContentItemId: variantBContentItemId,
            VariantBImpressions: variantBImpressions,
            VariantBPercentage: variantBPercentage,
            TotalImpressions: totalImpressions,
            GoalType: abTestPart.GoalType,
            GoalDisplayName: goalDisplayName,
            VariantAConversions: variantAConversions,
            VariantBConversions: variantBConversions,
            VariantAConversionRate: variantAConversionRate,
            VariantBConversionRate: variantBConversionRate,
            TotalConversions: totalConversions,
            Statistics: statistics
        );

        return View(shape);
    }

    private static string GetDefaultGoalName(GoalType goalType)
    {
        return goalType switch
        {
            GoalType.ButtonLinkClick => "Click",
            GoalType.FormSubmission => "Form Submit",
            GoalType.ScrollPercentage => "Scroll",
            GoalType.CustomEvent => "Event",
            _ => "None"
        };
    }
}
