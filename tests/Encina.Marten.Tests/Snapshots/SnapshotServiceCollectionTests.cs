using Encina.Marten.Snapshots;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(MartenSnapshotStore<>));
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
        descriptor.ShouldBeNull();
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
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(MartenSnapshotStore<TestSnapshotableAggregate>));
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
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(SnapshotAwareAggregateRepository<TestSnapshotableAggregate>));
    }

    [Fact]
    public void AddSnapshotableAggregate_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddSnapshotableAggregate<TestSnapshotableAggregate>();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
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
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void EncinaMartenOptions_HasSnapshotOptionsProperty()
    {
        // Arrange
        var options = new EncinaMartenOptions();

        // Assert
        options.Snapshots.ShouldNotBeNull();
        options.Snapshots.ShouldBeOfType<SnapshotOptions>();
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
        using var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetRequiredService<IOptions<EncinaMartenOptions>>();

        // Assert
        registeredOptions.Value.Snapshots.Enabled.ShouldBeTrue();
        registeredOptions.Value.Snapshots.SnapshotEvery.ShouldBe(50);
        registeredOptions.Value.Snapshots.KeepSnapshots.ShouldBe(5);
        registeredOptions.Value.Snapshots.AsyncSnapshotCreation.ShouldBeFalse();
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
        using var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetRequiredService<IOptions<EncinaMartenOptions>>();
        var aggregateConfig = registeredOptions.Value.Snapshots.GetConfigFor<TestSnapshotableAggregate>();

        // Assert
        aggregateConfig.SnapshotEvery.ShouldBe(25);
        aggregateConfig.KeepSnapshots.ShouldBe(10);
    }
}
