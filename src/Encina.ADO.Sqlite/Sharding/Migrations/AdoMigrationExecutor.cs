using System.Data;
using System.Data.Common;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;

namespace Encina.ADO.Sqlite.Sharding.Migrations;

/// <summary>
/// SQLite ADO.NET implementation of <see cref="IMigrationExecutor"/>
/// that executes DDL statements against sharded databases.
/// </summary>
internal sealed class AdoMigrationExecutor : IMigrationExecutor
{
    private readonly IShardedConnectionFactory _connectionFactory;

    public AdoMigrationExecutor(IShardedConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> ExecuteSqlAsync(
        ShardInfo shardInfo,
        string sql,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        try
        {
            var connectionResult = await _connectionFactory
                .GetConnectionAsync(shardInfo.ShardId, cancellationToken)
                .ConfigureAwait(false);

            return await connectionResult
                .MapAsync(async connection =>
                {
                    await using var disposable = connection as IAsyncDisposable;

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;

                    if (cmd is DbCommand dbCmd)
                    {
                        await dbCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        cmd.ExecuteNonQuery();
                    }

                    return Unit.Default;
                })
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.MigrationFailed,
                $"Failed to execute SQL on shard '{shardInfo.ShardId}': {ex.Message}",
                ex);
        }
    }
}
