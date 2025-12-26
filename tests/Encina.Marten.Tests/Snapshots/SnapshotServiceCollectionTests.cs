using Encina.Marten.Snapshots;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Marten.Tests.Snapshots;

public sealed class SnapshotServiceCollectionTests
{
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
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ISnapshotStore<>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(MartenSnapshotStore<>));
    }

    [Fact]
    public void AddEncinaMarten_WithSnapshotsDisabled_DoesNotRegisterSnapshotStore()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Snapshots.Enabled = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ISnapshotStore<>));
        descriptor.Should().BeNull();
    }

    [Fact]
    public void AddSnapshotableAggregate_RegistersSnapshotStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMarten(options =>
        {
            options.Snapshots.Enabled = true;
        });

        // Act
        services.AddSnapshotableAggregate<TestSnapshotableAggregate>();

        // Assert
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ISnapshotStore<TestSnapshotableAggregate>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(MartenSnapshotStore<TestSnapshotableAggregate>));
    }

    [Fact]
    public void AddSnapshotableAggregate_RegistersSnapshotAwareRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMarten(options =>
        {
            options.Snapshots.Enabled = true;
        });

        // Act
        services.AddSnapshotableAggregate<TestSnapshotableAggregate>();

        // Assert
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IAggregateRepository<TestSnapshotableAggregate>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(SnapshotAwareAggregateRepository<TestSnapshotableAggregate>));
    }

    [Fact]
    public void AddSnapshotableAggregate_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddSnapshotableAggregate<TestSnapshotableAggregate>();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddSnapshotableAggregate_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMarten(options =>
        {
            options.Snapshots.Enabled = true;
        });

        // Act
        var result = services.AddSnapshotableAggregate<TestSnapshotableAggregate>();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void EncinaMartenOptions_HasSnapshotOptionsProperty()
    {
        // Arrange
        var options = new EncinaMartenOptions();

        // Assert
        options.Snapshots.Should().NotBeNull();
        options.Snapshots.Should().BeOfType<SnapshotOptions>();
    }

    [Fact]
    public void SnapshotOptions_CanBeConfiguredViaEncinaMartenOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Snapshots.Enabled = true;
            options.Snapshots.SnapshotEvery = 50;
            options.Snapshots.KeepSnapshots = 5;
            options.Snapshots.AsyncSnapshotCreation = false;
        });

        // Assert - if no exception, configuration worked
        services.Should().NotBeEmpty();
    }

    [Fact]
    public void SnapshotOptions_AggregateConfigurationWorks()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Snapshots.Enabled = true;
            options.Snapshots.SnapshotEvery = 100;
            options.Snapshots.ConfigureAggregate<TestSnapshotableAggregate>(
                snapshotEvery: 25,
                keepSnapshots: 10);
        });

        // Assert - if no exception, configuration worked
        services.Should().NotBeEmpty();
    }
}
