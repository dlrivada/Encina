using Encina.NATS;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Encina.IntegrationTests.MessageBrokers.NATS;

/// <summary>
/// Shares one NATS container among the publisher integration tests of this assembly.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class NatsJetStreamCollection : ICollectionFixture<NatsFixture>
#pragma warning restore CA1711
{
    /// <summary>The name of the collection.</summary>
    public const string Name = "NATS-JetStream";
}

/// <summary>
/// Proves that the <c>Nats-Msg-Id</c> header sent by <see cref="NATSMessagePublisher"/> makes JetStream
/// drop a repeated id inside the stream's Duplicates window (#1792).
/// </summary>
[Collection(NatsJetStreamCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Messaging", "NATS")]
public sealed class NATSJetStreamDeduplicationIntegrationTests
{
    private readonly NatsFixture _fixture;

    public NATSJetStreamDeduplicationIntegrationTests(NatsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task JetStreamPublishAsync_SameMessageIdTwice_StoresOneMessage()
    {
        Assert.True(_fixture.IsAvailable, "The NATS container must be running.");
        var js = new NatsJSContext(_fixture.Connection!);
        var stream = $"S{Guid.NewGuid():N}";
        var subject = $"dedup.{Guid.NewGuid():N}";
        await js.CreateStreamAsync(new StreamConfig(stream, [subject]) { DuplicateWindow = TimeSpan.FromMinutes(2) });
        var publisher = new NATSMessagePublisher(
            _fixture.Connection!,
            js,
            NullLogger<NATSMessagePublisher>.Instance,
            Options.Create(new EncinaNATSOptions()));

        var first = await publisher.JetStreamPublishAsync(new Payload("a"), subject, "outbox-1");
        var second = await publisher.JetStreamPublishAsync(new Payload("a"), subject, "outbox-1");
        var third = await publisher.JetStreamPublishAsync(new Payload("b"), subject, "outbox-2");

        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();
        third.IsRight.ShouldBeTrue();
        second.IfRight(ack => ack.Duplicate.ShouldBeTrue());
        first.IfRight(ack => ack.Duplicate.ShouldBeFalse());
        var info = await js.GetStreamAsync(stream);
        info.Info.State.Messages.ShouldBe(2);
    }

    private sealed record Payload(string Value);
}
