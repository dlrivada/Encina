using System.Text.Json;
using Encina.AzureServiceBus;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static Encina.IntegrationTests.MessageBrokers.AzureServiceBus.AzureServiceBusTestSupport;

namespace Encina.IntegrationTests.MessageBrokers.AzureServiceBus;

/// <summary>
/// Integration tests for <see cref="AzureServiceBusMessagePublisher"/> against the official Azure Service Bus emulator.
/// Messages are received with the raw <c>ServiceBusClient</c> of the fixture.
/// </summary>
[Collection(ServiceBusEmulatorFixture.CollectionName)]
[Trait("Category", "Integration")]
[Trait("Messaging", "AzureServiceBus")]
public sealed class AzureServiceBusPublisherIntegrationTests : IAsyncLifetime
{
    private readonly ServiceBusEmulatorFixture _fixture;

    public AzureServiceBusPublisherIntegrationTests(ServiceBusEmulatorFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SendToQueueAsync_WithExplicitMessageId_DeliversBodyAndMetadata()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();
        var message = new OrderPlacedMessage("order-1", 12.5m);
        var messageId = UniqueId("caller");

        // Act
        var result = await publisher.SendToQueueAsync(message, ServiceBusEmulatorFixture.OrdersQueue, messageId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var received = await ReceiveFromQueueAsync(_fixture.Client, ServiceBusEmulatorFixture.OrdersQueue, 1);
        received.Count.ShouldBe(1);
        JsonSerializer.Deserialize<OrderPlacedMessage>(received[0].Body.ToArray()).ShouldBe(message);
        received[0].MessageId.ShouldBe(messageId);
        received[0].Subject.ShouldBe(nameof(OrderPlacedMessage));
        received[0].ContentType.ShouldBe("application/json");
    }

    [Fact]
    public async Task SendToQueueAsync_WithoutMessageId_GeneratesOne()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();

        // Act
        var result = await publisher.SendToQueueAsync(new OrderPlacedMessage("order-2", 1m), ServiceBusEmulatorFixture.OrdersQueue);

        // Assert
        result.IsRight.ShouldBeTrue();
        var received = await ReceiveFromQueueAsync(_fixture.Client, ServiceBusEmulatorFixture.OrdersQueue, 1);
        Guid.TryParse(received[0].MessageId, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task SendToQueueAsync_WithoutQueueName_UsesTheConfiguredDefaultQueue()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture, options => options.DefaultQueueName = ServiceBusEmulatorFixture.OrdersQueue);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();

        // Act
        var result = await publisher.SendToQueueAsync(new OrderPlacedMessage("order-3", 3m));

        // Assert
        result.IsRight.ShouldBeTrue();
        (await ReceiveFromQueueAsync(_fixture.Client, ServiceBusEmulatorFixture.OrdersQueue, 1)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task SendToQueueAsync_WhenQueueDoesNotExist_ReturnsSendFailed()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();

        // Act
        var result = await publisher.SendToQueueAsync(new OrderPlacedMessage("order-4", 4m), UniqueId("missing"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("AZURE_SB_SEND_FAILED"));
    }

    [Fact]
    public async Task PublishToTopicAsync_DeliversToEverySubscription()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();
        var message = new OrderPlacedMessage("order-5", 5m);
        var messageId = UniqueId("topic");

        // Act
        var result = await publisher.PublishToTopicAsync(message, ServiceBusEmulatorFixture.EventsTopic, messageId);

        // Assert
        result.IsRight.ShouldBeTrue();
        foreach (var subscription in new[] { ServiceBusEmulatorFixture.DefaultSubscription, ServiceBusEmulatorFixture.AuditSubscription })
        {
            var received = await ReceiveFromSubscriptionAsync(_fixture.Client, ServiceBusEmulatorFixture.EventsTopic, subscription, 1);
            received.Count.ShouldBe(1);
            received[0].MessageId.ShouldBe(messageId);
            received[0].Subject.ShouldBe(nameof(OrderPlacedMessage));
            JsonSerializer.Deserialize<OrderPlacedMessage>(received[0].Body.ToArray()).ShouldBe(message);
        }
    }

    [Fact]
    public async Task PublishToTopicAsync_WithoutTopicName_UsesTheConfiguredDefaultTopic()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();

        // Act
        var result = await publisher.PublishToTopicAsync(new OrderPlacedMessage("order-6", 6m));

        // Assert
        result.IsRight.ShouldBeTrue();
        (await ReceiveFromSubscriptionAsync(
            _fixture.Client,
            ServiceBusEmulatorFixture.EventsTopic,
            ServiceBusEmulatorFixture.DefaultSubscription,
            1)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task PublishToTopicAsync_WhenTopicDoesNotExist_ReturnsPublishFailed()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();

        // Act
        var result = await publisher.PublishToTopicAsync(new OrderPlacedMessage("order-7", 7m), UniqueId("missing"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("AZURE_SB_PUBLISH_FAILED"));
    }

    [Fact]
    public async Task ScheduleAsync_DeliversTheMessageAfterTheScheduledTimeWithItsSequenceNumber()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();
        var messageId = UniqueId("scheduled");

        // Act
        var result = await publisher.ScheduleAsync(
            new OrderPlacedMessage("order-8", 8m),
            DateTimeOffset.UtcNow.AddSeconds(3),
            ServiceBusEmulatorFixture.ScheduledQueue,
            messageId);

        // Assert
        var sequenceNumber = Unwrap(result);
        sequenceNumber.ShouldBeGreaterThan(0);

        // Until the scheduled time the message is pending: a peek by the returned sequence number finds it.
        await using (var peeker = _fixture.Client.CreateReceiver(ServiceBusEmulatorFixture.ScheduledQueue))
        {
            var pending = await peeker.PeekMessageAsync(sequenceNumber);
            pending.ShouldNotBeNull();
            pending.MessageId.ShouldBe(messageId);
        }

        // The emulator re-enqueues the message with a new sequence number when it is due, so only the id is compared.
        var received = await ReceiveFromQueueAsync(_fixture.Client, ServiceBusEmulatorFixture.ScheduledQueue, 1);
        received.Count.ShouldBe(1);
        received[0].MessageId.ShouldBe(messageId);
    }

    [Fact(Skip = "Emulator limitation (servicebus-emulator 2.0.1): CancelScheduledMessage returns success but the scheduled message is still delivered at its due time (probed 2026-10-27). Enable when a newer emulator honours the cancel (#2967).")]
    public async Task CancelScheduledAsync_BeforeTheScheduledTime_PreventsDelivery()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();
        var scheduled = await publisher.ScheduleAsync(
            new OrderPlacedMessage("order-9", 9m),
            DateTimeOffset.UtcNow.AddSeconds(4),
            ServiceBusEmulatorFixture.ScheduledQueue,
            UniqueId("cancelled"));
        var sequenceNumber = Unwrap(scheduled);

        // Act
        var cancelled = await publisher.CancelScheduledAsync(sequenceNumber, ServiceBusEmulatorFixture.ScheduledQueue);

        // Assert
        cancelled.IsRight.ShouldBeTrue();
        // The receive window outlasts the scheduled time (4 s), so a message that was not cancelled would arrive inside it.
        (await ReceiveWithinAsync(_fixture.Client, ServiceBusEmulatorFixture.ScheduledQueue, TimeSpan.FromSeconds(8))).ShouldBeEmpty();
    }

    [Fact]
    public async Task CancelScheduledAsync_WhenTheQueueDoesNotExist_ReturnsCancelFailed()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();

        // Act
        var result = await publisher.CancelScheduledAsync(1, UniqueId("missing"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("AZURE_SB_CANCEL_FAILED"));
    }

    [Fact]
    public async Task ScheduleAsync_WhenTheQueueDoesNotExist_ReturnsScheduleFailed()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();

        // Act
        var result = await publisher.ScheduleAsync(new OrderPlacedMessage("order-10", 10m), DateTimeOffset.UtcNow.AddMinutes(1), UniqueId("missing"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe("AZURE_SB_SCHEDULE_FAILED"));
    }
}
