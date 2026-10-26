#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Security.Audit;
using Encina.TestInfrastructure.Time;
using Encina.UnitTests.OpenTelemetry;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Unit tests for <see cref="ReadAuditRetentionService"/>.
/// </summary>
[Collection("ActivityListenerIsolation")]
public class ReadAuditRetentionServiceTests
{
    private readonly IReadAuditStore _mockStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReadAuditRetentionService> _logger;

    public ReadAuditRetentionServiceTests()
    {
        _mockStore = Substitute.For<IReadAuditStore>();
        _scopeFactory = new ServiceCollection()
            .AddSingleton(_mockStore)
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        _logger = NullLogger<ReadAuditRetentionService>.Instance;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_ShouldNotThrow()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new ReadAuditRetentionService(_scopeFactory, options, _logger);

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullScopeFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new ReadAuditRetentionService(null!, options, _logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new ReadAuditRetentionService(_scopeFactory, null!, _logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new ReadAuditRetentionService(_scopeFactory, options, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("logger");
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WhenAutoPurgeDisabled_ShouldReturnImmediately()
    {
        // Arrange
        var options = CreateOptions(enableAutoPurge: false);
        var service = new ReadAuditRetentionService(_scopeFactory, options, _logger);
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

        var fakeTime = new TimerSignallingTimeProvider(DateTimeOffset.UtcNow);
        var options = CreateOptions(enableAutoPurge: true, purgeIntervalHours: 1);
        var service = new ReadAuditRetentionService(_scopeFactory, options, _logger, fakeTime);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await fakeTime.WaitForTimersAsync(1, TestContext.Current.CancellationToken);
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

        var fakeTime = new TimerSignallingTimeProvider(DateTimeOffset.UtcNow);
        var options = CreateOptions(enableAutoPurge: true, purgeIntervalHours: 1);
        var service = new ReadAuditRetentionService(_scopeFactory, options, _logger, fakeTime);
        using var cts = new CancellationTokenSource();

        // Act - should not throw despite error
        await service.StartAsync(cts.Token);
        await fakeTime.WaitForTimersAsync(1, TestContext.Current.CancellationToken);
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

        var fakeTime = new TimerSignallingTimeProvider(DateTimeOffset.UtcNow);
        var options = CreateOptions(enableAutoPurge: true, purgeIntervalHours: 1);
        var service = new ReadAuditRetentionService(_scopeFactory, options, _logger, fakeTime);
        using var cts = new CancellationTokenSource();

        // Act - should not throw
        await service.StartAsync(cts.Token);
        await fakeTime.WaitForTimersAsync(1, TestContext.Current.CancellationToken);
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

    [Fact]
    public async Task ExecuteAsync_EveryPurgeRun_UsesItsOwnScopeAndDisposesTheStoreWithIt()
    {
        // Arrange: a scoped, disposable store (as every database provider registers it)
        var created = new List<ScopedStore>();
        var purgeCompleted = new[]
        {
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var storeIndex = 0;
        var services = new ServiceCollection();
        services.AddScoped<IReadAuditStore>(_ =>
        {
            var store = new ScopedStore(purgeCompleted[Interlocked.Increment(ref storeIndex) - 1]);
            lock (created)
            {
                created.Add(store);
            }

            return store;
        });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        var fakeTime = new TimerSignallingTimeProvider(DateTimeOffset.UtcNow);
        using var service = new ReadAuditRetentionService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            CreateOptions(enableAutoPurge: true, purgeIntervalHours: 1),
            _logger,
            fakeTime);

        // Act: two purge runs
        await service.StartAsync(CancellationToken.None);
        for (var run = 1; run <= 2; run++)
        {
            await fakeTime.WaitForTimersAsync(1, TestContext.Current.CancellationToken);
            fakeTime.Advance(TimeSpan.FromHours(1.1));
            await purgeCompleted[run - 1].Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        await service.StopAsync(CancellationToken.None);

        // Assert: one store per run, each disposed when its scope ended
        created.Count.ShouldBe(2);
        created[0].ShouldNotBeSameAs(created[1]);
        created.ShouldAllBe(s => s.PurgeCalls == 1 && s.Disposed);
    }

    private sealed class ScopedStore : IReadAuditStore, IDisposable
    {
        private readonly TaskCompletionSource<bool> _purgeCompleted;
        private int _purgeCalls;
        private int _disposed;

        public ScopedStore(TaskCompletionSource<bool> purgeCompleted) =>
            _purgeCompleted = purgeCompleted;

        public int PurgeCalls => Volatile.Read(ref _purgeCalls);

        public bool Disposed => Volatile.Read(ref _disposed) == 1;

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
            _purgeCompleted.TrySetResult(true);
        }

        public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
            DateTimeOffset olderThanUtc, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _purgeCalls);
            return ValueTask.FromResult<Either<EncinaError, int>>(Right(0));
        }

        public ValueTask<Either<EncinaError, Unit>> LogReadAsync(
            ReadAuditEntry entry, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetAccessHistoryAsync(
            string entityType, string entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<Either<EncinaError, PagedResult<ReadAuditEntry>>> QueryAsync(
            ReadAuditQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetUserAccessHistoryAsync(
            string userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoreReturnsError_ActivityStatusCarriesOnlyTheErrorCode()
    {
        // Arrange
        using var capture = new ActivityStatusCapture(ReadAuditSourceName);
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        _mockStore.PurgeEntriesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, int>>(
                Left(EncinaErrors.Create("test.purge_failed", "purge failed for " + secret))));

        // Act
        await RunOnePurgeCycleAsync(capture, "test.purge_failed");

        // Assert
        capture.ShouldHaveErrorStatus("test.purge_failed", secret);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoreThrows_ActivityStatusCarriesOnlyTheExceptionType()
    {
        // Arrange
        using var capture = new ActivityStatusCapture(ReadAuditSourceName);
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        _mockStore.PurgeEntriesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(secret));

        // Act
        await RunOnePurgeCycleAsync(capture, nameof(InvalidOperationException));

        // Assert
        capture.ShouldHaveErrorStatus(nameof(InvalidOperationException), secret);
    }

    #endregion

    #region Helpers

    private const string ReadAuditSourceName = "Encina.ReadAudit";

    private async Task RunOnePurgeCycleAsync(ActivityStatusCapture capture, string expectedStatusDescription)
    {
        var fakeTime = new TimerSignallingTimeProvider(DateTimeOffset.UtcNow);
        var options = CreateOptions(enableAutoPurge: true, purgeIntervalHours: 1);
        var service = new ReadAuditRetentionService(_scopeFactory, options, _logger, fakeTime);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        await fakeTime.WaitForTimersAsync(1, TestContext.Current.CancellationToken);
        fakeTime.Advance(TimeSpan.FromHours(1.1));
        await capture.WaitForErrorActivityAsync(expectedStatusDescription);
        await cts.CancelAsync();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling the background service
        }
    }

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
