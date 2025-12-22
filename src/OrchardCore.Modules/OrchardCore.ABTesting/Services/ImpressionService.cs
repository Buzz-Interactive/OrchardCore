using System.Data.Common;
using Dapper;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Records;
using OrchardCore.Data;
using YesSql;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for tracking and retrieving A/B test impressions.
/// Uses Dapper for efficient database operations.
/// </summary>
public class ImpressionService : IImpressionService
{
    private readonly IDbConnectionAccessor _dbConnectionAccessor;
    private readonly ISession _session;

    public ImpressionService(IDbConnectionAccessor dbConnectionAccessor, ISession session)
    {
        _dbConnectionAccessor = dbConnectionAccessor;
        _session = session;
    }

    /// <inheritdoc />
    public async Task RecordImpressionAsync(string testId, ABVariant variant)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return;
        }

        var configuration = _session.Store.Configuration;
        var tableName = $"{configuration.TablePrefix}{nameof(ABTestImpressionRecord)}";
        var dialect = configuration.SqlDialect;

        await using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel);

        try
        {
            // Try UPDATE first (atomic - no race condition)
            var column = variant == ABVariant.A ? "VariantAImpressions" : "VariantBImpressions";
            var updateSql = $"UPDATE {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                           $"SET {dialect.QuoteForColumnName(column)} = {dialect.QuoteForColumnName(column)} + 1 " +
                           $"WHERE {dialect.QuoteForColumnName("TestId")} = @TestId";

            var rowsAffected = await connection.ExecuteAsync(updateSql, new { TestId = testId }, transaction);

            if (rowsAffected == 0)
            {
                // Record doesn't exist - INSERT new record
                var variantACount = variant == ABVariant.A ? 1 : 0;
                var variantBCount = variant == ABVariant.B ? 1 : 0;

                var insertSql = $"INSERT INTO {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                               $"({dialect.QuoteForColumnName("TestId")}, " +
                               $"{dialect.QuoteForColumnName("VariantAImpressions")}, " +
                               $"{dialect.QuoteForColumnName("VariantBImpressions")}) " +
                               $"VALUES (@TestId, @VariantAImpressions, @VariantBImpressions)";

                try
                {
                    await connection.ExecuteAsync(insertSql, new
                    {
                        TestId = testId,
                        VariantAImpressions = variantACount,
                        VariantBImpressions = variantBCount,
                    }, transaction);
                }
                catch (DbException)
                {
                    // Another request inserted first - retry UPDATE
                    await connection.ExecuteAsync(updateSql, new { TestId = testId }, transaction);
                }
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
    public async Task<(long VariantA, long VariantB)> GetImpressionsAsync(string testId)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return (0, 0);
        }

        var configuration = _session.Store.Configuration;
        var tableName = $"{configuration.TablePrefix}{nameof(ABTestImpressionRecord)}";
        var dialect = configuration.SqlDialect;

        await using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync();

        var selectSql = $"SELECT {dialect.QuoteForColumnName("VariantAImpressions")}, " +
                       $"{dialect.QuoteForColumnName("VariantBImpressions")} " +
                       $"FROM {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                       $"WHERE {dialect.QuoteForColumnName("TestId")} = @TestId";

        var record = await connection.QueryFirstOrDefaultAsync<ABTestImpressionRecord>(
            selectSql,
            new { TestId = testId });

        if (record == null)
        {
            return (0, 0);
        }

        return (record.VariantAImpressions, record.VariantBImpressions);
    }
}
