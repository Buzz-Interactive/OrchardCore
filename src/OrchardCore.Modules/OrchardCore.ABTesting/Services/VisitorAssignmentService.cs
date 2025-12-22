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
/// Balances variant selection based on impression counts.
/// </summary>
public class VisitorAssignmentService : IVisitorAssignmentService
{
    private const string CookieName = ".OrchardCore.ABTesting.Assignments";
    private const string SessionKey = "ABTesting_Assignments";
    private const string HttpContextItemsKey = "ABTesting_CurrentRequestAssignments";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITrackingService _trackingService;

    public VisitorAssignmentService(
        IHttpContextAccessor httpContextAccessor,
        ITrackingService trackingService)
    {
        _httpContextAccessor = httpContextAccessor;
        _trackingService = trackingService;
    }

    /// <inheritdoc />
    public async Task<ABVariant> GetOrAssignVariantAsync(string testId, int percentageA)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            // No HTTP context - use random selection without persistence
            return SelectVariantByPercentage(percentageA);
        }

        // Check for existing assignment
        var existingAssignment = TryGetAssignmentFromStorage(testId);
        if (existingAssignment.HasValue)
        {
            return existingAssignment.Value;
        }

        // Get effective percentage based on impression balancing
        var effectivePercentageA = await GetBalancedPercentageAsync(testId, percentageA);

        // Select new variant based on balanced percentage
        var selectedVariant = SelectVariantByPercentage(effectivePercentageA);

        // Store assignment
        StoreAssignment(testId, selectedVariant);

        return selectedVariant;
    }

    /// <inheritdoc />
    public Task<ABVariant?> TryGetAssignmentAsync(string testId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.FromResult<ABVariant?>(null);
        }

        return Task.FromResult(TryGetAssignmentFromStorage(testId));
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

    private ABVariant? TryGetAssignmentFromStorage(string testId)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // First check HttpContext.Items for assignments made during this request
        // This handles the case where assignment was just made but cookie isn't available yet
        if (httpContext?.Items[HttpContextItemsKey] is Dictionary<string, ABVariant> currentRequestAssignments &&
            currentRequestAssignments.TryGetValue(testId, out var currentRequestVariant))
        {
            return currentRequestVariant;
        }

        var assignments = GetAssignments();
        if (assignments.TryGetValue(testId, out var variantString))
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

    private void StoreAssignment(string testId, ABVariant variant)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        // Store in HttpContext.Items for immediate access within this request
        // This ensures the tracking filter can access the assignment even though
        // cookies won't be available until the next request
        if (httpContext.Items[HttpContextItemsKey] is not Dictionary<string, ABVariant> currentRequestAssignments)
        {
            currentRequestAssignments = new Dictionary<string, ABVariant>();
            httpContext.Items[HttpContextItemsKey] = currentRequestAssignments;
        }
        currentRequestAssignments[testId] = variant;

        var assignments = GetAssignments();
        assignments[testId] = variant.ToString();
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

    /// <summary>
    /// Calculates the effective percentage for Variant A based on current impressions.
    /// Adjusts the target percentage to balance impression counts.
    /// </summary>
    private async Task<int> GetBalancedPercentageAsync(string testId, int targetPercentageA)
    {
        var (impressionsA, impressionsB) = await _trackingService.GetImpressionsAsync(testId);
        var totalImpressions = impressionsA + impressionsB;

        // If no impressions yet, use the target percentage
        if (totalImpressions == 0)
        {
            return targetPercentageA;
        }

        // Calculate the target ratio (what we want)
        var targetRatio = targetPercentageA / 100.0;

        // Calculate the actual ratio (what we have)
        var actualRatio = impressionsA / (double)totalImpressions;

        // Calculate imbalance (positive = A is under-represented, negative = A is over-represented)
        var imbalance = targetRatio - actualRatio;

        // Adjust effective percentage to correct imbalance
        // Multiply imbalance by a correction factor (50) to accelerate balancing
        // This means if A is 10% under target, we increase its effective % by 5
        var effectivePercentageA = targetPercentageA + (int)(imbalance * 50);

        // Clamp to valid range (5-95 to always give some chance to both variants)
        return Math.Clamp(effectivePercentageA, 5, 95);
    }
}
