using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

// Helper to safely access session when middleware may not be configured
internal static class SessionExtensions
{
    public static bool TryGetSession(this HttpContext httpContext, out ISession session)
    {
        session = null;
        var sessionFeature = httpContext.Features.Get<ISessionFeature>();
        if (sessionFeature?.Session != null)
        {
            session = sessionFeature.Session;
            return true;
        }
        return false;
    }
}

/// <summary>
/// Service for assigning visitors to A/B test variants.
/// Uses cookies for persistent storage with GDPR-compliant fallback to session storage.
/// </summary>
public class VisitorAssignmentService : IVisitorAssignmentService
{
    private const string CookieName = ".OrchardCore.ABTesting.Assignments";
    private const string SessionKey = "ABTesting_Assignments";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public VisitorAssignmentService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Task<ABVariant> GetOrAssignVariantAsync(string testContentItemId, int percentageA)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            // No HTTP context - use random selection without persistence
            return Task.FromResult(SelectVariantByPercentage(percentageA));
        }

        // Check for existing assignment
        var existingAssignment = TryGetAssignmentFromStorage(testContentItemId);
        if (existingAssignment.HasValue)
        {
            return Task.FromResult(existingAssignment.Value);
        }

        // Select new variant based on percentage
        var selectedVariant = SelectVariantByPercentage(percentageA);

        // Store assignment
        StoreAssignment(testContentItemId, selectedVariant);

        return Task.FromResult(selectedVariant);
    }

    /// <inheritdoc />
    public Task<ABVariant?> TryGetAssignmentAsync(string testContentItemId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.FromResult<ABVariant?>(null);
        }

        return Task.FromResult(TryGetAssignmentFromStorage(testContentItemId));
    }

    /// <inheritdoc />
    public Task ClearAssignmentsAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        // Clear cookie if tracking is allowed
        if (CanUseCookies())
        {
            httpContext.Response.Cookies.Delete(CookieName);
        }

        // Clear session if available
        if (httpContext.TryGetSession(out var session))
        {
            session.Remove(SessionKey);
        }

        return Task.CompletedTask;
    }

    private ABVariant? TryGetAssignmentFromStorage(string testContentItemId)
    {
        var assignments = GetAssignments();
        if (assignments.TryGetValue(testContentItemId, out var variantString))
        {
            if (Enum.TryParse<ABVariant>(variantString, out var variant))
            {
                return variant;
            }
        }

        return null;
    }

    private Dictionary<string, string> GetAssignments()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return new Dictionary<string, string>();
        }

        // Try cookie first (if tracking consent is granted)
        if (CanUseCookies())
        {
            if (httpContext.Request.Cookies.TryGetValue(CookieName, out var cookieValue))
            {
                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(cookieValue)
                           ?? new Dictionary<string, string>();
                }
                catch
                {
                    // Invalid cookie, ignore
                }
            }
        }

        // Fall back to session (GDPR-compliant, doesn't require consent)
        if (httpContext.TryGetSession(out var session))
        {
            var sessionValue = session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(sessionValue))
            {
                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(sessionValue)
                           ?? new Dictionary<string, string>();
                }
                catch
                {
                    // Invalid session data, ignore
                }
            }
        }

        return new Dictionary<string, string>();
    }

    private void StoreAssignment(string testContentItemId, ABVariant variant)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var assignments = GetAssignments();
        assignments[testContentItemId] = variant.ToString();
        var json = JsonSerializer.Serialize(assignments);

        // Store in cookie if tracking is allowed
        if (CanUseCookies())
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(365), // 1 year expiry
                IsEssential = false, // Not essential, requires consent
            };

            httpContext.Response.Cookies.Append(CookieName, json, cookieOptions);
        }
        else if (httpContext.TryGetSession(out var session))
        {
            // Fall back to session storage (GDPR-compliant)
            session.SetString(SessionKey, json);
        }
    }

    private bool CanUseCookies()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return false;
        }

        // Check ITrackingConsentFeature - this integrates with ASP.NET Core's cookie consent
        var trackingConsent = httpContext.Features.Get<ITrackingConsentFeature>();

        // If consent feature is not configured, default to NOT using cookies (safer for GDPR)
        // User explicitly requested GDPR fallback when cookies are rejected
        return trackingConsent?.CanTrack ?? false;
    }

    private static ABVariant SelectVariantByPercentage(int percentageA)
    {
        // Clamp percentage to valid range
        percentageA = Math.Clamp(percentageA, 0, 100);

        // Generate random number between 0-99
        var random = Random.Shared.Next(100);

        // Select variant based on percentage
        // If percentageA is 70, random < 70 returns A, otherwise B
        return random < percentageA ? ABVariant.A : ABVariant.B;
    }
}
