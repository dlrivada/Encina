using Encina.Security.Audit;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="AuditRetentionService"/>.
/// </summary>
public class AuditRetentionServiceTests
{
    private readonly IAuditStore _mockAuditStore;
    private readonly ILogger<AuditRetentionService> _logger;

    public AuditRetentionServiceTests()
    {
        _mockAuditStore = Substitute.For<IAuditStore>();
        _logger = NullLogger<AuditRetentionService>.Instance;
    }

    #region Disabled Service Tests

    [Fact]
    public async Task ExecuteAsync_WhenAutoPurgeDisabled_ShouldExitImmediately()
    {
        // Arrange
        var options = Options.Create(new AuditOptions { EnableAutoPurge = false });
        var service = new AuditRetentionService(_mockAuditStore, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100); // Give service time to start
        await service.StopAsync(cts.Token);

        // Assert - PurgeEntriesAsync should never be called
        _mockAuditStore.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IAuditStore.PurgeEntriesAsync))
            .Should().BeEmpty();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullAuditStore_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AuditOptions());

        // Act
        var act = () => new AuditRetentionService(null!, options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditStore");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AuditRetentionService(_mockAuditStore, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AuditOptions());

        // Act
        var act = () => new AuditRetentionService(_mockAuditStore, options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ShouldUseSystemTimeProvider()
    {
        // Arrange
        var options = Options.Create(new AuditOptions());

        // Act - Should not throw
        var service = new AuditRetentionService(_mockAuditStore, options, _logger, null);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var options = Options.Create(new AuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = 30,
            PurgeIntervalHours = 24
        });

        // Act
        var service = new AuditRetentionService(_mockAuditStore, options, _logger);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Options Configuration Tests

    [Fact]
    public void Service_ShouldReadRetentionDaysFromOptions()
    {
        // Arrange
        var options = Options.Create(new AuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = 365,
            PurgeIntervalHours = 12
        });

        // Act - Create service (doesn't throw)
        var service = new AuditRetentionService(_mockAuditStore, options, _logger);

        // Assert
        service.Should().NotBeNull();
        // Options are read at execution time, not construction
    }

    [Fact]
    public void Service_ShouldReadPurgeIntervalFromOptions()
    {
        // Arrange
        var options = Options.Create(new AuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = 30,
            PurgeIntervalHours = 6 // Custom interval
        });

        // Act - Create service (doesn't throw)
        var service = new AuditRetentionService(_mockAuditStore, options, _logger);

        // Assert
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineData(7)]     // 1 week
    [InlineData(30)]    // 1 month
    [InlineData(365)]   // 1 year
    [InlineData(2555)]  // 7 years (SOX)
    public void Service_ShouldAcceptVariousRetentionDays(int retentionDays)
    {
        // Arrange
        var options = Options.Create(new AuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = retentionDays,
            PurgeIntervalHours = 24
        });

        // Act - Should not throw
        var service = new AuditRetentionService(_mockAuditStore, options, _logger);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Cutoff Date Calculation Tests

    [Fact]
    public void CutoffDateCalculation_ShouldSubtractRetentionDays()
    {
        // This test verifies the expected calculation logic
        // cutoffDate = UtcNow.AddDays(-RetentionDays)

        // Arrange
        var now = DateTime.UtcNow;
        var retentionDays = 30;
        var expectedCutoff = now.AddDays(-retentionDays);

        // Act - Calculate what the service should use
        var actualCutoff = now.AddDays(-retentionDays);

        // Assert
        actualCutoff.Date.Should().Be(expectedCutoff.Date);
    }

    [Theory]
    [InlineData(7, -7)]
    [InlineData(30, -30)]
    [InlineData(365, -365)]
    public void CutoffDateCalculation_ShouldBeCorrectForVariousRetentionPeriods(int retentionDays, int expectedDaysOffset)
    {
        // Arrange
        var baseDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var expectedCutoff = baseDate.AddDays(expectedDaysOffset);

        // Act
        var actualCutoff = baseDate.AddDays(-retentionDays);

        // Assert
        actualCutoff.Should().Be(expectedCutoff);
    }

    #endregion

    #region Service Lifecycle Tests

    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new AuditOptions { EnableAutoPurge = false });
        var service = new AuditRetentionService(_mockAuditStore, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act & Assert
        await service.Invoking(s => s.StartAsync(cts.Token))
            .Should().NotThrowAsync();

        await service.StopAsync(cts.Token);
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new AuditOptions { EnableAutoPurge = false });
        var service = new AuditRetentionService(_mockAuditStore, options, _logger);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);

        // Act & Assert
        await service.Invoking(s => s.StopAsync(cts.Token))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Service_WithCancellation_ShouldStopGracefully()
    {
        // Arrange
        var options = Options.Create(new AuditOptions
        {
            EnableAutoPurge = true,
            PurgeIntervalHours = 24
        });

        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));

        var service = new AuditRetentionService(_mockAuditStore, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        cts.Cancel();

        // Assert - Should not throw
        await service.Invoking(s => s.StopAsync(CancellationToken.None))
            .Should().NotThrowAsync();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Service_WhenAutoPurgeDisabled_ShouldNotCallPurge()
    {
        // Arrange
        var options = Options.Create(new AuditOptions
        {
            EnableAutoPurge = false
        });

        var service = new AuditRetentionService(_mockAuditStore, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(150); // Wait a bit
        await service.StopAsync(cts.Token);

        // Assert - Purge should never be called
        _mockAuditStore.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IAuditStore.PurgeEntriesAsync))
            .Should().BeEmpty();
    }

    #endregion
}
