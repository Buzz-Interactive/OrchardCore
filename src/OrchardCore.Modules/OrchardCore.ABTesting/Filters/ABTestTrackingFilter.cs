using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using OrchardCore.ABTesting.Services;
using OrchardCore.Admin;
using OrchardCore.ContentManagement.Routing;
using OrchardCore.ResourceManagement;

namespace OrchardCore.ABTesting.Filters;

/// <summary>
/// Filter that injects JavaScript to track A/B test impressions.
/// </summary>
public sealed class ABTestTrackingFilter : IAsyncResultFilter
{
    private readonly IResourceManager _resourceManager;
    private readonly IABTestLookupService _lookupService;
    private readonly IVisitorAssignmentService _assignmentService;
    private readonly AutorouteOptions _autorouteOptions;
    private readonly JavaScriptEncoder _jsEncoder;

    public ABTestTrackingFilter(
        IResourceManager resourceManager,
        IABTestLookupService lookupService,
        IVisitorAssignmentService assignmentService,
        IOptions<AutorouteOptions> autorouteOptions,
        JavaScriptEncoder jsEncoder)
    {
        _resourceManager = resourceManager;
        _lookupService = lookupService;
        _assignmentService = assignmentService;
        _autorouteOptions = autorouteOptions.Value;
        _jsEncoder = jsEncoder;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Only run on front-end for full view results (not admin)
        if (context.IsViewOrPageResult() && !AdminAttribute.IsApplied(context.HttpContext))
        {
            await TryInjectTrackingScriptAsync(context);
        }

        await next.Invoke();
    }

    private async Task TryInjectTrackingScriptAsync(ResultExecutingContext context)
    {
        // Get the content item ID from route data
        var routeData = context.HttpContext.GetRouteData();
        if (routeData?.Values == null)
        {
            return;
        }

        if (!routeData.Values.TryGetValue(_autorouteOptions.ContentItemIdKey, out var contentItemIdObj) ||
            contentItemIdObj is not string contentItemId ||
            string.IsNullOrEmpty(contentItemId))
        {
            return;
        }

        // Check if this content is part of an active test
        var testInfo = await _lookupService.GetActiveTestForContentAsync(contentItemId);
        if (testInfo == null)
        {
            return;
        }

        // Get the visitor's assigned variant (don't create new assignment, just get existing)
        var assignedVariant = await _assignmentService.TryGetAssignmentAsync(testInfo.TestContentItemId);
        if (!assignedVariant.HasValue)
        {
            // Visitor hasn't been assigned yet - this shouldn't normally happen
            // since the middleware runs before the filter, but handle gracefully
            return;
        }

        var variantName = assignedVariant.Value.ToString();
        var testId = _jsEncoder.Encode(testInfo.TestContentItemId);

        // Inject tracking script
        var script = new HtmlString($@"<script>
(function() {{
    fetch('/api/abtest/impression', {{
        method: 'POST',
        headers: {{ 'Content-Type': 'application/json' }},
        body: JSON.stringify({{ testId: '{testId}', variant: '{variantName}' }})
    }}).catch(function() {{}});
}})();
</script>");

        _resourceManager.RegisterFootScript(script);
    }
}
