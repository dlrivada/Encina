using System.Data;
using Encina.Compliance.Consent;
using Encina.Compliance.GDPR;
using Encina.Dapper.PostgreSQL.Auditing;
using Encina.Dapper.PostgreSQL.Health;
using Encina.Dapper.PostgreSQL.Inbox;
using Encina.Dapper.PostgreSQL.Outbox;
using Encina.Dapper.PostgreSQL.Repository;
using Encina.Dapper.PostgreSQL.Sagas;
using Encina.Dapper.PostgreSQL.Scheduling;
using Encina.Dapper.PostgreSQL.UnitOfWork;
using Encina.Database;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Encina.Dapper.PostgreSQL;

/// <summary>
/// Extension methods for configuring Encina with Dapper for PostgreSQL.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence.
    /// All patterns are opt-in via configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var config = new MessagingConfiguration();
        configure(config);

        services.AddMessagingServices<
            OutboxStoreDapper,
            OutboxMessageFactory,
            InboxStoreDapper,
            InboxMessageFactory,
            SagaStoreDapper,
            SagaStateFactory,
            ScheduledMessageStoreDapper,
            ScheduledMessageFactory,
            OutboxProcessor>(config);

        // Register audit log store if enabled
        if (config.UseAuditLogStore)
        {
            services.AddScoped<IAuditLogStore, AuditLogStoreDapper>();
        }

        // Register provider health check if enabled
        if (config.ProviderHealthCheck.Enabled)
        {
            // Store options for later use in health check registration
            services.AddSingleton(config.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, PostgreSqlHealthCheck>();
        }

        // Register database health monitor for resilience infrastructure
        // Dapper shares the same underlying connection pool as ADO.NET (Npgsql)
        services.TryAddSingleton<IDatabaseHealthMonitor>(sp =>
            new DapperPostgreSqlDatabaseHealthMonitor(sp));

        // Register consent stores if enabled
        if (config.UseConsent)
        {
            services.TryAddScoped<IConsentStore, Consent.ConsentStoreDapper>();
            services.TryAddScoped<IConsentAuditStore, Consent.ConsentAuditStoreDapper>();
            services.TryAddScoped<IConsentVersionManager, Consent.ConsentVersionManagerDapper>();
        }

        return services;
    }

    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence using a connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create database connections.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddScoped(connectionFactory);
        return services.AddEncinaDapper(configure);
    }

    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence using a connection string.
    /// Creates PostgreSQL connections using Npgsql.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null, empty, or whitespace.</exception>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddEncinaDapper(
            _ => new NpgsqlConnection(connectionString),
            configure);
    }

    /// <summary>
    /// Registers a functional repository for an entity type using Dapper for PostgreSQL.
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
    /// Requires an <see cref="IDbConnection"/> to be registered in the service collection.
    /// </para>
    /// <para>
    /// PostgreSQL-specific: Uses double-quoted identifiers for table and column names.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =>
    ///     mapping.ToTable("orders")
    ///            .HasId(o => o.Id, "id")
    ///            .MapProperty(o => o.CustomerId, "customer_id")
    ///            .MapProperty(o => o.Total, "total")
    ///            .ExcludeFromInsert(o => o.Id));
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build the mapping once at registration time
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register the mapping as singleton (immutable)
        services.AddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register the repository with scoped lifetime, resolving audit dependencies
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var entityMapping = sp.GetRequiredService<IEntityMapping<TEntity, TId>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalRepositoryDapper<TEntity, TId>(
                connection, entityMapping, requestContext, timeProvider);
        });
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a read-only functional repository for an entity type using Dapper for PostgreSQL.
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
    /// Requires an <see cref="IDbConnection"/> to be registered in the service collection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaReadRepository&lt;OrderSummary, Guid&gt;(mapping =>
    ///     mapping.ToTable("vw_order_summaries")
    ///            .HasId(o => o.Id, "id")
    ///            .MapProperty(o => o.CustomerName, "customer_name")
    ///            .MapProperty(o => o.TotalAmount, "total_amount"));
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaReadRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build the mapping once at registration time
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register the mapping as singleton (immutable)
        services.AddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register only the read repository with scoped lifetime
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var entityMapping = sp.GetRequiredService<IEntityMapping<TEntity, TId>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalRepositoryDapper<TEntity, TId>(
                connection, entityMapping, requestContext, timeProvider);
        });

        return services;
    }

    /// <summary>
    /// Adds Encina Unit of Work pattern with Dapper for PostgreSQL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IUnitOfWork"/> implemented by <see cref="UnitOfWorkDapper"/>
    /// with scoped lifetime.
    /// </para>
    /// <para>
    /// Requires an <see cref="IDbConnection"/> to be registered in the service collection,
    /// typically via <see cref="AddEncinaDapper(IServiceCollection, string, Action{MessagingConfiguration})"/>.
    /// </para>
    /// <para>
    /// Entity mappings must be registered separately using
    /// <see cref="AddEncinaRepository{TEntity, TId}(IServiceCollection, Action{EntityMappingBuilder{TEntity, TId}})"/>
    /// for each entity type used with the Unit of Work.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDapper(connectionString, config => { });
    ///
    /// // Register entity mappings
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =&gt;
    ///     mapping.ToTable("orders")
    ///            .HasId(o =&gt; o.Id, "id")
    ///            .MapProperty(o =&gt; o.CustomerId, "customer_id"));
    ///
    /// // Add Unit of Work
    /// services.AddEncinaUnitOfWork();
    ///
    /// // Usage in handler
    /// public class TransferHandler(IUnitOfWork unitOfWork)
    /// {
    ///     public async Task HandleAsync(TransferCommand cmd, CancellationToken ct)
    ///     {
    ///         await unitOfWork.BeginTransactionAsync(ct);
    ///         var accounts = unitOfWork.Repository&lt;Account, Guid&gt;();
    ///         // ... operations ...
    ///         await unitOfWork.CommitAsync(ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaUnitOfWork(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Only register if not already registered
        services.TryAddScoped<IUnitOfWork, UnitOfWorkDapper>();

        return services;
    }

    /// <summary>
    /// Adds GDPR Lawful Basis persistent stores using Dapper for PostgreSQL.
    /// Registers <see cref="ILawfulBasisRegistry"/> and <see cref="ILIAStore"/> as singletons
    /// that create connections per operation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaLawfulBasisDapperPostgreSQL(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton<ILawfulBasisRegistry>(
            new LawfulBasis.LawfulBasisRegistryDapper(connectionString));
        services.TryAddSingleton<ILIAStore>(
            new LawfulBasis.LIAStoreDapper(connectionString));

        return services;
    }
}
