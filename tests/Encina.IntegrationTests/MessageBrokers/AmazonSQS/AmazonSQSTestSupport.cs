using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Encina.AmazonSQS;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.MessageBrokers.AmazonSQS;

/// <summary>
/// A message published by the Amazon SQS integration tests.
/// </summary>
/// <param name="OrderId">The order identifier.</param>
/// <param name="Amount">The order amount.</param>
public sealed record OrderPlacedMessage(string OrderId, decimal Amount);

/// <summary>
/// Builds the publisher under test against the LocalStack fixture and receives with the raw AWS SDK.
/// </summary>
internal static class AmazonSQSTestSupport
{
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Builds a provider that registers the fixture's clients before <c>AddEncinaAmazonSQS</c>,
    /// so the package's own TryAdd registrations leave them in place.
    /// </summary>
    public static ServiceProvider CreateServiceProvider(
        LocalStackFixture fixture,
        Action<EncinaAmazonSQSOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAmazonSQS>(fixture.SqsClient);
        services.AddSingleton<IAmazonSimpleNotificationService>(fixture.SnsClient);
        services.AddEncinaAmazonSQS(configure);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    /// <summary>
    /// Returns the value of a successful result, or fails the test with the error code.
    /// </summary>
    public static T Unwrap<T>(Either<EncinaError, T> result) =>
        result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(
                $"Expected a successful result but got {error.GetCode().IfNone("no code")}: {error.Exception.Map(e => e.ToString()).IfNone("no exception")}"));

    /// <summary>
    /// Receives exactly <paramref name="expectedCount"/> messages (polling within a bounded time) and deletes them.
    /// </summary>
    public static async Task<IReadOnlyList<Message>> ReceiveAsync(
        IAmazonSQS client,
        string queueUrl,
        int expectedCount)
    {
        using var cts = new CancellationTokenSource(ReceiveTimeout);
        var received = new List<Message>();

        while (received.Count < expectedCount && !cts.IsCancellationRequested)
        {
            var response = await client.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 2,
                    MessageAttributeNames = ["All"],
                    MessageSystemAttributeNames = ["All"]
                },
                cts.Token);

            foreach (var message in response.Messages ?? [])
            {
                received.Add(message);
                await client.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cts.Token);
            }
        }

        return received;
    }

    /// <summary>
    /// Tries to receive one message within a short window; returns an empty list when the queue stays empty.
    /// </summary>
    public static async Task<IReadOnlyList<Message>> ReceiveWithinAsync(
        IAmazonSQS client,
        string queueUrl,
        int waitSeconds)
    {
        var response = await client.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = waitSeconds,
                MessageAttributeNames = ["All"],
                MessageSystemAttributeNames = ["All"]
            });

        return response.Messages ?? [];
    }

    /// <summary>
    /// Gets the ARN of a queue.
    /// </summary>
    public static async Task<string> GetQueueArnAsync(IAmazonSQS client, string queueUrl)
    {
        var response = await client.GetQueueAttributesAsync(queueUrl, ["QueueArn"]);
        return response.QueueARN;
    }

    /// <summary>
    /// A unique resource name so tests never collide, even without clearing.
    /// </summary>
    public static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
}
