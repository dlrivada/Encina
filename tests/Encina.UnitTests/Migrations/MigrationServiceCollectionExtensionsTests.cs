using Encina.Sharding;
using Encina.Sharding.Migrations;

namespace Encina.UnitTests.Migrations;

/// <summary>
/// Unit tests for <see cref="MigrationServiceCollectionExtensions"/>.
/// Verifies DI registration of migration coordination services.
/// </summary>
public sealed class MigrationServiceCollectionExtensionsTests
{
    #region AddEncinaShardMigrationCoordination

    [Fact]
    public void AddEncinaShardMigrationCoordination_RegistersCoordinator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register required dependencies that the coordinator factory needs
        services.AddSingleton(new ShardTopology(new[] { new ShardInfo("shard-0", "Server=test") }));
        services.AddSingleton(Substitute.For<IMigrationExecutor>());
        services.AddSingleton(Substitute.For<ISchemaIntrospector>());
        services.AddSingleton(Substitute.For<IMigrationHistoryStore>());
        services.AddLogging();

        // Act
        services.AddEncinaShardMigrationCoordination();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var coordinator = scope.ServiceProvider.GetService<IShardedMigrationCoordinator>();

        // Assert
        coordinator.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaShardMigrationCoordination_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaShardMigrationCoordination());
    }

    [Fact]
    public void AddEncinaShardMigrationCoordination_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaShardMigrationCoordination(builder =>
        {
            builder
                .UseStrategy(MigrationStrategy.CanaryFirst)
                .WithMaxParallelism(16)
                .StopOnFirstFailure(false)
                .WithPerShardTimeout(TimeSpan.FromMinutes(15));
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<MigrationCoordinationOptions>();

        // Assert
        options.ShouldNotBeNull();
        options!.DefaultStrategy.ShouldBe(MigrationStrategy.CanaryFirst);
        options.MaxParallelism.ShouldBe(16);
        options.StopOnFirstFailure.ShouldBeFalse();
        options.PerShardTimeout.ShouldBe(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void AddEncinaShardMigrationCoordination_RegistersDriftDetectionOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaShardMigrationCoordination(builder =>
        {
            builder.WithDriftDetection(drift =>
            {
                drift.ComparisonDepth = SchemaComparisonDepth.Full;
                drift.BaselineShardId = "shard-0";
            });
        });

        var provider = services.BuildServiceProvider();
        var driftOptions = provider.GetService<DriftDetectionOptions>();

        // Assert
        driftOptions.ShouldNotBeNull();
        driftOptions!.ComparisonDepth.ShouldBe(SchemaComparisonDepth.Full);
        driftOptions.BaselineShardId.ShouldBe("shard-0");
    }

    [Fact]
    public void AddEncinaShardMigrationCoordination_WithoutConfigure_RegistersDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaShardMigrationCoordination();

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<MigrationCoordinationOptions>();

        // Assert
        options.ShouldNotBeNull();
        options!.DefaultStrategy.ShouldBe(MigrationStrategy.Sequential);
        options.MaxParallelism.ShouldBe(4);
        options.StopOnFirstFailure.ShouldBeTrue();
    }

    #endregion
}
