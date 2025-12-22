using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for declaring winners in A/B tests.
/// </summary>
public interface IABTestWinnerService
{
    /// <summary>
    /// Declares a winner for an A/B test. This will:
    /// - Mark the test as concluded with the winning variant
    /// - Deactivate the test
    /// - If B wins: Transfer A's route to B and set B as homepage if A was
    /// - Unpublish the losing variant and append "[TEST LOSER]" to its title
    /// </summary>
    /// <param name="testId">The test ID.</param>
    /// <param name="winner">The winning variant (A or B).</param>
    /// <returns>True if winner was declared successfully, false otherwise.</returns>
    Task<bool> DeclareWinnerAsync(string testId, ABVariant winner);
}
