using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.RabbitMQ.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaRabbitMQ_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddEncinaRabbitMQ());
    }

    [Fact]
    public void AddEncinaRabbitMQ_WithoutConfiguration_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRabbitMQ();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaRabbitMQOptions>>();

        options.Value.HostName.ShouldBe("localhost");
        options.Value.Port.ShouldBe(5672);
        options.Value.VirtualHost.ShouldBe("/");
        options.Value.UserName.ShouldBe("guest");
        options.Value.Password.ShouldBe("guest");
        options.Value.ExchangeName.ShouldBe("encina");
        options.Value.UsePublisherConfirms.ShouldBeTrue();
        options.Value.PrefetchCount.ShouldBe((ushort)10);
        options.Value.Durable.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaRabbitMQ_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRabbitMQ(opt =>
        {
            opt.HostName = "custom-host";
            opt.Port = 5673;
            opt.VirtualHost = "/custom";
            opt.UserName = "admin";
            opt.Password = "secret";
            opt.ExchangeName = "custom-exchange";
            opt.UsePublisherConfirms = false;
            opt.PrefetchCount = 20;
            opt.Durable = false;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaRabbitMQOptions>>();

        options.Value.HostName.ShouldBe("custom-host");
        options.Value.Port.ShouldBe(5673);
        options.Value.VirtualHost.ShouldBe("/custom");
        options.Value.UserName.ShouldBe("admin");
        options.Value.Password.ShouldBe("secret");
        options.Value.ExchangeName.ShouldBe("custom-exchange");
        options.Value.UsePublisherConfirms.ShouldBeFalse();
        options.Value.PrefetchCount.ShouldBe((ushort)20);
        options.Value.Durable.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaRabbitMQ_RegistersPublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRabbitMQ();

        // Assert - Verify publisher is registered (will fail at resolution without real connection)
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRabbitMQMessagePublisher));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaRabbitMQ_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaRabbitMQ();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaRabbitMQ_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRabbitMQ(opt =>
        {
            opt.ProviderHealthCheck.Enabled = true;
            opt.ProviderHealthCheck.Name = "custom-rabbitmq";
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaRabbitMQ_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRabbitMQ(opt =>
        {
            opt.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaRabbitMQ_RegistersOptionsWithConfigure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRabbitMQ(opt => opt.HostName = "test-host");

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EncinaRabbitMQOptions>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaRabbitMQ_MultipleInvocations_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRabbitMQ();
        services.AddEncinaRabbitMQ();

        // Assert - Should only have one publisher registration due to TryAddScoped
        var publisherDescriptors = services.Where(d =>
            d.ServiceType == typeof(IRabbitMQMessagePublisher)).ToList();
        publisherDescriptors.Count.ShouldBe(1);
    }
}
