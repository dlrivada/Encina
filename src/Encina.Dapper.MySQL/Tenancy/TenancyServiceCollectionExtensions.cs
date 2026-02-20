using System.Data;
using Encina.Dapper.MySQL.Repository;
using Encina.DomainModeling;
using Encina.Messaging;
using Encina.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Dapper.MySQL.Tenancy;

/// <summary>
/// Extension methods for configuring Encina Dapper with multi-tenancy support for MySQL.
/// </summary>
public static class TenancyServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence and multi-tenancy support for MySQL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <param name="configureTenancy">Configuration action for tenancy options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers all necessary services for multi-tenant Dapper operations:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="ITenantConnectionFactory"/> for tenant-aware connection routing</item>
    /// <item><see cref="DapperTenancyOptions"/> for configuring tenant filtering behavior</item>
    /// <item>Standard Dapper messaging stores (Outbox, Inbox, Saga, Scheduling)</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDapperWithTenancy(
    ///     config =>
    ///     {
    ///         config.UseOutbox = true;
    ///         config.UseInbox = true;
    ///     },
    ///     tenancy =>
    ///     {
    ///         tenancy.AutoFilterTenantQueries = true;
    ///         tenancy.AutoAssignTenantId = true;
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDapperWithTenancy(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure,
        Action<DapperTenancyOptions>? configureTenancy = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Configure Dapper tenancy options
        var tenancyOptions = new DapperTenancyOptions();
        configureTenancy?.Invoke(tenancyOptions);
        services.AddSingleton(Options.Create(tenancyOptions));

        // Register tenant connection factory
        services.TryAddScoped<ITenantConnectionFactory, TenantConnectionFactory>();

        // Register IDbConnection using tenant connection factory
        services.AddScoped<IDbConnection>(sp =>
        {
            var factory = sp.GetRequiredService<ITenantConnectionFactory>();
            // Note: This is synchronous for DI, but the factory can be used directly for async
            return factory.CreateConnectionAsync().AsTask().GetAwaiter().GetResult();
        });

        // Add standard Dapper messaging services
        return services.AddEncinaDapper(configure);
    }

    /// <summary>
    /// Registers a tenant-aware functional repository for an entity type using Dapper for MySQL.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for tenant entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IFunctionalRepository{TEntity, TId}"/> with tenant-aware
    /// filtering, assignment, and validation capabilities.
    /// </para>
    /// <para>
    /// Requires:
    /// <list type="bullet">
    /// <item><see cref="IDbConnection"/> to be registered</item>
    /// <item><see cref="ITenantProvider"/> to be registered (from Encina.Tenancy)</item>
    /// <item><see cref="DapperTenancyOptions"/> to be registered (via <see cref="AddEncinaDapperWithTenancy"/>)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTenantAwareRepository&lt;Order, Guid&gt;(mapping =&gt;
    ///     mapping.ToTable("Orders")
    ///            .HasId(o =&gt; o.Id)
    ///            .HasTenantId(o =&gt; o.TenantId)
    ///            .MapProperty(o =&gt; o.CustomerId)
    ///            .MapProperty(o =&gt; o.Total)
    ///            .ExcludeFromInsert(o =&gt; o.Id));
    /// </code>
    /// </example>
    public static IServiceCollection AddTenantAwareRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<TenantEntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build the mapping once at registration time
        var builder = new TenantEntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register the mapping as singleton (immutable)
        services.AddSingleton<ITenantEntityMapping<TEntity, TId>>(mapping);

        // Also register as IEntityMapping for compatibility
        services.AddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Register the tenant-aware repository with scoped lifetime
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            var options = sp.GetRequiredService<IOptions<DapperTenancyOptions>>().Value;
            var entityMapping = sp.GetRequiredService<ITenantEntityMapping<TEntity, TId>>();

            return new TenantAwareFunctionalRepositoryDapper<TEntity, TId>(
                connection,
                entityMapping,
                tenantProvider,
                options);
        });

        // Register read repository interface
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a read-only tenant-aware repository for an entity type using Dapper for MySQL.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for tenant entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Only registers <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// Use this for read-only scenarios where write operations are not needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTenantAwareReadRepository&lt;OrderSummary, Guid&gt;(mapping =&gt;
    ///     mapping.ToTable("vw_OrderSummaries")
    ///            .HasId(o =&gt; o.Id)
    ///            .HasTenantId(o =&gt; o.TenantId)
    ///            .MapProperty(o =&gt; o.CustomerName)
    ///            .MapProperty(o =&gt; o.TotalAmount));
    /// </code>
    /// </example>
    public static IServiceCollection AddTenantAwareReadRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<TenantEntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build the mapping once at registration time
        var builder = new TenantEntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register the mapping as singleton (immutable)
        services.AddSingleton<ITenantEntityMapping<TEntity, TId>>(mapping);

        // Register only the read repository with scoped lifetime
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            var options = sp.GetRequiredService<IOptions<DapperTenancyOptions>>().Value;
            var entityMapping = sp.GetRequiredService<ITenantEntityMapping<TEntity, TId>>();

            return new TenantAwareFunctionalRepositoryDapper<TEntity, TId>(
                connection,
                entityMapping,
                tenantProvider,
                options);
        });

        return services;
    }
}
