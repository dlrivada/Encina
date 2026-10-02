using Encina.Messaging.Health;
using Encina.NATS;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Encina.UnitTests.NATS;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private const string RemoteUrl = "nats://nats.example.com:4222";

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
    public void AddEncinaNATS_WithoutConfiguration_ThrowsBecauseDefaultUrlIsLoopback()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Should.Throw<OptionsValidationException>(() => services.AddEncinaNATS());
        ex.Message.ShouldContain("AllowLocalEndpoints");
    }

    [Fact]
    public void AddEncinaNATS_WithLocalEndpointsAllowed_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS(opt => opt.AllowLocalEndpoints = true);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaNATSOptions>>();

        options.Value.Url.ShouldBe("nats://localhost:4222");
        options.Value.AllowLocalEndpoints.ShouldBeTrue();
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
            opt.Url = RemoteUrl;
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

        options.Value.Url.ShouldBe(RemoteUrl);
        options.Value.SubjectPrefix.ShouldBe("myapp");
        options.Value.UseJetStream.ShouldBeTrue();
        options.Value.StreamName.ShouldBe("my-stream");
        options.Value.ConsumerName.ShouldBe("my-consumer");
        options.Value.UseDurableConsumer.ShouldBeTrue();
        options.Value.AckWait.ShouldBe(TimeSpan.FromSeconds(60));
        options.Value.MaxDeliver.ShouldBe(5);
    }

    [Theory]
    [InlineData("nats://169.254.169.254:4222")]
    [InlineData("nats://[::ffff:127.0.0.1]:4222")]
    [InlineData("http://nats.example.com:4222")]
    [InlineData("nats://nats.example.com:4222,nats://0.0.0.0:4222")]
    public void AddEncinaNATS_WithUnsafeUrl_ThrowsOptionsValidationException(string url)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Should.Throw<OptionsValidationException>(() => services.AddEncinaNATS(opt => opt.Url = url));
        ex.Message.ShouldContain("Url");
    }

    [Fact]
    public void AddEncinaNATS_RegistersOptionsValidator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS(opt => opt.Url = RemoteUrl);

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<EncinaNATSOptions>) &&
            d.ImplementationType == typeof(EncinaNATSOptionsValidator));
    }

    [Fact]
    public async Task AddEncinaNATS_ValidOptions_ProviderBuildsWithValidateOnBuildAndScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // NATSMessagePublisher requires INatsJSContext, which is only registered with JetStream.
        services.AddEncinaNATS(opt =>
        {
            opt.Url = RemoteUrl;
            opt.UseJetStream = true;
        });

        // Act (NatsConnection is only IAsyncDisposable)
        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // Assert
        provider.GetRequiredService<IOptions<EncinaNATSOptions>>().Value.Url.ShouldBe(RemoteUrl);
        provider.GetRequiredService<INatsConnection>().Opts.Url.ShouldBe(RemoteUrl);
    }

    [Fact]
    public void AddEncinaNATS_WithLocalEndpointsAllowed_LogsWarningOnce()
    {
        // Arrange
        var logger = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(logger));
        services.AddEncinaNATS(opt => opt.AllowLocalEndpoints = true);

        // Act
        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<EncinaNATSOptions>>().Value;
        _ = provider.GetRequiredService<IOptionsMonitor<EncinaNATSOptions>>().CurrentValue;

        // Assert
        logger.Count(4209, LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public void AddEncinaNATS_RegistersPublisherAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaNATS(opt => opt.Url = RemoteUrl);

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
        services.AddEncinaNATS(opt => opt.Url = RemoteUrl);

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
        services.AddEncinaNATS(opt =>
        {
            opt.Url = RemoteUrl;
            opt.UseJetStream = false;
        });

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
        services.AddEncinaNATS(opt =>
        {
            opt.Url = RemoteUrl;
            opt.UseJetStream = true;
        });

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
        var result = services.AddEncinaNATS(opt => opt.Url = RemoteUrl);

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
            opt.Url = RemoteUrl;
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
            opt.Url = RemoteUrl;
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
        services.AddEncinaNATS(opt => opt.Url = RemoteUrl);
        services.AddEncinaNATS(opt => opt.Url = RemoteUrl);

        // Assert - Should only have one publisher registration due to TryAddScoped
        var publisherDescriptors = services.Where(d =>
            d.ServiceType == typeof(INATSMessagePublisher)).ToList();
        publisherDescriptors.Count.ShouldBe(1);
    }
}
