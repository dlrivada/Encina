using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Sharding.Resharding;

/// <summary>
/// Extension methods for registering online resharding services in the dependency injection container.
/// </summary>
/// <remarks>
/// <para>
/// This class is used internally by the sharding registration pipeline. When
/// <see cref="Configuration.ShardingOptions{TEntity}.UseResharding"/> is <c>true</c>,
/// <see cref="ShardingServiceCollectionExtensions.AddEncinaSharding{TEntity}"/> calls
/// <see cref="AddResharding"/> to register the resharding orchestrator and state store.
/// </para>
/// </remarks>
internal static class ReshardingServiceCollectionExtensions
{
    /// <summary>
    /// Registers resharding services: <see cref="IReshardingOrchestrator"/>,
    /// <see cref="IReshardingStateStore"/>, and <see cref="ReshardingOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="builder">The resharding builder with configuration.</param>
    internal static void AddResharding(
        IServiceCollection services,
        ReshardingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Build();

        ValidateOptions(options);

        // Register options as singleton
        services.AddSingleton(options);

        // Register IReshardingStateStore — consumers must provide their own implementation.
        // TryAdd ensures it won't overwrite a user-registered implementation.
        // No default in-memory implementation is registered because resharding state
        // must survive process restarts for crash recovery.

        // Register IReshardingOrchestrator.
        // TryAdd ensures it won't overwrite a user-registered implementation.
        services.TryAddSingleton<IReshardingOrchestrator, ReshardingOrchestrator>();
    }

    private static void ValidateOptions(ReshardingOptions options)
    {
        if (options.CopyBatchSize <= 0)
        {
            throw new InvalidOperationException(
                $"ReshardingOptions.CopyBatchSize must be a positive integer, but was {options.CopyBatchSize}.");
        }

        if (options.CdcLagThreshold <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"ReshardingOptions.CdcLagThreshold must be a positive duration, but was {options.CdcLagThreshold}.");
        }

        if (options.CutoverTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"ReshardingOptions.CutoverTimeout must be a positive duration, but was {options.CutoverTimeout}.");
        }

        if (options.CleanupRetentionPeriod <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"ReshardingOptions.CleanupRetentionPeriod must be a positive duration, but was {options.CleanupRetentionPeriod}.");
        }
    }
}
