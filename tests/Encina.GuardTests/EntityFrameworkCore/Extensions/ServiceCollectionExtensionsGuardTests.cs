using Encina.EntityFrameworkCore;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Extensions;

/// <summary>
/// Guard and validation tests for <see cref="ServiceCollectionExtensions"/>.
/// Tests configuration paths, DI registration, and edge cases that exercise
/// multiple source lines per test.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class ServiceCollectionExtensionsGuardTests
{
    #region AddEncinaEntityFrameworkCore Configuration Paths

    [Fact]
    public void AddEncinaEntityFrameworkCore_DefaultConfig_RegistersDbContextMapping()
    {
        // Arrange - exercises: config creation (line 135), configure callback,
        // TryAddScoped DbContext mapping (line 142), return (line 383)
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config => { });

        // Assert - DbContext should be resolvable
        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetService<DbContext>();
        dbContext.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_NoConfigOverload_RegistersDbContextMapping()
    {
        // Arrange - exercises the no-config overload (lines 396-403)
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>();

        // Assert
        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetService<DbContext>();
        dbContext.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseOutbox_RegistersOutboxServices()
    {
        // Arrange - exercises: config.UseOutbox branch (lines 151-157):
        // AddSingleton OutboxOptions, AddScoped IOutboxStore, AddScoped IOutboxMessageFactory,
        // AddScoped IRequestPostProcessor, AddHostedService OutboxProcessor
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config =>
        {
            config.UseOutbox = true;
        });

        // Assert - IOutboxStore should be registered
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOutboxStore));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseInbox_RegistersInboxServices()
    {
        // Arrange - exercises: config.UseInbox branch (lines 159-166)
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config =>
        {
            config.UseInbox = true;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IInboxStore));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseSagas_RegistersSagaServices()
    {
        // Arrange - exercises: config.UseSagas branch (lines 168-174)
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config =>
        {
            config.UseSagas = true;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISagaStore));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseScheduling_RegistersSchedulingServices()
    {
        // Arrange - exercises: config.UseScheduling branch (lines 176-182)
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config =>
        {
            config.UseScheduling = true;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IScheduledMessageStore));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_AllPatternsEnabled_RegistersAllServices()
    {
        // Arrange - exercises multiple configuration branches in a single call
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config =>
        {
            config.UseTransactions = true;
            config.UseOutbox = true;
            config.UseInbox = true;
            config.UseSagas = true;
            config.UseScheduling = true;
            config.UseAuditing = true;
            config.UseSoftDelete = true;
        });

        // Assert - spot check that multiple services are registered
        services.Any(d => d.ServiceType == typeof(IOutboxStore)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(IInboxStore)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(ISagaStore)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(IScheduledMessageStore)).ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_NoPatternsEnabled_OnlyRegistersBaseServices()
    {
        // Arrange - exercises: config creation, all if-checks fail, only base registrations
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config =>
        {
            // All defaults = false
        });

        // Assert - messaging stores should NOT be registered
        services.Any(d => d.ServiceType == typeof(IOutboxStore)).ShouldBeFalse();
        services.Any(d => d.ServiceType == typeof(IInboxStore)).ShouldBeFalse();
        services.Any(d => d.ServiceType == typeof(ISagaStore)).ShouldBeFalse();
        services.Any(d => d.ServiceType == typeof(IScheduledMessageStore)).ShouldBeFalse();
    }

    #endregion

    #region AddEncinaBulkOperations Guard

    [Fact]
    public void AddEncinaBulkOperations_NullServices_ThrowsArgumentNullException()
    {
        // Arrange - exercises the ArgumentNullException.ThrowIfNull check (line 726)
        IServiceCollection services = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaBulkOperations<TestEntity>());
        ex.ParamName.ShouldBe("services");
    }

    #endregion

    #region AddEncinaProcessingActivityEFCore Guard

    [Fact]
    public void AddEncinaProcessingActivityEFCore_NullServices_ThrowsArgumentNullException()
    {
        // Arrange - exercises the ArgumentNullException.ThrowIfNull check (line 754)
        IServiceCollection services = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaProcessingActivityEFCore());
        ex.ParamName.ShouldBe("services");
    }

    #endregion

    #region Idempotent Registration (TryAdd)

    [Fact]
    public void AddEncinaEntityFrameworkCore_CalledTwice_DoesNotDuplicateRegistrations()
    {
        // Arrange - exercises TryAddScoped (line 142) which should skip on second call
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act - call twice
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config =>
        {
            config.UseOutbox = true;
        });
        services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config =>
        {
            config.UseOutbox = true;
        });

        // Assert - IOutboxStore registered twice (AddScoped, not TryAddScoped for stores)
        // but DbContext mapping only once (TryAddScoped)
        var dbContextDescriptors = services.Where(d =>
            d.ServiceType == typeof(DbContext)).ToList();
        dbContextDescriptors.Count.ShouldBe(1);
    }

    #endregion

    #region Chaining Pattern

    [Fact]
    public void AddEncinaEntityFrameworkCore_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        var result = services.AddEncinaEntityFrameworkCore<TestExtDbContext>(config => { });

        // Assert - should return the same IServiceCollection for fluent chaining
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_NoConfig_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        var result = services.AddEncinaEntityFrameworkCore<TestExtDbContext>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaRepository_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaRepository<TestEntity, Guid>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaUnitOfWork_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestExtDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        var result = services.AddEncinaUnitOfWork<TestExtDbContext>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestExtDbContext : DbContext
    {
        public TestExtDbContext(DbContextOptions<TestExtDbContext> options) : base(options)
        {
        }
    }

    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
