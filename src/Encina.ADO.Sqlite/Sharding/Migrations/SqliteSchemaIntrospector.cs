using System.Data;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;

namespace Encina.ADO.Sqlite.Sharding.Migrations;

/// <summary>
/// SQLite implementation of <see cref="ISchemaIntrospector"/> that reads schema
/// metadata from <c>sqlite_master</c> and <c>PRAGMA table_info</c>.
/// </summary>
internal sealed class SqliteSchemaIntrospector : ISchemaIntrospector
{
    private readonly IShardedConnectionFactory _connectionFactory;

    public SqliteSchemaIntrospector(IShardedConnectionFactory connectionFactory)
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

        // Query user tables from sqlite_master
        const string tablesSql = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
            ORDER BY name
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
        // Use PRAGMA table_info for column details
        // Note: table name cannot be parameterized in PRAGMA, but we retrieved it from sqlite_master
        var columnsSql = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"", StringComparison.Ordinal)}\")";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = columnsSql;

        var columns = new List<ColumnSchema>();

        using var reader = await AdoHelper.ExecuteReaderAsync(cmd, cancellationToken).ConfigureAwait(false);

        // PRAGMA table_info columns: cid, name, type, notnull, dflt_value, pk
        while (await AdoHelper.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            var name = reader.GetString(1);
            var dataType = reader.IsDBNull(2) ? "TEXT" : reader.GetString(2);
            var isNotNull = reader.GetInt32(3) != 0;
            var defaultValue = reader.IsDBNull(4) ? null : reader.GetString(4);

            columns.Add(new ColumnSchema(
                Name: name,
                DataType: dataType,
                IsNullable: !isNotNull,
                DefaultValue: defaultValue));
        }

        return columns;
    }
}
