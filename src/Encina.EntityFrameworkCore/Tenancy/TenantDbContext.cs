using System.Linq.Expressions;
using System.Reflection;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace Encina.EntityFrameworkCore.Tenancy;

/// <summary>
/// Base DbContext with built-in multi-tenancy support including query filters,
/// automatic tenant assignment, and tenant validation.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to enable automatic multi-tenancy features:
/// <list type="bullet">
/// <item><description>Global query filters for tenant isolation</description></item>
/// <item><description>Automatic tenant ID assignment on save</description></item>
/// <item><description>Tenant validation on save for modified entities</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Requirements:</b>
/// <list type="bullet">
/// <item><description>Tenant entities must implement <see cref="ITenantEntity"/></description></item>
/// <item><description><see cref="ITenantProvider"/> must be registered in DI</description></item>
/// <item><description>Tenancy must be configured via <see cref="EfCoreTenancyOptions"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class AppDbContext : TenantDbContext
/// {
///     public AppDbContext(
///         DbContextOptions&lt;AppDbContext&gt; options,
///         ITenantProvider tenantProvider,
///         IOptions&lt;EfCoreTenancyOptions&gt; tenancyOptions,
///         IOptions&lt;TenancyOptions&gt; coreOptions)
///         : base(options, tenantProvider, tenancyOptions, coreOptions)
///     {
///     }
///
///     public DbSet&lt;Order&gt; Orders { get; set; }
///
///     protected override void OnModelCreating(ModelBuilder modelBuilder)
///     {
///         base.OnModelCreating(modelBuilder); // Apply tenant filters
///         // Additional configuration...
///     }
/// }
/// </code>
/// </example>
public abstract class TenantDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    private readonly EfCoreTenancyOptions _tenancyOptions;
    private readonly TenancyOptions _coreOptions;
    private readonly ITenantSchemaConfigurator? _schemaConfigurator;
    private TenantInfo? _currentTenantInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    /// <param name="tenantProvider">The tenant provider for accessing current tenant.</param>
    /// <param name="tenancyOptions">The EF Core tenancy options.</param>
    /// <param name="coreOptions">The core tenancy options.</param>
    /// <param name="schemaConfigurator">Optional schema configurator for schema-per-tenant.</param>
    protected TenantDbContext(
        DbContextOptions options,
        ITenantProvider tenantProvider,
        IOptions<EfCoreTenancyOptions> tenancyOptions,
        IOptions<TenancyOptions> coreOptions,
        ITenantSchemaConfigurator? schemaConfigurator = null)
        : base(options)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _tenancyOptions = tenancyOptions?.Value ?? throw new ArgumentNullException(nameof(tenancyOptions));
        _coreOptions = coreOptions?.Value ?? throw new ArgumentNullException(nameof(coreOptions));
        _schemaConfigurator = schemaConfigurator;
    }

    /// <summary>
    /// Gets the current tenant ID from the tenant provider.
    /// </summary>
    protected string? CurrentTenantId => _tenantProvider.GetCurrentTenantId();

    /// <summary>
    /// Gets a value indicating whether a tenant context is available.
    /// </summary>
    protected bool HasTenantContext => !string.IsNullOrWhiteSpace(CurrentTenantId);

    /// <summary>
    /// Gets the current tenant information.
    /// </summary>
    /// <remarks>
    /// This property is cached after first access to avoid repeated async calls.
    /// </remarks>
    protected TenantInfo? CurrentTenantInfo
    {
        get
        {
            if (_currentTenantInfo is null && HasTenantContext)
            {
                // Sync lookup - tenant info should be cached in provider
                _currentTenantInfo = _tenantProvider.GetCurrentTenantAsync()
                    .AsTask().GetAwaiter().GetResult();
            }
            return _currentTenantInfo;
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (_tenancyOptions.UseQueryFilters)
        {
            ApplyTenantQueryFilters(modelBuilder);
        }

        // Apply schema configuration for schema-per-tenant
        _schemaConfigurator?.ConfigureSchema(modelBuilder, CurrentTenantInfo);
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ProcessTenantEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        ProcessTenantEntities();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProcessTenantEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProcessTenantEntities();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Applies global query filters for all entity types implementing <see cref="ITenantEntity"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            // Build expression: e => e.TenantId == CurrentTenantId
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

            // Use a method call to get the current tenant ID at query execution time
            var currentTenantIdMethod = GetType().GetProperty(
                nameof(CurrentTenantId),
                BindingFlags.NonPublic | BindingFlags.Instance)!.GetGetMethod(true)!;

            var currentTenantIdCall = Expression.Call(Expression.Constant(this), currentTenantIdMethod);

            // e.TenantId == CurrentTenantId
            var comparison = Expression.Equal(tenantIdProperty, currentTenantIdCall);

            var lambda = Expression.Lambda(comparison, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// Processes tenant entities before saving changes.
    /// </summary>
    private void ProcessTenantEntities()
    {
        var tenantId = CurrentTenantId;
        var entries = ChangeTracker.Entries<ITenantEntity>().ToList();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ProcessAddedEntity(entry, tenantId);
                    break;

                case EntityState.Modified:
                case EntityState.Deleted:
                    ProcessModifiedOrDeletedEntity(entry, tenantId);
                    break;
            }
        }
    }

    /// <summary>
    /// Processes a newly added tenant entity.
    /// </summary>
    private void ProcessAddedEntity(EntityEntry<ITenantEntity> entry, string? tenantId)
    {
        if (_tenancyOptions.AutoAssignTenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                if (_coreOptions.RequireTenant && _tenancyOptions.ThrowOnMissingTenantContext)
                {
                    throw new InvalidOperationException(
                        $"Cannot save entity '{entry.Entity.GetType().Name}' without tenant context. " +
                        "Either provide a tenant context or disable TenancyOptions.RequireTenant.");
                }

                // No tenant context and RequireTenant is false - leave TenantId as-is
                return;
            }

            // Auto-assign tenant ID
            entry.Entity.TenantId = tenantId;
        }
    }

    /// <summary>
    /// Processes a modified or deleted tenant entity.
    /// </summary>
    private void ProcessModifiedOrDeletedEntity(EntityEntry<ITenantEntity> entry, string? tenantId)
    {
        if (!_tenancyOptions.ValidateTenantOnSave)
        {
            return;
        }

        var entityTenantId = entry.Entity.TenantId;

        // Skip validation if no tenant context (e.g., admin operations)
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            if (_coreOptions.RequireTenant && _tenancyOptions.ThrowOnMissingTenantContext)
            {
                throw new InvalidOperationException(
                    $"Cannot modify entity '{entry.Entity.GetType().Name}' without tenant context. " +
                    "Either provide a tenant context or disable TenancyOptions.RequireTenant.");
            }

            return;
        }

        // Validate tenant ownership
        if (!string.Equals(entityTenantId, tenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Tenant mismatch: Entity '{entry.Entity.GetType().Name}' belongs to tenant '{entityTenantId}' " +
                $"but current tenant is '{tenantId}'. Cross-tenant data access is not allowed.");
        }
    }
}
