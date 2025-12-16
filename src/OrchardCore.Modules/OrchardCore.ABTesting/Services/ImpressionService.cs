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
    public async Task RecordImpressionAsync(string testContentItemId, ABVariant variant)
    {
        if (string.IsNullOrEmpty(testContentItemId))
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
            // Check if record exists
            var selectSql = $"SELECT {dialect.QuoteForColumnName("Id")}, " +
                           $"{dialect.QuoteForColumnName("VariantAImpressions")}, " +
                           $"{dialect.QuoteForColumnName("VariantBImpressions")} " +
                           $"FROM {dialect.QuoteForTableName(tableName, configuration.Schema)} " +
                           $"WHERE {dialect.QuoteForColumnName("TestContentItemId")} = @TestContentItemId";

            var existing = await connection.QueryFirstOrDefaultAsync<ABTestImpressionRecord>(
                selectSql,
                new { TestContentItemId = testContentItemId },
                transaction);

            if (existing != null)
            {
                // Update existing record
                var column = variant == ABVariant.A ? "VariantAImpressions" : "VariantBImpressions";
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
                               $"({dialect.QuoteForColumnName("TestContentItemId")}, " +
                               $"{dialect.QuoteForColumnName("VariantAImpressions")}, " +
                               $"{dialect.QuoteForColumnName("VariantBImpressions")}) " +
                               $"VALUES (@TestContentItemId, @VariantAImpressions, @VariantBImpressions)";

                await connection.ExecuteAsync(insertSql, new
                {
                    TestContentItemId = testContentItemId,
                    VariantAImpressions = variantACount,
                    VariantBImpressions = variantBCount,
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
    public async Task<(long VariantA, long VariantB)> GetImpressionsAsync(string testContentItemId)
    {
        if (string.IsNullOrEmpty(testContentItemId))
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
                       $"WHERE {dialect.QuoteForColumnName("TestContentItemId")} = @TestContentItemId";

        var record = await connection.QueryFirstOrDefaultAsync<ABTestImpressionRecord>(
            selectSql,
            new { TestContentItemId = testContentItemId });

        if (record == null)
        {
            return (0, 0);
        }

        return (record.VariantAImpressions, record.VariantBImpressions);
    }
}
