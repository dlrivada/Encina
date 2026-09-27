using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Health;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.EntityFrameworkCore.Tenancy;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.Scheduling;
using Encina.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    #region Inbox Pipeline Behavior Registration (#1333)

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseInboxTrue_RegistersInboxPipelineBehavior()
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

        // Assert - InboxPipelineBehavior is registered as open generic, matching every other
        // provider's UseInbox registration (ADO.NET, Dapper, and now EF Core via the shared
        // helper).
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(InboxPipelineBehavior<,>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseInboxFalse_DoesNotRegisterInboxPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseInbox = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(InboxPipelineBehavior<,>));
        descriptor.ShouldBeNull();
    }

    #endregion

    #region Saga Runner Registration (#1333)

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseSagasTrue_ResolvesSagaRunnerAndNotFoundDispatcher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseSagas = true;
        });

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        // Assert - the low-ceremony saga runner and the not-found dispatcher, previously only
        // registered by the shared ADO.NET/Dapper helper, must resolve for EF Core too (#1333).
        scope.ServiceProvider.GetRequiredService<ISagaRunner>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ISagaNotFoundDispatcher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseSagasFalse_DoesNotRegisterSagaRunner()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseSagas = false;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ISagaRunner>().ShouldBeNull();
        provider.GetService<ISagaNotFoundDispatcher>().ShouldBeNull();
    }

    #endregion

    #region All Messaging Patterns Validated (#1333)

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithAllMessagingPatternsEnabled_ValidatesAndResolvesEveryService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
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
        });

        // Assert - the provider builds under ValidateOnBuild/ValidateScopes (proving the DI graph
        // for every enabled messaging pattern is complete) and every service the shared helper
        // registers for EF Core is resolvable, closing the registration-completeness gap in #1333.
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        provider.GetService<OutboxOptions>().ShouldNotBeNull();
        provider.GetService<InboxOptions>().ShouldNotBeNull();
        provider.GetService<SagaOptions>().ShouldNotBeNull();
        provider.GetService<SchedulingOptions>().ShouldNotBeNull();

        scope.ServiceProvider.GetRequiredService<IOutboxStore>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IInboxStore>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IScheduledMessageStore>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ISagaRunner>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ISagaNotFoundDispatcher>().ShouldNotBeNull();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(InboxPipelineBehavior<,>));
        services.ShouldContain(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(global::Encina.EntityFrameworkCore.TransactionPipelineBehavior<,>));
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

    #region Tenancy Registration Tests

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseTenancy_RegistersEfCoreTenancyOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseTenancy = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<EfCoreTenancyOptions>>();
        options.ShouldNotBeNull();
        options.Value.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseTenancy_RegistersTenantSchemaConfigurator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseTenancy = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var schemaConfigurator = provider.GetService<ITenantSchemaConfigurator>();
        schemaConfigurator.ShouldNotBeNull();
        schemaConfigurator.ShouldBeOfType<DefaultTenantSchemaConfigurator>();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseTenancy_PropagatesOptionsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseTenancy = true;
            config.TenancyOptions.AutoAssignTenantId = false;
            config.TenancyOptions.ValidateTenantOnSave = false;
            config.TenancyOptions.UseQueryFilters = false;
            config.TenancyOptions.ThrowOnMissingTenantContext = false;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EfCoreTenancyOptions>>();
        options.Value.AutoAssignTenantId.ShouldBeFalse();
        options.Value.ValidateTenantOnSave.ShouldBeFalse();
        options.Value.UseQueryFilters.ShouldBeFalse();
        options.Value.ThrowOnMissingTenantContext.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseTenancyDisabled_DoesNotRegisterTenancyServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseTenancy = false;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<EfCoreTenancyOptions>>();
        options.ShouldBeNull();
        var schemaConfigurator = provider.GetService<ITenantSchemaConfigurator>();
        schemaConfigurator.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithUseTenancy_DefaultOptionsAreCorrect()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseTenancy = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert - Default values should be true
        var options = provider.GetRequiredService<IOptions<EfCoreTenancyOptions>>();
        options.Value.AutoAssignTenantId.ShouldBeTrue();
        options.Value.ValidateTenantOnSave.ShouldBeTrue();
        options.Value.UseQueryFilters.ShouldBeTrue();
        options.Value.ThrowOnMissingTenantContext.ShouldBeTrue();
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
            config.UseTenancy = true;
            config.ProviderHealthCheck.Enabled = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert - All services registered
        provider.GetService<OutboxOptions>().ShouldNotBeNull();
        provider.GetService<InboxOptions>().ShouldNotBeNull();
        provider.GetService<SagaOptions>().ShouldNotBeNull();
        provider.GetService<SchedulingOptions>().ShouldNotBeNull();
        provider.GetService<IEncinaHealthCheck>().ShouldNotBeNull();
        provider.GetService<IOptions<EfCoreTenancyOptions>>().ShouldNotBeNull();
        provider.GetService<ITenantSchemaConfigurator>().ShouldNotBeNull();
    }

    #endregion
}
