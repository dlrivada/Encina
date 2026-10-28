using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus;
using Encina.AzureServiceBus.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static Encina.IntegrationTests.MessageBrokers.AzureServiceBus.AzureServiceBusTestSupport;

namespace Encina.IntegrationTests.MessageBrokers.AzureServiceBus;

/// <summary>
/// Integration tests for <see cref="AzureServiceBusHealthCheck"/> and the package registration against the emulator.
/// </summary>
[Collection(ServiceBusEmulatorFixture.CollectionName)]
[Trait("Category", "Integration")]
[Trait("Messaging", "AzureServiceBus")]
public sealed class AzureServiceBusHealthCheckIntegrationTests : IAsyncLifetime
{
    private readonly ServiceBusEmulatorFixture _fixture;

    public AzureServiceBusHealthCheckIntegrationTests(ServiceBusEmulatorFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CheckHealthAsync_WhenClientIsOpen_ReturnsHealthy()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        var healthCheck = new AzureServiceBusHealthCheck(provider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        var healthCheck = new AzureServiceBusHealthCheck(provider, new ProviderHealthCheckOptions { Name = "my-servicebus" });

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-servicebus");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClientIsClosed_ReturnsUnhealthy()
    {
        // Arrange: a separate client closed before the check, so the shared fixture client stays open.
        var closedClient = _fixture.CreateClient();
        await closedClient.DisposeAsync();
        var services = new ServiceCollection();
        services.AddSingleton(closedClient);
        await using var provider = services.BuildServiceProvider();
        var healthCheck = new AzureServiceBusHealthCheck(provider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("closed");
    }

    [Fact]
    public async Task AddEncinaAzureServiceBus_RegistersHealthCheckThatReportsHealthy()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);

        // Act
        var healthChecks = provider.GetServices<IEncinaHealthCheck>().ToList();
        var serviceBusCheck = healthChecks.OfType<AzureServiceBusHealthCheck>().Single();
        var result = await serviceBusCheck.CheckHealthAsync();

        // Assert
        serviceBusCheck.Name.ShouldBe(AzureServiceBusHealthCheck.DefaultName);
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void AddEncinaAzureServiceBus_WithClientRegisteredFirst_KeepsThatClient()
    {
        // Arrange
        using var provider = CreateServiceProvider(_fixture);

        // Act
        var client = provider.GetRequiredService<ServiceBusClient>();

        // Assert
        client.ShouldBeSameAs(_fixture.Client);
    }

    [Fact]
    public async Task AddEncinaAzureServiceBus_WithoutAClientRegistered_BuildsOneFromTheConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAzureServiceBus(options => options.ConnectionString = _fixture.ConnectionString);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // Act
        var client = provider.GetRequiredService<ServiceBusClient>();

        // Assert
        client.ShouldNotBeSameAs(_fixture.Client);
        client.IsClosed.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaAzureServiceBus_WithHealthCheckDisabled_RegistersNoHealthCheck()
    {
        // Arrange
        using var provider = CreateServiceProvider(_fixture, options => options.ProviderHealthCheck.Enabled = false);

        // Act
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();

        // Assert
        healthChecks.OfType<AzureServiceBusHealthCheck>().ShouldBeEmpty();
    }
}
