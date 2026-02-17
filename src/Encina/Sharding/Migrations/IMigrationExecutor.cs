using LanguageExt;

namespace Encina.Sharding.Migrations;

/// <summary>
/// Executes DDL statements against a single shard's database.
/// </summary>
/// <remarks>
/// <para>
/// This is the provider-agnostic abstraction that the <see cref="ShardedMigrationCoordinator"/>
/// uses to apply and rollback migration scripts. Provider-specific implementations (ADO.NET,
/// Dapper, EF Core, MongoDB) handle the actual database interaction.
/// </para>
/// </remarks>
public interface IMigrationExecutor
{
    /// <summary>
    /// Executes a SQL DDL statement against the specified shard.
    /// </summary>
    /// <param name="shardInfo">The target shard.</param>
    /// <param name="sql">The DDL statement(s) to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if the execution fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> ExecuteSqlAsync(
        ShardInfo shardInfo,
        string sql,
        CancellationToken cancellationToken = default);
}
