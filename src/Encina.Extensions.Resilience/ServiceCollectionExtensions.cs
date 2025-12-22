using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;

namespace Encina.Extensions.Resilience;

/// <summary>
/// Extension methods for configuring Encina with Microsoft.Extensions.Resilience.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string DefaultPipelineKey = "Encina.StandardResilience";

    /// <summary>
    /// Adds Microsoft Standard Resilience Handler to Encina pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Configures the standard resilience pipeline with default settings:
    /// - Rate limiter: 1,000 permits
    /// - Total timeout: 30 seconds
    /// - Retry: 3 attempts with exponential backoff
    /// - Circuit breaker: 10% failure threshold
    /// - Attempt timeout: 10 seconds per attempt
    ///
    /// Use the overload with options to customize these settings.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncina()
    ///     .AddEncinaStandardResilience();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaStandardResilience(
        this IServiceCollection services)
    {
        return services.AddEncinaStandardResilience(_ => { });
    }

    /// <summary>
    /// Adds Microsoft Standard Resilience Handler to Encina pipeline with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback for resilience options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina()
    ///     .AddEncinaStandardResilience(options =>
    ///     {
    ///         // Configure retry
    ///         options.Retry.MaxRetryAttempts = 5;
    ///         options.Retry.BackoffType = DelayBackoffType.Exponential;
    ///
    ///         // Configure circuit breaker
    ///         options.CircuitBreaker.FailureRatio = 0.2;
    ///         options.CircuitBreaker.MinimumThroughput = 10;
    ///
    ///         // Configure timeouts
    ///         options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    ///         options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaStandardResilience(
        this IServiceCollection services,
        Action<StandardResilienceOptions> configure)
    {
        var options = new StandardResilienceOptions();
        configure(options);

        // Register the pipeline in a singleton ResiliencePipelineProvider
        services.AddSingleton<ResiliencePipelineProvider<string>>(sp =>
        {
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(DefaultPipelineKey, (builder, context) =>
            {
                builder.AddRateLimiter(options.RateLimiter)
                    .AddTimeout(options.TotalRequestTimeout)
                    .AddRetry(options.Retry)
                    .AddCircuitBreaker(options.CircuitBreaker)
                    .AddTimeout(options.AttemptTimeout);
            });
            return registry;
        });

        // Register the pipeline behavior
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(StandardResiliencePipelineBehavior<,>));

        return services;
    }

    /// <summary>
    /// Adds Microsoft Standard Resilience Handler to Encina pipeline for specific request types.
    /// </summary>
    /// <typeparam name="TRequest">The request type to apply resilience to.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback for resilience options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Apply custom resilience only to specific commands
    /// services.AddEncina()
    ///     .AddEncinaStandardResilienceFor&lt;PlaceOrderCommand, OrderResult&gt;(options =>
    ///     {
    ///         options.Retry.MaxRetryAttempts = 5;
    ///         options.CircuitBreaker.FailureRatio = 0.15;
    ///     })
    ///     .AddEncinaStandardResilienceFor&lt;ProcessPaymentCommand, PaymentResult&gt;(options =>
    ///     {
    ///         options.Retry.MaxRetryAttempts = 3;
    ///         options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaStandardResilienceFor<TRequest, TResponse>(
        this IServiceCollection services,
        Action<StandardResilienceOptions> configure)
        where TRequest : IRequest<TResponse>
    {
        var options = new StandardResilienceOptions();
        configure(options);

        var requestTypeName = typeof(TRequest).Name;

        // Register a named resilience pipeline for this specific request type
        services.AddSingleton<ResiliencePipelineProvider<string>>(sp =>
        {
            var existing = sp.GetService<ResiliencePipelineProvider<string>>();
            if (existing is ResiliencePipelineRegistry<string> registry)
            {
                registry.TryAddBuilder(requestTypeName, (builder, context) =>
                {
                    builder.AddRateLimiter(options.RateLimiter)
                        .AddTimeout(options.TotalRequestTimeout)
                        .AddRetry(options.Retry)
                        .AddCircuitBreaker(options.CircuitBreaker)
                        .AddTimeout(options.AttemptTimeout);
                });
                return registry;
            }

            var newRegistry = new ResiliencePipelineRegistry<string>();
            newRegistry.TryAddBuilder(requestTypeName, (builder, context) =>
            {
                builder.AddRateLimiter(options.RateLimiter)
                    .AddTimeout(options.TotalRequestTimeout)
                    .AddRetry(options.Retry)
                    .AddCircuitBreaker(options.CircuitBreaker)
                    .AddTimeout(options.AttemptTimeout);
            });
            return newRegistry;
        });

        return services;
    }
}
