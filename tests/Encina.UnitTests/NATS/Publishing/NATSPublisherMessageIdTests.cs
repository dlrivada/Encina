using Encina.NATS;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NSubstitute.Core;

namespace Encina.UnitTests.NATS.Publishing;

/// <summary>
/// Regression tests for #1792: the caller-supplied message id (for example the outbox id) travels in the
/// <c>Nats-Msg-Id</c> header, which JetStream also uses for de-duplication.
/// </summary>
public sealed class NATSPublisherMessageIdTests
{
    private readonly INatsConnection _connection = Substitute.For<INatsConnection>();
    private readonly INatsJSContext _jetStream = Substitute.For<INatsJSContext>();
    private readonly NATSMessagePublisher _publisher;

    public NATSPublisherMessageIdTests()
    {
        _publisher = new NATSMessagePublisher(
            _connection,
            _jetStream,
            Substitute.For<ILogger<NATSMessagePublisher>>(),
            Options.Create(new EncinaNATSOptions { SubjectPrefix = "t" }));
    }

    private static string HeaderOf(ICall call)
    {
        var headers = call.GetArguments().OfType<NatsHeaders>().Single();
        return headers["Nats-Msg-Id"].ToString();
    }

    private static ICall Single(object target, string method) =>
        target.ReceivedCalls().Single(c => c.GetMethodInfo().Name == method);

    [Fact]
    public async Task PublishAsync_WithMessageId_SendsItInTheNatsMsgIdHeader()
    {
        await _publisher.PublishAsync(new Payload("a"), messageId: "outbox-1");

        HeaderOf(Single(_connection, nameof(INatsConnection.PublishAsync))).ShouldBe("outbox-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task PublishAsync_WithoutMessageId_GeneratesANewGuid(string? messageId)
    {
        await _publisher.PublishAsync(new Payload("a"), messageId: messageId);

        Guid.TryParse(HeaderOf(Single(_connection, nameof(INatsConnection.PublishAsync))), out _).ShouldBeTrue();
    }

    [Fact]
    public async Task JetStreamPublishAsync_WithMessageId_SendsItInTheNatsMsgIdHeader()
    {
        await _publisher.JetStreamPublishAsync(new Payload("a"), messageId: "outbox-2");

        HeaderOf(Single(_jetStream, nameof(INatsJSContext.PublishAsync))).ShouldBe("outbox-2");
    }

    private sealed record Payload(string Value);
}
