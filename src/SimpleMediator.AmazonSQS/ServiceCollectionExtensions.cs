using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.AmazonSQS;

/// <summary>
/// Extension methods for configuring SimpleMediator Amazon SQS/SNS integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator Amazon SQS/SNS integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorAmazonSQS(
        this IServiceCollection services,
        Action<SimpleMediatorAmazonSQSOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimpleMediatorAmazonSQSOptions();
        configure?.Invoke(options);

        services.Configure<SimpleMediatorAmazonSQSOptions>(opt =>
        {
            opt.Region = options.Region;
            opt.DefaultQueueUrl = options.DefaultQueueUrl;
            opt.DefaultTopicArn = options.DefaultTopicArn;
            opt.UseFifoQueues = options.UseFifoQueues;
            opt.MaxNumberOfMessages = options.MaxNumberOfMessages;
            opt.VisibilityTimeoutSeconds = options.VisibilityTimeoutSeconds;
            opt.WaitTimeSeconds = options.WaitTimeSeconds;
            opt.UseContentBasedDeduplication = options.UseContentBasedDeduplication;
        });

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

        return services;
    }
}
