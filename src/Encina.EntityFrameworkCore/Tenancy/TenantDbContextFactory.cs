using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.EntityFrameworkCore.Tenancy;

/// <summary>
/// Factory for creating DbContext instances with tenant-specific configuration.
/// </summary>
/// <typeparam name="TContext">The type of DbContext.</typeparam>
/// <remarks>
/// <para>
/// This factory supports database-per-tenant scenarios by configuring
/// connection strings based on the current tenant context.
/// </para>
/// <para>
/// <b>Supported isolation strategies:</b>
/// <list type="bullet">
/// <item><description><see cref="TenantIsolationStrategy.DatabasePerTenant"/>: Uses tenant-specific connection string</description></item>
/// <item><description><see cref="TenantIsolationStrategy.SchemaPerTenant"/>: Uses default connection with tenant schema</description></item>
/// <item><description><see cref="TenantIsolationStrategy.SharedSchema"/>: Uses default connection</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddDbContextFactory&lt;AppDbContext&gt;((sp, options) =>
/// {
///     var factory = sp.GetRequiredService&lt;TenantDbContextFactory&lt;AppDbContext&gt;&gt;();
///     factory.ConfigureOptions(options);
/// });
///
/// // Usage
/// var factory = serviceProvider.GetRequiredService&lt;IDbContextFactory&lt;AppDbContext&gt;&gt;();
/// using var context = await factory.CreateDbContextAsync();
/// </code>
/// </example>
public sealed class TenantDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly TenancyOptions _tenancyOptions;
    private readonly Func<DbContextOptionsBuilder<TContext>, IServiceProvider, TenantInfo?, DbContextOptionsBuilder<TContext>>? _configureOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantDbContextFactory{TContext}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="tenantStore">The tenant store.</param>
    /// <param name="tenancyOptions">The tenancy options.</param>
    /// <param name="configureOptions">Optional custom options configurator.</param>
    public TenantDbContextFactory(
        IServiceProvider serviceProvider,
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        IOptions<TenancyOptions> tenancyOptions,
        Func<DbContextOptionsBuilder<TContext>, IServiceProvider, TenantInfo?, DbContextOptionsBuilder<TContext>>? configureOptions = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
        _tenancyOptions = tenancyOptions?.Value ?? throw new ArgumentNullException(nameof(tenancyOptions));
        _configureOptions = configureOptions;
    }

    /// <summary>
    /// Creates a new DbContext instance configured for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new DbContext instance.</returns>
    public async ValueTask<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var tenantInfo = await _tenantProvider.GetCurrentTenantAsync(cancellationToken);
        return CreateDbContextForTenant(tenantInfo);
    }

    /// <summary>
    /// Creates a new DbContext instance for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new DbContext instance.</returns>
    public async ValueTask<TContext> CreateDbContextForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenantInfo = await _tenantStore.GetTenantAsync(tenantId, cancellationToken);
        return CreateDbContextForTenant(tenantInfo);
    }

    /// <summary>
    /// Gets the connection string for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connection string.</returns>
    public async ValueTask<string> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var tenantInfo = await _tenantProvider.GetCurrentTenantAsync(cancellationToken);
        return GetConnectionStringForTenant(tenantInfo);
    }

    /// <summary>
    /// Configures DbContext options for the current tenant.
    /// </summary>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    /// <remarks>
    /// This method is synchronous and uses the current tenant context.
    /// For async scenarios, use <see cref="CreateDbContextAsync"/>.
    /// </remarks>
    public void ConfigureOptions(DbContextOptionsBuilder<TContext> optionsBuilder)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        TenantInfo? tenantInfo = null;

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            // Sync lookup - consider caching in production
            tenantInfo = _tenantStore.GetTenantAsync(tenantId).AsTask().GetAwaiter().GetResult();
        }

        ConfigureOptionsForTenant(optionsBuilder, tenantInfo);
    }

    private TContext CreateDbContextForTenant(TenantInfo? tenantInfo)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        ConfigureOptionsForTenant(optionsBuilder, tenantInfo);

        // Use ActivatorUtilities to create context with DI support
        return ActivatorUtilities.CreateInstance<TContext>(
            _serviceProvider,
            optionsBuilder.Options);
    }

    private void ConfigureOptionsForTenant(DbContextOptionsBuilder<TContext> optionsBuilder, TenantInfo? tenantInfo)
    {
        if (_configureOptions is not null)
        {
            _configureOptions(optionsBuilder, _serviceProvider, tenantInfo);
            return;
        }

        // Default behavior: configure connection string based on tenant
        var connectionString = GetConnectionStringForTenant(tenantInfo);

        // Note: The actual provider configuration (UseSqlServer, UseNpgsql, etc.)
        // should be done in the custom configureOptions delegate or in the
        // DbContext's OnConfiguring method
    }

    private string GetConnectionStringForTenant(TenantInfo? tenantInfo)
    {
        // For DatabasePerTenant, use tenant-specific connection string
        if (tenantInfo?.HasDedicatedDatabase == true &&
            !string.IsNullOrWhiteSpace(tenantInfo.ConnectionString))
        {
            return tenantInfo.ConnectionString;
        }

        // Fall back to default connection string
        if (!string.IsNullOrWhiteSpace(_tenancyOptions.DefaultConnectionString))
        {
            return _tenancyOptions.DefaultConnectionString;
        }

        throw new InvalidOperationException(
            "No connection string available. Either configure a tenant-specific connection string " +
            "or set TenancyOptions.DefaultConnectionString.");
    }
}
