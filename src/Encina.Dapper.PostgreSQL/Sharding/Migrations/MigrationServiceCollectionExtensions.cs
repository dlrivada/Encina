using Encina.ADO.PostgreSQL.Sharding.Migrations;
using Encina.Sharding.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Dapper.PostgreSQL.Sharding.Migrations;

/// <summary>
/// Extension methods for registering Encina Dapper PostgreSQL shard migration services.
/// </summary>
/// <remarks>
/// <para>
/// Dapper delegates migration infrastructure to the ADO.NET PostgreSQL provider since both
/// operate on <see cref="System.Data.IDbConnection"/> via <c>IShardedConnectionFactory</c>.
/// </para>
/// </remarks>
public static class MigrationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina shard migration services for Dapper with PostgreSQL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following services using the ADO.NET PostgreSQL implementations:
    /// <list type="bullet">
    /// <item><see cref="IMigrationExecutor"/> — executes DDL statements on sharded databases</item>
    /// <item><see cref="IMigrationHistoryStore"/> — tracks applied and rolled-back migrations per shard</item>
    /// <item><see cref="ISchemaIntrospector"/> — compares schemas between shards using information_schema</item>
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
        services.TryAddScoped<ISchemaIntrospector, PostgreSqlSchemaIntrospector>();

        return services;
    }
}
