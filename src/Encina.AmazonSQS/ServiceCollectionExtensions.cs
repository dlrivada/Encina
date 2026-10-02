using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Encina.AmazonSQS.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.AmazonSQS;

/// <summary>
/// Extension methods for configuring Encina Amazon SQS/SNS integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Amazon SQS/SNS integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="OptionsValidationException">
    /// Thrown when <see cref="EncinaAmazonSQSOptions.DefaultQueueUrl"/> is set but is not an absolute
    /// <c>https</c> URL (unless <see cref="EncinaAmazonSQSOptions.AllowInsecureHttp"/> is set), targets a
    /// loopback address (unless <see cref="EncinaAmazonSQSOptions.AllowLocalEndpoints"/> is set) or
    /// targets a link-local, cloud metadata or unspecified address. The same validation runs again at
    /// host startup (<c>ValidateOnStart</c>).
    /// </exception>
    public static IServiceCollection AddEncinaAmazonSQS(
        this IServiceCollection services,
        Action<EncinaAmazonSQSOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Validate eagerly so a plain BuildServiceProvider (no host, no ValidateOnStart) is protected too.
        var options = new EncinaAmazonSQSOptions();
        configure?.Invoke(options);
        var validation = new EncinaAmazonSQSOptionsValidator().Validate(Options.DefaultName, options);
        if (validation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(EncinaAmazonSQSOptions), validation.Failures);
        }

        services.AddOptions<EncinaAmazonSQSOptions>()
            .Configure(opt => configure?.Invoke(opt))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<EncinaAmazonSQSOptions>, EncinaAmazonSQSOptionsValidator>());

        services.TryAddSingleton<IAmazonSQS>(sp =>
        {
            var config = new AmazonSQSConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };
            return new AmazonSQSClient(config);
        });

        services.TryAddSingleton<IAmazonSimpleNotificationService>(sp =>
        {
            var config = new AmazonSimpleNotificationServiceConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };
            return new AmazonSimpleNotificationServiceClient(config);
        });

        services.TryAddScoped<IAmazonSQSMessagePublisher, AmazonSQSMessagePublisher>();

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, AmazonSQSHealthCheck>();
        }

        return services;
    }
}
