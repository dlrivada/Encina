using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus;
using Encina.Messaging.Health;

namespace Encina.UnitTests.AzureServiceBus;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private const string ValidConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TestKey123456789012345678901234567890==";

    [Fact]
    public void AddEncinaAzureServiceBus_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            services!.AddEncinaAzureServiceBus(opt => opt.ConnectionString = ValidConnectionString));
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAzureServiceBus_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            services.AddEncinaAzureServiceBus(null!));
        ex.ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddEncinaAzureServiceBus_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            services.AddEncinaAzureServiceBus(opt => opt.ConnectionString = ""));
        ex.ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddEncinaAzureServiceBus_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            services.AddEncinaAzureServiceBus(opt => { }));
        ex.ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddEncinaAzureServiceBus_WithValidConnectionString_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureServiceBus(opt =>
        {
            opt.ConnectionString = ValidConnectionString;
            opt.DefaultQueueName = "test-queue";
            opt.DefaultTopicName = "test-topic";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaAzureServiceBusOptions>>();
        options.Value.ConnectionString.ShouldBe(ValidConnectionString);
        options.Value.DefaultQueueName.ShouldBe("test-queue");
        options.Value.DefaultTopicName.ShouldBe("test-topic");
    }

    [Fact]
    public void AddEncinaAzureServiceBus_RegistersPublisherAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureServiceBus(opt => opt.ConnectionString = ValidConnectionString);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAzureServiceBusMessagePublisher));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaAzureServiceBus_RegistersServiceBusClientAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureServiceBus(opt => opt.ConnectionString = ValidConnectionString);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ServiceBusClient));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaAzureServiceBus_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAzureServiceBus(opt => opt.ConnectionString = ValidConnectionString);

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAzureServiceBus_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureServiceBus(opt =>
        {
            opt.ConnectionString = ValidConnectionString;
            opt.ProviderHealthCheck.Enabled = true;
            opt.ProviderHealthCheck.Name = "custom-azure-sb";
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaAzureServiceBus_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureServiceBus(opt =>
        {
            opt.ConnectionString = ValidConnectionString;
            opt.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaAzureServiceBus_AppliesAllOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureServiceBus(opt =>
        {
            opt.ConnectionString = ValidConnectionString;
            opt.DefaultQueueName = "custom-queue";
            opt.DefaultTopicName = "custom-topic";
            opt.SubscriptionName = "custom-subscription";
            opt.UseSessions = true;
            opt.MaxConcurrentCalls = 10;
            opt.PrefetchCount = 20;
            opt.MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10);
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaAzureServiceBusOptions>>();
        options.Value.DefaultQueueName.ShouldBe("custom-queue");
        options.Value.DefaultTopicName.ShouldBe("custom-topic");
        options.Value.SubscriptionName.ShouldBe("custom-subscription");
        options.Value.UseSessions.ShouldBeTrue();
        options.Value.MaxConcurrentCalls.ShouldBe(10);
        options.Value.PrefetchCount.ShouldBe(20);
        options.Value.MaxAutoLockRenewalDuration.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void AddEncinaAzureServiceBus_MultipleInvocations_DoesNotDuplicatePublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureServiceBus(opt => opt.ConnectionString = ValidConnectionString);
        services.AddEncinaAzureServiceBus(opt => opt.ConnectionString = ValidConnectionString);

        // Assert - Should only have one publisher registration due to TryAddScoped
        var publisherDescriptors = services.Where(d =>
            d.ServiceType == typeof(IAzureServiceBusMessagePublisher)).ToList();
        publisherDescriptors.Count.ShouldBe(1);
    }
}
