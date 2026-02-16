using System.Data;
using System.Data.Common;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;

namespace Encina.ADO.SqlServer.Sharding.Migrations;

/// <summary>
/// SQL Server implementation of <see cref="ISchemaIntrospector"/> that reads schema
/// metadata from <c>INFORMATION_SCHEMA</c> and <c>sys.objects</c>.
/// </summary>
internal sealed class SqlServerSchemaIntrospector : ISchemaIntrospector
{
    private readonly IShardedConnectionFactory _connectionFactory;

    public SqlServerSchemaIntrospector(IShardedConnectionFactory connectionFactory)
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

        const string tablesSql = """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_SCHEMA = 'dbo'
            ORDER BY TABLE_NAME
            """;

        using var tablesCmd = connection.CreateCommand();
        tablesCmd.CommandText = tablesSql;

        using var reader = await ExecuteReaderAsync(tablesCmd, cancellationToken).ConfigureAwait(false);
        var tableNames = new List<string>();

        while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            tableNames.Add(reader.GetString(0));
        }

        await CloseReaderAsync(reader).ConfigureAwait(false);

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
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName
              AND TABLE_SCHEMA = 'dbo'
            ORDER BY ORDINAL_POSITION
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = columnsSql;

        var param = cmd.CreateParameter();
        param.ParameterName = "@TableName";
        param.Value = tableName;
        cmd.Parameters.Add(param);

        var columns = new List<ColumnSchema>();

        using var reader = await ExecuteReaderAsync(cmd, cancellationToken).ConfigureAwait(false);

        while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            columns.Add(new ColumnSchema(
                Name: reader.GetString(0),
                DataType: reader.GetString(1),
                IsNullable: string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                DefaultValue: reader.IsDBNull(3) ? null : reader.GetString(3)));
        }

        return columns;
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCmd)
        {
            return await dbCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }

        return command.ExecuteReader();
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is DbDataReader dbReader)
        {
            return await dbReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        return reader.Read();
    }

    private static async Task CloseReaderAsync(IDataReader reader)
    {
        if (reader is DbDataReader dbReader)
        {
            await dbReader.CloseAsync().ConfigureAwait(false);
            return;
        }

        reader.Close();
    }
}
