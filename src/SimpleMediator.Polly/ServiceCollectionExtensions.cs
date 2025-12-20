using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.Polly;

/// <summary>
/// Extension methods for configuring SimpleMediator with Polly resilience patterns.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Polly resilience behaviors (Retry and Circuit Breaker) to the SimpleMediator pipeline.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for fluent chaining.</returns>
    /// <remarks>
    /// This extension registers:
    /// - <see cref="RetryPipelineBehavior{TRequest, TResponse}"/> for automatic retries
    /// - <see cref="CircuitBreakerPipelineBehavior{TRequest, TResponse}"/> for circuit breaker pattern
    ///
    /// Behaviors are only applied when requests are decorated with:
    /// - <see cref="RetryAttribute"/> for retry logic
    /// - <see cref="CircuitBreakerAttribute"/> for circuit breaker logic
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSimpleMediator(config => { });
    /// services.AddSimpleMediatorPolly();
    ///
    /// // Now use attributes on requests:
    /// [Retry(MaxAttempts = 3, BackoffType = BackoffType.Exponential)]
    /// [CircuitBreaker(FailureThreshold = 5, DurationOfBreakSeconds = 60)]
    /// public record CallExternalApiQuery(string Url) : IRequest&lt;ApiResponse&gt;;
    /// </code>
    /// </example>
    public static IServiceCollection AddSimpleMediatorPolly(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register Retry behavior - Use AddTransient (not Try) to add both behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RetryPipelineBehavior<,>));

        // Register Circuit Breaker behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CircuitBreakerPipelineBehavior<,>));

        return services;
    }

    /// <summary>
    /// Adds Polly resilience behaviors with custom configuration action.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Configuration action for Polly options.</param>
    /// <returns>Service collection for fluent chaining.</returns>
    /// <remarks>
    /// This overload allows customizing Polly behavior configuration at registration time.
    /// Currently reserved for future extensibility (e.g., custom retry predicates, logging).
    /// </remarks>
    public static IServiceCollection AddSimpleMediatorPolly(
        this IServiceCollection services,
        Action<SimpleMediatorPollyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SimpleMediatorPollyOptions();
        configure(options);

        // Register options
        services.TryAddSingleton(options);

        // Register behaviors
        return AddSimpleMediatorPolly(services);
    }
}

/// <summary>
/// Configuration options for SimpleMediator.Polly integration.
/// </summary>
/// <remarks>
/// Reserved for future extensibility. Currently, all configuration is done via attributes.
/// </remarks>
public sealed class SimpleMediatorPollyOptions
{
    /// <summary>
    /// Whether to enable telemetry/metrics for Polly policies.
    /// </summary>
    /// <remarks>
    /// When enabled, emits metrics for:
    /// - Retry attempts
    /// - Circuit breaker state changes
    /// - Policy execution duration
    ///
    /// Default: true
    /// </remarks>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Whether to log policy executions.
    /// </summary>
    /// <remarks>
    /// When enabled, logs:
    /// - Retry attempts with delays
    /// - Circuit breaker state transitions
    /// - Policy failures
    ///
    /// Default: true
    /// </remarks>
    public bool EnableLogging { get; set; } = true;
}
