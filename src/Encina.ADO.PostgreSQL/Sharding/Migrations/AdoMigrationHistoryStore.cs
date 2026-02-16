using System.Data;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;

namespace Encina.ADO.PostgreSQL.Sharding.Migrations;

/// <summary>
/// PostgreSQL ADO.NET implementation of <see cref="IMigrationHistoryStore"/>
/// that persists migration history in a <c>__encina_migration_history</c> table.
/// </summary>
internal sealed class AdoMigrationHistoryStore : IMigrationHistoryStore
{
    // PostgreSQL convention: lowercase with underscores
    private const string HistoryTableName = "__encina_migration_history";

    private readonly IShardedConnectionFactory _connectionFactory;

    public AdoMigrationHistoryStore(IShardedConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<string>>> GetAppliedAsync(
        string shardId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        try
        {
            var connectionResult = await _connectionFactory.GetConnectionAsync(shardId, cancellationToken).ConfigureAwait(false);
            return await connectionResult.MapAsync(async connection =>
            {
                await using var disposable = connection as IAsyncDisposable;
                var sql = $"SELECT migration_id FROM {HistoryTableName} WHERE rolled_back_at_utc IS NULL ORDER BY applied_at_utc";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                var results = new List<string>();
                using var reader = await AdoHelper.ExecuteReaderAsync(cmd, cancellationToken).ConfigureAwait(false);
                while (await AdoHelper.ReadAsync(reader, cancellationToken).ConfigureAwait(false))
                {
                    results.Add(reader.GetString(0));
                }

                return (IReadOnlyList<string>)results;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.MigrationFailed,
                $"Failed to get applied migrations for shard '{shardId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RecordAppliedAsync(
        string shardId, MigrationScript script, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentNullException.ThrowIfNull(script);

        try
        {
            var connectionResult = await _connectionFactory.GetConnectionAsync(shardId, cancellationToken).ConfigureAwait(false);
            return await connectionResult.MapAsync(async connection =>
            {
                await using var disposable = connection as IAsyncDisposable;
                var sql = $"""
                    INSERT INTO {HistoryTableName}
                        (migration_id, description, checksum, applied_at_utc, duration_ms)
                    VALUES
                        (@MigrationId, @Description, @Checksum, @AppliedAtUtc, @DurationMs)
                    """;
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                AddParam(cmd, "@MigrationId", script.Id);
                AddParam(cmd, "@Description", script.Description);
                AddParam(cmd, "@Checksum", script.Checksum);
                AddParam(cmd, "@AppliedAtUtc", DateTime.UtcNow);
                AddParam(cmd, "@DurationMs", (long)duration.TotalMilliseconds);
                await AdoHelper.ExecuteNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
                return Unit.Default;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.MigrationFailed,
                $"Failed to record migration '{script.Id}' for shard '{shardId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RecordRolledBackAsync(
        string shardId, string migrationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationId);

        try
        {
            var connectionResult = await _connectionFactory.GetConnectionAsync(shardId, cancellationToken).ConfigureAwait(false);
            return await connectionResult.MapAsync(async connection =>
            {
                await using var disposable = connection as IAsyncDisposable;
                var sql = $"""
                    UPDATE {HistoryTableName}
                    SET rolled_back_at_utc = @RolledBackAtUtc
                    WHERE migration_id = @MigrationId AND rolled_back_at_utc IS NULL
                    """;
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                AddParam(cmd, "@MigrationId", migrationId);
                AddParam(cmd, "@RolledBackAtUtc", DateTime.UtcNow);
                await AdoHelper.ExecuteNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
                return Unit.Default;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.RollbackFailed,
                $"Failed to record rollback '{migrationId}' for shard '{shardId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> EnsureHistoryTableExistsAsync(
        ShardInfo shardInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardInfo);

        try
        {
            var connectionResult = await _connectionFactory.GetConnectionAsync(shardInfo.ShardId, cancellationToken).ConfigureAwait(false);
            return await connectionResult.MapAsync(async connection =>
            {
                await using var disposable = connection as IAsyncDisposable;
                var sql = $"""
                    CREATE TABLE IF NOT EXISTS {HistoryTableName} (
                        migration_id VARCHAR(256) NOT NULL PRIMARY KEY,
                        description VARCHAR(1024),
                        checksum VARCHAR(128) NOT NULL,
                        applied_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
                        duration_ms BIGINT NOT NULL,
                        rolled_back_at_utc TIMESTAMP WITH TIME ZONE
                    )
                    """;
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                await AdoHelper.ExecuteNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
                return Unit.Default;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.MigrationFailed,
                $"Failed to create history table on shard '{shardInfo.ShardId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> ApplyHistoricalMigrationsAsync(
        string shardId, IReadOnlyList<MigrationScript> scripts, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentNullException.ThrowIfNull(scripts);

        try
        {
            var connectionResult = await _connectionFactory.GetConnectionAsync(shardId, cancellationToken).ConfigureAwait(false);
            return await connectionResult.MapAsync(async connection =>
            {
                await using var disposable = connection as IAsyncDisposable;
                foreach (var script in scripts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var sql = $"""
                        INSERT INTO {HistoryTableName}
                            (migration_id, description, checksum, applied_at_utc, duration_ms)
                        VALUES
                            (@MigrationId, @Description, @Checksum, @AppliedAtUtc, 0)
                        ON CONFLICT (migration_id) DO NOTHING
                        """;
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    AddParam(cmd, "@MigrationId", script.Id);
                    AddParam(cmd, "@Description", script.Description);
                    AddParam(cmd, "@Checksum", script.Checksum);
                    AddParam(cmd, "@AppliedAtUtc", DateTime.UtcNow);
                    await AdoHelper.ExecuteNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
                }

                return Unit.Default;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.MigrationFailed,
                $"Failed to apply historical migrations for shard '{shardId}': {ex.Message}", ex);
        }
    }

    private static void AddParam(IDbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
