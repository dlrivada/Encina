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
/// <remarks>
/// <para>
/// The image is pinned to <c>localstack/localstack:4.14.0</c>, which starts without a
/// <c>LOCALSTACK_AUTH_TOKEN</c> (issue #2699, decision D2). No token and no process-global environment variable is involved.
/// </para>
/// <para>
/// One container is shared by every test class of the <c>Cloud-LocalStack</c> collection
/// (<see cref="CollectionName"/>). Each test assembly declares its own
/// <c>[CollectionDefinition]</c> class, because xUnit collection definitions are per assembly.
/// Test classes call <see cref="ClearAllDataAsync"/> in <c>InitializeAsync()</c> and never dispose the fixture.
/// </para>
/// </remarks>
public sealed class LocalStackFixture : IAsyncLifetime
{
    /// <summary>
    /// The name of the shared xUnit collection that owns the LocalStack container.
    /// </summary>
    public const string CollectionName = "Cloud-LocalStack";

    /// <summary>
    /// The pinned LocalStack image.
    /// </summary>
    public const string Image = "localstack/localstack:4.14.0";

    private const string Region = "us-east-1";

    private LocalStackContainer? _container;
    private IAmazonSQS? _sqsClient;
    private IAmazonSimpleNotificationService? _snsClient;

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
    /// Gets the SQS client shared by the collection. The fixture owns it: do not dispose it.
    /// </summary>
    public IAmazonSQS SqsClient => _sqsClient ?? throw new InvalidOperationException("The fixture is not initialized.");

    /// <summary>
    /// Gets the SNS client shared by the collection. The fixture owns it: do not dispose it.
    /// </summary>
    public IAmazonSimpleNotificationService SnsClient => _snsClient ?? throw new InvalidOperationException("The fixture is not initialized.");

    /// <summary>
    /// Creates an SQS client configured to use the LocalStack container.
    /// </summary>
    /// <returns>An <see cref="IAmazonSQS"/> instance the caller disposes.</returns>
    public IAmazonSQS CreateSqsClient()
    {
        return new AmazonSQSClient(
            Credentials,
            new AmazonSQSConfig
            {
                ServiceURL = Endpoint,
                AuthenticationRegion = Region,
                UseHttp = true
            });
    }

    /// <summary>
    /// Creates an SNS client configured to use the LocalStack container.
    /// </summary>
    /// <returns>An <see cref="IAmazonSimpleNotificationService"/> instance the caller disposes.</returns>
    public IAmazonSimpleNotificationService CreateSnsClient()
    {
        return new AmazonSimpleNotificationServiceClient(
            Credentials,
            new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = Endpoint,
                AuthenticationRegion = Region,
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

        var response = await SqsClient.CreateQueueAsync(request, cancellationToken).ConfigureAwait(false);
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
        var response = await SnsClient.CreateTopicAsync(topicName, cancellationToken).ConfigureAwait(false);
        return response.TopicArn;
    }

    /// <summary>
    /// Deletes every SQS queue and SNS topic in the container so the next test starts empty.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all queues and topics are deleted.</returns>
    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        var queues = await SqsClient.ListQueuesAsync(new ListQueuesRequest(), cancellationToken).ConfigureAwait(false);
        foreach (var queueUrl in queues.QueueUrls ?? [])
        {
            await SqsClient.DeleteQueueAsync(queueUrl, cancellationToken).ConfigureAwait(false);
        }

        var topics = await SnsClient.ListTopicsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var topic in topics.Topics ?? [])
        {
            await SnsClient.DeleteTopicAsync(topic.TopicArn, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new LocalStackBuilder(Image)
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        _sqsClient = CreateSqsClient();
        _snsClient = CreateSnsClient();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _sqsClient?.Dispose();
        _snsClient?.Dispose();

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
