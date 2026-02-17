using Encina.EntityFrameworkCore.Sharding.Migrations;
using Encina.Sharding.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.EntityFrameworkCore.Sharding.Migrations;

/// <summary>
/// Extension methods for registering Encina EF Core shard migration services.
/// </summary>
/// <remarks>
/// <para>
/// These methods register the EF Core-specific migration infrastructure
/// (<see cref="IMigrationExecutor"/>, <see cref="IMigrationHistoryStore"/>,
/// <see cref="ISchemaIntrospector"/>) backed by Entity Framework Core.
/// </para>
/// <para>
/// The <see cref="AddEncinaEFCoreShardMigration{TContext}"/> method should be called
/// after <c>AddEncinaShardMigrationCoordination</c> from the core package.
/// </para>
/// </remarks>
public static class MigrationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina shard migration services for EF Core with the specified DbContext type.
    /// </summary>
    /// <typeparam name="TContext">
    /// The <see cref="DbContext"/> type used for shard connections. This context must be
    /// resolvable from the service provider for each shard.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for EF Core-specific migration options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// <list type="bullet">
    /// <item><see cref="IMigrationExecutor"/> — executes DDL statements via EF Core's
    /// <c>Database.ExecuteSqlRaw</c></item>
    /// <item><see cref="IMigrationHistoryStore"/> — tracks applied and rolled-back migrations
    /// using EF Core's database access</item>
    /// <item><see cref="ISchemaIntrospector"/> — compares schemas between shards using
    /// EF Core's <c>Database.SqlQueryRaw</c></item>
    /// <item><see cref="EfCoreMigrationOptions"/> — EF Core-specific configuration as singleton</item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires that an <c>IShardedDbContextFactory&lt;TContext&gt;</c> is already registered,
    /// typically via the EF Core sharding extensions (e.g., <c>AddEncinaEFCoreShardingSqlServer</c>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register EF Core migration services with custom options
    /// services.AddEncinaEFCoreShardMigration&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.UseEfCoreMigrate = true;
    ///     options.HistoryTableName = "__ShardMigrationHistory";
    ///     options.CommandTimeoutSeconds = 600;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaEFCoreShardMigration<TContext>(
        this IServiceCollection services,
        Action<EfCoreMigrationOptions>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EfCoreMigrationOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);

        services.TryAddScoped<IMigrationExecutor, EfCoreMigrationExecutor<TContext>>();
        services.TryAddScoped<IMigrationHistoryStore, EfCoreMigrationHistoryStore<TContext>>();
        services.TryAddScoped<ISchemaIntrospector, EfCoreSchemaIntrospector<TContext>>();

        return services;
    }
}
