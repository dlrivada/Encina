using Encina.Messaging.Health;
using Encina.MQTT;
using MQTTnet;

namespace Encina.UnitTests.MQTT;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMQTT_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => services!.AddEncinaMQTT());
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaMQTT_WithoutConfiguration_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaMQTTOptions>>();

        options.Value.Host.ShouldBe("localhost");
        options.Value.Port.ShouldBe(1883);
        options.Value.ClientId.ShouldStartWith("encina-");
        options.Value.TopicPrefix.ShouldBe("encina");
        options.Value.Username.ShouldBeNull();
        options.Value.Password.ShouldBeNull();
        options.Value.QualityOfService.ShouldBe(MqttQualityOfService.AtLeastOnce);
        options.Value.UseTls.ShouldBeFalse();
        options.Value.CleanSession.ShouldBeTrue();
        options.Value.KeepAliveSeconds.ShouldBe(60);
    }

    [Fact]
    public void AddEncinaMQTT_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt =>
        {
            opt.Host = "mqtt.example.com";
            opt.Port = 8883;
            opt.ClientId = "my-client";
            opt.TopicPrefix = "myapp";
            opt.Username = "admin";
            opt.Password = "secret";
            opt.QualityOfService = MqttQualityOfService.ExactlyOnce;
            opt.UseTls = true;
            opt.CleanSession = false;
            opt.KeepAliveSeconds = 120;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaMQTTOptions>>();

        options.Value.Host.ShouldBe("mqtt.example.com");
        options.Value.Port.ShouldBe(8883);
        options.Value.ClientId.ShouldBe("my-client");
        options.Value.TopicPrefix.ShouldBe("myapp");
        options.Value.Username.ShouldBe("admin");
        options.Value.Password.ShouldBe("secret");
        options.Value.QualityOfService.ShouldBe(MqttQualityOfService.ExactlyOnce);
        options.Value.UseTls.ShouldBeTrue();
        options.Value.CleanSession.ShouldBeFalse();
        options.Value.KeepAliveSeconds.ShouldBe(120);
    }

    [Fact]
    public void AddEncinaMQTT_RegistersPublisherAsScoped()
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

    [Fact]
    public void AddEncinaMQTT_RegistersClientAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMqttClient));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaMQTT_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaMQTT();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaMQTT_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt =>
        {
            opt.ProviderHealthCheck.Enabled = true;
            opt.ProviderHealthCheck.Name = "custom-mqtt";
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
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
    public void AddEncinaMQTT_MultipleInvocations_DoesNotDuplicatePublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT();
        services.AddEncinaMQTT();

        // Assert - Should only have one publisher registration due to TryAddScoped
        var publisherDescriptors = services.Where(d =>
            d.ServiceType == typeof(IMQTTMessagePublisher)).ToList();
        publisherDescriptors.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(MqttQualityOfService.AtMostOnce)]
    [InlineData(MqttQualityOfService.AtLeastOnce)]
    [InlineData(MqttQualityOfService.ExactlyOnce)]
    public void AddEncinaMQTT_WithDifferentQoS_RegistersCorrectly(MqttQualityOfService qos)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt => opt.QualityOfService = qos);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMqttClient));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMQTT_WithCredentials_RegistersClientWithCredentials()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt =>
        {
            opt.Username = "testuser";
            opt.Password = "testpass";
        });

        // Assert - verify options are configured
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaMQTTOptions>>();

        options.Value.Username.ShouldBe("testuser");
        options.Value.Password.ShouldBe("testpass");
    }

    [Fact]
    public void AddEncinaMQTT_WithTlsEnabled_RegistersClientWithTls()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt =>
        {
            opt.UseTls = true;
        });

        // Assert - verify options are configured
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaMQTTOptions>>();

        options.Value.UseTls.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaMQTT_WithEmptyUsername_DoesNotUseCredentials()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt =>
        {
            opt.Username = string.Empty;
            opt.Password = "password";
        });

        // Assert - verify options are configured with empty username
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaMQTTOptions>>();

        options.Value.Username.ShouldBe(string.Empty);
    }

    [Fact]
    public void AddEncinaMQTT_WithNullUsername_DoesNotUseCredentials()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMQTT(opt =>
        {
            opt.Username = null;
            opt.Password = "password";
        });

        // Assert - verify options are configured with null username
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaMQTTOptions>>();

        options.Value.Username.ShouldBeNull();
    }
}
