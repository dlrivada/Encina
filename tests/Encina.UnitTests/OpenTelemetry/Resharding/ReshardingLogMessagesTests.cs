using Encina.OpenTelemetry.Resharding;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingLogMessages"/>.
/// Verifies that each log extension method invokes the logger without throwing.
/// </summary>
public sealed class ReshardingLogMessagesTests
{
    private readonly ILogger _logger;

    public ReshardingLogMessagesTests()
    {
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void ReshardingStarted_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingStarted(Guid.NewGuid(), 5, 10000));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingPhaseStarted_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingPhaseStarted(Guid.NewGuid(), "Copying"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingPhaseCompleted_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingPhaseCompleted(Guid.NewGuid(), "Copying", 12345.6));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingCopyProgress_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingCopyProgress(Guid.NewGuid(), 5000, 50.0));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingCdcLagUpdate_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingCdcLagUpdate(Guid.NewGuid(), 120.5));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingVerificationResult_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingVerificationResult(Guid.NewGuid(), true, 0));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingCutoverStarted_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingCutoverStarted(Guid.NewGuid()));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingCutoverCompleted_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingCutoverCompleted(Guid.NewGuid(), 250.0));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingFailed_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingFailed(Guid.NewGuid(), "DATA_CORRUPTION"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingRolledBack_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _logger.ReshardingRolledBack(Guid.NewGuid(), "Copying"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void ReshardingStarted_LogsAtInformationLevel()
    {
        _logger.ReshardingStarted(Guid.NewGuid(), 3, 5000);

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ReshardingFailed_LogsAtErrorLevel()
    {
        _logger.ReshardingFailed(Guid.NewGuid(), "TIMEOUT");

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ReshardingCutoverStarted_LogsAtWarningLevel()
    {
        _logger.ReshardingCutoverStarted(Guid.NewGuid());

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ReshardingRolledBack_LogsAtWarningLevel()
    {
        _logger.ReshardingRolledBack(Guid.NewGuid(), "Replicating");

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
