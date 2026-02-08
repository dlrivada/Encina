using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

using Encina.Caching;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Encina.EntityFrameworkCore.Caching;

/// <summary>
/// Default implementation of <see cref="IQueryCacheKeyGenerator"/> that generates
/// deterministic cache keys from database commands.
/// </summary>
/// <remarks>
/// <para>
/// This generator produces cache keys by:
/// </para>
/// <list type="number">
/// <item><description>Extracting table names from the SQL command text using <see cref="SqlTableExtractor"/></description></item>
/// <item><description>Mapping table names to EF Core entity types via <see cref="DbContext.Model"/></description></item>
/// <item><description>Computing a SHA256 hash from the command text and serialized parameter values</description></item>
/// <item><description>Composing a structured cache key with prefix, tenant, primary entity type, and hash</description></item>
/// </list>
/// <para>
/// The generated cache key format is:
/// <c>{prefix}:{tenant}:{primaryEntityType}:{hash}</c>
/// </para>
/// <para>
/// When no tenant is present, the tenant segment is omitted:
/// <c>{prefix}:{primaryEntityType}:{hash}</c>
/// </para>
/// <para>
/// When no entity types can be resolved, <c>"unknown"</c> is used as the primary entity type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddSingleton&lt;IQueryCacheKeyGenerator, DefaultQueryCacheKeyGenerator&gt;();
///
/// // Key examples:
/// // "sm:qc:Order:a1b2c3d4"        - Order query, no tenant
/// // "sm:qc:tenant-1:Order:a1b2c3d4" - Order query with tenant
/// // "sm:qc:unknown:e5f6g7h8"      - Query where entity type couldn't be resolved
/// </code>
/// </example>
public sealed class DefaultQueryCacheKeyGenerator : IQueryCacheKeyGenerator
{
    private readonly QueryCacheOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultQueryCacheKeyGenerator"/> class.
    /// </summary>
    /// <param name="options">The query cache options containing the key prefix configuration.</param>
    public DefaultQueryCacheKeyGenerator(IOptions<QueryCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public QueryCacheKey Generate(DbCommand command, DbContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        var entityTypes = ResolveEntityTypes(command.CommandText, context);
        var primaryEntityType = entityTypes.Count > 0 ? entityTypes[0] : "unknown";
        var hash = ComputeHash(command);

        var key = $"{_options.KeyPrefix}:{primaryEntityType}:{hash}";

        return new QueryCacheKey(key, entityTypes);
    }

    /// <inheritdoc />
    public QueryCacheKey Generate(DbCommand command, DbContext context, IRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestContext);

        var entityTypes = ResolveEntityTypes(command.CommandText, context);
        var primaryEntityType = entityTypes.Count > 0 ? entityTypes[0] : "unknown";
        var hash = ComputeHash(command);

        var key = string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? $"{_options.KeyPrefix}:{primaryEntityType}:{hash}"
            : $"{_options.KeyPrefix}:{requestContext.TenantId}:{primaryEntityType}:{hash}";

        return new QueryCacheKey(key, entityTypes);
    }

    /// <summary>
    /// Extracts table names from SQL and maps them to EF Core entity type names.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="SqlTableExtractor"/> to parse table names from the SQL command text,
    /// then resolves each table name to an entity type via <see cref="DbContext.Model"/>.
    /// Table names that cannot be resolved to entity types are included as-is to ensure
    /// the cache key captures all relevant tables for invalidation.
    /// </remarks>
    private static List<string> ResolveEntityTypes(string? commandText, DbContext context)
    {
        var tableNames = SqlTableExtractor.ExtractTableNames(commandText);
        if (tableNames.Count == 0)
        {
            return [];
        }

        var entityTypes = new List<string>(tableNames.Count);
        var model = context.Model;

        foreach (var tableName in tableNames)
        {
            var entityType = model
                .GetEntityTypes()
                .FirstOrDefault(e =>
                    string.Equals(e.GetTableName(), tableName, StringComparison.OrdinalIgnoreCase));

            // Use the CLR type name if resolved, otherwise use the raw table name.
            // Including unresolved table names ensures cache invalidation still works
            // even for tables not mapped in the current DbContext.
            entityTypes.Add(entityType?.ClrType.Name ?? tableName);
        }

        return entityTypes;
    }

    /// <summary>
    /// Computes a deterministic SHA256 hash from the command text and parameters.
    /// </summary>
    /// <remarks>
    /// The hash includes both the SQL text and all parameter values to ensure that
    /// identical queries with different parameter values produce different cache keys.
    /// Parameters are serialized in ordinal order with name-value pairs separated by
    /// a null character to prevent ambiguity.
    /// </remarks>
    private static string ComputeHash(DbCommand command)
    {
        var contentLength = (command.CommandText?.Length ?? 0) + (command.Parameters.Count * 64);
        var builder = new StringBuilder(contentLength);

        builder.Append(command.CommandText);

        // Append parameters in ordinal order for determinism.
        // Use null character as separator to avoid collisions between
        // param names and values (e.g., "Name=A\0Value=B" vs "Name=AValue\0=B").
        foreach (DbParameter parameter in command.Parameters)
        {
            builder.Append('\0');
            builder.Append(parameter.ParameterName);
            builder.Append('\0');
            builder.Append(parameter.Value?.ToString() ?? "NULL");
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));

        // Use first 16 hex characters (8 bytes) for a compact but sufficiently unique key.
        // 64-bit hash space gives negligible collision probability for cache keys.
        return Convert.ToHexString(hashBytes, 0, 8).ToLowerInvariant();
    }
}
