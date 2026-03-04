#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Security.Audit;
using Encina.Testing.Time;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Unit tests for <see cref="ReadAuditRetentionService"/>.
/// </summary>
public class ReadAuditRetentionServiceTests
{
    private readonly IReadAuditStore _mockStore;
    private readonly ILogger<ReadAuditRetentionService> _logger;

    public ReadAuditRetentionServiceTests()
    {
        _mockStore = Substitute.For<IReadAuditStore>();
        _logger = NullLogger<ReadAuditRetentionService>.Instance;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_ShouldNotThrow()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new ReadAuditRetentionService(_mockStore, options, _logger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullStore_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new ReadAuditRetentionService(null!, options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("readAuditStore");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new ReadAuditRetentionService(_mockStore, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new ReadAuditRetentionService(_mockStore, options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WhenAutoPurgeDisabled_ShouldReturnImmediately()
    {
        // Arrange
        var options = CreateOptions(enableAutoPurge: false);
        var service = new ReadAuditRetentionService(_mockStore, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await _mockStore.DidNotReceive().PurgeEntriesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAutoPurgeEnabled_ShouldCallPurge()
    {
        // Arrange
        _mockStore.PurgeEntriesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, int>>(Right(5)));

        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var options = CreateOptions(enableAutoPurge: true, purgeIntervalHours: 1);
        var service = new ReadAuditRetentionService(_mockStore, options, _logger, fakeTime);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50); // Allow background task to reach Task.Delay
        fakeTime.Advance(TimeSpan.FromHours(1.1));
        await Task.Delay(200); // Allow purge to complete
        await cts.CancelAsync();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling background service
        }

        // Assert
        await _mockStore.Received(1).PurgeEntriesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoreReturnsError_ShouldContinueRunning()
    {
        // Arrange
        _mockStore.PurgeEntriesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, int>>(
                Left(EncinaError.New("purge failed"))));

        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var options = CreateOptions(enableAutoPurge: true, purgeIntervalHours: 1);
        var service = new ReadAuditRetentionService(_mockStore, options, _logger, fakeTime);
        using var cts = new CancellationTokenSource();

        // Act - should not throw despite error
        await service.StartAsync(cts.Token);
        await Task.Delay(50); // Allow background task to reach Task.Delay
        fakeTime.Advance(TimeSpan.FromHours(1.1));
        await Task.Delay(200);
        await cts.CancelAsync();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - should still have called purge
        await _mockStore.Received(1).PurgeEntriesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoreThrows_ShouldContinueRunning()
    {
        // Arrange
        _mockStore.PurgeEntriesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("database down"));

        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var options = CreateOptions(enableAutoPurge: true, purgeIntervalHours: 1);
        var service = new ReadAuditRetentionService(_mockStore, options, _logger, fakeTime);
        using var cts = new CancellationTokenSource();

        // Act - should not throw
        await service.StartAsync(cts.Token);
        await Task.Delay(50); // Allow background task to reach Task.Delay
        fakeTime.Advance(TimeSpan.FromHours(1.1));
        await Task.Delay(200);
        await cts.CancelAsync();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - called despite exception
        await _mockStore.Received(1).PurgeEntriesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static IOptions<ReadAuditOptions> CreateOptions(
        bool enableAutoPurge = false,
        int retentionDays = 365,
        int purgeIntervalHours = 24)
    {
        var options = new ReadAuditOptions
        {
            EnableAutoPurge = enableAutoPurge,
            RetentionDays = retentionDays,
            PurgeIntervalHours = purgeIntervalHours
        };
        return Options.Create(options);
    }

    #endregion
}
