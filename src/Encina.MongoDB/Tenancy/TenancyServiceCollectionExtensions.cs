using Encina.DomainModeling;
using Encina.MongoDB.Repository;
using Encina.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Tenancy;

/// <summary>
/// Extension methods for configuring Encina MongoDB tenancy services.
/// </summary>
public static class TenancyServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina MongoDB integration with multi-tenancy support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureMongoDB">Configuration action for MongoDB options.</param>
    /// <param name="configureTenancy">Optional configuration action for tenancy options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers all necessary services for multi-tenant MongoDB support:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="IMongoCollectionFactory"/> for tenant-aware collection routing</item>
    /// <item><see cref="MongoDbTenancyOptions"/> for tenancy configuration</item>
    /// </list>
    /// <para>
    /// Requires <see cref="ITenantProvider"/> and <see cref="ITenantStore"/> to be
    /// registered, typically via the Encina.Tenancy package.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaMongoDBWithTenancy(
    ///     config =>
    ///     {
    ///         config.ConnectionString = "mongodb://localhost:27017";
    ///         config.DatabaseName = "MyApp";
    ///         config.UseOutbox = true;
    ///     },
    ///     tenancy =>
    ///     {
    ///         tenancy.AutoFilterTenantQueries = true;
    ///         tenancy.EnableDatabasePerTenant = false;
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaMongoDBWithTenancy(
        this IServiceCollection services,
        Action<EncinaMongoDbOptions> configureMongoDB,
        Action<MongoDbTenancyOptions>? configureTenancy = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureMongoDB);

        // Register base MongoDB services
        services.AddEncinaMongoDB(configureMongoDB);

        // Configure tenancy options
        var tenancyOptions = new MongoDbTenancyOptions();
        configureTenancy?.Invoke(tenancyOptions);

        services.Configure<MongoDbTenancyOptions>(options =>
        {
            options.AutoFilterTenantQueries = tenancyOptions.AutoFilterTenantQueries;
            options.AutoAssignTenantId = tenancyOptions.AutoAssignTenantId;
            options.ValidateTenantOnModify = tenancyOptions.ValidateTenantOnModify;
            options.ThrowOnMissingTenantContext = tenancyOptions.ThrowOnMissingTenantContext;
            options.TenantFieldName = tenancyOptions.TenantFieldName;
            options.EnableDatabasePerTenant = tenancyOptions.EnableDatabasePerTenant;
            options.DatabaseNamePattern = tenancyOptions.DatabaseNamePattern;
        });

        // Register the collection factory
        services.TryAddScoped<IMongoCollectionFactory, TenantAwareMongoCollectionFactory>();

        return services;
    }

    /// <summary>
    /// Registers a tenant-aware functional repository for an entity type using MongoDB.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for the entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IFunctionalRepository{TEntity, TId}"/> and
    /// <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// The repository automatically:
    /// <list type="bullet">
    /// <item>Filters queries by tenant ID</item>
    /// <item>Assigns tenant ID on insert</item>
    /// <item>Validates tenant ownership on update/delete</item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires <see cref="AddEncinaMongoDBWithTenancy"/> to be called first.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTenantAwareRepository&lt;Order, Guid&gt;(mapping =>
    ///     mapping.ToCollection("orders")
    ///            .HasId(o => o.Id)
    ///            .HasTenantId(o => o.TenantId)
    ///            .MapField(o => o.CustomerId)
    ///            .MapField(o => o.Total));
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

        // Build mapping
        var builder = new TenantEntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build();

        // Register the mapping
        services.AddSingleton<ITenantEntityMapping<TEntity, TId>>(mapping);

        // Register the collection as scoped (uses factory for tenant routing)
        services.AddScoped<IMongoCollection<TEntity>>(sp =>
        {
            // Note: GetCollectionAsync is async but we need sync here
            // For DI resolution, we use the simpler approach of getting the collection synchronously
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var mongoOptions = sp.GetRequiredService<IOptions<EncinaMongoDbOptions>>().Value;
            var tenancyOptions = sp.GetRequiredService<IOptions<MongoDbTenancyOptions>>().Value;
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            var tenantStore = sp.GetRequiredService<ITenantStore>();

            // Determine database name synchronously
            var databaseName = GetDatabaseNameSync(
                tenantProvider,
                tenantStore,
                mongoOptions,
                tenancyOptions);

            var database = mongoClient.GetDatabase(databaseName);
            return database.GetCollection<TEntity>(mapping.CollectionName);
        });

        // Register the repository
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var collection = sp.GetRequiredService<IMongoCollection<TEntity>>();
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            var tenancyOptions = sp.GetRequiredService<IOptions<MongoDbTenancyOptions>>().Value;
            var entityMapping = sp.GetRequiredService<ITenantEntityMapping<TEntity, TId>>();

            // Create ID selector expression from mapping
            var idSelector = CreateIdSelector<TEntity, TId>(entityMapping);

            return new TenantAwareFunctionalRepositoryMongoDB<TEntity, TId>(
                collection,
                entityMapping,
                tenantProvider,
                tenancyOptions,
                idSelector);
        });

        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a tenant-aware read-only repository for an entity type using MongoDB.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for the entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Only registers <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// Use this for read-only scenarios where write operations are not needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTenantAwareReadRepository&lt;OrderSummary, Guid&gt;(mapping =>
    ///     mapping.ToCollection("order_summaries")
    ///            .HasId(o => o.Id)
    ///            .HasTenantId(o => o.TenantId)
    ///            .MapField(o => o.Total));
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

        // Build mapping
        var builder = new TenantEntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build();

        // Register the mapping
        services.TryAddSingleton<ITenantEntityMapping<TEntity, TId>>(mapping);

        // Register the collection as scoped
        services.TryAddScoped<IMongoCollection<TEntity>>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var mongoOptions = sp.GetRequiredService<IOptions<EncinaMongoDbOptions>>().Value;
            var tenancyOptions = sp.GetRequiredService<IOptions<MongoDbTenancyOptions>>().Value;
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            var tenantStore = sp.GetRequiredService<ITenantStore>();

            var databaseName = GetDatabaseNameSync(
                tenantProvider,
                tenantStore,
                mongoOptions,
                tenancyOptions);

            var database = mongoClient.GetDatabase(databaseName);
            return database.GetCollection<TEntity>(mapping.CollectionName);
        });

        // Register only the read repository
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
        {
            var collection = sp.GetRequiredService<IMongoCollection<TEntity>>();
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            var tenancyOptions = sp.GetRequiredService<IOptions<MongoDbTenancyOptions>>().Value;
            var entityMapping = sp.GetRequiredService<ITenantEntityMapping<TEntity, TId>>();

            var idSelector = CreateIdSelector<TEntity, TId>(entityMapping);

            return new TenantAwareFunctionalRepositoryMongoDB<TEntity, TId>(
                collection,
                entityMapping,
                tenantProvider,
                tenancyOptions,
                idSelector);
        });

        return services;
    }

    private static string GetDatabaseNameSync(
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        EncinaMongoDbOptions mongoOptions,
        MongoDbTenancyOptions tenancyOptions)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();

        // No tenant context or database-per-tenant disabled - use default
        if (string.IsNullOrEmpty(tenantId) || !tenancyOptions.EnableDatabasePerTenant)
        {
            return mongoOptions.DatabaseName;
        }

        // Get tenant info synchronously (blocking call for DI resolution)
        var tenant = tenantStore.GetTenantAsync(tenantId, CancellationToken.None)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        if (tenant is null || tenant.Strategy != TenantIsolationStrategy.DatabasePerTenant)
        {
            return mongoOptions.DatabaseName;
        }

        return tenancyOptions.GetDatabaseName(mongoOptions.DatabaseName, tenantId);
    }

    private static System.Linq.Expressions.Expression<Func<TEntity, TId>> CreateIdSelector<TEntity, TId>(
        ITenantEntityMapping<TEntity, TId> mapping)
        where TEntity : class
        where TId : notnull
    {
        // Create a simple ID selector based on the mapping
        // This assumes there's a property that matches the ID field name
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "x");

        // Find the property that maps to the ID field
        var idPropertyName = mapping.FieldMappings
            .FirstOrDefault(kvp => kvp.Value == mapping.IdFieldName).Key;

        if (string.IsNullOrEmpty(idPropertyName))
        {
            // Fallback: look for common ID property names
            var idProperty = typeof(TEntity).GetProperty("Id")
                ?? typeof(TEntity).GetProperty("ID")
                ?? typeof(TEntity).GetProperty(typeof(TEntity).Name + "Id");

            if (idProperty is null)
            {
                throw new InvalidOperationException(
                    $"Cannot determine ID property for entity {typeof(TEntity).Name}. " +
                    $"Ensure the mapping includes an ID property that maps to '{mapping.IdFieldName}'.");
            }

            idPropertyName = idProperty.Name;
        }

        var property = typeof(TEntity).GetProperty(idPropertyName)
            ?? throw new InvalidOperationException(
                $"Property '{idPropertyName}' not found on entity {typeof(TEntity).Name}.");

        var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, property);
        var converted = System.Linq.Expressions.Expression.Convert(propertyAccess, typeof(TId));

        return System.Linq.Expressions.Expression.Lambda<Func<TEntity, TId>>(converted, parameter);
    }
}
