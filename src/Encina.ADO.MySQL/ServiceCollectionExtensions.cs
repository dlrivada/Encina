using System.Data;
using Encina.ADO.MySQL.Auditing;
using Encina.ADO.MySQL.Health;
using Encina.ADO.MySQL.Inbox;
using Encina.ADO.MySQL.Outbox;
using Encina.ADO.MySQL.Repository;
using Encina.ADO.MySQL.Sagas;
using Encina.ADO.MySQL.Scheduling;
using Encina.Compliance.Anonymization;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DPIA;
using Encina.Compliance.GDPR;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.Retention;
using Encina.Database;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Security.ABAC.Persistence;
using Encina.Security.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;

namespace Encina.ADO.MySQL;

/// <summary>
/// Extension methods for configuring Encina with ADO.NET provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a registered IDbConnection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var config = new MessagingConfiguration();
        configure(config);

        services.AddMessagingServices<
            OutboxStoreADO,
            OutboxMessageFactory,
            InboxStoreADO,
            InboxMessageFactory,
            SagaStoreADO,
            SagaStateFactory,
            ScheduledMessageStoreADO,
            ScheduledMessageFactory,
            OutboxProcessor>(config);

        // Register audit log store if enabled
        if (config.UseAuditLogStore)
        {
            services.AddScoped<IAuditLogStore, AuditLogStoreADO>();
        }

        // Register read audit store if enabled
        if (config.UseReadAuditStore)
        {
            services.TryAddScoped<IReadAuditStore, Auditing.ReadAuditStoreADO>();
        }

        // Register Anonymization token mapping store if enabled
        if (config.UseAnonymization)
        {
            services.TryAddScoped<ITokenMappingStore, Anonymization.TokenMappingStoreADO>();
        }

        // Register Retention stores if enabled
        if (config.UseRetention)
        {
            services.TryAddScoped<IRetentionPolicyStore, Retention.RetentionPolicyStoreADO>();
            services.TryAddScoped<IRetentionRecordStore, Retention.RetentionRecordStoreADO>();
            services.TryAddScoped<ILegalHoldStore, Retention.LegalHoldStoreADO>();
            services.TryAddScoped<IRetentionAuditStore, Retention.RetentionAuditStoreADO>();
        }

        // Register Data Residency stores if enabled
        if (config.UseDataResidency)
        {
            services.TryAddScoped<IDataLocationStore, DataResidency.DataLocationStoreADO>();
            services.TryAddScoped<IResidencyPolicyStore, DataResidency.ResidencyPolicyStoreADO>();
            services.TryAddScoped<IResidencyAuditStore, DataResidency.ResidencyAuditStoreADO>();
        }

        // Register Breach Notification stores if enabled
        if (config.UseBreachNotification)
        {
            services.TryAddScoped<IBreachRecordStore, BreachNotification.BreachRecordStoreADO>();
            services.TryAddScoped<IBreachAuditStore, BreachNotification.BreachAuditStoreADO>();
        }

        if (config.UseDPIA)
        {
            services.TryAddScoped<IDPIAStore, DPIA.DPIAStoreADO>();
            services.TryAddScoped<IDPIAAuditStore, DPIA.DPIAAuditStoreADO>();
        }

        if (config.UseProcessorAgreements)
        {
            services.TryAddScoped<IProcessorRegistry, ProcessorAgreements.ProcessorRegistryADO>();
            services.TryAddScoped<IDPAStore, ProcessorAgreements.DPAStoreADO>();
            services.TryAddScoped<IProcessorAuditStore, ProcessorAgreements.ProcessorAuditStoreADO>();
        }

        // Register ABAC Policy Store if enabled
        if (config.UseABACPolicyStore)
        {
            services.TryAddScoped<IPolicyStore, ABAC.PolicyStoreADO>();
        }

        // Register provider health check if enabled
        if (config.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(config.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, MySqlHealthCheck>();
        }

        // Register database health monitor for resilience infrastructure
        services.TryAddSingleton<IDatabaseHealthMonitor>(sp =>
            new MySqlDatabaseHealthMonitor(sp));

        return services;
    }

    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MySQL/MariaDB connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddScoped<IDbConnection>(_ => new MySqlConnection(connectionString));

        return services.AddEncinaADO(configure);
    }

    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a custom connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create IDbConnection instances.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddScoped(connectionFactory);

        return services.AddEncinaADO(configure);
    }

    /// <summary>
    /// Registers a functional repository for an entity type using ADO.NET.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IFunctionalRepository{TEntity, TId}"/> and
    /// <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// Requires <see cref="IDbConnection"/> to be registered, typically via
    /// <see cref="AddEncinaADO(IServiceCollection, string, Action{MessagingConfiguration})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaADO(connectionString, config => { });
    ///
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =&gt;
    /// {
    ///     mapping.ToTable("Orders")
    ///         .HasId(o =&gt; o.Id)
    ///         .MapProperty(o =&gt; o.CustomerId, "CustomerId")
    ///         .MapProperty(o =&gt; o.Total, "Total")
    ///         .MapProperty(o =&gt; o.CreatedAtUtc, "CreatedAtUtc");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class, new()
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build mapping
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configure(builder);
        // ROP boundary: DI configuration errors must prevent application startup per .NET conventions.
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register the repository with scoped lifetime, resolving audit dependencies
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalRepositoryADO<TEntity, TId>(
                connection, mapping, requestContext, timeProvider);
        });

        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a read-only functional repository for an entity type using ADO.NET.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Only registers <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// Use this for read-only scenarios where write operations are not needed.
    /// </para>
    /// <para>
    /// Requires <see cref="IDbConnection"/> to be registered, typically via
    /// <see cref="AddEncinaADO(IServiceCollection, string, Action{MessagingConfiguration})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaReadRepository&lt;OrderSummary, Guid&gt;(mapping =&gt;
    /// {
    ///     mapping.ToTable("OrderSummaries")
    ///         .HasId(o =&gt; o.Id)
    ///         .MapProperty(o =&gt; o.CustomerName, "CustomerName")
    ///         .MapProperty(o =&gt; o.TotalAmount, "TotalAmount");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaReadRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class, new()
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build mapping
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configure(builder);
        // ROP boundary: DI configuration errors must prevent application startup per .NET conventions.
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register only the read repository with scoped lifetime
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalRepositoryADO<TEntity, TId>(
                connection, mapping, requestContext, timeProvider);
        });

        return services;
    }

    /// <summary>
    /// Adds persistent GDPR processing activity registry using ADO.NET for MySQL/MariaDB.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IProcessingActivityRegistry"/> as a singleton backed by a MySQL
    /// <c>ProcessingActivities</c> table. Each operation creates a new connection, ensuring thread safety.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MySQL/MariaDB connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaProcessingActivityADOMySQL(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton<IProcessingActivityRegistry>(
            new ProcessingActivity.ProcessingActivityRegistryADO(connectionString));

        return services;
    }
}
