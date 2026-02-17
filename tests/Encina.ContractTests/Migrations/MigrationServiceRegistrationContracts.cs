using Encina.Sharding.Migrations;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.ContractTests.Migrations;

/// <summary>
/// Contract tests verifying that <see cref="MigrationServiceCollectionExtensions.AddEncinaShardMigrationCoordination"/>
/// registers the expected services with the correct lifetimes.
/// </summary>
[Trait("Category", "Contract")]
public sealed class MigrationServiceRegistrationContracts
{
    [Fact]
    public void AddEncinaShardMigrationCoordination_RegistersIShardedMigrationCoordinator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaShardMigrationCoordination();

        // Assert
        var descriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IShardedMigrationCoordinator));

        descriptor.ShouldNotBeNull(
            "AddEncinaShardMigrationCoordination should register IShardedMigrationCoordinator");
    }

    [Fact]
    public void AddEncinaShardMigrationCoordination_CoordinatorIsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaShardMigrationCoordination();

        // Assert
        var descriptor = services.First(
            sd => sd.ServiceType == typeof(IShardedMigrationCoordinator));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped,
            "IShardedMigrationCoordinator should be registered as Scoped");
    }
}
