using Encina.RabbitMQ;
using Encina.RabbitMQ.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using RabbitMQ.Client;

using Shouldly;

namespace Encina.GuardTests.RabbitMQ;

/// <summary>
/// Guard tests for Encina.RabbitMQ covering constructor and method null guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class RabbitMQGuardTests
{
    private static readonly IConnection Connection = Substitute.For<IConnection>();
    private static readonly IChannel Channel = Substitute.For<IChannel>();

    // ─── RabbitMQMessagePublisher constructor guards ───

    [Fact]
    public void Constructor_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RabbitMQMessagePublisher(null!, Channel,
                NullLogger<RabbitMQMessagePublisher>.Instance,
                Options.Create(new EncinaRabbitMQOptions())));
    }

    [Fact]
    public void Constructor_NullChannel_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RabbitMQMessagePublisher(Connection, null!,
                NullLogger<RabbitMQMessagePublisher>.Instance,
                Options.Create(new EncinaRabbitMQOptions())));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RabbitMQMessagePublisher(Connection, Channel, null!,
                Options.Create(new EncinaRabbitMQOptions())));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RabbitMQMessagePublisher(Connection, Channel,
                NullLogger<RabbitMQMessagePublisher>.Instance, null!));
    }

    [Fact]
    public void Constructor_ValidArgs_Constructs()
    {
        var sut = new RabbitMQMessagePublisher(Connection, Channel,
            NullLogger<RabbitMQMessagePublisher>.Instance,
            Options.Create(new EncinaRabbitMQOptions()));
        sut.ShouldNotBeNull();
    }

    // ─── RabbitMQMessagePublisher method guards ───

    [Fact]
    public async Task PublishAsync_NullMessage_Throws()
    {
        var sut = new RabbitMQMessagePublisher(Connection, Channel,
            NullLogger<RabbitMQMessagePublisher>.Instance,
            Options.Create(new EncinaRabbitMQOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.PublishAsync<object>(null!));
    }

    [Fact]
    public async Task SendToQueueAsync_NullQueueName_Throws()
    {
        var sut = new RabbitMQMessagePublisher(Connection, Channel,
            NullLogger<RabbitMQMessagePublisher>.Instance,
            Options.Create(new EncinaRabbitMQOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SendToQueueAsync<object>(null!, new { }));
    }

    [Fact]
    public async Task SendToQueueAsync_NullMessage_Throws()
    {
        var sut = new RabbitMQMessagePublisher(Connection, Channel,
            NullLogger<RabbitMQMessagePublisher>.Instance,
            Options.Create(new EncinaRabbitMQOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SendToQueueAsync<object>("queue", null!));
    }

    [Fact]
    public async Task RequestAsync_NullRequest_Throws()
    {
        var sut = new RabbitMQMessagePublisher(Connection, Channel,
            NullLogger<RabbitMQMessagePublisher>.Instance,
            Options.Create(new EncinaRabbitMQOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.RequestAsync<object, string>(null!));
    }

    // ─── RabbitMQHealthCheck ───

    [Fact]
    public void RabbitMQHealthCheck_Constructs()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new RabbitMQHealthCheck(sp, null);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaRabbitMQ_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaRabbitMQ(_ => { }));
    }

    [Fact]
    public void AddEncinaRabbitMQ_ValidServices_Registers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddEncinaRabbitMQ(o =>
            o.HostName = "localhost");
        result.ShouldNotBeNull();
    }

    // ─── EncinaRabbitMQOptions ───

    [Fact]
    public void EncinaRabbitMQOptions_Defaults()
    {
        var options = new EncinaRabbitMQOptions();
        options.ShouldNotBeNull();
    }
}
