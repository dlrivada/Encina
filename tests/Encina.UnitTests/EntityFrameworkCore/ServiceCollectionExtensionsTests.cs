using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Health;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    #region AddEncinaEntityFrameworkCore with Configuration

    [Fact]
    public void AddEncinaEntityFrameworkCore_ValidConfiguration_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        var result = services.AddEncinaEntityFrameworkCore<TestDbContext>(_ => { });

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_RegistersDbContextMapping()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(_ => { });
        using var provider = services.BuildServiceProvider();

        // Assert
        var dbContext = provider.GetService<DbContext>();
        dbContext.ShouldNotBeNull();
        dbContext.ShouldBeOfType<TestDbContext>();
    }

    #endregion

    #region AddEncinaEntityFrameworkCore without Configuration

    [Fact]
    public void AddEncinaEntityFrameworkCore_NoConfiguration_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        var result = services.AddEncinaEntityFrameworkCore<TestDbContext>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_NoConfiguration_RegistersDbContextMapping()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>();
        using var provider = services.BuildServiceProvider();

        // Assert
        var dbContext = provider.GetService<DbContext>();
        dbContext.ShouldNotBeNull();
        dbContext.ShouldBeOfType<TestDbContext>();
    }

    #endregion

    #region Pattern Registration Tests

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseTransactions_RegistersTransactionBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseTransactions = true;
        });

        // Assert - TransactionPipelineBehavior is registered as open generic
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(global::Encina.EntityFrameworkCore.TransactionPipelineBehavior<,>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseOutbox_RegistersOutboxServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseOutbox = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<OutboxOptions>().ShouldNotBeNull();
        provider.GetService<IOutboxStore>().ShouldNotBeNull();
        provider.GetService<IOutboxMessageFactory>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseInbox_RegistersInboxServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseInbox = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<InboxOptions>().ShouldNotBeNull();
        provider.GetService<IInboxStore>().ShouldNotBeNull();
        provider.GetService<IInboxMessageFactory>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseSagas_RegistersSagaServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseSagas = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<SagaOptions>().ShouldNotBeNull();
        provider.GetService<ISagaStore>().ShouldNotBeNull();
        provider.GetService<ISagaStateFactory>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseScheduling_RegistersSchedulingServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseScheduling = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<SchedulingOptions>().ShouldNotBeNull();
        provider.GetService<IScheduledMessageStore>().ShouldNotBeNull();
        provider.GetService<IScheduledMessageFactory>().ShouldNotBeNull();
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithProviderHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.ProviderHealthCheck.Enabled = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var healthCheck = provider.GetService<IEncinaHealthCheck>();
        healthCheck.ShouldNotBeNull();
        healthCheck.ShouldBeOfType<EntityFrameworkCoreHealthCheck>();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithProviderHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.ProviderHealthCheck.Enabled = false;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var healthCheck = provider.GetService<IEncinaHealthCheck>();
        healthCheck.ShouldBeNull();
    }

    #endregion

    #region All Patterns Enabled

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithAllPatternsEnabled_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseTransactions = true;
            config.UseOutbox = true;
            config.UseInbox = true;
            config.UseSagas = true;
            config.UseScheduling = true;
            config.ProviderHealthCheck.Enabled = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert - All services registered
        provider.GetService<OutboxOptions>().ShouldNotBeNull();
        provider.GetService<InboxOptions>().ShouldNotBeNull();
        provider.GetService<SagaOptions>().ShouldNotBeNull();
        provider.GetService<SchedulingOptions>().ShouldNotBeNull();
        provider.GetService<IEncinaHealthCheck>().ShouldNotBeNull();
    }

    #endregion
}
