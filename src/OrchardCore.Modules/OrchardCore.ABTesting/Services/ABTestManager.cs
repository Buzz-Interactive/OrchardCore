using OrchardCore.ABTesting.Indexes;
using OrchardCore.ABTesting.Models;
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

    public ABTestManager(
        ISession session,
        IIdGenerator idGenerator)
    {
        _session = session;
        _idGenerator = idGenerator;
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
}
