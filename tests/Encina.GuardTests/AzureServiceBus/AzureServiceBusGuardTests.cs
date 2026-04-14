using Azure.Messaging.ServiceBus;

using Encina.AzureServiceBus;
using Encina.AzureServiceBus.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.AzureServiceBus;

/// <summary>
/// Guard tests for Encina.AzureServiceBus covering constructor and method null guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class AzureServiceBusGuardTests
{
    private static ServiceBusClient CreateMockClient()
    {
        // ServiceBusClient can be constructed with a fake connection string for guard testing
        // (it only validates format, doesn't connect)
        return new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==");
    }

    // ─── AzureServiceBusMessagePublisher constructor guards ───

    [Fact]
    public void Constructor_NullClient_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AzureServiceBusMessagePublisher(null!,
                NullLogger<AzureServiceBusMessagePublisher>.Instance,
                Options.Create(new EncinaAzureServiceBusOptions())));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var client = CreateMockClient();
        Should.Throw<ArgumentNullException>(() =>
            new AzureServiceBusMessagePublisher(client, null!,
                Options.Create(new EncinaAzureServiceBusOptions())));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        var client = CreateMockClient();
        Should.Throw<ArgumentNullException>(() =>
            new AzureServiceBusMessagePublisher(client,
                NullLogger<AzureServiceBusMessagePublisher>.Instance, null!));
    }

    [Fact]
    public void Constructor_ValidArgs_Constructs()
    {
        var client = CreateMockClient();
        var sut = new AzureServiceBusMessagePublisher(client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(new EncinaAzureServiceBusOptions()));
        sut.ShouldNotBeNull();
    }

    // ─── AzureServiceBusMessagePublisher method guards ───

    [Fact]
    public async Task SendToQueueAsync_NullMessage_Throws()
    {
        var client = CreateMockClient();
        var sut = new AzureServiceBusMessagePublisher(client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(new EncinaAzureServiceBusOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SendToQueueAsync<object>(null!));
    }

    [Fact]
    public async Task PublishToTopicAsync_NullMessage_Throws()
    {
        var client = CreateMockClient();
        var sut = new AzureServiceBusMessagePublisher(client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(new EncinaAzureServiceBusOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.PublishToTopicAsync<object>(null!));
    }

    // ─── ServiceCollectionExtensions guards ───

    [Fact]
    public void AddEncinaAzureServiceBus_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaAzureServiceBus(o =>
                o.ConnectionString = "Endpoint=sb://x.servicebus.windows.net/;SharedAccessKeyName=y;SharedAccessKey=dA=="));
    }

    [Fact]
    public void AddEncinaAzureServiceBus_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaAzureServiceBus(null!));
    }

    [Fact]
    public void AddEncinaAzureServiceBus_EmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaAzureServiceBus(_ => { }));
    }

    [Fact]
    public void AddEncinaAzureServiceBus_ValidConfig_Registers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddEncinaAzureServiceBus(o =>
            o.ConnectionString = "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==");

        result.ShouldNotBeNull();
    }

    // ─── AzureServiceBusHealthCheck ───

    [Fact]
    public void AzureServiceBusHealthCheck_Constructs()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new AzureServiceBusHealthCheck(sp, null);
        sut.ShouldNotBeNull();
    }

    // ─── EncinaAzureServiceBusOptions ───

    [Fact]
    public void EncinaAzureServiceBusOptions_Defaults()
    {
        var options = new EncinaAzureServiceBusOptions();
        options.ShouldNotBeNull();
        options.ConnectionString.ShouldBe(string.Empty);
    }

    // ─── ScheduleAsync null guard (regression) ───

    [Fact]
    public async Task ScheduleAsync_NullMessage_Throws()
    {
        var client = CreateMockClient();
        var sut = new AzureServiceBusMessagePublisher(client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(new EncinaAzureServiceBusOptions()));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.ScheduleAsync<object>(null!, DateTimeOffset.UtcNow));
    }
}