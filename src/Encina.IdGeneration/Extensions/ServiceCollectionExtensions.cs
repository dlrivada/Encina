using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.IdGeneration;

/// <summary>
/// Extension methods for registering Encina ID generation services in a
/// <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="AddEncinaIdGeneration"/> with the fluent options callback to enable
/// only the ID generation strategies your application requires. Each strategy is opt-in;
/// no generators are registered unless explicitly enabled.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaIdGeneration(options =>
/// {
///     options.UseSnowflake(snowflake =>
///     {
///         snowflake.MachineId = 1;
///         snowflake.ClockDriftToleranceMs = 10;
///     });
///     options.UseUlid();
///     options.UseUuidV7();
///     options.UseShardPrefixed(sp =>
///     {
///         sp.Format = ShardPrefixedFormat.Ulid;
///     });
/// });
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina ID generation services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">A callback to configure which ID generation strategies to enable.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.
    /// </exception>
    public static IServiceCollection AddEncinaIdGeneration(
        this IServiceCollection services,
        Action<IdGenerationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new IdGenerationOptions();
        configure(options);

        if (options.SnowflakeEnabled)
        {
            var generator = new SnowflakeIdGenerator(options.SnowflakeOptions);
            services.TryAddSingleton<IIdGenerator<SnowflakeId>>(generator);
            services.TryAddSingleton<IShardedIdGenerator<SnowflakeId>>(generator);
        }

        if (options.UlidEnabled)
        {
            services.TryAddSingleton<IIdGenerator<UlidId>>(new UlidIdGenerator());
        }

        if (options.UuidV7Enabled)
        {
            services.TryAddSingleton<IIdGenerator<UuidV7Id>>(new UuidV7IdGenerator());
        }

        if (options.ShardPrefixedEnabled)
        {
            var generator = new ShardPrefixedIdGenerator(options.ShardPrefixedOptions);
            services.TryAddSingleton<IIdGenerator<ShardPrefixedId>>(generator);
            services.TryAddSingleton<IShardedIdGenerator<ShardPrefixedId>>(generator);
        }

        return services;
    }
}
