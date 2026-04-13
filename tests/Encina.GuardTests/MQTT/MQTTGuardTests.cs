using Encina.MQTT;
using Encina.MQTT.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using MQTTnet;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.MQTT;

/// <summary>
/// Guard tests for Encina.MQTT covering constructor and method null guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class MQTTGuardTests
{
    private static readonly IMqttClient Client = Substitute.For<IMqttClient>();

    // ─── MQTTMessagePublisher constructor guards ───

    [Fact]
    public void Constructor_NullClient_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MQTTMessagePublisher(null!,
                NullLogger<MQTTMessagePublisher>.Instance,
                Options.Create(new EncinaMQTTOptions())));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MQTTMessagePublisher(Client, null!,
                Options.Create(new EncinaMQTTOptions())));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MQTTMessagePublisher(Client,
                NullLogger<MQTTMessagePublisher>.Instance, null!));
    }

    [Fact]
    public void Constructor_ValidArgs_Constructs()
    {
        var sut = new MQTTMessagePublisher(Client,
            NullLogger<MQTTMessagePublisher>.Instance,
            Options.Create(new EncinaMQTTOptions()));
        sut.ShouldNotBeNull();
    }

    // ─── MQTTMessagePublisher method guards ───

    [Fact]
    public async Task PublishAsync_NullMessage_Throws()
    {
        var sut = new MQTTMessagePublisher(Client,
            NullLogger<MQTTMessagePublisher>.Instance,
            Options.Create(new EncinaMQTTOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.PublishAsync<object>(null!));
    }

    [Fact]
    public async Task SubscribeAsync_NullHandler_Throws()
    {
        var sut = new MQTTMessagePublisher(Client,
            NullLogger<MQTTMessagePublisher>.Instance,
            Options.Create(new EncinaMQTTOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SubscribeAsync<object>(null!, "topic"));
    }

    [Fact]
    public async Task SubscribeAsync_NullTopic_Throws()
    {
        var sut = new MQTTMessagePublisher(Client,
            NullLogger<MQTTMessagePublisher>.Instance,
            Options.Create(new EncinaMQTTOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SubscribeAsync<object>(_ => ValueTask.CompletedTask, null!));
    }

    [Fact]
    public async Task SubscribePatternAsync_NullHandler_Throws()
    {
        var sut = new MQTTMessagePublisher(Client,
            NullLogger<MQTTMessagePublisher>.Instance,
            Options.Create(new EncinaMQTTOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SubscribePatternAsync<object>(null!, "topic/#"));
    }

    [Fact]
    public async Task SubscribePatternAsync_NullTopicFilter_Throws()
    {
        var sut = new MQTTMessagePublisher(Client,
            NullLogger<MQTTMessagePublisher>.Instance,
            Options.Create(new EncinaMQTTOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SubscribePatternAsync<object>((_, _) => ValueTask.CompletedTask, null!));
    }

    // ─── MQTTHealthCheck ───

    [Fact]
    public void MQTTHealthCheck_Constructs()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new MQTTHealthCheck(sp, null);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaMQTT_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaMQTT(_ => { }));
    }

    [Fact]
    public void AddEncinaMQTT_ValidServices_Registers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddEncinaMQTT(o =>
            o.Host = "localhost");
        result.ShouldNotBeNull();
    }

    // ─── EncinaMQTTOptions ───

    [Fact]
    public void EncinaMQTTOptions_Defaults()
    {
        var options = new EncinaMQTTOptions();
        options.ShouldNotBeNull();
    }
}
