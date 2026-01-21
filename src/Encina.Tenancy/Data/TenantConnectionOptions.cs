namespace Encina.Tenancy;

/// <summary>
/// Configuration options for tenant connection management.
/// </summary>
/// <remarks>
/// <para>
/// These options control how database connections are created and managed
/// for multi-tenant scenarios.
/// </para>
/// </remarks>
public sealed class TenantConnectionOptions
{
    /// <summary>
    /// Gets or sets the default connection string used when no tenant-specific
    /// connection string is available.
    /// </summary>
    /// <value>The default is <c>null</c>.</value>
    /// <remarks>
    /// <para>
    /// This connection string is used for:
    /// <list type="bullet">
    /// <item><see cref="TenantIsolationStrategy.SharedSchema"/> tenants</item>
    /// <item><see cref="TenantIsolationStrategy.SchemaPerTenant"/> tenants</item>
    /// <item>Tenants without a dedicated <see cref="TenantInfo.ConnectionString"/></item>
    /// <item>Operations without tenant context (when allowed)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? DefaultConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to open connections automatically
    /// when created by the factory.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// When <c>true</c>, <see cref="ITenantConnectionFactory{TConnection}.CreateConnectionAsync"/>
    /// returns an already-opened connection. Set to <c>false</c> if you need to configure
    /// the connection before opening.
    /// </remarks>
    public bool AutoOpenConnections { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    /// <value>The default is 30 seconds.</value>
    /// <remarks>
    /// Applied when opening new connections. This timeout is separate from
    /// any timeout specified in the connection string.
    /// </remarks>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to throw an exception when
    /// no connection string can be resolved.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, throws <see cref="InvalidOperationException"/> if
    /// no connection string is available for the tenant.
    /// </para>
    /// <para>
    /// When <c>false</c>, returns a connection using the default connection string
    /// even if the tenant is not found (useful for graceful degradation scenarios).
    /// </para>
    /// </remarks>
    public bool ThrowOnMissingConnectionString { get; set; } = true;
}
