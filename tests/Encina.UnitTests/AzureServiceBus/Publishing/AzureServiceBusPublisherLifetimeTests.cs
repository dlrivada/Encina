using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus;

namespace Encina.UnitTests.AzureServiceBus.Publishing;

/// <summary>
/// Regression tests for #1608: ending a scope that resolved the scoped publisher must not dispose
/// the DI-singleton <see cref="ServiceBusClient"/>.
/// </summary>
public sealed class AzureServiceBusPublisherLifetimeTests
{
    private const string ValidConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TestKey123456789012345678901234567890==";

    [Fact]
    public async Task ResolvingAndDisposingPublisherScope_LeavesSingletonClientUntouched()
    {
        // Arrange
        var client = Substitute.For<ServiceBusClient>();
        await using var provider = BuildProvider(client);

        // Act
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>().ShouldNotBeNull();
        }

        // Assert
        await client.Received(0).DisposeAsync();
        using var secondScope = provider.CreateScope();
        secondScope.ServiceProvider.GetRequiredService<ServiceBusClient>().ShouldBeSameAs(client);
        secondScope.ServiceProvider.GetRequiredService<IAzureServiceBusMessagePublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAzureServiceBus_BuildsWithScopeAndBuildValidation()
    {
        // Arrange
        var client = Substitute.For<ServiceBusClient>();

        // Act
        var act = () => BuildProvider(client).Dispose();

        // Assert
        Should.NotThrow(act);
    }

    private static ServiceProvider BuildProvider(ServiceBusClient client)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(client);
        services.AddEncinaAzureServiceBus(opt => opt.ConnectionString = ValidConnectionString);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
