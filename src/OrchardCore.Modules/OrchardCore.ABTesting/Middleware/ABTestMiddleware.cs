using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using OrchardCore.ABTesting.Services;
using OrchardCore.ContentManagement.Routing;

namespace OrchardCore.ABTesting.Middleware;

/// <summary>
/// Middleware that intercepts requests to content items that are part of an A/B test
/// and modifies the route values to serve the assigned variant.
/// </summary>
public class ABTestMiddleware
{
    private readonly RequestDelegate _next;

    public ABTestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IABTestContentResolver resolver,
        IOptions<AutorouteOptions> autorouteOptions)
    {
        // Get route data after routing has completed
        var routeData = context.GetRouteData();
        var contentItemIdKey = autorouteOptions.Value.ContentItemIdKey;

        // Check if this request has a content item ID (set by autoroute or other routing)
        if (routeData?.Values != null &&
            routeData.Values.TryGetValue(contentItemIdKey, out var contentItemIdObj) &&
            contentItemIdObj is string contentItemId &&
            !string.IsNullOrEmpty(contentItemId))
        {
            // Check if we should swap to a different variant
            var resolvedContentItemId = await resolver.ResolveContentItemIdAsync(contentItemId);

            if (resolvedContentItemId != contentItemId)
            {
                // Swap the content item ID to show the assigned variant
                routeData.Values[contentItemIdKey] = resolvedContentItemId;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering the A/B testing middleware.
/// </summary>
public static class ABTestMiddlewareExtensions
{
    /// <summary>
    /// Adds the A/B testing middleware to the application pipeline.
    /// Should be called after UseRouting() but before UseEndpoints().
    /// </summary>
    public static IApplicationBuilder UseABTesting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ABTestMiddleware>();
    }
}
