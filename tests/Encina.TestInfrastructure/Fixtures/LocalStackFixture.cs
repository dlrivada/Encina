using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Testcontainers.LocalStack;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// LocalStack fixture using Testcontainers.
/// Provides a throwaway LocalStack instance for AWS integration tests (SQS, SNS).
/// </summary>
public sealed class LocalStackFixture : IAsyncLifetime
{
    private LocalStackContainer? _container;

    /// <summary>
    /// Gets the LocalStack endpoint URL.
    /// </summary>
    public string Endpoint => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the LocalStack container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && !string.IsNullOrEmpty(Endpoint);

    /// <summary>
    /// Gets the AWS credentials for LocalStack.
    /// </summary>
    public BasicAWSCredentials Credentials { get; } = new("test", "test");

    /// <summary>
    /// Creates an SQS client configured to use the LocalStack container.
    /// </summary>
    /// <returns>An <see cref="IAmazonSQS"/> instance.</returns>
    public IAmazonSQS CreateSqsClient()
    {
        return new AmazonSQSClient(
            Credentials,
            new AmazonSQSConfig
            {
                ServiceURL = Endpoint,
                UseHttp = true
            });
    }

    /// <summary>
    /// Creates an SNS client configured to use the LocalStack container.
    /// </summary>
    /// <returns>An <see cref="IAmazonSimpleNotificationService"/> instance.</returns>
    public IAmazonSimpleNotificationService CreateSnsClient()
    {
        return new AmazonSimpleNotificationServiceClient(
            Credentials,
            new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = Endpoint,
                UseHttp = true
            });
    }

    /// <summary>
    /// Creates a test queue and returns its URL.
    /// </summary>
    /// <param name="queueName">The name of the queue to create.</param>
    /// <param name="isFifo">Whether to create a FIFO queue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The queue URL.</returns>
    public async Task<string> CreateQueueAsync(
        string queueName,
        bool isFifo = false,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateSqsClient();

        var request = new CreateQueueRequest
        {
            QueueName = isFifo ? $"{queueName}.fifo" : queueName
        };

        if (isFifo)
        {
            request.Attributes = new Dictionary<string, string>
            {
                ["FifoQueue"] = "true",
                ["ContentBasedDeduplication"] = "true"
            };
        }

        var response = await client.CreateQueueAsync(request, cancellationToken).ConfigureAwait(false);
        return response.QueueUrl;
    }

    /// <summary>
    /// Creates a test topic and returns its ARN.
    /// </summary>
    /// <param name="topicName">The name of the topic to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The topic ARN.</returns>
    public async Task<string> CreateTopicAsync(
        string topicName,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateSnsClient();

        var response = await client.CreateTopicAsync(topicName, cancellationToken).ConfigureAwait(false);
        return response.TopicArn;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new LocalStackBuilder()
                .WithImage("localstack/localstack:3.8")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start LocalStack container: {ex.Message}");
            // Container might not be available in CI without Docker
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for LocalStack integration tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class LocalStackCollection : ICollectionFixture<LocalStackFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "LocalStack";
}
