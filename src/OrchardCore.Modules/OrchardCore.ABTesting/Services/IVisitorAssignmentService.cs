using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for assigning visitors to A/B test variants.
/// Handles GDPR-compliant cookie storage with session fallback.
/// </summary>
public interface IVisitorAssignmentService
{
    /// <summary>
    /// Gets the assigned variant for a visitor, or assigns one if not already assigned.
    /// </summary>
    /// <param name="testId">The unique identifier of the ABTest.</param>
    /// <param name="percentageA">The percentage of traffic for Variant A (0-100).</param>
    /// <returns>The assigned variant (A or B).</returns>
    Task<ABVariant> GetOrAssignVariantAsync(string testId, int percentageA);

    /// <summary>
    /// Tries to get an existing assignment for a visitor without creating a new one.
    /// </summary>
    /// <param name="testId">The unique identifier of the ABTest.</param>
    /// <returns>The assigned variant if one exists, null otherwise.</returns>
    Task<ABVariant?> TryGetAssignmentAsync(string testId);

    /// <summary>
    /// Clears all A/B test assignments for the current visitor.
    /// </summary>
    Task ClearAssignmentsAsync();
}
