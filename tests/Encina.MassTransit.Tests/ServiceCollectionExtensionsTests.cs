using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Encina.MassTransit;

namespace Encina.MassTransit.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMassTransit_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMassTransit();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<EncinaMassTransitOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaMassTransit_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMassTransit(options =>
        {
            options.ThrowOnMediatorError = false;
            options.QueueNamePrefix = "custom-prefix";
            options.AutoRegisterRequestConsumers = false;
            options.AutoRegisterNotificationConsumers = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EncinaMassTransitOptions>>();
        options.Value.ThrowOnMediatorError.Should().BeFalse();
        options.Value.QueueNamePrefix.Should().Be("custom-prefix");
        options.Value.AutoRegisterRequestConsumers.Should().BeFalse();
        options.Value.AutoRegisterNotificationConsumers.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaMassTransit_RegistersMessagePublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMassTransit();
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMassTransitMessagePublisher));

        // Assert
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(MassTransitMessagePublisher));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaMassTransit_RegistersGenericRequestConsumer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMassTransit();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(MassTransitRequestConsumer<,>));

        // Assert
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaMassTransit_RegistersGenericNotificationConsumer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMassTransit();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(MassTransitNotificationConsumer<>));

        // Assert
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaMassTransit_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaMassTransit();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaMassTransit_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaMassTransit());
    }

    [Fact]
    public void AddEncinaMassTransit_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddEncinaMassTransit(null!));
    }

    [Fact]
    public void EncinaMassTransitOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new EncinaMassTransitOptions();

        // Assert
        options.ThrowOnMediatorError.Should().BeTrue();
        options.QueueNamePrefix.Should().Be("Encina");
        options.AutoRegisterRequestConsumers.Should().BeTrue();
        options.AutoRegisterNotificationConsumers.Should().BeTrue();
    }
}
