using Azure.Messaging.ServiceBus;

using Encina.AzureServiceBus;
using Encina.AzureServiceBus.Health;
using Encina.Messaging.Health;

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

        var publisherDescriptor = services.Single(sd => sd.ServiceType == typeof(IAzureServiceBusMessagePublisher));
        publisherDescriptor.ImplementationType.ShouldBe(typeof(AzureServiceBusMessagePublisher));
        publisherDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);

        var clientDescriptor = services.Single(sd => sd.ServiceType == typeof(ServiceBusClient));
        clientDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<EncinaAzureServiceBusOptions>>();
        var client = provider.GetService<ServiceBusClient>();
        var publisher = provider.GetService<IAzureServiceBusMessagePublisher>();

        options.ShouldNotBeNull();
        options.Value.ConnectionString.ShouldBe(connectionString);
        client.ShouldNotBeNull();
        publisher.ShouldBeOfType<AzureServiceBusMessagePublisher>();
    }

    [Fact]
    public async Task AddEncinaAzureServiceBus_WhenCalled_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        const string connectionString = "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

        var result = services.AddEncinaAzureServiceBus(o => o.ConnectionString = connectionString);

        result.ShouldBeSameAs(services);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddEncinaAzureServiceBus_ValidConfig_RegistersOptionsDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        const string connectionString = "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

        services.AddEncinaAzureServiceBus(o => o.ConnectionString = connectionString);

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaAzureServiceBusOptions>>();

        options.Value.DefaultQueueName.ShouldBe("encina-commands");
        options.Value.DefaultTopicName.ShouldBe("encina-events");
        options.Value.SubscriptionName.ShouldBe("default");
    }

    [Fact]
    public async Task AddEncinaAzureServiceBus_ValidConfig_ServiceBusClientIsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        const string connectionString = "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

        services.AddEncinaAzureServiceBus(o => o.ConnectionString = connectionString);

        await using var provider = services.BuildServiceProvider();
        var client1 = provider.GetRequiredService<ServiceBusClient>();
        var client2 = provider.GetRequiredService<ServiceBusClient>();

        client1.ShouldBeSameAs(client2);
    }

    [Fact]
    public async Task AddEncinaAzureServiceBus_ValidConfig_PublisherIsScopedDifferentPerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        const string connectionString = "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

        services.AddEncinaAzureServiceBus(o => o.ConnectionString = connectionString);

        await using var provider = services.BuildServiceProvider();

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var publisher1 = scope1.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();
        var publisher2 = scope2.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>();

        publisher1.ShouldNotBeSameAs(publisher2);
    }

    [Fact]
    public async Task AddEncinaAzureServiceBus_ValidConfig_CalledTwice_DoesNotDuplicateClientRegistration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        const string connectionString = "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

        services.AddEncinaAzureServiceBus(o => o.ConnectionString = connectionString);
        services.AddEncinaAzureServiceBus(o => o.ConnectionString = connectionString);

        var clientDescriptors = services.Where(sd => sd.ServiceType == typeof(ServiceBusClient)).ToList();
        clientDescriptors.Count.ShouldBe(1);

        var publisherDescriptors = services.Where(sd => sd.ServiceType == typeof(IAzureServiceBusMessagePublisher)).ToList();
        publisherDescriptors.Count.ShouldBe(1);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddEncinaAzureServiceBus_ValidConfig_HealthCheckRegisteredByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        const string connectionString = "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

        services.AddEncinaAzureServiceBus(o => o.ConnectionString = connectionString);

        var healthCheckDescriptor = services.SingleOrDefault(sd => sd.ServiceType == typeof(IEncinaHealthCheck));
        healthCheckDescriptor.ShouldNotBeNull();

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ScheduleAsync_NullMessage_Throws()
    {
        await using var client = CreateMockClient();
        var sut = new AzureServiceBusMessagePublisher(client,
            NullLogger<AzureServiceBusMessagePublisher>.Instance,
            Options.Create(new EncinaAzureServiceBusOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.ScheduleAsync<object>(null!, DateTimeOffset.UtcNow).AsTask());
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