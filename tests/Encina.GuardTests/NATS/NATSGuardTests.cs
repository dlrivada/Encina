using Encina.NATS;
using Encina.NATS.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NATS.Client.Core;
using NATS.Client.JetStream;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.NATS;

/// <summary>
/// Guard tests for Encina.NATS covering constructor and method null guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class NATSGuardTests
{
    private static readonly INatsConnection Connection = Substitute.For<INatsConnection>();

    // ─── NATSMessagePublisher constructor guards ───

    [Fact]
    public void Constructor_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new NATSMessagePublisher(null!, null,
                NullLogger<NATSMessagePublisher>.Instance,
                Options.Create(new EncinaNATSOptions())));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new NATSMessagePublisher(Connection, null, null!,
                Options.Create(new EncinaNATSOptions())));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new NATSMessagePublisher(Connection, null,
                NullLogger<NATSMessagePublisher>.Instance, null!));
    }

    [Fact]
    public void Constructor_ValidArgs_Constructs()
    {
        var sut = new NATSMessagePublisher(Connection, null,
            NullLogger<NATSMessagePublisher>.Instance,
            Options.Create(new EncinaNATSOptions()));
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithJetStream_Constructs()
    {
        var jetStream = Substitute.For<INatsJSContext>();
        var sut = new NATSMessagePublisher(Connection, jetStream,
            NullLogger<NATSMessagePublisher>.Instance,
            Options.Create(new EncinaNATSOptions()));
        sut.ShouldNotBeNull();
    }

    // ─── NATSMessagePublisher method guards ───

    [Fact]
    public async Task PublishAsync_NullMessage_Throws()
    {
        var sut = new NATSMessagePublisher(Connection, null,
            NullLogger<NATSMessagePublisher>.Instance,
            Options.Create(new EncinaNATSOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.PublishAsync<object>(null!));
    }

    [Fact]
    public async Task RequestAsync_NullRequest_Throws()
    {
        var sut = new NATSMessagePublisher(Connection, null,
            NullLogger<NATSMessagePublisher>.Instance,
            Options.Create(new EncinaNATSOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.RequestAsync<object, string>(null!));
    }

    [Fact]
    public async Task JetStreamPublishAsync_NullMessage_Throws()
    {
        var sut = new NATSMessagePublisher(Connection, null,
            NullLogger<NATSMessagePublisher>.Instance,
            Options.Create(new EncinaNATSOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.JetStreamPublishAsync<object>(null!));
    }

    // ─── NATSHealthCheck ───

    [Fact]
    public void NATSHealthCheck_Constructs()
    {
        using var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new NATSHealthCheck(sp, null);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaNATS_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaNATS(_ => { }));
    }

    [Fact]
    public void AddEncinaNATS_ValidServices_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddEncinaNATS(o =>
            o.Url = "nats://localhost:4222");

        result.ShouldNotBeNull();
        services.ShouldContain(sd => sd.ServiceType == typeof(INATSMessagePublisher));
    }

    // ─── EncinaNATSOptions ───

    [Fact]
    public void EncinaNATSOptions_Defaults()
    {
        var options = new EncinaNATSOptions();

        options.Url.ShouldBe("nats://localhost:4222");
        options.SubjectPrefix.ShouldBe("encina");
        options.UseJetStream.ShouldBeFalse();
        options.StreamName.ShouldBe("ENCINA");
        options.ConsumerName.ShouldBe("encina-consumer");
        options.UseDurableConsumer.ShouldBeTrue();
        options.AckWait.ShouldBe(TimeSpan.FromSeconds(30));
        options.MaxDeliver.ShouldBe(5);
    }

    // ─── NATSPublishAck record ───

    [Fact]
    public void NATSPublishAck_PropertiesAssignable()
    {
        var ack = new NATSPublishAck(
            Stream: "orders",
            Sequence: 42,
            Duplicate: false);

        ack.Stream.ShouldBe("orders");
        ack.Sequence.ShouldBe(42UL);
        ack.Duplicate.ShouldBeFalse();
    }
}
