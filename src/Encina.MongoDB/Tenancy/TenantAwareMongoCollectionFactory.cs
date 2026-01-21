using Encina.Tenancy;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Tenancy;

/// <summary>
/// MongoDB implementation of <see cref="IMongoCollectionFactory"/> for tenant-aware collection routing.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates MongoDB collections based on the current tenant context
/// and tenant isolation strategy:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>SharedDatabase</term>
/// <description>Uses the default database (tenant filtering is done at query level)</description>
/// </item>
/// <item>
/// <term>DatabasePerTenant</term>
/// <description>Uses a tenant-specific database based on the configured pattern</description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaMongoDBWithTenancy(config =&gt; { }, tenancy =&gt;
/// {
///     tenancy.EnableDatabasePerTenant = true;
///     tenancy.DatabaseNamePattern = "{baseName}_{tenantId}";
/// });
///
/// // Usage
/// public class OrderService(IMongoCollectionFactory collectionFactory)
/// {
///     public async Task&lt;IMongoCollection&lt;Order&gt;&gt; GetCollectionAsync(CancellationToken ct)
///     {
///         return await collectionFactory.GetCollectionAsync&lt;Order&gt;("orders", ct);
///     }
/// }
/// </code>
/// </example>
public sealed class TenantAwareMongoCollectionFactory : IMongoCollectionFactory
{
    private readonly IMongoClient _mongoClient;
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly EncinaMongoDbOptions _mongoOptions;
    private readonly MongoDbTenancyOptions _tenancyOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareMongoCollectionFactory"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="tenantStore">The tenant store for retrieving tenant information.</param>
    /// <param name="mongoOptions">The MongoDB configuration options.</param>
    /// <param name="tenancyOptions">The tenancy configuration options.</param>
    public TenantAwareMongoCollectionFactory(
        IMongoClient mongoClient,
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        IOptions<EncinaMongoDbOptions> mongoOptions,
        IOptions<MongoDbTenancyOptions> tenancyOptions)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(tenantStore);
        ArgumentNullException.ThrowIfNull(mongoOptions);
        ArgumentNullException.ThrowIfNull(tenancyOptions);

        _mongoClient = mongoClient;
        _tenantProvider = tenantProvider;
        _tenantStore = tenantStore;
        _mongoOptions = mongoOptions.Value;
        _tenancyOptions = tenancyOptions.Value;
    }

    /// <inheritdoc/>
    public async ValueTask<IMongoCollection<TEntity>> GetCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);

        var databaseName = await GetDatabaseNameAsync(cancellationToken).ConfigureAwait(false);
        var database = _mongoClient.GetDatabase(databaseName);
        return database.GetCollection<TEntity>(collectionName);
    }

    /// <inheritdoc/>
    public async ValueTask<IMongoCollection<TEntity>> GetCollectionForTenantAsync<TEntity>(
        string collectionName,
        string tenantId,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var databaseName = await GetDatabaseNameForTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var database = _mongoClient.GetDatabase(databaseName);
        return database.GetCollection<TEntity>(collectionName);
    }

    /// <inheritdoc/>
    public async ValueTask<string> GetDatabaseNameAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // No tenant context - use default
        if (string.IsNullOrEmpty(tenantId))
        {
            return GetDefaultDatabaseName();
        }

        return await GetDatabaseNameForTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<string> GetDatabaseNameForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        // If database-per-tenant is not enabled, always use default
        if (!_tenancyOptions.EnableDatabasePerTenant)
        {
            return GetDefaultDatabaseName();
        }

        var tenant = await _tenantStore.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);

        // Tenant not found - use default
        if (tenant is null)
        {
            return GetDefaultDatabaseName();
        }

        // Check if tenant has DatabasePerTenant strategy
        if (tenant.Strategy == TenantIsolationStrategy.DatabasePerTenant)
        {
            // Use tenant's connection string database if available
            if (!string.IsNullOrEmpty(tenant.ConnectionString))
            {
                return ExtractDatabaseNameFromConnectionString(tenant.ConnectionString)
                    ?? _tenancyOptions.GetDatabaseName(_mongoOptions.DatabaseName, tenantId);
            }

            // Use the configured pattern
            return _tenancyOptions.GetDatabaseName(_mongoOptions.DatabaseName, tenantId);
        }

        // SharedSchema and SchemaPerTenant use the default database
        return GetDefaultDatabaseName();
    }

    private string GetDefaultDatabaseName()
    {
        if (string.IsNullOrEmpty(_mongoOptions.DatabaseName))
        {
            throw new InvalidOperationException(
                "No default database name configured. " +
                "Set EncinaMongoDbOptions.DatabaseName in your configuration.");
        }

        return _mongoOptions.DatabaseName;
    }

    private static string? ExtractDatabaseNameFromConnectionString(string connectionString)
    {
        // MongoDB connection strings can have database name after the host:
        // mongodb://localhost:27017/myDatabase
        // mongodb+srv://user:pass@cluster.mongodb.net/myDatabase?options

        try
        {
            var uri = new Uri(connectionString.Replace("mongodb+srv://", "mongodb://", StringComparison.OrdinalIgnoreCase));
            var path = uri.AbsolutePath?.TrimStart('/');

            if (!string.IsNullOrEmpty(path))
            {
                // Remove any query parameters that might be in the path
                var questionMarkIndex = path.IndexOf('?', StringComparison.Ordinal);
                if (questionMarkIndex >= 0)
                {
                    path = path[..questionMarkIndex];
                }

                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }
        }
        catch (UriFormatException)
        {
            // If we can't parse the connection string, return null
        }

        return null;
    }
}
