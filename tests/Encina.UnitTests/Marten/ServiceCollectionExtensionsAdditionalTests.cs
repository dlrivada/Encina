using Encina.DomainModeling;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Marten.Snapshots;
using Encina.Marten.Versioning;
using Encina.Messaging.Health;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Marten;

public class ServiceCollectionExtensionsAdditionalTests
{
    // Projection registration tests

    [Fact]
    public void AddEncinaMarten_WithProjectionsEnabled_RegistersProjectionInfrastructure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Projections.Enabled = true;
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ProjectionRegistry) &&
            d.Lifetime == ServiceLifetime.Singleton);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IProjectionManager) &&
            d.Lifetime == ServiceLifetime.Singleton);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IReadModelRepository<>) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaMarten_WithInlineProjections_RegistersDispatcher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Projections.Enabled = true;
            options.Projections.UseInlineProjections = true;
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IInlineProjectionDispatcher) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaMarten_ProjectionsDisabled_DoesNotRegisterProjectionInfrastructure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Projections.Enabled = false;
        });

        // Assert
        services.ShouldNotContain(d => d.ServiceType == typeof(ProjectionRegistry));
    }

    // Snapshot registration tests

    [Fact]
    public void AddEncinaMarten_WithSnapshotsEnabled_RegistersSnapshotStore()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Snapshots.Enabled = true;
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ISnapshotStore<>) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaMarten_SnapshotsDisabled_DoesNotRegisterSnapshotStore()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Snapshots.Enabled = false;
        });

        // Assert
        services.ShouldNotContain(d => d.ServiceType == typeof(ISnapshotStore<>));
    }

    // Event versioning registration tests

    [Fact]
    public void AddEncinaMarten_WithVersioningEnabled_RegistersVersioningInfrastructure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.EventVersioning.Enabled = true;
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(EventUpcasterRegistry) &&
            d.Lifetime == ServiceLifetime.Singleton);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<StoreOptions>) &&
            d.ImplementationType == typeof(ConfigureMartenEventVersioning) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaMarten_VersioningDisabled_DoesNotRegisterVersioningInfrastructure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.EventVersioning.Enabled = false;
        });

        // Assert
        services.ShouldNotContain(d => d.ServiceType == typeof(EventUpcasterRegistry));
    }

    // Health check registration tests

    [Fact]
    public void AddEncinaMarten_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.ProviderHealthCheck.Enabled = true;
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IEncinaHealthCheck) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaMarten_HealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        services.ShouldNotContain(d => d.ServiceType == typeof(IEncinaHealthCheck));
    }

    // AddProjection tests

    [Fact]
    public void AddProjection_NullServices_ThrowsArgumentNullException()
    {
        var act = () => ((IServiceCollection)null!).AddProjection<TestProjection, TestReadModel>();
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddProjection_RegistersProjectionType()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddProjection<TestProjection, TestReadModel>();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(TestProjection) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddProjection_RegistersReadModelRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddProjection<TestProjection, TestReadModel>();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IReadModelRepository<TestReadModel>) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddProjection_RegistersProjectionRegistrar()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddProjection<TestProjection, TestReadModel>();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IProjectionRegistrar));
    }

    // AddEventUpcaster tests

    [Fact]
    public void AddEventUpcaster_NullServices_ThrowsArgumentNullException()
    {
        var act = () => ((IServiceCollection)null!).AddEventUpcaster<TestUpcaster>();
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddEventUpcaster_RegistersRegistrar()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventUpcaster<TestUpcaster>();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IEventUpcasterRegistrar));
    }

    // AddSnapshotableAggregate tests

    [Fact]
    public void AddSnapshotableAggregate_NullServices_ThrowsArgumentNullException()
    {
        var act = () => ((IServiceCollection)null!).AddSnapshotableAggregate<TestSnapshotAggregate>();
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddSnapshotableAggregate_RegistersSnapshotStore()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSnapshotableAggregate<TestSnapshotAggregate>();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ISnapshotStore<TestSnapshotAggregate>));
    }

    [Fact]
    public void AddSnapshotableAggregate_RegistersSnapshotAwareRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSnapshotableAggregate<TestSnapshotAggregate>();

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(IAggregateRepository<TestSnapshotAggregate>) &&
            d.ImplementationType == typeof(SnapshotAwareAggregateRepository<TestSnapshotAggregate>));
    }

    // AddAggregateRepository tests

    [Fact]
    public void AddAggregateRepository_NullServices_ThrowsArgumentNullException()
    {
        var act = () => ((IServiceCollection)null!).AddAggregateRepository<TestAggregate>();
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddAggregateRepository_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddAggregateRepository<TestAggregate>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    // ProjectionRegistrar tests

    [Fact]
    public void ProjectionRegistrar_Register_AddsToRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProjection<TestProjection, TestReadModel>();

        var provider = services.BuildServiceProvider();
        var registrars = provider.GetServices<IProjectionRegistrar>();
        var registry = new ProjectionRegistry();

        // Act
        foreach (var registrar in registrars)
        {
            registrar.Register(registry);
        }

        // Assert
        registry.GetProjectionForReadModel<TestReadModel>().ShouldNotBeNull();
    }

    // EventUpcasterRegistrar tests

    [Fact]
    public void EventUpcasterRegistrar_Register_AddsToRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventUpcaster<TestUpcaster>();

        var provider = services.BuildServiceProvider();
        var registrars = provider.GetServices<IEventUpcasterRegistrar>();
        var registry = new EventUpcasterRegistry();

        // Act
        foreach (var registrar in registrars)
        {
            registrar.Register(registry);
        }

        // Assert
        registry.HasUpcasterFor("TestOldEvent").ShouldBeTrue();
    }

    // Test types

    private sealed class TestAggregate : AggregateBase
    {
        protected override void Apply(object domainEvent) { }
    }

    public sealed class TestSnapshotAggregate : AggregateBase, ISnapshotable<TestSnapshotAggregate>
    {
        public TestSnapshotAggregate() { }
        protected override void Apply(object domainEvent) { }
    }

    public sealed class TestReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }

    public sealed record TestCreatedEvent(string Name);

    public sealed class TestProjection :
        IProjection<TestReadModel>,
        IProjectionCreator<TestCreatedEvent, TestReadModel>
    {
        public string ProjectionName => "Test";
        public TestReadModel Create(TestCreatedEvent domainEvent, ProjectionContext context) =>
            new() { Id = context.StreamId };
    }

    public sealed class TestUpcaster : IEventUpcaster
    {
        public string SourceEventTypeName => "TestOldEvent";
        public Type TargetEventType => typeof(string);
        public Type SourceEventType => typeof(string);
    }
}
