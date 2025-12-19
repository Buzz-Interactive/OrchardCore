using Dapper;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Records;
using OrchardCore.Data;
using YesSql;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for tracking and retrieving A/B test goal conversions.
/// Uses Dapper for efficient database operations.
/// </summary>
public class GoalService : IGoalService
{
    private readonly IDbConnectionAccessor _dbConnectionAccessor;
    private readonly ISession _session;

    public GoalService(IDbConnectionAccessor dbConnectionAccessor, ISession session)
    {
        _dbConnectionAccessor = dbConnectionAccessor;
        _session = session;
    }

    /// <inheritdoc />
    public async Task RecordConversionAsync(string testId, ABVariant variant)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return;
        }

        var configuration = _session.Store.Configuration;
        var tableName = $"{configuration.TablePrefix}{nameof(ABTestGoalRecord)}";
        var dialect = configuration.SqlDialect;

        await using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel);

        try
        {
            // Check if record exists
            var selectSql = $"SELECT {dialect.QuoteForColumnName("Id")}, " +
                           $"{dialect.QuoteForColumnName("VariantAConversions")}, " +
                           $"{dialect.QuoteForColumnName("VariantBConversions")} " +
                           $"FROM {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                           $"WHERE {dialect.QuoteForColumnName("TestId")} = @TestId";

            var existing = await connection.QueryFirstOrDefaultAsync<ABTestGoalRecord>(
                selectSql,
                new { TestId = testId },
                transaction);

            if (existing != null)
            {
                // Update existing record
                var column = variant == ABVariant.A ? "VariantAConversions" : "VariantBConversions";
                var updateSql = $"UPDATE {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                               $"SET {dialect.QuoteForColumnName(column)} = {dialect.QuoteForColumnName(column)} + 1 " +
                               $"WHERE {dialect.QuoteForColumnName("Id")} = @Id";

                await connection.ExecuteAsync(updateSql, new { existing.Id }, transaction);
            }
            else
            {
                // Insert new record
                var variantACount = variant == ABVariant.A ? 1 : 0;
                var variantBCount = variant == ABVariant.B ? 1 : 0;

                var insertSql = $"INSERT INTO {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                               $"({dialect.QuoteForColumnName("TestId")}, " +
                               $"{dialect.QuoteForColumnName("VariantAConversions")}, " +
                               $"{dialect.QuoteForColumnName("VariantBConversions")}) " +
                               $"VALUES (@TestId, @VariantAConversions, @VariantBConversions)";

                await connection.ExecuteAsync(insertSql, new
                {
                    TestId = testId,
                    VariantAConversions = variantACount,
                    VariantBConversions = variantBCount,
                }, transaction);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(long VariantA, long VariantB)> GetConversionsAsync(string testId)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return (0, 0);
        }

        var configuration = _session.Store.Configuration;
        var tableName = $"{configuration.TablePrefix}{nameof(ABTestGoalRecord)}";
        var dialect = configuration.SqlDialect;

        await using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync();

        var selectSql = $"SELECT {dialect.QuoteForColumnName("VariantAConversions")}, " +
                       $"{dialect.QuoteForColumnName("VariantBConversions")} " +
                       $"FROM {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                       $"WHERE {dialect.QuoteForColumnName("TestId")} = @TestId";

        var record = await connection.QueryFirstOrDefaultAsync<ABTestGoalRecord>(
            selectSql,
            new { TestId = testId });

        if (record == null)
        {
            return (0, 0);
        }

        return (record.VariantAConversions, record.VariantBConversions);
    }
}
