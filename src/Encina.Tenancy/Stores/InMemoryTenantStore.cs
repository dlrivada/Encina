using System.Collections.Concurrent;

namespace Encina.Tenancy;

/// <summary>
/// In-memory implementation of <see cref="ITenantStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// This store keeps tenant information in memory using a thread-safe
/// <see cref="ConcurrentDictionary{TKey, TValue}"/>. It supports:
/// </para>
/// <list type="bullet">
/// <item><b>Configuration-based:</b> Load tenants from <see cref="TenancyOptions.Tenants"/></item>
/// <item><b>Programmatic:</b> Register tenants at runtime via <see cref="RegisterTenant"/></item>
/// </list>
/// <para><b>Use Cases:</b></para>
/// <list type="bullet">
/// <item>Development and local testing</item>
/// <item>Unit and integration tests</item>
/// <item>Small applications with static tenant lists</item>
/// <item>Prototyping before implementing database-backed store</item>
/// </list>
/// <para>
/// For production with dynamic tenant management, implement a custom
/// <see cref="ITenantStore"/> backed by a database or external service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Via DI configuration
/// services.AddEncinaTenancy(options =>
/// {
///     options.Tenants.Add(new TenantInfo("tenant-1", "Acme", TenantIsolationStrategy.SharedSchema));
///     options.Tenants.Add(new TenantInfo("tenant-2", "Contoso", TenantIsolationStrategy.SchemaPerTenant, SchemaName: "contoso"));
/// });
///
/// // Programmatic registration (e.g., in tests)
/// var store = serviceProvider.GetRequiredService&lt;InMemoryTenantStore&gt;();
/// store.RegisterTenant(new TenantInfo("test-tenant", "Test", TenantIsolationStrategy.SharedSchema));
/// </code>
/// </example>
public sealed class InMemoryTenantStore : ITenantStore
{
    private readonly ConcurrentDictionary<string, TenantInfo> _tenants = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTenantStore"/> class.
    /// </summary>
    public InMemoryTenantStore()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTenantStore"/> class
    /// with initial tenants.
    /// </summary>
    /// <param name="tenants">The initial tenants to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenants"/> is null.</exception>
    public InMemoryTenantStore(IEnumerable<TenantInfo> tenants)
    {
        ArgumentNullException.ThrowIfNull(tenants);

        foreach (var tenant in tenants)
        {
            RegisterTenant(tenant);
        }
    }

    /// <inheritdoc />
    public ValueTask<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        _tenants.TryGetValue(tenantId, out var tenant);
        return new ValueTask<TenantInfo?>(tenant);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TenantInfo> tenants = _tenants.Values.ToList();
        return new ValueTask<IReadOnlyList<TenantInfo>>(tenants);
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        return new ValueTask<bool>(_tenants.ContainsKey(tenantId));
    }

    /// <summary>
    /// Registers a tenant in the store.
    /// </summary>
    /// <param name="tenant">The tenant information to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenant"/> is null.</exception>
    /// <remarks>
    /// If a tenant with the same ID already exists, it will be replaced.
    /// </remarks>
    public void RegisterTenant(TenantInfo tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        _tenants[tenant.TenantId] = tenant;
    }

    /// <summary>
    /// Removes a tenant from the store.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to remove.</param>
    /// <returns><c>true</c> if the tenant was removed; <c>false</c> if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is null.</exception>
    public bool RemoveTenant(string tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        return _tenants.TryRemove(tenantId, out _);
    }

    /// <summary>
    /// Removes all tenants from the store.
    /// </summary>
    /// <remarks>
    /// Useful for test cleanup scenarios.
    /// </remarks>
    public void Clear()
    {
        _tenants.Clear();
    }

    /// <summary>
    /// Gets the number of registered tenants.
    /// </summary>
    public int Count => _tenants.Count;
}
