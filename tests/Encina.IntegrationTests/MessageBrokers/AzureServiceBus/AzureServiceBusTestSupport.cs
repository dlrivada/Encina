using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.MessageBrokers.AzureServiceBus;

/// <summary>
/// A message published by the Azure Service Bus integration tests.
/// </summary>
/// <param name="OrderId">The order identifier.</param>
/// <param name="Amount">The order amount.</param>
public sealed record OrderPlacedMessage(string OrderId, decimal Amount);

/// <summary>
/// Builds the publisher under test against the emulator fixture and receives with the raw Service Bus client.
/// </summary>
internal static class AzureServiceBusTestSupport
{
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Asserts that a failed result carries the expected error code and that its cause is the broker reporting
    /// a missing entity, so the code is not the only evidence (the publisher uses one code for any exception).
    /// </summary>
    public static void ShouldFailBecauseTheEntityIsMissing<T>(Either<EncinaError, T> result, string expectedCode)
    {
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().IfNone(string.Empty).ShouldBe(expectedCode);
            var cause = error.Exception.IfNone(() => throw new InvalidOperationException("The error does not expose its exception."));
            var serviceBusException = cause.ShouldBeOfType<ServiceBusException>();
            serviceBusException.Reason.ShouldBe(ServiceBusFailureReason.MessagingEntityNotFound);
        });
    }

    /// <summary>
    /// Builds a provider that registers the fixture's client before <c>AddEncinaAzureServiceBus</c>,
    /// so the package's own TryAdd registration leaves it in place.
    /// </summary>
    public static ServiceProvider CreateServiceProvider(
        ServiceBusEmulatorFixture fixture,
        Action<EncinaAzureServiceBusOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(fixture.Client);
        services.AddEncinaAzureServiceBus(options =>
        {
            options.ConnectionString = fixture.ConnectionString;
            configure?.Invoke(options);
        });

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
    /// Receives exactly <paramref name="expectedCount"/> messages from a queue within a bounded time.
    /// </summary>
    public static Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveFromQueueAsync(
        ServiceBusClient client,
        string queueName,
        int expectedCount) =>
        ReceiveAsync(client.CreateReceiver(queueName, ReceiveAndDelete), expectedCount);

    /// <summary>
    /// Receives exactly <paramref name="expectedCount"/> messages from a topic subscription within a bounded time.
    /// </summary>
    public static Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveFromSubscriptionAsync(
        ServiceBusClient client,
        string topicName,
        string subscriptionName,
        int expectedCount) =>
        ReceiveAsync(client.CreateReceiver(topicName, subscriptionName, ReceiveAndDelete), expectedCount);

    /// <summary>
    /// Tries to receive messages within a short window; returns an empty list when the queue stays empty.
    /// </summary>
    public static async Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveWithinAsync(
        ServiceBusClient client,
        string queueName,
        TimeSpan wait)
    {
        await using var receiver = client.CreateReceiver(queueName, ReceiveAndDelete);
        return await receiver.ReceiveMessagesAsync(10, wait);
    }

    /// <summary>
    /// A unique message id so tests never collide, even without clearing.
    /// </summary>
    public static string UniqueId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static ServiceBusReceiverOptions ReceiveAndDelete { get; } =
        new() { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };

    private static async Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveAsync(
        ServiceBusReceiver receiver,
        int expectedCount)
    {
        await using (receiver)
        {
            using var cts = new CancellationTokenSource(ReceiveTimeout);
            var received = new List<ServiceBusReceivedMessage>();

            while (received.Count < expectedCount && !cts.IsCancellationRequested)
            {
                received.AddRange(await receiver.ReceiveMessagesAsync(10, TimeSpan.FromSeconds(2), cts.Token));
            }

            return received;
        }
    }
}
