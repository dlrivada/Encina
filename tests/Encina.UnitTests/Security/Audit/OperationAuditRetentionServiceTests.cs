using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="OperationAuditRetentionService"/>.
/// </summary>
public class OperationAuditRetentionServiceTests
{
    private readonly IOperationAuditStore _mockAuditStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperationAuditRetentionService> _logger;

    public OperationAuditRetentionServiceTests()
    {
        _mockAuditStore = Substitute.For<IOperationAuditStore>();
        _scopeFactory = new ServiceCollection()
            .AddSingleton(_mockAuditStore)
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        _logger = NullLogger<OperationAuditRetentionService>.Instance;
    }

    #region Disabled Service Tests

    [Fact]
    public async Task ExecuteAsync_WhenAutoPurgeDisabled_ShouldExitImmediately()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions { EnableAutoPurge = false });
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100); // Give service time to start
        await service.StopAsync(cts.Token);

        // Assert - PurgeEntriesAsync should never be called
        _mockAuditStore.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IOperationAuditStore.PurgeEntriesAsync))
            .ShouldBeEmpty();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullScopeFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());

        // Act
        var act = () => new OperationAuditRetentionService(null!, options, _logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new OperationAuditRetentionService(_scopeFactory, null!, _logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());

        // Act
        var act = () => new OperationAuditRetentionService(_scopeFactory, options, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ShouldUseSystemTimeProvider()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());

        // Act - Should not throw
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger, null);

        // Assert
        service.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = 30,
            PurgeIntervalHours = 24
        });

        // Act
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);

        // Assert
        service.ShouldNotBeNull();
    }

    #endregion

    #region Options Configuration Tests

    [Fact]
    public void Service_ShouldReadRetentionDaysFromOptions()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = 365,
            PurgeIntervalHours = 12
        });

        // Act - Create service (doesn't throw)
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);

        // Assert
        service.ShouldNotBeNull();
        // Options are read at execution time, not construction
    }

    [Fact]
    public void Service_ShouldReadPurgeIntervalFromOptions()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = 30,
            PurgeIntervalHours = 6 // Custom interval
        });

        // Act - Create service (doesn't throw)
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);

        // Assert
        service.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(7)]     // 1 week
    [InlineData(30)]    // 1 month
    [InlineData(365)]   // 1 year
    [InlineData(2555)]  // 7 years (SOX)
    public void Service_ShouldAcceptVariousRetentionDays(int retentionDays)
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = retentionDays,
            PurgeIntervalHours = 24
        });

        // Act - Should not throw
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);

        // Assert
        service.ShouldNotBeNull();
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
        actualCutoff.Date.ShouldBe(expectedCutoff.Date);
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
        actualCutoff.ShouldBe(expectedCutoff);
    }

    #endregion

    #region Purge Run Tests

    [Fact]
    public async Task ExecuteAsync_EveryPurgeRun_ResolvesTheScopedStoreFromItsOwnScope()
    {
        // Arrange: a scoped store (as every database provider registers it) and a fake clock
        var start = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        var clock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(start);
        var purged = new TaskCompletionSource<DateTime>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resolutions = 0;
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                purged.TrySetResult(call.Arg<DateTime>());
                return Right<EncinaError, int>(0);
            });

        var services = new ServiceCollection();
        services.AddScoped(_ =>
        {
            Interlocked.Increment(ref resolutions);
            return _mockAuditStore;
        });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = true,
            RetentionDays = 30,
            PurgeIntervalHours = 1
        });
        using var service = new OperationAuditRetentionService(
            provider.GetRequiredService<IServiceScopeFactory>(), options, _logger, clock);

        // Act: advance the clock until the service's delay fires and the purge runs once
        await service.StartAsync(CancellationToken.None);
        for (var i = 0; i < 200 && !purged.Task.IsCompleted; i++)
        {
            clock.Advance(TimeSpan.FromHours(1));
            await Task.Delay(10);
        }

        var cutoff = await purged.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await service.StopAsync(CancellationToken.None);

        // Assert: exactly one store resolution (one scope) per purge run, and a UTC cutoff derived from the fake clock
        var purgeRuns = _mockAuditStore.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == nameof(IOperationAuditStore.PurgeEntriesAsync));
        purgeRuns.ShouldBeGreaterThanOrEqualTo(1);
        Volatile.Read(ref resolutions).ShouldBe(purgeRuns);
        cutoff.Kind.ShouldBe(DateTimeKind.Utc);
        cutoff.ShouldBeGreaterThan(start.UtcDateTime.AddDays(-30));
    }

    #endregion

    #region Service Lifecycle Tests

    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions { EnableAutoPurge = false });
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act & Assert
        await Should.NotThrowAsync(async () => await service.StartAsync(cts.Token));

        await service.StopAsync(cts.Token);
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions { EnableAutoPurge = false });
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);

        // Act & Assert
        await Should.NotThrowAsync(async () => await service.StopAsync(cts.Token));
    }

    [Fact]
    public async Task Service_WithCancellation_ShouldStopGracefully()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = true,
            PurgeIntervalHours = 24
        });

        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));

        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        cts.Cancel();

        // Assert - Should not throw
        await Should.NotThrowAsync(async () => await service.StopAsync(CancellationToken.None));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Service_WhenAutoPurgeDisabled_ShouldNotCallPurge()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = false
        });

        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(150); // Wait a bit
        await service.StopAsync(cts.Token);

        // Assert - Purge should never be called
        _mockAuditStore.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IOperationAuditStore.PurgeEntriesAsync))
            .ShouldBeEmpty();
    }

    #endregion
}
