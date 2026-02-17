using Encina.Sharding.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.MongoDB.Sharding.Migrations;

/// <summary>
/// Extension methods for registering Encina MongoDB shard migration services.
/// </summary>
public static class MigrationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina shard migration services for MongoDB.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// <list type="bullet">
    /// <item><see cref="IMigrationExecutor"/> — executes index and validator commands on sharded databases</item>
    /// <item><see cref="IMigrationHistoryStore"/> — tracks applied and rolled-back migrations per shard
    /// in a <c>__shard_migration_history</c> collection</item>
    /// <item><see cref="ISchemaIntrospector"/> — compares collection schemas between shards using
    /// <c>listCollections</c> command</item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires that an <c>IShardedMongoCollectionFactory</c> is already registered, typically
    /// via <c>AddEncinaMongoSharding</c> from the MongoDB provider.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaMongoShardMigration(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IMigrationExecutor, MongoMigrationExecutor>();
        services.TryAddScoped<IMigrationHistoryStore, MongoMigrationHistoryStore>();
        services.TryAddScoped<ISchemaIntrospector, MongoSchemaIntrospector>();

        return services;
    }
}
