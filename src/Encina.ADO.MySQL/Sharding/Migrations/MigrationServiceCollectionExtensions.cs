using Encina.Sharding.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.ADO.MySQL.Sharding.Migrations;

/// <summary>
/// Extension methods for registering Encina ADO.NET MySQL shard migration services.
/// </summary>
public static class MigrationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina shard migration services for ADO.NET with MySQL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// <list type="bullet">
    /// <item><see cref="IMigrationExecutor"/> — executes DDL statements on sharded databases</item>
    /// <item><see cref="IMigrationHistoryStore"/> — tracks applied and rolled-back migrations per shard</item>
    /// <item><see cref="ISchemaIntrospector"/> — compares schemas between shards using INFORMATION_SCHEMA</item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires that an <c>IShardedConnectionFactory</c> is already registered, typically
    /// via <c>AddEncinaADOSharding</c> from the ADO.NET provider.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaADOShardMigration(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IMigrationExecutor, AdoMigrationExecutor>();
        services.TryAddScoped<IMigrationHistoryStore, AdoMigrationHistoryStore>();
        services.TryAddScoped<ISchemaIntrospector, MySqlSchemaIntrospector>();

        return services;
    }
}
