using System.Data;
using Encina.Dapper.Sqlite.BulkOperations;
using Encina.Dapper.Sqlite.Health;
using Encina.Dapper.Sqlite.Inbox;
using Encina.Dapper.Sqlite.Outbox;
using Encina.Dapper.Sqlite.Repository;
using Encina.Dapper.Sqlite.Sagas;
using Encina.Dapper.Sqlite.Scheduling;
using Encina.Dapper.Sqlite.UnitOfWork;
using Encina.DomainModeling;
using Encina.Messaging;
using Encina.Messaging.Health;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Dapper.Sqlite;

/// <summary>
/// Extension methods for configuring Encina with Dapper for SQLite.
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

        // Register provider health check if enabled
        if (config.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(config.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, SqliteHealthCheck>();
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="connectionFactory"/>, or <paramref name="configure"/> is null.</exception>
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
    /// Creates SQLite connections by default.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string (e.g., "Data Source=app.db").</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="connectionString"/>, or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddEncinaDapper(
            _ => new SqliteConnection(connectionString),
            configure);
    }

    /// <summary>
    /// Registers a functional repository for an entity type using Dapper.
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
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =>
    ///     mapping.ToTable("Orders")
    ///            .HasId(o => o.Id)
    ///            .MapProperty(o => o.CustomerId)
    ///            .MapProperty(o => o.Total)
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
        var mapping = builder.Build();

        // Register the mapping as singleton (immutable)
        services.AddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Register the repository with scoped lifetime
        services.AddScoped<IFunctionalRepository<TEntity, TId>, FunctionalRepositoryDapper<TEntity, TId>>();
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a read-only functional repository for an entity type using Dapper.
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
    ///     mapping.ToTable("vw_OrderSummaries")
    ///            .HasId(o => o.Id)
    ///            .MapProperty(o => o.CustomerName)
    ///            .MapProperty(o => o.TotalAmount));
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
        var mapping = builder.Build();

        // Register the mapping as singleton (immutable)
        services.AddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Register only the read repository with scoped lifetime
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>, FunctionalRepositoryDapper<TEntity, TId>>();

        return services;
    }

    /// <summary>
    /// Adds Encina Unit of Work pattern with Dapper for SQLite.
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
        services.TryAddScoped<IUnitOfWork, UnitOfWorkDapper>();

        return services;
    }

    /// <summary>
    /// Registers bulk operations for an entity type using Dapper.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IBulkOperations{TEntity}"/> implemented by
    /// <see cref="BulkOperationsDapper{TEntity, TId}"/> with scoped lifetime.
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
    /// services.AddEncinaDapper(connectionString, config => { });
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

        services.TryAddScoped<IBulkOperations<TEntity>, BulkOperationsDapper<TEntity, TId>>();

        return services;
    }
}
