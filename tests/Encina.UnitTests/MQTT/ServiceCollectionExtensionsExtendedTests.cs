using Encina.Messaging.Health;
using Encina.MQTT;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.MQTT;

/// <summary>
/// Extended unit tests for MQTT <see cref="ServiceCollectionExtensions"/>.
/// Covers DI registration paths: default config, custom config, health check registration.
/// Note: actual MqttClient connection is not tested here (integration test).
/// </summary>
public sealed class ServiceCollectionExtensionsExtendedTests
{
    [Fact]
    public void AddEncinaMQTT_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaMQTT());
    }

    [Fact]
    public void AddEncinaMQTT_WithNullConfigure_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - null configure should be allowed
        services.AddEncinaMQTT(null);

        // Assert - should register EncinaMQTTOptions via Configure
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EncinaMQTTOptions>>();
        options.Value.ShouldNotBeNull();
        options.Value.Host.ShouldBe("localhost");
    }

    [Fact]
    public void AddEncinaMQTT_WithConfigure_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt =>
        {
            opt.Host = "mqtt.example.com";
            opt.Port = 8883;
            opt.TopicPrefix = "myapp";
            opt.QualityOfService = MqttQualityOfService.ExactlyOnce;
            opt.UseTls = true;
            opt.CleanSession = false;
            opt.KeepAliveSeconds = 120;
            opt.Username = "admin";
            opt.Password = "secret";
        });

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EncinaMQTTOptions>>();
        options.Value.Host.ShouldBe("mqtt.example.com");
        options.Value.Port.ShouldBe(8883);
        options.Value.TopicPrefix.ShouldBe("myapp");
        options.Value.QualityOfService.ShouldBe(MqttQualityOfService.ExactlyOnce);
        options.Value.UseTls.ShouldBeTrue();
        options.Value.CleanSession.ShouldBeFalse();
        options.Value.KeepAliveSeconds.ShouldBe(120);
    }

    [Fact]
    public void AddEncinaMQTT_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - health check is enabled by default
        services.AddEncinaMQTT(opt =>
        {
            opt.Host = "localhost";
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMQTT_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt =>
        {
            opt.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaMQTT_RegistersIMQTTMessagePublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMQTTMessagePublisher));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
