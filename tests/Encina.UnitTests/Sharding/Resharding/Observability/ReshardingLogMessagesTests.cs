using Encina.OpenTelemetry.Resharding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Sharding.Resharding.Observability;

/// <summary>
/// Unit tests for <see cref="ReshardingLogMessages"/>.
/// Validates that all 10 structured log extension methods can be invoked without
/// throwing exceptions. Uses <see cref="NullLogger"/> for safe smoke testing.
/// </summary>
public sealed class ReshardingLogMessagesTests
{
    private readonly ILogger _logger = NullLogger.Instance;

    #region ReshardingStarted (EventId 7000)

    [Fact]
    public void ReshardingStarted_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingStarted(Guid.NewGuid(), stepCount: 3, estimatedRows: 10000));
    }

    #endregion

    #region ReshardingPhaseStarted (EventId 7001)

    [Fact]
    public void ReshardingPhaseStarted_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingPhaseStarted(Guid.NewGuid(), "Copying"));
    }

    #endregion

    #region ReshardingPhaseCompleted (EventId 7002)

    [Fact]
    public void ReshardingPhaseCompleted_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingPhaseCompleted(Guid.NewGuid(), "Copying", durationMs: 5432.1));
    }

    #endregion

    #region ReshardingCopyProgress (EventId 7003)

    [Fact]
    public void ReshardingCopyProgress_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingCopyProgress(Guid.NewGuid(), rowsCopied: 5000, percentComplete: 50.0));
    }

    #endregion

    #region ReshardingCdcLagUpdate (EventId 7004)

    [Fact]
    public void ReshardingCdcLagUpdate_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingCdcLagUpdate(Guid.NewGuid(), lagMs: 150.5));
    }

    #endregion

    #region ReshardingVerificationResult (EventId 7005)

    [Fact]
    public void ReshardingVerificationResult_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingVerificationResult(Guid.NewGuid(), matched: true, mismatchCount: 0));
    }

    #endregion

    #region ReshardingCutoverStarted (EventId 7006)

    [Fact]
    public void ReshardingCutoverStarted_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingCutoverStarted(Guid.NewGuid()));
    }

    #endregion

    #region ReshardingCutoverCompleted (EventId 7007)

    [Fact]
    public void ReshardingCutoverCompleted_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingCutoverCompleted(Guid.NewGuid(), cutoverDurationMs: 250.0));
    }

    #endregion

    #region ReshardingFailed (EventId 7008)

    [Fact]
    public void ReshardingFailed_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingFailed(Guid.NewGuid(), errorCode: "VERIFICATION_MISMATCH"));
    }

    #endregion

    #region ReshardingRolledBack (EventId 7009)

    [Fact]
    public void ReshardingRolledBack_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _logger.ReshardingRolledBack(Guid.NewGuid(), lastCompletedPhase: "Copying"));
    }

    #endregion
}
