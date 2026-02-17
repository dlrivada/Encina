using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Sharding.Migrations;

/// <summary>
/// EF Core implementation of <see cref="IMigrationHistoryStore"/> that uses
/// <see cref="RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade,string,object[])"/>
/// to manage the migration history table.
/// </summary>
/// <typeparam name="TContext">The DbContext type used for sharded databases.</typeparam>
internal sealed class EfCoreMigrationHistoryStore<TContext> : IMigrationHistoryStore
    where TContext : DbContext
{
    private const string HistoryTableName = "__EncinaMigrationHistory";

    private readonly IShardedDbContextFactory<TContext> _contextFactory;

    public EfCoreMigrationHistoryStore(IShardedDbContextFactory<TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<string>>> GetAppliedAsync(
        string shardId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        try
        {
            var contextResult = _contextFactory.CreateContextForShard(shardId);
            return await contextResult.MapAsync(async context =>
            {
                await using (context)
                {
                    var sql = "SELECT MigrationId FROM [" + HistoryTableName + "] WHERE RolledBackAtUtc IS NULL ORDER BY AppliedAtUtc";
                    var results = await context.Database
                        .SqlQueryRaw<string>(sql)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return (IReadOnlyList<string>)results;
                }
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
            var contextResult = _contextFactory.CreateContextForShard(shardId);
            return await contextResult.MapAsync(async context =>
            {
                await using (context)
                {
                    var sql = "INSERT INTO [" + HistoryTableName + "] " +
                        "(MigrationId, Description, Checksum, AppliedAtUtc, DurationMs) " +
                        "VALUES ({0}, {1}, {2}, {3}, {4})";

                    await context.Database.ExecuteSqlRawAsync(
                        sql,
                        [script.Id, script.Description, script.Checksum, DateTime.UtcNow, (long)duration.TotalMilliseconds],
                        cancellationToken).ConfigureAwait(false);
                }

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
            var contextResult = _contextFactory.CreateContextForShard(shardId);
            return await contextResult.MapAsync(async context =>
            {
                await using (context)
                {
                    var sql = "UPDATE [" + HistoryTableName + "] " +
                        "SET RolledBackAtUtc = {0} " +
                        "WHERE MigrationId = {1} AND RolledBackAtUtc IS NULL";

                    await context.Database.ExecuteSqlRawAsync(
                        sql,
                        [DateTime.UtcNow, migrationId],
                        cancellationToken).ConfigureAwait(false);
                }

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
            var contextResult = _contextFactory.CreateContextForShard(shardInfo.ShardId);
            return await contextResult.MapAsync(async context =>
            {
                await using (context)
                {
                    // Use provider-agnostic DDL that works across EF Core supported databases
                    var sql = """
                        CREATE TABLE IF NOT EXISTS [__EncinaMigrationHistory] (
                            MigrationId NVARCHAR(256) NOT NULL PRIMARY KEY,
                            Description NVARCHAR(1024) NULL,
                            Checksum NVARCHAR(128) NOT NULL,
                            AppliedAtUtc DATETIME2 NOT NULL,
                            DurationMs BIGINT NOT NULL,
                            RolledBackAtUtc DATETIME2 NULL
                        )
                        """;

                    await context.Database.ExecuteSqlRawAsync(
                        sql,
                        cancellationToken).ConfigureAwait(false);
                }

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
            var contextResult = _contextFactory.CreateContextForShard(shardId);
            return await contextResult.MapAsync(async context =>
            {
                await using (context)
                {
                    foreach (var script in scripts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var sql = "INSERT INTO [" + HistoryTableName + "] " +
                            "(MigrationId, Description, Checksum, AppliedAtUtc, DurationMs) " +
                            "SELECT {0}, {1}, {2}, {3}, 0 " +
                            "WHERE NOT EXISTS (SELECT 1 FROM [" + HistoryTableName + "] WHERE MigrationId = {0})";

                        await context.Database.ExecuteSqlRawAsync(
                            sql,
                            [script.Id, script.Description, script.Checksum, DateTime.UtcNow],
                            cancellationToken).ConfigureAwait(false);
                    }
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
}
