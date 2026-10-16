using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus;

namespace Encina.UnitTests.AzureServiceBus.Publishing;

/// <summary>
/// Regression tests for #1792: the caller-supplied message id (for example the outbox id) is the
/// broker MessageId, and a new id is generated only when none is supplied.
/// </summary>
public sealed class AzureServiceBusPublisherMessageIdTests
{
    private readonly ServiceBusClient _client = Substitute.For<ServiceBusClient>();
    private readonly ServiceBusSender _sender = Substitute.For<ServiceBusSender>();
    private readonly AzureServiceBusMessagePublisher _publisher;

    public AzureServiceBusPublisherMessageIdTests()
    {
        _client.CreateSender(Arg.Any<string>()).Returns(_sender);
        _publisher = new AzureServiceBusMessagePublisher(
            _client,
            Substitute.For<ILogger<AzureServiceBusMessagePublisher>>(),
            Options.Create(new EncinaAzureServiceBusOptions
            {
                DefaultQueueName = "q",
                DefaultTopicName = "t"
            }));
    }

    private string CapturedMessageId(string method) =>
        _sender.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == method)
            .GetArguments()
            .OfType<ServiceBusMessage>()
            .Single()
            .MessageId;

    [Fact]
    public async Task SendToQueueAsync_WithMessageId_PreservesIt()
    {
        await _publisher.SendToQueueAsync(new Payload("a"), messageId: "outbox-1");

        CapturedMessageId(nameof(ServiceBusSender.SendMessageAsync)).ShouldBe("outbox-1");
    }

    [Fact]
    public async Task PublishToTopicAsync_WithMessageId_PreservesIt()
    {
        await _publisher.PublishToTopicAsync(new Payload("a"), messageId: "outbox-2");

        CapturedMessageId(nameof(ServiceBusSender.SendMessageAsync)).ShouldBe("outbox-2");
    }

    [Fact]
    public async Task ScheduleAsync_WithMessageId_PreservesIt()
    {
        await _publisher.ScheduleAsync(new Payload("a"), DateTimeOffset.UtcNow.AddMinutes(1), messageId: "outbox-3");

        CapturedMessageId(nameof(ServiceBusSender.ScheduleMessageAsync)).ShouldBe("outbox-3");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SendToQueueAsync_WithoutMessageId_GeneratesANewGuid(string? messageId)
    {
        await _publisher.SendToQueueAsync(new Payload("a"), messageId: messageId);

        Guid.TryParse(CapturedMessageId(nameof(ServiceBusSender.SendMessageAsync)), out _).ShouldBeTrue();
    }

    private sealed record Payload(string Value);
}
