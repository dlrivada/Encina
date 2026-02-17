using Encina.OpenTelemetry.Resharding;

namespace Encina.UnitTests.Sharding.Resharding.Observability;

/// <summary>
/// Unit tests for <see cref="ReshardingMetrics"/>.
/// Validates constructor null checks and smoke-tests that recording methods
/// do not throw exceptions when invoked. Metrics instruments use a static Meter,
/// so value assertions are not feasible without a MeterListener; instead we verify
/// that calls complete without error.
/// </summary>
public sealed class ReshardingMetricsTests
{
    #region Constructor Validation

    [Fact]
    public void Constructor_NullCallbacks_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetrics(null!));
    }

    #endregion

    #region RecordPhaseDuration

    [Fact]
    public void RecordPhaseDuration_ValidInput_DoesNotThrow()
    {
        // Arrange
        var callbacks = CreateNoOpCallbacks();
        var sut = new ReshardingMetrics(callbacks);

        // Act & Assert
        Should.NotThrow(() =>
            sut.RecordPhaseDuration(Guid.NewGuid(), "Copying", 1234.5));
    }

    #endregion

    #region RecordRowsCopied

    [Fact]
    public void RecordRowsCopied_ValidInput_DoesNotThrow()
    {
        // Arrange
        var callbacks = CreateNoOpCallbacks();
        var sut = new ReshardingMetrics(callbacks);

        // Act & Assert
        Should.NotThrow(() =>
            sut.RecordRowsCopied("shard-0", "shard-1", 5000));
    }

    #endregion

    #region RecordVerificationMismatch

    [Fact]
    public void RecordVerificationMismatch_ValidInput_DoesNotThrow()
    {
        // Arrange
        var callbacks = CreateNoOpCallbacks();
        var sut = new ReshardingMetrics(callbacks);

        // Act & Assert
        Should.NotThrow(() =>
            sut.RecordVerificationMismatch(Guid.NewGuid()));
    }

    #endregion

    #region RecordCutoverDuration

    [Fact]
    public void RecordCutoverDuration_ValidInput_DoesNotThrow()
    {
        // Arrange
        var callbacks = CreateNoOpCallbacks();
        var sut = new ReshardingMetrics(callbacks);

        // Act & Assert
        Should.NotThrow(() =>
            sut.RecordCutoverDuration(Guid.NewGuid(), 250.0));
    }

    #endregion

    #region Test Helpers

    private static ReshardingMetricsCallbacks CreateNoOpCallbacks()
    {
        return new ReshardingMetricsCallbacks(
            rowsPerSecondCallback: () => 0.0,
            cdcLagMsCallback: () => 0.0,
            activeReshardingCountCallback: () => 0);
    }

    #endregion
}
