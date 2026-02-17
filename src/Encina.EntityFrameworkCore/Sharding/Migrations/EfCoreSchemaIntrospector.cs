using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Sharding.Migrations;

/// <summary>
/// EF Core implementation of <see cref="ISchemaIntrospector"/> that reads schema
/// metadata from the EF Core relational model metadata.
/// </summary>
/// <typeparam name="TContext">The DbContext type used for sharded databases.</typeparam>
internal sealed class EfCoreSchemaIntrospector<TContext> : ISchemaIntrospector
    where TContext : DbContext
{
    private readonly IShardedDbContextFactory<TContext> _contextFactory;

    public EfCoreSchemaIntrospector(IShardedDbContextFactory<TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ShardSchemaDiff>> CompareAsync(
        ShardInfo shard,
        ShardInfo baselineShard,
        bool includeColumnDiffs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shard);
        ArgumentNullException.ThrowIfNull(baselineShard);

        try
        {
            var shardSchemaResult = IntrospectFromModel(shard);
            var baselineSchemaResult = IntrospectFromModel(baselineShard);

            return await Task.FromResult(
                from shardSchema in shardSchemaResult
                from baselineSchema in baselineSchemaResult
                select SchemaComparer.Compare(
                    shard.ShardId,
                    baselineShard.ShardId,
                    shardSchema,
                    baselineSchema,
                    includeColumnDiffs))
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.SchemaComparisonFailed,
                $"Schema comparison failed between shard '{shard.ShardId}' and baseline '{baselineShard.ShardId}': {ex.Message}",
                ex);
        }
    }

    private Either<EncinaError, ShardSchema> IntrospectFromModel(ShardInfo shard)
    {
        var contextResult = _contextFactory.CreateContextForShard(shard.ShardId);

        return contextResult.Map(context =>
        {
            using (context)
            {
                var model = context.Model;
                var tables = new List<TableSchema>();

                foreach (var entityType in model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();
                    if (string.IsNullOrEmpty(tableName))
                    {
                        continue;
                    }

                    var columns = new List<ColumnSchema>();

                    foreach (var property in entityType.GetProperties())
                    {
                        var columnName = property.GetColumnName();
                        if (string.IsNullOrEmpty(columnName))
                        {
                            continue;
                        }

                        columns.Add(new ColumnSchema(
                            Name: columnName,
                            DataType: property.GetColumnType() ?? "unknown",
                            IsNullable: property.IsNullable,
                            DefaultValue: property.GetDefaultValueSql()));
                    }

                    tables.Add(new TableSchema(tableName, columns));
                }

                return new ShardSchema(shard.ShardId, tables, DateTimeOffset.UtcNow);
            }
        });
    }
}
