using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Tenancy;

/// <summary>
/// Configures entity schema based on tenant information.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to customize how schemas are applied to tenant entities.
/// The default implementation uses the tenant's schema name for all <see cref="ITenantEntity"/> types.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomSchemaConfigurator : ITenantSchemaConfigurator
/// {
///     public void ConfigureSchema(ModelBuilder modelBuilder, TenantInfo tenantInfo)
///     {
///         // Custom schema naming convention
///         var schemaName = $"tenant_{tenantInfo.TenantId}";
///
///         foreach (var entityType in modelBuilder.Model.GetEntityTypes())
///         {
///             if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
///             {
///                 modelBuilder.Entity(entityType.ClrType).ToTable(
///                     entityType.GetTableName()!,
///                     schemaName);
///             }
///         }
///     }
/// }
/// </code>
/// </example>
public interface ITenantSchemaConfigurator
{
    /// <summary>
    /// Configures the schema for tenant entities in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="tenantInfo">The tenant information, or null if no tenant context.</param>
    void ConfigureSchema(ModelBuilder modelBuilder, TenantInfo? tenantInfo);
}

/// <summary>
/// Default implementation of <see cref="ITenantSchemaConfigurator"/> that applies
/// the tenant's schema name to all tenant entities.
/// </summary>
/// <remarks>
/// <para>
/// This configurator:
/// <list type="bullet">
/// <item>Only applies schemas for <see cref="TenantIsolationStrategy.SchemaPerTenant"/> tenants</item>
/// <item>Uses the tenant's <see cref="TenantInfo.SchemaName"/> if available</item>
/// <item>Falls back to <see cref="TenancyOptions.DefaultSchemaName"/> if not specified</item>
/// </list>
/// </para>
/// </remarks>
public sealed class DefaultTenantSchemaConfigurator : ITenantSchemaConfigurator
{
    private readonly TenancyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTenantSchemaConfigurator"/> class.
    /// </summary>
    /// <param name="options">The tenancy options.</param>
    public DefaultTenantSchemaConfigurator(Microsoft.Extensions.Options.IOptions<TenancyOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public void ConfigureSchema(ModelBuilder modelBuilder, TenantInfo? tenantInfo)
    {
        // Only apply schema configuration for SchemaPerTenant strategy
        if (tenantInfo?.Strategy != TenantIsolationStrategy.SchemaPerTenant)
        {
            return;
        }

        var schemaName = GetSchemaName(tenantInfo);

        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return;
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Only configure tenant entities
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var tableName = entityType.GetTableName();

            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType).ToTable(tableName, schemaName);
        }
    }

    private string GetSchemaName(TenantInfo tenantInfo)
    {
        // Use tenant-specific schema if available
        if (!string.IsNullOrWhiteSpace(tenantInfo.SchemaName))
        {
            return tenantInfo.SchemaName;
        }

        // Fall back to default schema
        return _options.DefaultSchemaName;
    }
}
