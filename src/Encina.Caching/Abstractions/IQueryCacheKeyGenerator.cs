using System.Data.Common;

using Microsoft.EntityFrameworkCore;

namespace Encina.Caching;

/// <summary>
/// Generates cache keys from database commands for EF Core query caching.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the counterpart to <see cref="ICacheKeyGenerator"/> for database-level
/// query caching. While <see cref="ICacheKeyGenerator"/> generates keys from request objects,
/// <see cref="IQueryCacheKeyGenerator"/> generates keys from the actual SQL commands executed
/// by EF Core.
/// </para>
/// <para>
/// Cache keys must be:
/// </para>
/// <list type="bullet">
/// <item><description>Unique - Different queries produce different keys</description></item>
/// <item><description>Deterministic - Same query with same parameters always produces the same key</description></item>
/// <item><description>Entity-aware - Track which entity types are involved for targeted invalidation</description></item>
/// </list>
/// <para>
/// The default implementation extracts table names from SQL, maps them to entity types via
/// <see cref="DbContext.Model"/>, and computes a hash from the command text and parameters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom cache key generator with tenant isolation
/// public class TenantQueryCacheKeyGenerator : IQueryCacheKeyGenerator
/// {
///     public QueryCacheKey Generate(DbCommand command, DbContext context)
///     {
///         var hash = ComputeHash(command.CommandText, command.Parameters);
///         var entityTypes = ExtractEntityTypes(command.CommandText, context.Model);
///         return new QueryCacheKey($"query:{hash}", entityTypes);
///     }
///
///     public QueryCacheKey Generate(DbCommand command, DbContext context, IRequestContext requestContext)
///     {
///         var tenantId = requestContext.TenantId ?? "default";
///         var hash = ComputeHash(command.CommandText, command.Parameters);
///         var entityTypes = ExtractEntityTypes(command.CommandText, context.Model);
///         return new QueryCacheKey($"tenant:{tenantId}:query:{hash}", entityTypes);
///     }
/// }
/// </code>
/// </example>
public interface IQueryCacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key from a database command.
    /// </summary>
    /// <param name="command">The database command to generate a key for.</param>
    /// <param name="context">The DbContext used to resolve entity type metadata from table names.</param>
    /// <returns>A <see cref="QueryCacheKey"/> containing the cache key and associated entity types.</returns>
    QueryCacheKey Generate(DbCommand command, DbContext context);

    /// <summary>
    /// Generates a cache key from a database command with tenant isolation support.
    /// </summary>
    /// <param name="command">The database command to generate a key for.</param>
    /// <param name="context">The DbContext used to resolve entity type metadata from table names.</param>
    /// <param name="requestContext">The request context containing tenant and user metadata for key isolation.</param>
    /// <returns>A <see cref="QueryCacheKey"/> containing the tenant-scoped cache key and associated entity types.</returns>
    /// <remarks>
    /// Use this overload in multi-tenant applications to ensure cache isolation between tenants.
    /// When <see cref="IRequestContext.TenantId"/> is present, the cache key is prefixed with the tenant identifier.
    /// </remarks>
    QueryCacheKey Generate(DbCommand command, DbContext context, IRequestContext requestContext);
}

/// <summary>
/// Represents a generated cache key with metadata about the entity types involved.
/// </summary>
/// <param name="Key">The full cache key string used for cache storage and retrieval.</param>
/// <param name="EntityTypes">
/// The list of entity type names involved in the query, used for targeted cache invalidation
/// when those entity types are modified via <c>SaveChanges</c>.
/// </param>
/// <remarks>
/// <para>
/// The <paramref name="EntityTypes"/> list enables fine-grained cache invalidation: when an entity
/// is saved, only cached queries involving that entity type are invalidated, rather than flushing
/// the entire query cache.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A query cache key for a query involving Orders and Customers
/// var key = new QueryCacheKey(
///     Key: "sm:qc:a1b2c3d4e5",
///     EntityTypes: ["Order", "Customer"]);
///
/// // When an Order is saved, all cached queries with "Order" in EntityTypes are invalidated
/// </code>
/// </example>
public sealed record QueryCacheKey(string Key, IReadOnlyList<string> EntityTypes);
