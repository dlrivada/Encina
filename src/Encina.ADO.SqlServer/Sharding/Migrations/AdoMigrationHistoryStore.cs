using System.Data;
using System.Data.Common;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;

namespace Encina.ADO.SqlServer.Sharding.Migrations;

/// <summary>
/// SQL Server ADO.NET implementation of <see cref="IMigrationHistoryStore"/>
/// that persists migration history in a <c>__EncinaMigrationHistory</c> table.
/// </summary>
internal sealed class AdoMigrationHistoryStore : IMigrationHistoryStore
{
    private const string HistoryTableName = "__EncinaMigrationHistory";

    private readonly IShardedConnectionFactory _connectionFactory;

    public AdoMigrationHistoryStore(IShardedConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<string>>> GetAppliedAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        try
        {
            var connectionResult = await _connectionFactory
                .GetConnectionAsync(shardId, cancellationToken)
                .ConfigureAwait(false);

            return await connectionResult
                .MapAsync(async connection =>
                {
                    await using var disposable = connection as IAsyncDisposable;

                    var sql = $"""
                        SELECT MigrationId
                        FROM [{HistoryTableName}]
                        WHERE RolledBackAtUtc IS NULL
                        ORDER BY AppliedAtUtc
                        """;

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;

                    var results = new List<string>();
                    using var reader = await ExecuteReaderAsync(cmd, cancellationToken).ConfigureAwait(false);

                    while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
                    {
                        results.Add(reader.GetString(0));
                    }

                    return (IReadOnlyList<string>)results;
                })
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.MigrationFailed,
                $"Failed to get applied migrations for shard '{shardId}': {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RecordAppliedAsync(
        string shardId,
        MigrationScript script,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentNullException.ThrowIfNull(script);

        try
        {
            var connectionResult = await _connectionFactory
                .GetConnectionAsync(shardId, cancellationToken)
                .ConfigureAwait(false);

            return await connectionResult
                .MapAsync(async connection =>
                {
                    await using var disposable = connection as IAsyncDisposable;

                    var sql = $"""
                        INSERT INTO [{HistoryTableName}]
                            (MigrationId, Description, Checksum, AppliedAtUtc, DurationMs)
                        VALUES
                            (@MigrationId, @Description, @Checksum, @AppliedAtUtc, @DurationMs)
                        """;

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    AddParameter(cmd, "@MigrationId", script.Id);
                    AddParameter(cmd, "@Description", script.Description);
                    AddParameter(cmd, "@Checksum", script.Checksum);
                    AddParameter(cmd, "@AppliedAtUtc", DateTime.UtcNow);
                    AddParameter(cmd, "@DurationMs", (long)duration.TotalMilliseconds);

                    await ExecuteNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
                    return Unit.Default;
                })
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.MigrationFailed,
                $"Failed to record applied migration '{script.Id}' for shard '{shardId}': {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RecordRolledBackAsync(
        string shardId,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationId);

        try
        {
            var connectionResult = await _connectionFactory
                .GetConnectionAsync(shardId, cancellationToken)
                .ConfigureAwait(false);

            return await connectionResult
                .MapAsync(async connection =>
                {
                    await using var disposable = connection as IAsyncDisposable;

                    var sql = $"""
                        UPDATE [{HistoryTableName}]
                        SET RolledBackAtUtc = @RolledBackAtUtc
                        WHERE MigrationId = @MigrationId
                          AND RolledBackAtUtc IS NULL
                        """;

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    AddParameter(cmd, "@MigrationId", migrationId);
                    AddParameter(cmd, "@RolledBackAtUtc", DateTime.UtcNow);

                    await ExecuteNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
                    return Unit.Default;
                })
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.RollbackFailed,
                $"Failed to record rollback of migration '{migrationId}' for shard '{shardId}': {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> EnsureHistoryTableExistsAsync(
        ShardInfo shardInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardInfo);

        try
        {
            var connectionResult = await _connectionFactory
                .GetConnectionAsync(shardInfo.ShardId, cancellationToken)
                .ConfigureAwait(false);

            return await connectionResult
                .MapAsync(async connection =>
                {
                    await using var disposable = connection as IAsyncDisposable;

                    var sql = $"""
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{HistoryTableName}')
                        BEGIN
                            CREATE TABLE [{HistoryTableName}] (
                                MigrationId NVARCHAR(256) NOT NULL,
                                Description NVARCHAR(1024) NULL,
                                Checksum NVARCHAR(128) NOT NULL,
                                AppliedAtUtc DATETIME2 NOT NULL,
                                DurationMs BIGINT NOT NULL,
                                RolledBackAtUtc DATETIME2 NULL,
                                CONSTRAINT PK_{HistoryTableName} PRIMARY KEY (MigrationId)
                            );
                        END
                        """;

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;

                    await ExecuteNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
                    return Unit.Default;
                })
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.MigrationFailed,
                $"Failed to create history table on shard '{shardInfo.ShardId}': {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> ApplyHistoricalMigrationsAsync(
        string shardId,
        IReadOnlyList<MigrationScript> scripts,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentNullException.ThrowIfNull(scripts);

        try
        {
            var connectionResult = await _connectionFactory
                .GetConnectionAsync(shardId, cancellationToken)
                .ConfigureAwait(false);

            return await connectionResult
                .MapAsync(async connection =>
                {
                    await using var disposable = connection as IAsyncDisposable;

                    foreach (var script in scripts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var sql = $"""
                            IF NOT EXISTS (SELECT 1 FROM [{HistoryTableName}] WHERE MigrationId = @MigrationId)
                            INSERT INTO [{HistoryTableName}]
                                (MigrationId, Description, Checksum, AppliedAtUtc, DurationMs)
                            VALUES
                                (@MigrationId, @Description, @Checksum, @AppliedAtUtc, 0)
                            """;

                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = sql;
                        AddParameter(cmd, "@MigrationId", script.Id);
                        AddParameter(cmd, "@Description", script.Description);
                        AddParameter(cmd, "@Checksum", script.Checksum);
                        AddParameter(cmd, "@AppliedAtUtc", DateTime.UtcNow);

                        await ExecuteNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
                    }

                    return Unit.Default;
                })
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.MigrationFailed,
                $"Failed to apply historical migrations for shard '{shardId}': {ex.Message}",
                ex);
        }
    }

    private static void AddParameter(IDbCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        cmd.Parameters.Add(param);
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd, CancellationToken cancellationToken)
    {
        if (cmd is DbCommand dbCmd)
        {
            return await dbCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }

        return cmd.ExecuteReader();
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is DbDataReader dbReader)
        {
            return await dbReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        return reader.Read();
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand cmd, CancellationToken cancellationToken)
    {
        if (cmd is DbCommand dbCmd)
        {
            return await dbCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return cmd.ExecuteNonQuery();
    }
}
