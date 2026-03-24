using Encina.Redis.PubSub;
using StackExchange.Redis;

namespace Encina.UnitTests.Messaging.RedisPubSub;

/// <summary>
/// Tests for the <see cref="ServiceCollectionExtensions"/> class.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    #region Null Guards

    [Fact]
    public void AddEncinaRedisPubSub_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddEncinaRedisPubSub());
    }

    #endregion

    #region Service Registration

    [Fact]
    public void AddEncinaRedisPubSub_RegistersIRedisPubSubMessagePublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRedisPubSub();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IRedisPubSubMessagePublisher) &&
            d.ImplementationType == typeof(RedisPubSubMessagePublisher) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaRedisPubSub_RegistersIConnectionMultiplexerAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRedisPubSub();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IConnectionMultiplexer) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaRedisPubSub_RegistersOptionsConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRedisPubSub(opt =>
        {
            opt.ChannelPrefix = "custom";
            opt.EventChannel = "my-events";
            opt.CommandChannel = "my-commands";
            opt.ConnectTimeout = 10000;
            opt.SyncTimeout = 3000;
            opt.UsePatternSubscription = true;
        });

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<EncinaRedisPubSubOptions>>().Value;

        options.ChannelPrefix.ShouldBe("custom");
        options.EventChannel.ShouldBe("my-events");
        options.CommandChannel.ShouldBe("my-commands");
        options.ConnectTimeout.ShouldBe(10000);
        options.SyncTimeout.ShouldBe(3000);
        options.UsePatternSubscription.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaRedisPubSub_WithNullConfigure_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRedisPubSub(null);

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<EncinaRedisPubSubOptions>>().Value;

        options.ChannelPrefix.ShouldBe("encina");
        options.EventChannel.ShouldBe("events");
        options.CommandChannel.ShouldBe("commands");
        options.ConnectTimeout.ShouldBe(5000);
        options.SyncTimeout.ShouldBe(5000);
        options.UsePatternSubscription.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaRedisPubSub_WithConnectionString_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRedisPubSub(opt =>
        {
            opt.ConnectionString = "my-redis:6380,password=secret";
        });

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<EncinaRedisPubSubOptions>>().Value;
        options.ConnectionString.ShouldBe("my-redis:6380,password=secret");
    }

    #endregion

    #region TryAdd Behavior

    [Fact]
    public void AddEncinaRedisPubSub_CalledTwice_DoesNotDuplicatePublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRedisPubSub();
        services.AddEncinaRedisPubSub();

        // Assert — TryAddScoped prevents duplicates
        var publisherDescriptors = services
            .Where(d => d.ServiceType == typeof(IRedisPubSubMessagePublisher))
            .ToList();
        publisherDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaRedisPubSub_CalledTwice_DoesNotDuplicateConnectionMultiplexer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRedisPubSub();
        services.AddEncinaRedisPubSub();

        // Assert — TryAddSingleton prevents duplicates
        var multiplexerDescriptors = services
            .Where(d => d.ServiceType == typeof(IConnectionMultiplexer))
            .ToList();
        multiplexerDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaRedisPubSub_WithExistingConnectionMultiplexer_DoesNotOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingMultiplexer = Substitute.For<IConnectionMultiplexer>();
        services.AddSingleton(existingMultiplexer);

        // Act
        services.AddEncinaRedisPubSub();

        // Assert — The pre-registered singleton should be kept
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IConnectionMultiplexer))
            .ToList();
        descriptors.Count.ShouldBe(1);
        descriptors[0].ImplementationInstance.ShouldBeSameAs(existingMultiplexer);
    }

    #endregion

    #region Chaining

    [Fact]
    public void AddEncinaRedisPubSub_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaRedisPubSub();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaRedisPubSub_WithConfigure_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaRedisPubSub(opt => opt.ChannelPrefix = "chain");

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion
}
