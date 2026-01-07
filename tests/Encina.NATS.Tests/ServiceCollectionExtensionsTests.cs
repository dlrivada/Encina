using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Encina.NATS.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaNATS_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => services!.AddEncinaNATS());
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaNATS_WithoutConfiguration_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaNATSOptions>>();

        options.Value.Url.ShouldBe("nats://localhost:4222");
        options.Value.SubjectPrefix.ShouldBe("encina");
        options.Value.UseJetStream.ShouldBeFalse();
        options.Value.StreamName.ShouldBe("ENCINA");
        options.Value.ConsumerName.ShouldBe("encina-consumer");
        options.Value.UseDurableConsumer.ShouldBeTrue();
        options.Value.AckWait.ShouldBe(TimeSpan.FromSeconds(30));
        options.Value.MaxDeliver.ShouldBe(5);
    }

    [Fact]
    public void AddEncinaNATS_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS(opt =>
        {
            opt.Url = "nats://nats.example.com:4222";
            opt.SubjectPrefix = "myapp";
            opt.UseJetStream = true;
            opt.StreamName = "my-stream";
            opt.ConsumerName = "my-consumer";
            opt.UseDurableConsumer = true;
            opt.AckWait = TimeSpan.FromSeconds(60);
            opt.MaxDeliver = 5;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaNATSOptions>>();

        options.Value.Url.ShouldBe("nats://nats.example.com:4222");
        options.Value.SubjectPrefix.ShouldBe("myapp");
        options.Value.UseJetStream.ShouldBeTrue();
        options.Value.StreamName.ShouldBe("my-stream");
        options.Value.ConsumerName.ShouldBe("my-consumer");
        options.Value.UseDurableConsumer.ShouldBeTrue();
        options.Value.AckWait.ShouldBe(TimeSpan.FromSeconds(60));
        options.Value.MaxDeliver.ShouldBe(5);
    }

    [Fact]
    public void AddEncinaNATS_RegistersPublisherAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(INATSMessagePublisher));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaNATS_RegistersConnectionAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(INatsConnection));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaNATS_WithoutJetStream_DoesNotRegisterJetStreamContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS(opt => opt.UseJetStream = false);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(INatsJSContext));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaNATS_WithJetStream_RegistersJetStreamContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS(opt => opt.UseJetStream = true);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(INatsJSContext));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaNATS_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaNATS();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaNATS_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS(opt =>
        {
            opt.ProviderHealthCheck.Enabled = true;
            opt.ProviderHealthCheck.Name = "custom-nats";
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaNATS_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS(opt =>
        {
            opt.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaNATS_MultipleInvocations_DoesNotDuplicatePublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS();
        services.AddEncinaNATS();

        // Assert - Should only have one publisher registration due to TryAddScoped
        var publisherDescriptors = services.Where(d =>
            d.ServiceType == typeof(INATSMessagePublisher)).ToList();
        publisherDescriptors.Count.ShouldBe(1);
    }
}
