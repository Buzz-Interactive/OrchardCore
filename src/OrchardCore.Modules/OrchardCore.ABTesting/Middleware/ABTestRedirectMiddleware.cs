using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using OrchardCore.ABTesting.Services;

namespace OrchardCore.ABTesting.Middleware;

/// <summary>
/// Middleware that handles redirects for A/B test URLs.
/// - During active tests: redirects direct visits to Variant B's URL back to Variant A
/// - After test conclusion: redirects loser's original URL to winner's URL
/// Authenticated users are exempt from redirects.
/// </summary>
public class ABTestRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public ABTestRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IABTestRedirectService redirectService)
    {
        // Skip for authenticated users (admin exemption)
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Only redirect GET/HEAD requests
        if (!HttpMethods.IsGet(context.Request.Method) &&
            !HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Get the request path
        var requestPath = context.Request.Path.Value;
        if (string.IsNullOrEmpty(requestPath) || requestPath == "/")
        {
            await _next(context);
            return;
        }

        // Check if this path needs a redirect
        var redirectPath = await redirectService.GetRedirectPathAsync(requestPath);

        if (!string.IsNullOrEmpty(redirectPath) &&
            !string.Equals(redirectPath, requestPath, StringComparison.OrdinalIgnoreCase))
        {
            // Preserve query string
            var queryString = context.Request.QueryString.Value;
            var redirectUrl = redirectPath + queryString;

            // Perform 301 Permanent Redirect
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = redirectUrl;
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering the A/B test redirect middleware.
/// </summary>
public static class ABTestRedirectMiddlewareExtensions
{
    /// <summary>
    /// Adds the A/B test redirect middleware to the application pipeline.
    /// Should be called before UseABTesting().
    /// </summary>
    public static IApplicationBuilder UseABTestRedirects(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ABTestRedirectMiddleware>();
    }
}
