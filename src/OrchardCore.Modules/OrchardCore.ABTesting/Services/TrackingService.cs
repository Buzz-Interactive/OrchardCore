using System.Data.Common;
using Dapper;
using OrchardCore.ABTesting.Models;
using OrchardCore.Data;
using YesSql;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Service for tracking and retrieving A/B test impressions and conversions.
/// Uses Dapper for efficient database operations.
/// </summary>
public sealed class TrackingService : ITrackingService
{
    private readonly IDbConnectionAccessor _dbConnectionAccessor;
    private readonly ISession _session;

    public TrackingService(IDbConnectionAccessor dbConnectionAccessor, ISession session)
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

        var column = variant == ABVariant.A ? "VariantAImpressions" : "VariantBImpressions";
        await RecordMetricAsync(
            testId,
            nameof(ABTestImpressionRecord),
            column,
            "VariantAImpressions",
            "VariantBImpressions",
            variant);
    }

    /// <inheritdoc />
    public async Task<(long VariantA, long VariantB)> GetImpressionsAsync(string testId)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return (0, 0);
        }

        return await GetMetricsAsync<ABTestImpressionRecord>(
            testId,
            nameof(ABTestImpressionRecord),
            "VariantAImpressions",
            "VariantBImpressions");
    }

    /// <inheritdoc />
    public async Task RecordConversionAsync(string testId, ABVariant variant)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return;
        }

        var column = variant == ABVariant.A ? "VariantAConversions" : "VariantBConversions";
        await RecordMetricAsync(
            testId,
            nameof(ABTestGoalRecord),
            column,
            "VariantAConversions",
            "VariantBConversions",
            variant);
    }

    /// <inheritdoc />
    public async Task<(long VariantA, long VariantB)> GetConversionsAsync(string testId)
    {
        if (string.IsNullOrEmpty(testId))
        {
            return (0, 0);
        }

        return await GetMetricsAsync<ABTestGoalRecord>(
            testId,
            nameof(ABTestGoalRecord),
            "VariantAConversions",
            "VariantBConversions");
    }

    private async Task RecordMetricAsync(
        string testId,
        string recordName,
        string columnToIncrement,
        string variantAColumn,
        string variantBColumn,
        ABVariant variant)
    {
        var configuration = _session.Store.Configuration;
        var tableName = $"{configuration.TablePrefix}{recordName}";
        var dialect = configuration.SqlDialect;

        await using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel);

        try
        {
            // Try UPDATE first (atomic - no race condition)
            var updateSql = $"UPDATE {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                           $"SET {dialect.QuoteForColumnName(columnToIncrement)} = {dialect.QuoteForColumnName(columnToIncrement)} + 1 " +
                           $"WHERE {dialect.QuoteForColumnName("TestId")} = @TestId";

            var rowsAffected = await connection.ExecuteAsync(updateSql, new { TestId = testId }, transaction);

            if (rowsAffected == 0)
            {
                // Record doesn't exist - INSERT new record
                var variantACount = variant == ABVariant.A ? 1 : 0;
                var variantBCount = variant == ABVariant.B ? 1 : 0;

                var insertSql = $"INSERT INTO {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                               $"({dialect.QuoteForColumnName("TestId")}, " +
                               $"{dialect.QuoteForColumnName(variantAColumn)}, " +
                               $"{dialect.QuoteForColumnName(variantBColumn)}) " +
                               $"VALUES (@TestId, @VariantA, @VariantB)";

                try
                {
                    await connection.ExecuteAsync(insertSql, new
                    {
                        TestId = testId,
                        VariantA = variantACount,
                        VariantB = variantBCount,
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

    private async Task<(long VariantA, long VariantB)> GetMetricsAsync<TRecord>(
        string testId,
        string recordName,
        string variantAColumn,
        string variantBColumn)
        where TRecord : class
    {
        var configuration = _session.Store.Configuration;
        var tableName = $"{configuration.TablePrefix}{recordName}";
        var dialect = configuration.SqlDialect;

        await using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync();

        var selectSql = $"SELECT {dialect.QuoteForColumnName(variantAColumn)} AS VariantA, " +
                       $"{dialect.QuoteForColumnName(variantBColumn)} AS VariantB " +
                       $"FROM {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                       $"WHERE {dialect.QuoteForColumnName("TestId")} = @TestId";

        var result = await connection.QueryFirstOrDefaultAsync<MetricResult>(
            selectSql,
            new { TestId = testId });

        if (result == null)
        {
            return (0, 0);
        }

        return (result.VariantA, result.VariantB);
    }

    private sealed class MetricResult
    {
        public long VariantA { get; set; }
        public long VariantB { get; set; }
    }
}
