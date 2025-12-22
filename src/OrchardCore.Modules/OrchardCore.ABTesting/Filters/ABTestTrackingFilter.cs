using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using OrchardCore.ABTesting.Models;
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
        var assignedVariant = await _assignmentService.TryGetAssignmentAsync(testInfo.TestId);
        if (!assignedVariant.HasValue)
        {
            // Visitor hasn't been assigned yet - this shouldn't normally happen
            // since the middleware runs before the filter, but handle gracefully
            return;
        }

        var variantName = assignedVariant.Value.ToString();
        var testId = _jsEncoder.Encode(testInfo.TestId);

        // Inject impression tracking script with session-based deduplication
        var impressionScript = new HtmlString($@"<script>
(function() {{
    var key = 'abtest_imp_{testId}';
    if (sessionStorage.getItem(key)) return;
    sessionStorage.setItem(key, '1');
    fetch('/api/abtest/impression', {{
        method: 'POST',
        headers: {{ 'Content-Type': 'application/json' }},
        body: JSON.stringify({{ testId: '{testId}', variant: '{variantName}' }})
    }}).catch(function() {{}});
}})();
</script>");

        _resourceManager.RegisterFootScript(impressionScript);

        // Inject goal tracking script if a goal is configured
        if (testInfo.GoalType != GoalType.None)
        {
            var goalScript = GenerateGoalTrackingScript(testInfo, testId, variantName);
            if (!string.IsNullOrEmpty(goalScript))
            {
                _resourceManager.RegisterFootScript(new HtmlString(goalScript));
            }
        }
    }

    private string GenerateGoalTrackingScript(ABTestInfo testInfo, string testId, string variantName)
    {
        return testInfo.GoalType switch
        {
            GoalType.ButtonLinkClick => GenerateClickTrackingScript(testInfo.GoalSelector, testId, variantName),
            GoalType.FormSubmission => GenerateFormSubmissionScript(testInfo.GoalSelector, testId, variantName),
            GoalType.ScrollPercentage => GenerateScrollTrackingScript(testInfo.GoalScrollPercentage, testId, variantName),
            GoalType.CustomEvent => GenerateCustomEventScript(testInfo.GoalEventName, testId, variantName),
            _ => null
        };
    }

    private string GenerateClickTrackingScript(string selector, string testId, string variant)
    {
        if (string.IsNullOrEmpty(selector))
        {
            return null;
        }

        var encodedSelector = _jsEncoder.Encode(selector);
        return $@"<script>
(function() {{
    var key = 'abtest_conv_{testId}';
    if (sessionStorage.getItem(key)) return;
    document.querySelectorAll('{encodedSelector}').forEach(function(el) {{
        el.addEventListener('click', function() {{
            if (!sessionStorage.getItem(key)) {{
                sessionStorage.setItem(key, '1');
                fetch('/api/abtest/conversion', {{
                    method: 'POST',
                    headers: {{ 'Content-Type': 'application/json' }},
                    body: JSON.stringify({{ testId: '{testId}', variant: '{variant}' }})
                }}).catch(function() {{}});
            }}
        }});
    }});
}})();
</script>";
    }

    private string GenerateFormSubmissionScript(string selector, string testId, string variant)
    {
        if (string.IsNullOrEmpty(selector))
        {
            return null;
        }

        var encodedSelector = _jsEncoder.Encode(selector);
        return $@"<script>
(function() {{
    var key = 'abtest_conv_{testId}';
    if (sessionStorage.getItem(key)) return;
    var forms = document.querySelectorAll('{encodedSelector}');
    forms.forEach(function(form) {{
        if (form.tagName === 'FORM') {{
            form.addEventListener('submit', function() {{
                if (!sessionStorage.getItem(key)) {{
                    sessionStorage.setItem(key, '1');
                    fetch('/api/abtest/conversion', {{
                        method: 'POST',
                        headers: {{ 'Content-Type': 'application/json' }},
                        body: JSON.stringify({{ testId: '{testId}', variant: '{variant}' }})
                    }}).catch(function() {{}});
                }}
            }});
        }}
    }});
}})();
</script>";
    }

    private static string GenerateScrollTrackingScript(int percentage, string testId, string variant)
    {
        return $@"<script>
(function() {{
    var key = 'abtest_conv_{testId}';
    if (sessionStorage.getItem(key)) return;
    var threshold = {percentage};
    window.addEventListener('scroll', function() {{
        if (!sessionStorage.getItem(key)) {{
            var scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            var docHeight = document.documentElement.scrollHeight - window.innerHeight;
            if (docHeight > 0) {{
                var scrollPercent = (scrollTop / docHeight) * 100;
                if (scrollPercent >= threshold) {{
                    sessionStorage.setItem(key, '1');
                    fetch('/api/abtest/conversion', {{
                        method: 'POST',
                        headers: {{ 'Content-Type': 'application/json' }},
                        body: JSON.stringify({{ testId: '{testId}', variant: '{variant}' }})
                    }}).catch(function() {{}});
                }}
            }}
        }}
    }});
}})();
</script>";
    }

    private string GenerateCustomEventScript(string eventName, string testId, string variant)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            return null;
        }

        var encodedEventName = _jsEncoder.Encode(eventName);
        return $@"<script>
(function() {{
    var key = 'abtest_conv_{testId}';
    if (sessionStorage.getItem(key)) return;
    window.addEventListener('{encodedEventName}', function() {{
        if (!sessionStorage.getItem(key)) {{
            sessionStorage.setItem(key, '1');
            fetch('/api/abtest/conversion', {{
                method: 'POST',
                headers: {{ 'Content-Type': 'application/json' }},
                body: JSON.stringify({{ testId: '{testId}', variant: '{variant}' }})
            }}).catch(function() {{}});
        }}
    }});
}})();
</script>";
    }
}
