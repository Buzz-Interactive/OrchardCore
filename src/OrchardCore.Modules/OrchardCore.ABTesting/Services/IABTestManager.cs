using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for managing ABTest entities (CRUD operations).
/// </summary>
public interface IABTestManager
{
    /// <summary>
    /// Gets an ABTest by its unique identifier.
    /// </summary>
    /// <param name="testId">The test ID.</param>
    /// <returns>The ABTest entity, or null if not found.</returns>
    Task<ABTest> GetAsync(string testId);

    /// <summary>
    /// Gets an ABTest that contains the specified content item as a variant.
    /// </summary>
    /// <param name="contentItemId">The content item ID of either variant.</param>
    /// <returns>The ABTest entity, or null if not found.</returns>
    Task<ABTest> GetByVariantAsync(string contentItemId);

    /// <summary>
    /// Gets all ABTest entities.
    /// </summary>
    /// <returns>All ABTest entities.</returns>
    Task<IEnumerable<ABTest>> GetAllAsync();

    /// <summary>
    /// Gets all active (running) ABTest entities.
    /// </summary>
    /// <returns>All active ABTest entities.</returns>
    Task<IEnumerable<ABTest>> GetActiveAsync();

    /// <summary>
    /// Creates a new ABTest entity.
    /// </summary>
    /// <param name="test">The test to create.</param>
    /// <returns>The created ABTest entity with generated TestId.</returns>
    Task<ABTest> CreateAsync(ABTest test);

    /// <summary>
    /// Updates an existing ABTest entity.
    /// </summary>
    /// <param name="test">The test to update.</param>
    /// <returns>The updated ABTest entity.</returns>
    Task<ABTest> UpdateAsync(ABTest test);

    /// <summary>
    /// Deletes an ABTest entity.
    /// </summary>
    /// <param name="testId">The test ID to delete.</param>
    Task DeleteAsync(string testId);

    /// <summary>
    /// Activates an ABTest (starts the test).
    /// </summary>
    /// <param name="testId">The test ID to activate.</param>
    /// <returns>The activated ABTest entity.</returns>
    Task<ABTest> ActivateAsync(string testId);

    /// <summary>
    /// Deactivates an ABTest (stops the test).
    /// </summary>
    /// <param name="testId">The test ID to deactivate.</param>
    /// <returns>The deactivated ABTest entity.</returns>
    Task<ABTest> DeactivateAsync(string testId);
}
