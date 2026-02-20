using System.Data;
using Encina;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Tenancy;

/// <summary>
/// PostgreSQL implementation of <see cref="ITenantConnectionFactory"/> for ADO.NET.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates database connections based on the current tenant context
/// and tenant isolation strategy:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>SharedSchema</term>
/// <description>Uses the default connection string</description>
/// </item>
/// <item>
/// <term>SchemaPerTenant</term>
/// <description>Uses the default connection string (schema is handled separately)</description>
/// </item>
/// <item>
/// <term>DatabasePerTenant</term>
/// <description>Uses the tenant's specific connection string if available</description>
/// </item>
/// </list>
/// <para>
/// All methods return <see cref="Either{EncinaError, T}"/> following the Railway Oriented
/// Programming pattern instead of throwing exceptions for runtime configuration errors.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaADOPostgreSQLWithTenancy(config =&gt; { });
///
/// // Usage
/// public class OrderService(ITenantConnectionFactory connectionFactory)
/// {
///     public async Task&lt;Either&lt;EncinaError, IDbConnection&gt;&gt; GetConnectionAsync(CancellationToken ct)
///     {
///         return await connectionFactory.CreateConnectionAsync(ct);
///     }
/// }
/// </code>
/// </example>
public sealed class TenantConnectionFactory : ITenantConnectionFactory
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly TenancyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConnectionFactory"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="tenantStore">The tenant store for retrieving tenant information.</param>
    /// <param name="options">The tenancy configuration options.</param>
    public TenantConnectionFactory(
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        IOptions<TenancyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(tenantStore);
        ArgumentNullException.ThrowIfNull(options);

        _tenantProvider = tenantProvider;
        _tenantStore = tenantStore;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IDbConnection>> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await GetConnectionStringAsync(cancellationToken).ConfigureAwait(false);

        return await connectionStringResult.MatchAsync<Either<EncinaError, IDbConnection>>(
            RightAsync: async cs =>
            {
                var connection = new NpgsqlConnection(cs);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return Either<EncinaError, IDbConnection>.Right(connection);
            },
            Left: error => error);
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IDbConnection>> CreateConnectionForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var tenant = await _tenantStore.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null)
        {
            return EncinaError.New($"Tenant '{tenantId}' not found.");
        }

        return await GetConnectionStringForTenant(tenant)
            .MatchAsync<Either<EncinaError, IDbConnection>>(
                RightAsync: async cs =>
                {
                    var connection = new NpgsqlConnection(cs);
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    return Either<EncinaError, IDbConnection>.Right(connection);
                },
                Left: error => error);
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, string>> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // No tenant context - use default
        if (string.IsNullOrEmpty(tenantId))
        {
            return GetDefaultConnectionString();
        }

        var tenant = await _tenantStore.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);

        // Tenant not found - use default
        if (tenant is null)
        {
            return GetDefaultConnectionString();
        }

        return GetConnectionStringForTenant(tenant);
    }

    private Either<EncinaError, string> GetConnectionStringForTenant(TenantInfo tenant)
    {
        // Only DatabasePerTenant strategy uses tenant-specific connection strings
        if (tenant.Strategy == TenantIsolationStrategy.DatabasePerTenant)
        {
            // Use tenant's connection string if available, otherwise fall back to default
            if (!string.IsNullOrEmpty(tenant.ConnectionString))
            {
                return tenant.ConnectionString;
            }
        }

        // SharedSchema and SchemaPerTenant use the default connection string
        return GetDefaultConnectionString();
    }

    private Either<EncinaError, string> GetDefaultConnectionString()
    {
        if (string.IsNullOrEmpty(_options.DefaultConnectionString))
        {
            return EncinaError.New(
                "No default connection string configured. " +
                "Set TenancyOptions.DefaultConnectionString in your configuration.");
        }

        return _options.DefaultConnectionString;
    }
}
