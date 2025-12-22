using OrchardCore.ABTesting.Indexes;
using OrchardCore.ABTesting.Models;
using OrchardCore.ContentManagement;
using YesSql;
using IIdGenerator = OrchardCore.Entities.IIdGenerator;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Manager service for ABTest entity CRUD operations.
/// </summary>
public class ABTestManager : IABTestManager
{
    private readonly ISession _session;
    private readonly IIdGenerator _idGenerator;
    private readonly IContentManager _contentManager;

    public ABTestManager(
        ISession session,
        IIdGenerator idGenerator,
        IContentManager contentManager)
    {
        _session = session;
        _idGenerator = idGenerator;
        _contentManager = contentManager;
    }

    /// <inheritdoc />
    public async Task<ABTest> GetAsync(string testId)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return null;
        }

        return await _session.Query<ABTest, ABTestIndex>(collection: ABTest.Collection)
            .Where(i => i.TestId == testId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<ABTest> GetByVariantAsync(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        // Find active test that contains this content item as either variant
        return await _session.Query<ABTest, ABTestIndex>(collection: ABTest.Collection)
            .Where(i => i.IsActive &&
                (i.VariantAContentItemId == contentItemId || i.VariantBContentItemId == contentItemId))
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ABTest>> GetAllAsync()
    {
        return await _session.Query<ABTest, ABTestIndex>(collection: ABTest.Collection)
            .OrderByDescending(i => i.CreatedUtc)
            .ListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ABTest>> GetActiveAsync()
    {
        return await _session.Query<ABTest, ABTestIndex>(collection: ABTest.Collection)
            .Where(i => i.IsActive)
            .ListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ABTest>> GetActiveTestsWithConflictingVariantsAsync(
        string variantAContentItemId,
        string variantBContentItemId,
        string excludeTestId = null)
    {
        var activeTests = await GetActiveAsync();

        return activeTests.Where(t =>
            (excludeTestId == null || t.TestId != excludeTestId) &&
            (t.VariantAContentItemId == variantAContentItemId ||
             t.VariantBContentItemId == variantAContentItemId ||
             t.VariantAContentItemId == variantBContentItemId ||
             t.VariantBContentItemId == variantBContentItemId));
    }

    /// <inheritdoc />
    public async Task<ABTest> CreateAsync(ABTest test)
    {
        ArgumentNullException.ThrowIfNull(test);

        // Generate a unique TestId
        test.TestId = _idGenerator.GenerateUniqueId();
        test.CreatedUtc = DateTime.UtcNow;
        test.IsActive = false;

        // Validate variants are different
        if (!string.IsNullOrEmpty(test.VariantAContentItemId) &&
            test.VariantAContentItemId == test.VariantBContentItemId)
        {
            throw new InvalidOperationException("Variant A and Variant B must be different content items.");
        }

        // Capture variant display names
        test.VariantADisplayName = await GetContentDisplayNameAsync(test.VariantAContentItemId);
        test.VariantBDisplayName = await GetContentDisplayNameAsync(test.VariantBContentItemId);

        await _session.SaveAsync(test, collection: ABTest.Collection);

        return test;
    }

    /// <inheritdoc />
    public async Task<ABTest> UpdateAsync(ABTest test)
    {
        ArgumentNullException.ThrowIfNull(test);

        if (string.IsNullOrEmpty(test.TestId))
        {
            throw new InvalidOperationException("Cannot update a test without a TestId.");
        }

        // Validate variants are different
        if (!string.IsNullOrEmpty(test.VariantAContentItemId) &&
            test.VariantAContentItemId == test.VariantBContentItemId)
        {
            throw new InvalidOperationException("Variant A and Variant B must be different content items.");
        }

        // Update variant display names only if variants are available
        if (test.VariantAState == VariantState.Available)
        {
            test.VariantADisplayName = await GetContentDisplayNameAsync(test.VariantAContentItemId);
        }

        if (test.VariantBState == VariantState.Available)
        {
            test.VariantBDisplayName = await GetContentDisplayNameAsync(test.VariantBContentItemId);
        }

        test.ModifiedUtc = DateTime.UtcNow;

        await _session.SaveAsync(test, collection: ABTest.Collection);

        return test;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string testId)
    {
        var test = await GetAsync(testId);
        if (test != null)
        {
            _session.Delete(test, collection: ABTest.Collection);
        }
    }

    /// <inheritdoc />
    public async Task<ABTest> ActivateAsync(string testId)
    {
        var test = await GetAsync(testId);
        if (test == null)
        {
            throw new InvalidOperationException($"ABTest with ID '{testId}' not found.");
        }

        // Validate variants are set before activating
        if (string.IsNullOrEmpty(test.VariantAContentItemId) ||
            string.IsNullOrEmpty(test.VariantBContentItemId))
        {
            throw new InvalidOperationException("Both variants must be selected before activating a test.");
        }

        // Check if any variant has been deleted
        if (test.VariantAState == VariantState.Deleted)
        {
            throw new InvalidOperationException(
                $"Cannot activate test: Variant A ({test.VariantADisplayName ?? test.VariantAContentItemId}) has been deleted.");
        }

        if (test.VariantBState == VariantState.Deleted)
        {
            throw new InvalidOperationException(
                $"Cannot activate test: Variant B ({test.VariantBDisplayName ?? test.VariantBContentItemId}) has been deleted.");
        }

        // Check if any variant is unpublished
        if (test.VariantAState == VariantState.Unpublished)
        {
            throw new InvalidOperationException(
                $"Cannot activate test: Variant A ({test.VariantADisplayName ?? test.VariantAContentItemId}) is unpublished.");
        }

        if (test.VariantBState == VariantState.Unpublished)
        {
            throw new InvalidOperationException(
                $"Cannot activate test: Variant B ({test.VariantBDisplayName ?? test.VariantBContentItemId}) is unpublished.");
        }

        // Validate variants are not used in other active tests
        var conflictingTests = await GetActiveTestsWithConflictingVariantsAsync(
            test.VariantAContentItemId,
            test.VariantBContentItemId,
            test.TestId);

        if (conflictingTests.Any())
        {
            var conflictingNames = string.Join(", ", conflictingTests.Select(t => t.Name ?? t.TestId));
            throw new InvalidOperationException(
                $"Cannot activate test: one or both variants are already used in active test(s): {conflictingNames}");
        }

        test.IsActive = true;
        test.ModifiedUtc = DateTime.UtcNow;

        await _session.SaveAsync(test, collection: ABTest.Collection);

        return test;
    }

    /// <inheritdoc />
    public async Task<ABTest> DeactivateAsync(string testId)
    {
        var test = await GetAsync(testId);
        if (test == null)
        {
            throw new InvalidOperationException($"ABTest with ID '{testId}' not found.");
        }

        test.IsActive = false;
        test.ModifiedUtc = DateTime.UtcNow;

        await _session.SaveAsync(test, collection: ABTest.Collection);

        return test;
    }

    private async Task<string> GetContentDisplayNameAsync(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return null;
        }

        var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
        return contentItem?.DisplayText ?? contentItemId;
    }
}
