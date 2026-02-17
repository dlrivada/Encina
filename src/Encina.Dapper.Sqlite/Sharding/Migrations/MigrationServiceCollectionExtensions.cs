using Encina.ADO.Sqlite.Sharding.Migrations;
using Encina.Sharding.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Dapper.Sqlite.Sharding.Migrations;

/// <summary>
/// Extension methods for registering Encina Dapper SQLite shard migration services.
/// </summary>
/// <remarks>
/// <para>
/// Dapper delegates migration infrastructure to the ADO.NET SQLite provider since both
/// operate on <see cref="System.Data.IDbConnection"/> via <c>IShardedConnectionFactory</c>.
/// </para>
/// </remarks>
public static class MigrationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina shard migration services for Dapper with SQLite.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following services using the ADO.NET SQLite implementations:
    /// <list type="bullet">
    /// <item><see cref="IMigrationExecutor"/> — executes DDL statements on sharded databases</item>
    /// <item><see cref="IMigrationHistoryStore"/> — tracks applied and rolled-back migrations per shard</item>
    /// <item><see cref="ISchemaIntrospector"/> — compares schemas between shards using sqlite_master</item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires that an <c>IShardedConnectionFactory</c> is already registered, typically
    /// via <c>AddEncinaADOSharding</c> from the ADO.NET provider or the Dapper sharding extensions.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaDapperShardMigration(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IMigrationExecutor, AdoMigrationExecutor>();
        services.TryAddScoped<IMigrationHistoryStore, AdoMigrationHistoryStore>();
        services.TryAddScoped<ISchemaIntrospector, SqliteSchemaIntrospector>();

        return services;
    }
}
