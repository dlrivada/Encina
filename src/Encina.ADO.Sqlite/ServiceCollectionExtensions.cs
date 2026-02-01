using System.Data;
using Encina.ADO.Sqlite.Auditing;
using Encina.ADO.Sqlite.BulkOperations;
using Encina.ADO.Sqlite.Health;
using Encina.ADO.Sqlite.Inbox;
using Encina.ADO.Sqlite.Outbox;
using Encina.ADO.Sqlite.Repository;
using Encina.ADO.Sqlite.Sagas;
using Encina.ADO.Sqlite.Scheduling;
using Encina.ADO.Sqlite.UnitOfWork;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging;
using Encina.Messaging.Health;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.ADO.Sqlite;

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

        // Register provider health check if enabled
        if (config.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(config.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, SqliteHealthCheck>();
        }

        return services;
    }

    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQLite connection string (e.g., "Data Source=app.db").</param>
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

        services.TryAddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));

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
    /// Registers a functional repository for an entity type using ADO.NET for SQLite.
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
    /// <para>
    /// Also registers <see cref="IEntityMapping{TEntity, TId}"/> for use with Unit of Work.
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
        var mapping = builder.Build();

        // Register the mapping as a singleton for Unit of Work to resolve
        services.AddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Register the repository with scoped lifetime
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            return new FunctionalRepositoryADO<TEntity, TId>(connection, mapping);
        });

        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a read-only functional repository for an entity type using ADO.NET for SQLite.
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
        var mapping = builder.Build();

        // Register only the read repository with scoped lifetime
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            return new FunctionalRepositoryADO<TEntity, TId>(connection, mapping);
        });

        return services;
    }

    /// <summary>
    /// Adds Encina Unit of Work pattern with ADO.NET for SQLite.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IUnitOfWork"/> implemented by <see cref="UnitOfWorkADO"/>
    /// with scoped lifetime.
    /// </para>
    /// <para>
    /// Requires an <see cref="IDbConnection"/> to be registered in the service collection,
    /// typically via <see cref="AddEncinaADO(IServiceCollection, string, Action{MessagingConfiguration})"/>.
    /// </para>
    /// <para>
    /// Entity mappings must be registered separately using
    /// <see cref="AddEncinaRepository{TEntity, TId}(IServiceCollection, Action{EntityMappingBuilder{TEntity, TId}})"/>
    /// for each entity type used with the Unit of Work.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaADO(connectionString, config => { });
    ///
    /// // Register entity mappings
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =&gt;
    ///     mapping.ToTable("Orders")
    ///            .HasId(o =&gt; o.Id)
    ///            .MapProperty(o =&gt; o.CustomerId));
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
        services.TryAddScoped<IUnitOfWork, UnitOfWorkADO>();

        return services;
    }

    /// <summary>
    /// Registers bulk operations for an entity type using ADO.NET for SQLite.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IBulkOperations{TEntity}"/> implemented by
    /// <see cref="BulkOperationsSqlite{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><see cref="IDbConnection"/> must be registered in DI</description></item>
    /// <item><description><see cref="IEntityMapping{TEntity, TId}"/> must be registered for the entity</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Typically, you register the entity mapping using
    /// <see cref="AddEncinaRepository{TEntity, TId}(IServiceCollection, Action{EntityMappingBuilder{TEntity, TId}})"/>
    /// which automatically registers both the repository and the entity mapping.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register connection and repository
    /// services.AddEncinaADO(connectionString, config => { });
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =&gt;
    ///     mapping.ToTable("Orders")
    ///            .HasId(o =&gt; o.Id)
    ///            .MapProperty(o =&gt; o.CustomerId)
    ///            .MapProperty(o =&gt; o.Total));
    ///
    /// // Register bulk operations
    /// services.AddEncinaBulkOperations&lt;Order, Guid&gt;();
    ///
    /// // Usage in service
    /// public class OrderBatchService(IBulkOperations&lt;Order&gt; bulkOps)
    /// {
    ///     public async Task&lt;Either&lt;EncinaError, int&gt;&gt; ImportOrdersAsync(
    ///         IEnumerable&lt;Order&gt; orders,
    ///         CancellationToken ct)
    ///     {
    ///         var config = BulkConfig.Default with { BatchSize = 5000 };
    ///         return await bulkOps.BulkInsertAsync(orders, config, ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaBulkOperations<TEntity, TId>(
        this IServiceCollection services)
        where TEntity : class, new()
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IBulkOperations<TEntity>, BulkOperationsSqlite<TEntity, TId>>();

        return services;
    }
}
