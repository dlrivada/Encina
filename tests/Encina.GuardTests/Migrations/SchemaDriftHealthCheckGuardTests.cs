using Encina.OpenTelemetry.Migrations;
using Encina.Sharding.Migrations;

namespace Encina.GuardTests.Migrations;

/// <summary>
/// Guard clause tests for <see cref="SchemaDriftHealthCheck"/> constructor validation.
/// Verifies that null coordinator and null options are rejected.
/// </summary>
public sealed class SchemaDriftHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullCoordinator_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SchemaDriftHealthCheckOptions();

        // Act
        var act = () => new SchemaDriftHealthCheck(null!, options);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var coordinator = Substitute.For<IShardedMigrationCoordinator>();

        // Act
        var act = () => new SchemaDriftHealthCheck(coordinator, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var coordinator = Substitute.For<IShardedMigrationCoordinator>();
        var options = new SchemaDriftHealthCheckOptions();

        // Act
        var healthCheck = new SchemaDriftHealthCheck(coordinator, options);

        // Assert
        healthCheck.ShouldNotBeNull();
    }
}
