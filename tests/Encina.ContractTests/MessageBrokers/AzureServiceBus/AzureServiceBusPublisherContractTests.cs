// CA1859: a contract test deliberately holds the implementation behind its public interface.
#pragma warning disable CA1859
using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus;
using Encina.AzureServiceBus.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.ContractTests.MessageBrokers.AzureServiceBus;

/// <summary>
/// Contract tests of the public interfaces implemented by the Azure Service Bus package
/// (<see cref="IAzureServiceBusMessagePublisher"/> and <see cref="IEncinaHealthCheck"/>), run with the real
/// implementations against the official Azure Service Bus emulator.
/// </summary>
[Collection(ServiceBusEmulatorFixture.CollectionName)]
[Trait("Category", "Integration")]
[Trait("Messaging", "AzureServiceBus")]
public sealed class AzureServiceBusPublisherContractTests : IAsyncLifetime
{
    private static readonly ServiceBusReceiverOptions ReceiveAndDelete = new() { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };

    private readonly ServiceBusEmulatorFixture _fixture;

    public AzureServiceBusPublisherContractTests(ServiceBusEmulatorFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private IAzureServiceBusMessagePublisher CreatePublisher(EncinaAzureServiceBusOptions? options = null) =>
        new AzureServiceBusMessagePublisher(
            _fixture.Client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(options ?? new EncinaAzureServiceBusOptions()));

    private static string UniqueId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static string CodeOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty));

    private static ServiceBusFailureReason? ReasonOf<T>(Either<EncinaError, T> result) =>
        result.Match(
            Right: _ => (ServiceBusFailureReason?)null,
            Left: e => e.Exception.Match(Some: x => (x as ServiceBusException)?.Reason, None: () => null));

    private async Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveFromQueueAsync(string queueName, int expectedCount)
    {
        await using var receiver = _fixture.Client.CreateReceiver(queueName, ReceiveAndDelete);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var received = new List<ServiceBusReceivedMessage>();
        while (received.Count < expectedCount && !cts.IsCancellationRequested)
        {
            received.AddRange(await receiver.ReceiveMessagesAsync(10, TimeSpan.FromSeconds(2), cts.Token));
        }

        return received;
    }

    [Fact]
    public async Task SendToQueueAsync_OnSuccess_ReturnsRight()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        var result = await publisher.SendToQueueAsync(new TestMessage("a"), ServiceBusEmulatorFixture.OrdersQueue);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task SendToQueueAsync_AlwaysCarriesTheMessageIdSubjectAndJsonContentType()
    {
        // Arrange
        var publisher = CreatePublisher();
        var messageId = UniqueId("propagated");

        // Act
        var result = await publisher.SendToQueueAsync(new TestMessage("a"), ServiceBusEmulatorFixture.OrdersQueue, messageId);
        result.IsRight.ShouldBeTrue();
        var received = await ReceiveFromQueueAsync(ServiceBusEmulatorFixture.OrdersQueue, 1);

        // Assert
        received.Count.ShouldBe(1);
        received[0].MessageId.ShouldBe(messageId);
        received[0].Subject.ShouldBe(nameof(TestMessage));
        received[0].ContentType.ShouldBe("application/json");
    }

    [Fact]
    public async Task PublishToTopicAsync_OnSuccess_ReturnsRight()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        var result = await publisher.PublishToTopicAsync(new TestMessage("a"), ServiceBusEmulatorFixture.EventsTopic);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduleAsync_OnSuccess_ReturnsRightWithAPositiveSequenceNumberThatCancelAccepts()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        var scheduled = await publisher.ScheduleAsync(
            new TestMessage("a"),
            DateTimeOffset.UtcNow.AddMinutes(10),
            ServiceBusEmulatorFixture.ScheduledQueue);
        scheduled.IsRight.ShouldBeTrue();
        var sequenceNumber = scheduled.Match(Right: n => n, Left: _ => 0L);
        var cancelled = await publisher.CancelScheduledAsync(sequenceNumber, ServiceBusEmulatorFixture.ScheduledQueue);

        // Assert
        sequenceNumber.ShouldBeGreaterThan(0);
        cancelled.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task EveryOperation_WhenTheBrokerRejectsTheCall_ReturnsLeftWithAStableErrorCodeInsteadOfThrowing()
    {
        // Arrange
        var publisher = CreatePublisher();
        var missing = UniqueId("missing");

        // Act
        var send = await publisher.SendToQueueAsync(new TestMessage("a"), missing);
        var topic = await publisher.PublishToTopicAsync(new TestMessage("a"), missing);
        var schedule = await publisher.ScheduleAsync(new TestMessage("a"), DateTimeOffset.UtcNow.AddMinutes(1), missing);
        var cancel = await publisher.CancelScheduledAsync(1, missing);

        // Assert
        CodeOf(send).ShouldBe("AZURE_SB_SEND_FAILED");
        CodeOf(topic).ShouldBe("AZURE_SB_PUBLISH_FAILED");
        CodeOf(schedule).ShouldBe("AZURE_SB_SCHEDULE_FAILED");
        CodeOf(cancel).ShouldBe("AZURE_SB_CANCEL_FAILED");

        // The code alone is shared by every exception, so the cause proves the broker rejected the missing entity.
        ReasonOf(send).ShouldBe(ServiceBusFailureReason.MessagingEntityNotFound);
        ReasonOf(topic).ShouldBe(ServiceBusFailureReason.MessagingEntityNotFound);
        ReasonOf(schedule).ShouldBe(ServiceBusFailureReason.MessagingEntityNotFound);
        ReasonOf(cancel).ShouldBe(ServiceBusFailureReason.MessagingEntityNotFound);
    }

    [Fact]
    public void HealthCheck_ImplementsTheEncinaHealthCheckContract()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_fixture.Client);

        // Act
        using var provider = services.BuildServiceProvider();
        IEncinaHealthCheck healthCheck = new AzureServiceBusHealthCheck(provider, null);

        // Assert
        healthCheck.Name.ShouldBe(AzureServiceBusHealthCheck.DefaultName);
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("azure-servicebus");
    }

    [Fact]
    public async Task HealthCheck_AgainstTheEmulatorClient_ReportsHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_fixture.Client);
        using var provider = services.BuildServiceProvider();
        IEncinaHealthCheck healthCheck = new AzureServiceBusHealthCheck(provider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    private sealed record TestMessage(string Value);
}
