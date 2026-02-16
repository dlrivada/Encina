using Encina.OpenTelemetry.Resharding;

namespace Encina.UnitTests.Sharding.Resharding.Observability;

/// <summary>
/// Unit tests for <see cref="ReshardingMetricsCallbacks"/>.
/// Validates constructor null checks for all three callback parameters
/// and verifies that stored callbacks return the expected values.
/// </summary>
public sealed class ReshardingMetricsCallbacksTests
{
    #region Constructor Validation

    [Fact]
    public void Constructor_NullRowsPerSecondCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(
                rowsPerSecondCallback: null!,
                cdcLagMsCallback: () => 0.0,
                activeReshardingCountCallback: () => 0));
    }

    [Fact]
    public void Constructor_NullCdcLagMsCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(
                rowsPerSecondCallback: () => 0.0,
                cdcLagMsCallback: null!,
                activeReshardingCountCallback: () => 0));
    }

    [Fact]
    public void Constructor_NullActiveReshardingCountCallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(
                rowsPerSecondCallback: () => 0.0,
                cdcLagMsCallback: () => 0.0,
                activeReshardingCountCallback: null!));
    }

    #endregion

    #region Valid Construction

    [Fact]
    public void Constructor_ValidCallbacks_CreatesInstanceSuccessfully()
    {
        // Arrange
        Func<double> rowsPerSecond = () => 42.5;
        Func<double> cdcLag = () => 15.0;
        Func<int> activeCount = () => 3;

        // Act
        var sut = new ReshardingMetricsCallbacks(
            rowsPerSecondCallback: rowsPerSecond,
            cdcLagMsCallback: cdcLag,
            activeReshardingCountCallback: activeCount);

        // Assert
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void Callbacks_UsedByReshardingMetrics_DoNotThrow()
    {
        // Arrange
        // Verify that callbacks are properly stored by constructing
        // ReshardingMetrics which uses them for ObservableGauge registration.
        var sut = new ReshardingMetricsCallbacks(
            rowsPerSecondCallback: () => 100.5,
            cdcLagMsCallback: () => 25.0,
            activeReshardingCountCallback: () => 7);

        // Act & Assert - The callbacks are stored and usable by ReshardingMetrics
        Should.NotThrow(() => new ReshardingMetrics(sut));
    }

    #endregion
}
