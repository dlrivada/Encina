using Amazon.SQS;
using Amazon.SQS.Model;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.AmazonSQS.Health;

/// <summary>
/// Health check for Amazon SQS connectivity.
/// </summary>
public sealed class AmazonSQSHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the Amazon SQS health check.
    /// </summary>
    public const string DefaultName = "encina-amazon-sqs";

    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonSQSHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve IAmazonSQS from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public AmazonSQSHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "messaging", "amazon-sqs", "ready"])
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sqsClient = _serviceProvider.GetRequiredService<IAmazonSQS>();

            // List queues as a health check (limit to 1 for efficiency)
            var response = await sqsClient.ListQueuesAsync(new ListQueuesRequest
            {
                MaxResults = 1
            }, cancellationToken);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return HealthCheckResult.Healthy($"{Name} is connected");
            }

            return HealthCheckResult.Unhealthy($"{Name} returned status code: {response.HttpStatusCode}");
        }
        catch (AmazonSQSException ex)
        {
            return HealthCheckResult.Unhealthy($"{Name} error: {ex.ErrorCode} - {ex.Message}");
        }
    }
}
