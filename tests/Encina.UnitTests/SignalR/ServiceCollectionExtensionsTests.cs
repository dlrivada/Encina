using Encina.SignalR;

namespace Encina.UnitTests.SignalR;

/// <summary>
/// Tests for the <see cref="ServiceCollectionExtensions"/> class.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaSignalR_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSignalR();

        // Assert - Inspect ServiceDescriptor directly instead of building provider
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISignalRNotificationBroadcaster));
        descriptor.ShouldNotBeNull("Expected ISignalRNotificationBroadcaster to be registered");
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(SignalRNotificationBroadcaster));
    }

    [Fact]
    public void AddEncinaSignalR_WithConfiguration_RegistersConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSignalR(options =>
        {
            options.EnableNotificationBroadcast = false;
            options.AuthorizationPolicy = "TestPolicy";
            options.IncludeDetailedErrors = true;
        });

        // Assert - Inspect ServiceCollection for IConfigureOptions<SignalROptions> registration
        var configureOptionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<SignalROptions>));
        configureOptionsDescriptor.ShouldNotBeNull("Expected IConfigureOptions<SignalROptions> to be registered");
        configureOptionsDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaSignalR_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddEncinaSignalR();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaSignalR_CalledTwice_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaSignalR();
        services.AddEncinaSignalR();

        // Assert
        var broadcasterDescriptors = services
            .Where(d => d.ServiceType == typeof(ISignalRNotificationBroadcaster))
            .ToList();
        broadcasterDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaSignalR_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert - should throw ArgumentNullException when configuration is null
        Should.Throw<ArgumentNullException>(() => services.AddEncinaSignalR(null!));
    }

    [Fact]
    public void AddSignalRBroadcasting_RegistersOpenGenericHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSignalRBroadcasting();

        // Assert
        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(INotificationHandler<>) &&
            d.ImplementationType == typeof(SignalRBroadcastHandler<>));
        handlerDescriptor.ShouldNotBeNull();
        handlerDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddSignalRBroadcasting_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSignalRBroadcasting();

        // Assert
        result.ShouldBeSameAs(services);
    }
}
