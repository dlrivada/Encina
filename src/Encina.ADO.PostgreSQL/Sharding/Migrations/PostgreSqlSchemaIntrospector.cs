using System.Data;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;

namespace Encina.ADO.PostgreSQL.Sharding.Migrations;

/// <summary>
/// PostgreSQL implementation of <see cref="ISchemaIntrospector"/> that reads schema
/// metadata from <c>information_schema</c> and <c>pg_catalog</c>.
/// </summary>
internal sealed class PostgreSqlSchemaIntrospector : ISchemaIntrospector
{
    private readonly IShardedConnectionFactory _connectionFactory;

    public PostgreSqlSchemaIntrospector(IShardedConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
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
            var shardSchemaResult = await IntrospectAsync(shard, includeColumnDiffs, cancellationToken)
                .ConfigureAwait(false);
            var baselineSchemaResult = await IntrospectAsync(baselineShard, includeColumnDiffs, cancellationToken)
                .ConfigureAwait(false);

            return from shardSchema in shardSchemaResult
                   from baselineSchema in baselineSchemaResult
                   select SchemaComparer.Compare(shard.ShardId, baselineShard.ShardId, shardSchema, baselineSchema, includeColumnDiffs);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.SchemaComparisonFailed,
                $"Schema comparison failed between shard '{shard.ShardId}' and baseline '{baselineShard.ShardId}': {ex.Message}",
                ex);
        }
    }

    internal async Task<Either<EncinaError, ShardSchema>> IntrospectAsync(
        ShardInfo shard,
        bool includeColumns,
        CancellationToken cancellationToken)
    {
        var connectionResult = await _connectionFactory
            .GetConnectionAsync(shard.ShardId, cancellationToken)
            .ConfigureAwait(false);

        return await connectionResult
            .MapAsync(async connection =>
            {
                await using var disposable = connection as IAsyncDisposable;
                return await ReadSchemaAsync(shard.ShardId, connection, includeColumns, cancellationToken)
                    .ConfigureAwait(false);
            })
            .ConfigureAwait(false);
    }

    private static async Task<ShardSchema> ReadSchemaAsync(
        string shardId,
        IDbConnection connection,
        bool includeColumns,
        CancellationToken cancellationToken)
    {
        var tables = new List<TableSchema>();

        // Query user tables from information_schema (public schema only)
        const string tablesSql = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
            ORDER BY table_name
            """;

        using var tablesCmd = connection.CreateCommand();
        tablesCmd.CommandText = tablesSql;

        using var reader = await AdoHelper.ExecuteReaderAsync(tablesCmd, cancellationToken).ConfigureAwait(false);
        var tableNames = new List<string>();

        while (await AdoHelper.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            tableNames.Add(reader.GetString(0));
        }

        await AdoHelper.CloseReaderAsync(reader).ConfigureAwait(false);

        foreach (var tableName in tableNames)
        {
            var columns = includeColumns
                ? await ReadColumnsAsync(connection, tableName, cancellationToken).ConfigureAwait(false)
                : [];

            tables.Add(new TableSchema(tableName, columns));
        }

        return new ShardSchema(shardId, tables, DateTimeOffset.UtcNow);
    }

    private static async Task<IReadOnlyList<ColumnSchema>> ReadColumnsAsync(
        IDbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string columnsSql = """
            SELECT column_name, data_type, is_nullable, column_default
            FROM information_schema.columns
            WHERE table_name = @TableName
              AND table_schema = 'public'
            ORDER BY ordinal_position
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = columnsSql;

        var param = cmd.CreateParameter();
        param.ParameterName = "@TableName";
        param.Value = tableName;
        cmd.Parameters.Add(param);

        var columns = new List<ColumnSchema>();

        using var reader = await AdoHelper.ExecuteReaderAsync(cmd, cancellationToken).ConfigureAwait(false);

        while (await AdoHelper.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            columns.Add(new ColumnSchema(
                Name: reader.GetString(0),
                DataType: reader.GetString(1),
                IsNullable: string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                DefaultValue: reader.IsDBNull(3) ? null : reader.GetString(3)));
        }

        return columns;
    }
}
