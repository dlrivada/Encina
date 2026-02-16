using Encina.OpenTelemetry.Migrations;

namespace Encina.GuardTests.Migrations;

/// <summary>
/// Guard clause tests for <see cref="MigrationMetricsCallbacks"/> constructor validation.
/// Verifies that null callback delegates are rejected.
/// </summary>
public sealed class MigrationMetricsCallbacksGuardTests
{
    [Fact]
    public void Constructor_NullDriftDetectedCountCallback_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MigrationMetricsCallbacks(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ValidCallback_CreatesInstance()
    {
        // Arrange & Act
        var callbacks = new MigrationMetricsCallbacks(() => 42);

        // Assert
        callbacks.ShouldNotBeNull();
    }
}
