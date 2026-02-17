using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Sharding.Migrations;

/// <summary>
/// EF Core implementation of <see cref="IMigrationExecutor"/> that executes DDL statements
/// using <see cref="RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade,string,object[])"/>.
/// </summary>
/// <typeparam name="TContext">The DbContext type used for sharded databases.</typeparam>
internal sealed class EfCoreMigrationExecutor<TContext> : IMigrationExecutor
    where TContext : DbContext
{
    private readonly IShardedDbContextFactory<TContext> _contextFactory;

    public EfCoreMigrationExecutor(IShardedDbContextFactory<TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
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
            var contextResult = _contextFactory.CreateContextForShard(shardInfo.ShardId);

            return await contextResult
                .MapAsync(async context =>
                {
                    await using (context)
                    {
                        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken)
                            .ConfigureAwait(false);
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
