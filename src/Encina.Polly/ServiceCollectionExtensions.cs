using Encina.Database;
using Encina.Polly.Predicates;
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
    /// Adds Polly resilience behaviors (Retry, Circuit Breaker, Rate Limiting, and Bulkhead) to the Encina pipeline.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for fluent chaining.</returns>
    /// <remarks>
    /// This extension registers:
    /// - <see cref="BulkheadPipelineBehavior{TRequest, TResponse}"/> for bulkhead isolation
    /// - <see cref="RateLimitingPipelineBehavior{TRequest, TResponse}"/> for adaptive rate limiting
    /// - <see cref="RetryPipelineBehavior{TRequest, TResponse}"/> for automatic retries
    /// - <see cref="CircuitBreakerPipelineBehavior{TRequest, TResponse}"/> for circuit breaker pattern
    ///
    /// Behaviors are only applied when requests are decorated with:
    /// - <see cref="BulkheadAttribute"/> for bulkhead isolation (concurrency limiting)
    /// - <see cref="RateLimitAttribute"/> for rate limiting with adaptive throttling
    /// - <see cref="RetryAttribute"/> for retry logic
    /// - <see cref="CircuitBreakerAttribute"/> for circuit breaker logic
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncina(config => { });
    /// services.AddEncinaPolly();
    ///
    /// // Now use attributes on requests:
    /// [Bulkhead(MaxConcurrency = 10, MaxQueuedActions = 20)]
    /// [Retry(MaxAttempts = 3, BackoffType = BackoffType.Exponential)]
    /// [CircuitBreaker(FailureThreshold = 5, DurationOfBreakSeconds = 60)]
    /// [RateLimit(MaxRequestsPerWindow = 100, WindowSizeSeconds = 60)]
    /// public record CallExternalApiQuery(string Url) : IRequest&lt;ApiResponse&gt;;
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaPolly(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register Bulkhead Manager as singleton (shared state across requests)
        services.TryAddSingleton<IBulkheadManager, BulkheadManager>();

        // Register Adaptive Rate Limiter as singleton (shared state across requests)
        services.TryAddSingleton<IRateLimiter, AdaptiveRateLimiter>();

        // Register Bulkhead behavior (first in pipeline to limit concurrency early)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(BulkheadPipelineBehavior<,>));

        // Register Rate Limiting behavior (second to reject early based on rate)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RateLimitingPipelineBehavior<,>));

        // Register Retry behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RetryPipelineBehavior<,>));

        // Register Circuit Breaker behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CircuitBreakerPipelineBehavior<,>));

        return services;
    }

    /// <summary>
    /// Adds database-aware circuit breaker behavior to the Encina pipeline.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the <see cref="DatabaseCircuitBreakerPipelineBehavior{TRequest, TResponse}"/>
    /// with default <see cref="DatabaseCircuitBreakerOptions"/> (50% failure threshold,
    /// 30-second break duration, 10 minimum throughput).
    /// </para>
    /// <para>
    /// This method requires an <see cref="IDatabaseHealthMonitor"/> implementation to be
    /// registered by one of the database provider packages (ADO.NET, Dapper, EF Core, or MongoDB).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaPolly();
    /// services.AddDatabaseCircuitBreaker();
    /// </code>
    /// </example>
    public static IServiceCollection AddDatabaseCircuitBreaker(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddDatabaseCircuitBreaker(services, _ => { });
    }

    /// <summary>
    /// Adds database-aware circuit breaker behavior to the Encina pipeline with custom configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Action to configure <see cref="DatabaseCircuitBreakerOptions"/>.</param>
    /// <returns>Service collection for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the <see cref="DatabaseCircuitBreakerPipelineBehavior{TRequest, TResponse}"/>
    /// along with the <see cref="DatabaseTransientErrorPredicate"/> and configured
    /// <see cref="DatabaseCircuitBreakerOptions"/>.
    /// </para>
    /// <para>
    /// This method requires an <see cref="IDatabaseHealthMonitor"/> implementation to be
    /// registered by one of the database provider packages (ADO.NET, Dapper, EF Core, or MongoDB).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaPolly();
    /// services.AddDatabaseCircuitBreaker(options =>
    /// {
    ///     options.FailureThreshold = 0.3;
    ///     options.BreakDuration = TimeSpan.FromMinutes(1);
    ///     options.MinimumThroughput = 20;
    ///     options.IncludeTimeouts = true;
    ///     options.IncludeConnectionFailures = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddDatabaseCircuitBreaker(
        this IServiceCollection services,
        Action<DatabaseCircuitBreakerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new DatabaseCircuitBreakerOptions();
        configure(options);

        // Register options and predicate as singletons
        services.TryAddSingleton(options);
        services.TryAddSingleton(new DatabaseTransientErrorPredicate(options));

        // Register the database-aware circuit breaker behavior
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(DatabaseCircuitBreakerPipelineBehavior<,>));

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
