using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Polly;

/// <summary>
/// Extension methods for configuring Encina with Polly resilience patterns.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Polly resilience behaviors (Retry, Circuit Breaker, and Rate Limiting) to the Encina pipeline.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for fluent chaining.</returns>
    /// <remarks>
    /// This extension registers:
    /// - <see cref="RetryPipelineBehavior{TRequest, TResponse}"/> for automatic retries
    /// - <see cref="CircuitBreakerPipelineBehavior{TRequest, TResponse}"/> for circuit breaker pattern
    /// - <see cref="RateLimitingPipelineBehavior{TRequest, TResponse}"/> for adaptive rate limiting
    ///
    /// Behaviors are only applied when requests are decorated with:
    /// - <see cref="RetryAttribute"/> for retry logic
    /// - <see cref="CircuitBreakerAttribute"/> for circuit breaker logic
    /// - <see cref="RateLimitAttribute"/> for rate limiting with adaptive throttling
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncina(config => { });
    /// services.AddEncinaPolly();
    ///
    /// // Now use attributes on requests:
    /// [Retry(MaxAttempts = 3, BackoffType = BackoffType.Exponential)]
    /// [CircuitBreaker(FailureThreshold = 5, DurationOfBreakSeconds = 60)]
    /// [RateLimit(MaxRequestsPerWindow = 100, WindowSizeSeconds = 60)]
    /// public record CallExternalApiQuery(string Url) : IRequest&lt;ApiResponse&gt;;
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaPolly(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register Adaptive Rate Limiter as singleton (shared state across requests)
        services.TryAddSingleton<IRateLimiter, AdaptiveRateLimiter>();

        // Register Rate Limiting behavior (first in pipeline to reject early)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RateLimitingPipelineBehavior<,>));

        // Register Retry behavior
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
    public static IServiceCollection AddEncinaPolly(
        this IServiceCollection services,
        Action<EncinaPollyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EncinaPollyOptions();
        configure(options);

        // Register options
        services.TryAddSingleton(options);

        // Register behaviors
        return AddEncinaPolly(services);
    }
}

/// <summary>
/// Configuration options for Encina.Polly integration.
/// </summary>
/// <remarks>
/// Reserved for future extensibility. Currently, all configuration is done via attributes.
/// </remarks>
public sealed class EncinaPollyOptions
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
