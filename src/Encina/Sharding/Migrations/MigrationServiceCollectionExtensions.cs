using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Migrations;

/// <summary>
/// Extension methods for registering Encina shard migration coordination services.
/// </summary>
/// <remarks>
/// <para>
/// These methods register the <see cref="IShardedMigrationCoordinator"/> and its dependencies
/// in the dependency injection container. Provider-specific services (executor, introspector,
/// history store) must be registered separately by the provider's own extension methods.
/// </para>
/// </remarks>
public static class MigrationServiceCollectionExtensions
{
    /// <summary>
    /// Adds shard migration coordination services using a fluent builder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure the migration coordination options via the builder.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><see cref="MigrationCoordinationOptions"/> — global configuration as singleton</item>
    /// <item><see cref="IShardedMigrationCoordinator"/> — the coordinator as scoped</item>
    /// </list>
    /// </para>
    /// <para>
    /// Migration strategies are stateless and used internally by the coordinator; they are not
    /// registered in the container. The coordinator selects the appropriate strategy based on
    /// <see cref="MigrationCoordinationOptions.DefaultStrategy"/> or per-call
    /// <see cref="MigrationOptions.Strategy"/>.
    /// </para>
    /// <para>
    /// Provider-specific services (<see cref="IMigrationExecutor"/>, <see cref="ISchemaIntrospector"/>,
    /// <see cref="IMigrationHistoryStore"/>) are <strong>not</strong> registered by this method.
    /// They must be registered by the provider's own extension methods (e.g.,
    /// <c>AddEncinaADOShardMigration</c>, <c>AddEncinaEFCoreShardMigration</c>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaShardMigrationCoordination(migration =>
    /// {
    ///     migration
    ///         .UseStrategy(MigrationStrategy.RollingUpdate)
    ///         .WithMaxParallelism(8)
    ///         .StopOnFirstFailure()
    ///         .OnShardMigrated((shard, outcome) =>
    ///             logger.LogInformation("Shard {Shard}: {Outcome}", shard, outcome));
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaShardMigrationCoordination(
        this IServiceCollection services,
        Action<MigrationCoordinationBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = new MigrationCoordinationBuilder();
        configure?.Invoke(builder);

        var options = builder.Build();

        services.TryAddSingleton(options);
        services.TryAddSingleton(options.DriftDetection);

        // Register the coordinator as scoped (depends on scoped provider services)
        services.TryAddScoped<IShardedMigrationCoordinator>(sp =>
        {
            var topology = sp.GetRequiredService<ShardTopology>();
            var executor = sp.GetRequiredService<IMigrationExecutor>();
            var introspector = sp.GetRequiredService<ISchemaIntrospector>();
            var historyStore = sp.GetRequiredService<IMigrationHistoryStore>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<ShardedMigrationCoordinator>();

            return new ShardedMigrationCoordinator(topology, executor, introspector, historyStore, logger);
        });

        return services;
    }
}
