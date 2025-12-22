using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
using OrchardCore.ABTesting.Settings;
using OrchardCore.ABTesting.ViewModels;
using OrchardCore.Admin;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Contents;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Settings;
using YesSql;
using YesSql.Services;

namespace OrchardCore.ABTesting.Controllers;

[Admin("ABTesting/{action}/{testId?}", "ABTesting.{action}")]
public class AdminController : Controller
{
    private readonly IABTestManager _abTestManager;
    private readonly IContentManager _contentManager;
    private readonly ITrackingService _trackingService;
    private readonly IStatisticalAnalysisService _statisticalAnalysisService;
    private readonly IABTestWinnerService _winnerService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShapeFactory _shapeFactory;
    private readonly INotifier _notifier;
    private readonly ISiteService _siteService;
    private readonly ISession _session;
    private readonly IContentDefinitionManager _contentDefinitionManager;
    private readonly IHtmlLocalizer H;

    public AdminController(
        IABTestManager abTestManager,
        IContentManager contentManager,
        ITrackingService trackingService,
        IStatisticalAnalysisService statisticalAnalysisService,
        IABTestWinnerService winnerService,
        IAuthorizationService authorizationService,
        IShapeFactory shapeFactory,
        INotifier notifier,
        ISiteService siteService,
        ISession session,
        IContentDefinitionManager contentDefinitionManager,
        IHtmlLocalizer<AdminController> htmlLocalizer)
    {
        _abTestManager = abTestManager;
        _contentManager = contentManager;
        _trackingService = trackingService;
        _statisticalAnalysisService = statisticalAnalysisService;
        _winnerService = winnerService;
        _authorizationService = authorizationService;
        _shapeFactory = shapeFactory;
        _notifier = notifier;
        _siteService = siteService;
        _session = session;
        _contentDefinitionManager = contentDefinitionManager;
        H = htmlLocalizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        var tests = await _abTestManager.GetAllAsync();
        var entries = new List<ABTestEntry>();

        foreach (var test in tests)
        {
            var (impressionsA, impressionsB) = await _trackingService.GetImpressionsAsync(test.TestId);
            var (conversionsA, conversionsB) = await _trackingService.GetConversionsAsync(test.TestId);

            var variantAName = GetVariantDisplayText(test, isVariantA: true);
            var variantBName = GetVariantDisplayText(test, isVariantA: false);

            var hasDeletedVariant = test.VariantAState == VariantState.Deleted ||
                                    test.VariantBState == VariantState.Deleted;
            var hasUnavailableVariant = test.VariantAState != VariantState.Available ||
                                        test.VariantBState != VariantState.Available;

            var winningVariantDisplayName = test.WinningVariant switch
            {
                ABVariant.A => variantAName,
                ABVariant.B => variantBName,
                _ => null,
            };

            entries.Add(new ABTestEntry
            {
                TestId = test.TestId,
                Name = test.Name ?? "Unnamed Test",
                IsActive = test.IsActive,
                VariantADisplayText = variantAName,
                VariantBDisplayText = variantBName,
                PercentageA = test.PercentageA,
                TotalImpressions = impressionsA + impressionsB,
                TotalConversions = conversionsA + conversionsB,
                CreatedUtc = test.CreatedUtc,
                VariantAState = test.VariantAState,
                VariantBState = test.VariantBState,
                HasDeletedVariant = hasDeletedVariant,
                HasUnavailableVariant = hasUnavailableVariant,
                IsConcluded = test.IsConcluded,
                WinningVariant = test.WinningVariant,
                WinningVariantDisplayName = winningVariantDisplayName,
            });
        }

        var viewModel = new ABTestIndexViewModel
        {
            Tests = entries,
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        var viewModel = new ABTestViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ABTestViewModel viewModel)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        // Validate variants are different
        if (viewModel.VariantAContentItemId == viewModel.VariantBContentItemId)
        {
            ModelState.AddModelError(nameof(viewModel.VariantBContentItemId), "Variant A and Variant B must be different.");
            return View(viewModel);
        }

        // Validate goal configuration
        if (!ValidateGoalConfiguration(viewModel))
        {
            return View(viewModel);
        }

        // Validate variants are not in active tests
        var conflictingTests = await _abTestManager.GetActiveTestsWithConflictingVariantsAsync(
            viewModel.VariantAContentItemId,
            viewModel.VariantBContentItemId);

        if (conflictingTests.Any())
        {
            var conflictingNames = string.Join(", ", conflictingTests.Select(t => t.Name ?? t.TestId));
            ModelState.AddModelError(string.Empty,
                $"One or both variants are already used in active test(s): {conflictingNames}");
            return View(viewModel);
        }

        var test = new ABTest
        {
            Name = viewModel.Name,
            VariantAContentItemId = viewModel.VariantAContentItemId,
            VariantBContentItemId = viewModel.VariantBContentItemId,
            PercentageA = viewModel.PercentageA,
            GoalType = viewModel.GoalType,
            GoalSelector = viewModel.GoalSelector,
            GoalScrollPercentage = viewModel.GoalScrollPercentage,
            GoalEventName = viewModel.GoalEventName,
            GoalTimeOnPageSeconds = viewModel.GoalTimeOnPageSeconds,
            MinimumSampleSize = viewModel.MinimumSampleSize,
            ConfidenceThreshold = viewModel.ConfidenceThreshold,
        };

        await _abTestManager.CreateAsync(test);

        await _notifier.SuccessAsync(H["A/B Test created successfully."]);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string testId)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        var test = await _abTestManager.GetAsync(testId);
        if (test == null)
        {
            return NotFound();
        }

        // If any variant has been deleted, redirect to Results page (test is read-only)
        var hasDeletedVariant = test.VariantAState == VariantState.Deleted ||
                                test.VariantBState == VariantState.Deleted;
        if (hasDeletedVariant)
        {
            await _notifier.WarningAsync(H["This test cannot be edited because one or more variants have been deleted. You can view the results below."]);
            return RedirectToAction(nameof(Results), new { testId });
        }

        var (impressionsA, impressionsB) = await _trackingService.GetImpressionsAsync(testId);
        var (conversionsA, conversionsB) = await _trackingService.GetConversionsAsync(testId);
        var totalImpressions = impressionsA + impressionsB;

        var viewModel = new ABTestViewModel
        {
            TestId = test.TestId,
            Name = test.Name,
            VariantAContentItemId = test.VariantAContentItemId,
            VariantBContentItemId = test.VariantBContentItemId,
            VariantADisplayText = await GetContentDisplayTextAsync(test.VariantAContentItemId),
            VariantBDisplayText = await GetContentDisplayTextAsync(test.VariantBContentItemId),
            SelectedVariantA = await GetSelectedItemAsync(test.VariantAContentItemId),
            SelectedVariantB = await GetSelectedItemAsync(test.VariantBContentItemId),
            PercentageA = test.PercentageA,
            GoalType = test.GoalType,
            GoalSelector = test.GoalSelector,
            GoalScrollPercentage = test.GoalScrollPercentage,
            GoalEventName = test.GoalEventName,
            GoalTimeOnPageSeconds = test.GoalTimeOnPageSeconds,
            MinimumSampleSize = test.MinimumSampleSize,
            ConfidenceThreshold = test.ConfidenceThreshold,
            IsActive = test.IsActive,
            TotalImpressions = totalImpressions,
            TotalConversions = conversionsA + conversionsB,
            AreGoalsLocked = totalImpressions > 0,
            CreatedUtc = test.CreatedUtc,
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string testId, ABTestViewModel viewModel)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        viewModel.TestId = testId;
        var test = await _abTestManager.GetAsync(testId);
        if (test == null)
        {
            return NotFound();
        }

        // If any variant has been deleted, block edits
        var hasDeletedVariant = test.VariantAState == VariantState.Deleted ||
                                test.VariantBState == VariantState.Deleted;
        if (hasDeletedVariant)
        {
            await _notifier.ErrorAsync(H["This test cannot be edited because one or more variants have been deleted."]);
            return RedirectToAction(nameof(Results), new { testId });
        }

        if (!ModelState.IsValid)
        {
            await PopulateViewModelDisplayData(viewModel);
            return View(viewModel);
        }

        // Validate variants are different
        if (viewModel.VariantAContentItemId == viewModel.VariantBContentItemId)
        {
            ModelState.AddModelError(nameof(viewModel.VariantBContentItemId), "Variant A and Variant B must be different.");
            await PopulateViewModelDisplayData(viewModel);
            return View(viewModel);
        }

        // Check if goals are locked
        var (impressionsA, impressionsB) = await _trackingService.GetImpressionsAsync(viewModel.TestId);
        var totalImpressions = impressionsA + impressionsB;
        var areGoalsLocked = totalImpressions > 0;

        // Validate goal configuration only if not locked
        if (!areGoalsLocked && !ValidateGoalConfiguration(viewModel))
        {
            await PopulateViewModelDisplayData(viewModel);
            return View(viewModel);
        }

        // Validate variants are not in other active tests (exclude current test)
        var conflictingTests = await _abTestManager.GetActiveTestsWithConflictingVariantsAsync(
            viewModel.VariantAContentItemId,
            viewModel.VariantBContentItemId,
            testId);

        if (conflictingTests.Any())
        {
            var conflictingNames = string.Join(", ", conflictingTests.Select(t => t.Name ?? t.TestId));
            ModelState.AddModelError(string.Empty,
                $"One or both variants are already used in active test(s): {conflictingNames}");
            await PopulateViewModelDisplayData(viewModel);
            return View(viewModel);
        }

        // Update the test
        test.Name = viewModel.Name;
        test.VariantAContentItemId = viewModel.VariantAContentItemId;
        test.VariantBContentItemId = viewModel.VariantBContentItemId;
        test.PercentageA = viewModel.PercentageA;
        test.MinimumSampleSize = viewModel.MinimumSampleSize;
        test.ConfidenceThreshold = viewModel.ConfidenceThreshold;

        // Only update goals if not locked
        if (!areGoalsLocked)
        {
            test.GoalType = viewModel.GoalType;
            test.GoalSelector = viewModel.GoalSelector;
            test.GoalScrollPercentage = viewModel.GoalScrollPercentage;
            test.GoalEventName = viewModel.GoalEventName;
            test.GoalTimeOnPageSeconds = viewModel.GoalTimeOnPageSeconds;
        }

        await _abTestManager.UpdateAsync(test);

        await _notifier.SuccessAsync(H["A/B Test updated successfully."]);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string testId)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        var test = await _abTestManager.GetAsync(testId);
        if (test == null)
        {
            return NotFound();
        }

        await _abTestManager.DeleteAsync(testId);

        await _notifier.SuccessAsync(H["A/B Test deleted successfully."]);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string testId)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        try
        {
            await _abTestManager.ActivateAsync(testId);
            await _notifier.SuccessAsync(H["A/B Test activated successfully."]);
        }
        catch (InvalidOperationException ex)
        {
            await _notifier.ErrorAsync(H[ex.Message]);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string testId)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        try
        {
            await _abTestManager.DeactivateAsync(testId);
            await _notifier.SuccessAsync(H["A/B Test deactivated successfully."]);
        }
        catch (InvalidOperationException ex)
        {
            await _notifier.ErrorAsync(H[ex.Message]);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Results(string testId)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return NotFound();
        }

        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        var test = await _abTestManager.GetAsync(testId);
        if (test == null)
        {
            return NotFound();
        }

        // Get impression counts
        var (variantAImpressions, variantBImpressions) = await _trackingService.GetImpressionsAsync(testId);

        // Get variant names (using cached names if content items are unavailable)
        var variantAName = GetVariantDisplayText(test, isVariantA: true);
        var variantBName = GetVariantDisplayText(test, isVariantA: false);

        // Calculate percentages
        var totalImpressions = variantAImpressions + variantBImpressions;
        var variantAPercentage = totalImpressions > 0
            ? Math.Round((double)variantAImpressions / totalImpressions * 100, 1)
            : 0;
        var variantBPercentage = totalImpressions > 0
            ? Math.Round((double)variantBImpressions / totalImpressions * 100, 1)
            : 0;

        // Get conversion counts
        var (variantAConversions, variantBConversions) = await _trackingService.GetConversionsAsync(testId);
        var totalConversions = variantAConversions + variantBConversions;

        // Calculate conversion rates (conversions / impressions)
        var variantAConversionRate = variantAImpressions > 0
            ? Math.Round((double)variantAConversions / variantAImpressions * 100, 2)
            : 0;
        var variantBConversionRate = variantBImpressions > 0
            ? Math.Round((double)variantBConversions / variantBImpressions * 100, 2)
            : 0;

        // Get goal display name based on type
        var goalDisplayName = GetDefaultGoalName(test.GoalType);

        // Perform statistical analysis (only meaningful when there's a goal)
        var statistics = test.GoalType != GoalType.None
            ? _statisticalAnalysisService.Analyze(
                variantAImpressions,
                variantBImpressions,
                variantAConversions,
                variantBConversions,
                test.MinimumSampleSize,
                test.ConfidenceThreshold)
            : null;

        // Build shape with all the data
        var shape = await _shapeFactory.New.ABTestResults(
            TestName: test.Name ?? "Unnamed Test",
            TestId: testId,
            TargetPercentageA: test.PercentageA,
            TargetPercentageB: 100 - test.PercentageA,
            IsActive: test.IsActive,
            IsConcluded: test.IsConcluded,
            WinningVariant: test.WinningVariant,
            ConcludedUtc: test.ConcludedUtc,
            VariantAName: variantAName,
            VariantAContentItemId: test.VariantAContentItemId,
            VariantAImpressions: variantAImpressions,
            VariantAPercentage: variantAPercentage,
            VariantBName: variantBName,
            VariantBContentItemId: test.VariantBContentItemId,
            VariantBImpressions: variantBImpressions,
            VariantBPercentage: variantBPercentage,
            TotalImpressions: totalImpressions,
            GoalType: test.GoalType,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclareWinner(string testId, ABVariant winner)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        try
        {
            var success = await _winnerService.DeclareWinnerAsync(testId, winner);
            if (success)
            {
                var winnerLabel = winner == ABVariant.A ? "Control (A)" : "Challenger (B)";
                await _notifier.SuccessAsync(H["Winner declared: {0}. The test has been concluded.", winnerLabel]);
            }
        }
        catch (InvalidOperationException ex)
        {
            await _notifier.ErrorAsync(H[ex.Message]);
        }

        return RedirectToAction(nameof(Results), new { testId });
    }

    [HttpGet]
    public async Task<IActionResult> SearchContentItems(string query)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageABTests))
        {
            return Forbid();
        }

        // Get the A/B Testing settings
        var settings = await _siteService.GetSettingsAsync<ABTestingSettings>();

        // Determine which content types to search
        IEnumerable<string> contentTypes;
        if (settings.DisplayAllContentTypes)
        {
            // Get all content types without a stereotype (regular content types)
            contentTypes = (await _contentDefinitionManager.ListTypeDefinitionsAsync())
                .Where(x => !x.HasStereotype())
                .Select(x => x.Name);
        }
        else
        {
            contentTypes = settings.AllowedContentTypes ?? [];
        }

        if (!contentTypes.Any())
        {
            return new ObjectResult(new List<VueMultiselectItemViewModel>());
        }

        // Query the content index
        var contentQuery = _session.Query<ContentItem, ContentItemIndex>()
            .With<ContentItemIndex>(x => x.ContentType.IsIn(contentTypes) && x.Latest);

        if (!string.IsNullOrEmpty(query))
        {
            contentQuery.With<ContentItemIndex>(x => x.DisplayText.Contains(query) || x.ContentType.Contains(query));
        }

        var contentItems = await contentQuery.Take(50).ListAsync();

        // Build the result list
        var results = new List<VueMultiselectItemViewModel>();
        foreach (var contentItem in contentItems)
        {
            results.Add(new VueMultiselectItemViewModel
            {
                Id = contentItem.ContentItemId,
                DisplayText = contentItem.DisplayText ?? contentItem.ContentItemId,
                HasPublished = contentItem.IsPublished(),
                IsViewable = await _authorizationService.AuthorizeAsync(User, CommonPermissions.EditContent, contentItem),
            });
        }

        return new ObjectResult(results.OrderBy(x => x.DisplayText));
    }

    private async Task<string> GetContentDisplayTextAsync(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return "(Not selected)";
        }

        var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
        return contentItem?.DisplayText ?? "(Not found)";
    }

    private async Task<VueMultiselectItemViewModel> GetSelectedItemAsync(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
        if (contentItem == null)
        {
            return null;
        }

        return new VueMultiselectItemViewModel
        {
            Id = contentItem.ContentItemId,
            DisplayText = contentItem.DisplayText ?? contentItem.ContentItemId,
            HasPublished = contentItem.IsPublished(),
            IsViewable = await _authorizationService.AuthorizeAsync(User, CommonPermissions.EditContent, contentItem),
        };
    }

    private async Task PopulateViewModelDisplayData(ABTestViewModel viewModel)
    {
        viewModel.VariantADisplayText = await GetContentDisplayTextAsync(viewModel.VariantAContentItemId);
        viewModel.VariantBDisplayText = await GetContentDisplayTextAsync(viewModel.VariantBContentItemId);
        viewModel.SelectedVariantA = await GetSelectedItemAsync(viewModel.VariantAContentItemId);
        viewModel.SelectedVariantB = await GetSelectedItemAsync(viewModel.VariantBContentItemId);

        var (impressionsA, impressionsB) = await _trackingService.GetImpressionsAsync(viewModel.TestId);
        var (conversionsA, conversionsB) = await _trackingService.GetConversionsAsync(viewModel.TestId);
        viewModel.TotalImpressions = impressionsA + impressionsB;
        viewModel.TotalConversions = conversionsA + conversionsB;

        var test = await _abTestManager.GetAsync(viewModel.TestId);
        if (test != null)
        {
            viewModel.IsActive = test.IsActive;
            viewModel.AreGoalsLocked = viewModel.TotalImpressions > 0;
            viewModel.CreatedUtc = test.CreatedUtc;
        }
    }

    private bool ValidateGoalConfiguration(ABTestViewModel viewModel)
    {
        switch (viewModel.GoalType)
        {
            case GoalType.ButtonLinkClick:
            case GoalType.FormSubmission:
                if (string.IsNullOrWhiteSpace(viewModel.GoalSelector))
                {
                    ModelState.AddModelError(nameof(viewModel.GoalSelector), "CSS selector is required for this goal type.");
                    return false;
                }
                break;

            case GoalType.ScrollPercentage:
                if (viewModel.GoalScrollPercentage < 0 || viewModel.GoalScrollPercentage > 100)
                {
                    ModelState.AddModelError(nameof(viewModel.GoalScrollPercentage), "Scroll percentage must be between 0 and 100.");
                    return false;
                }
                break;

            case GoalType.CustomEvent:
                if (string.IsNullOrWhiteSpace(viewModel.GoalEventName))
                {
                    ModelState.AddModelError(nameof(viewModel.GoalEventName), "Event name is required for custom event goals.");
                    return false;
                }
                break;

            case GoalType.TimeOnPage:
                if (viewModel.GoalTimeOnPageSeconds < 5 || viewModel.GoalTimeOnPageSeconds > 300)
                {
                    ModelState.AddModelError(nameof(viewModel.GoalTimeOnPageSeconds), "Time on page must be between 5 and 300 seconds.");
                    return false;
                }
                break;
        }

        return true;
    }

    private static string GetDefaultGoalName(GoalType goalType)
    {
        return goalType switch
        {
            GoalType.ButtonLinkClick => "Click",
            GoalType.FormSubmission => "Form Submit",
            GoalType.ScrollPercentage => "Scroll",
            GoalType.CustomEvent => "Event",
            GoalType.TimeOnPage => "Time on Page",
            _ => "None"
        };
    }

    private static string GetVariantDisplayText(ABTest test, bool isVariantA)
    {
        var state = isVariantA ? test.VariantAState : test.VariantBState;
        var cachedName = isVariantA ? test.VariantADisplayName : test.VariantBDisplayName;
        var contentItemId = isVariantA ? test.VariantAContentItemId : test.VariantBContentItemId;

        // Use cached name if variant is unavailable, otherwise return the ID as fallback
        var displayName = cachedName ?? contentItemId ?? "(Not selected)";

        return state switch
        {
            VariantState.Unpublished => $"{displayName} (Unpublished)",
            VariantState.Deleted => $"{displayName} (Deleted)",
            _ => displayName
        };
    }
}
