using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Encina.Cdc.Processing;
using Encina.IntegrationTests.Cdc.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Cdc;

/// <summary>
/// Integration tests for CDC <see cref="ServiceCollectionExtensions.AddEncinaCdc"/>
/// verifying that full DI wiring resolves correctly with a real <see cref="ServiceProvider"/>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CDC")]
public sealed class ServiceCollectionExtensionsIntegrationTests
{
    [Fact]
    public void AddEncinaCdc_MinimalConfiguration_ResolvesDispatcher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities");
        });

        // Act
        using var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetService<ICdcDispatcher>();

        // Assert
        dispatcher.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaCdc_MinimalConfiguration_ResolvesPositionStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities");
        });

        // Act
        using var sp = services.BuildServiceProvider();
        var store = sp.GetService<ICdcPositionStore>();

        // Assert
        store.ShouldNotBeNull();
        store.ShouldBeOfType<InMemoryCdcPositionStore>();
    }

    [Fact]
    public void AddEncinaCdc_WithCdcEnabled_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<ICdcConnector>(new TestCdcConnector());
        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities");
        });

        // Act
        using var sp = services.BuildServiceProvider();
        var hostedServices = sp.GetServices<IHostedService>();

        // Assert
        hostedServices.ShouldContain(s => s.GetType().Name == "CdcProcessor");
    }

    [Fact]
    public void AddEncinaCdc_WithoutCdcEnabled_DoesNotRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities");
        });

        // Act
        using var sp = services.BuildServiceProvider();
        var hostedServices = sp.GetServices<IHostedService>();

        // Assert
        hostedServices.ShouldNotContain(s => s.GetType().Name == "CdcProcessor");
    }

    [Fact]
    public void AddEncinaCdc_WithMessagingBridge_RegistersInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton(Substitute.For<IEncina>());
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithMessagingBridge();
        });

        // Act
        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var interceptors = scope.ServiceProvider.GetServices<ICdcEventInterceptor>();

        // Assert
        interceptors.ShouldNotBeEmpty();
        interceptors.ShouldContain(i => i.GetType() == typeof(CdcMessagingBridge));
    }

    [Fact]
    public void AddEncinaCdc_WithMessagingBridge_RegistersMessagingOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton(Substitute.For<IEncina>());
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithMessagingBridge(opts =>
                  {
                      opts.TopicPattern = "cdc.{tableName}.{operation}";
                  });
        });

        // Act
        using var sp = services.BuildServiceProvider();
        var messagingOptions = sp.GetService<CdcMessagingOptions>();

        // Assert
        messagingOptions.ShouldNotBeNull();
        messagingOptions.TopicPattern.ShouldBe("cdc.{tableName}.{operation}");
    }

    [Fact]
    public void AddEncinaCdc_WithOutboxCdc_RegistersOutboxHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton(Substitute.For<IEncina>());
        services.AddEncinaCdc(config =>
        {
            config.UseOutboxCdc("OutboxMessages");
        });

        // Act
        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var outboxHandler = scope.ServiceProvider.GetService<OutboxCdcHandler>();

        // Assert
        outboxHandler.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaCdc_HandlerRegisteredAsScoped_DifferentInstancesPerScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities");
        });

        // Act
        using var sp = services.BuildServiceProvider();
        IChangeEventHandler<TestEntity> handler1;
        IChangeEventHandler<TestEntity> handler2;

        using (var scope1 = sp.CreateScope())
        {
            handler1 = scope1.ServiceProvider.GetRequiredService<IChangeEventHandler<TestEntity>>();
        }

        using (var scope2 = sp.CreateScope())
        {
            handler2 = scope2.ServiceProvider.GetRequiredService<IChangeEventHandler<TestEntity>>();
        }

        // Assert - Different scopes produce different handler instances
        handler1.ShouldNotBeSameAs(handler2);
    }

    [Fact]
    public void AddEncinaCdc_ConfigurationRegisteredAsSingleton_SameInstanceAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithOptions(opts => opts.BatchSize = 42);
        });

        // Act
        using var sp = services.BuildServiceProvider();
        var config1 = sp.GetRequiredService<CdcConfiguration>();
        var config2 = sp.GetRequiredService<CdcConfiguration>();
        var options1 = sp.GetRequiredService<CdcOptions>();
        var options2 = sp.GetRequiredService<CdcOptions>();

        // Assert
        config1.ShouldBeSameAs(config2);
        options1.ShouldBeSameAs(options2);
        options1.BatchSize.ShouldBe(42);
    }

    [Fact]
    public void AddEncinaCdc_DefaultPositionStore_CanBeOverridden()
    {
        // Arrange
        var customStore = Substitute.For<ICdcPositionStore>();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton(customStore); // Register BEFORE AddEncinaCdc
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities");
        });

        // Act
        using var sp = services.BuildServiceProvider();
        var store = sp.GetService<ICdcPositionStore>();

        // Assert - Custom store registered before AddEncinaCdc takes priority (TryAdd pattern)
        store.ShouldBeSameAs(customStore);
    }

    [Fact]
    public void AddEncinaCdc_FullConfiguration_AllServicesResolvable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<ICdcConnector>(new TestCdcConnector());
        services.AddSingleton(Substitute.For<IEncina>());
        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithOptions(opts =>
                  {
                      opts.BatchSize = 200;
                      opts.MaxRetries = 5;
                  })
                  .WithMessagingBridge(opts =>
                  {
                      opts.TopicPattern = "cdc.{tableName}.{operation}";
                  })
                  .UseOutboxCdc();
        });

        // Act
        using var sp = services.BuildServiceProvider();

        // Assert - All services resolvable without exceptions
        sp.GetRequiredService<ICdcDispatcher>().ShouldNotBeNull();
        sp.GetRequiredService<ICdcPositionStore>().ShouldNotBeNull();
        sp.GetRequiredService<CdcConfiguration>().ShouldNotBeNull();
        sp.GetRequiredService<CdcOptions>().ShouldNotBeNull();
        sp.GetRequiredService<CdcMessagingOptions>().ShouldNotBeNull();
        sp.GetServices<IHostedService>().ShouldContain(s => s.GetType().Name == "CdcProcessor");

        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<IChangeEventHandler<TestEntity>>().ShouldNotBeNull();
        scope.ServiceProvider.GetServices<ICdcEventInterceptor>().ShouldNotBeEmpty();
        scope.ServiceProvider.GetRequiredService<OutboxCdcHandler>().ShouldNotBeNull();
    }
}
