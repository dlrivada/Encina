using Azure.Messaging.ServiceBus;

using Encina.AzureServiceBus;
using Encina.AzureServiceBus.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
    public async Task Constructor_NullLogger_Throws()
    {
        await using var client = CreateMockClient();
        Should.Throw<ArgumentNullException>(() =>
            new AzureServiceBusMessagePublisher(client, null!,
                Options.Create(new EncinaAzureServiceBusOptions())));
    }

    [Fact]
    public async Task Constructor_NullOptions_Throws()
    {
        await using var client = CreateMockClient();
        Should.Throw<ArgumentNullException>(() =>
            new AzureServiceBusMessagePublisher(client,
                NullLogger<AzureServiceBusMessagePublisher>.Instance, null!));
    }

    [Fact]
    public async Task Constructor_ValidArgs_Constructs()
    {
        await using var client = CreateMockClient();
        var sut = new AzureServiceBusMessagePublisher(client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(new EncinaAzureServiceBusOptions()));
        sut.ShouldNotBeNull();
    }

    // ─── AzureServiceBusMessagePublisher method guards ───

    [Fact]
    public async Task SendToQueueAsync_NullMessage_Throws()
    {
        await using var client = CreateMockClient();
        var sut = new AzureServiceBusMessagePublisher(client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(new EncinaAzureServiceBusOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SendToQueueAsync<object>(null!).AsTask());
    }

    [Fact]
    public async Task PublishToTopicAsync_NullMessage_Throws()
    {
        await using var client = CreateMockClient();
        var sut = new AzureServiceBusMessagePublisher(client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(new EncinaAzureServiceBusOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.PublishToTopicAsync<object>(null!).AsTask());
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
    public async Task AddEncinaAzureServiceBus_ValidConfig_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        const string connectionString = "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

        var result = services.AddEncinaAzureServiceBus(o => o.ConnectionString = connectionString);

        result.ShouldNotBeNull();
        services.ShouldContain(sd => sd.ServiceType == typeof(IAzureServiceBusMessagePublisher));

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<EncinaAzureServiceBusOptions>>();
        var client = provider.GetService<ServiceBusClient>();

        options.ShouldNotBeNull();
        options.Value.ConnectionString.ShouldBe(connectionString);
        client.ShouldNotBeNull();
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
}
