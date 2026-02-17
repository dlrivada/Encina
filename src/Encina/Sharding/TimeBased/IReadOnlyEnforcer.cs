using LanguageExt;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Enforces read-only state on a shard at the database or infrastructure level.
/// </summary>
/// <remarks>
/// <para>
/// This interface is provider-specific. Implementations may set the database to read-only mode
/// (e.g., <c>ALTER DATABASE SET READ_ONLY</c>), revoke write permissions, or apply
/// infrastructure-level restrictions depending on the database provider.
/// </para>
/// <para>
/// When no <see cref="IReadOnlyEnforcer"/> is registered, the <see cref="ShardArchiver"/>
/// falls back to updating the <see cref="ITierStore"/> metadata only, relying on the
/// <see cref="ITimeBasedShardRouter"/> to block writes at the application level.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // SQL Server implementation example
/// public class SqlServerReadOnlyEnforcer : IReadOnlyEnforcer
/// {
///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; EnforceReadOnlyAsync(
///         string shardId, string connectionString, CancellationToken ct)
///     {
///         // ALTER DATABASE [db] SET READ_ONLY
///     }
/// }
/// </code>
/// </example>
public interface IReadOnlyEnforcer
{
    /// <summary>
    /// Enforces read-only state on the specified shard at the database or infrastructure level.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="connectionString">The connection string for the shard's database.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an error if enforcement fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> EnforceReadOnlyAsync(
        string shardId,
        string connectionString,
        CancellationToken cancellationToken = default);
}
