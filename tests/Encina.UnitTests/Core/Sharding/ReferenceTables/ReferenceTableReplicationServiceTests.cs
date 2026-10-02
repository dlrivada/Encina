using Encina.Sharding;
using Encina.Sharding.ReferenceTables;
using Encina.UnitTests.Support;

using LanguageExt;

using Microsoft.Extensions.Logging.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableReplicationService"/>: startup synchronization outcomes and the
/// polling loop (due and not-due tables, failed checks, exceptions with redacted logging). Cancellation is
/// signalled from inside the collaborators, so no test waits on a timer (#1557).
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReferenceTableReplicationServiceTests : IDisposable
{
    private const string Sentinel = "SENTINEL-REPLICATION-DSN-2c9";

    private readonly IReferenceTableReplicator _replicator = Substitute.For<IReferenceTableReplicator>();
    private readonly IReferenceTableRegistry _registry = Substitute.For<IReferenceTableRegistry>();
    private readonly IShardTopologyProvider _topologyProvider = Substitute.For<IShardTopologyProvider>();
    private readonly FakeLogger<ReferenceTableReplicationService> _logger = new();
    private readonly CancellationTokenSource _cts = new();

    public void Dispose() => _cts.Dispose();

    private ReferenceTableReplicationService CreateSut(params ReferenceTableConfiguration[] configurations)
    {
        _registry.GetAllConfigurations().Returns(configurations);
        var detector = new PollingRefreshDetector(
            _replicator,
            Substitute.For<IReferenceTableStoreFactory>(),
            _topologyProvider,
            Substitute.For<IReferenceTableStateStore>(),
            new FakeLogger<PollingRefreshDetector>());
        return new ReferenceTableReplicationService(_replicator, _registry, detector, _logger);
    }

    private static ReferenceTableConfiguration Config(
        Type entityType,
        RefreshStrategy strategy,
        bool syncOnStartup = false,
        TimeSpan? interval = null) =>
        new(entityType, new ReferenceTableOptions
        {
            RefreshStrategy = strategy,
            SyncOnStartup = syncOnStartup,
            PollingInterval = interval ?? TimeSpan.FromMilliseconds(1),
        });

    private static ReplicationResult Complete() =>
        new(10, TimeSpan.FromSeconds(1), [new ShardReplicationResult("s1", 10, TimeSpan.FromSeconds(1))], []);

    private static ReplicationResult Partial() =>
        new(
            5,
            TimeSpan.FromSeconds(1),
            [new ShardReplicationResult("s1", 5, TimeSpan.FromSeconds(1))],
            [new ShardFailure("s2", EncinaErrors.Create("test.failure", "failed"))]);

    private async Task RunToCompletionAsync(ReferenceTableReplicationService sut)
    {
        await sut.StartAsync(_cts.Token);
        await sut.ExecuteTask!;
    }

    private async Task RunUntilCancelledAsync(ReferenceTableReplicationService sut)
    {
        await sut.StartAsync(_cts.Token);
        await Should.ThrowAsync<OperationCanceledException>(() => sut.ExecuteTask!);
    }

    private void CancelOnTopologyCall(int callNumber, Func<ShardTopology>? topology = null)
    {
        var calls = 0;
        _topologyProvider.GetTopology().Returns(_ =>
        {
            if (++calls == callNumber)
            {
                _cts.Cancel();
            }

            return topology?.Invoke() ?? new ShardTopology([]);
        });
    }

    [Fact]
    public async Task ExecuteAsync_NoRegisteredTables_ReturnsWithoutReplicating()
    {
        var sut = CreateSut();

        await RunToCompletionAsync(sut);

        await _replicator.DidNotReceiveWithAnyArgs().ReplicateAllAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_NoStartupSyncAndNoPollingTables_ReturnsWithoutReplicating()
    {
        var sut = CreateSut(Config(typeof(string), RefreshStrategy.Manual, syncOnStartup: false));

        await RunToCompletionAsync(sut);

        await _replicator.DidNotReceiveWithAnyArgs().ReplicateAllAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_StartupSyncComplete_ReplicatesOnceAndStopsWithoutPolling()
    {
        _replicator.ReplicateAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, ReplicationResult>(Complete())));
        var sut = CreateSut(Config(typeof(string), RefreshStrategy.Manual, syncOnStartup: true));

        await RunToCompletionAsync(sut);

        await _replicator.Received(1).ReplicateAllAsync(Arg.Any<CancellationToken>());
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Message.Contains("Startup sync completed:"));
    }

    [Fact]
    public async Task ExecuteAsync_StartupSyncPartial_LogsWarning()
    {
        _replicator.ReplicateAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, ReplicationResult>(Partial())));
        var sut = CreateSut(Config(typeof(string), RefreshStrategy.Manual, syncOnStartup: true));

        await RunToCompletionAsync(sut);

        _logger.Collector.GetSnapshot()
            .ShouldContain(r => r.Level == LogLevel.Warning && r.Message.Contains("partial failures"));
    }

    [Fact]
    public async Task ExecuteAsync_StartupSyncFails_LogsErrorCode()
    {
        _replicator.ReplicateAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, ReplicationResult>(EncinaErrors.Create("test.sync", "boom"))));
        var sut = CreateSut(Config(typeof(string), RefreshStrategy.Manual, syncOnStartup: true));

        await RunToCompletionAsync(sut);

        _logger.Collector.GetSnapshot()
            .ShouldContain(r => r.Level == LogLevel.Error && r.Message.Contains("Startup sync failed"));
    }

    [Fact]
    public async Task ExecuteAsync_PollingCheckFails_LogsWarningAndStopsOnCancellation()
    {
        // No shard in the topology: the detector answers Left, which the loop logs as a warning.
        CancelOnTopologyCall(callNumber: 1);
        var sut = CreateSut(Config(typeof(string), RefreshStrategy.Polling));

        await RunUntilCancelledAsync(sut);

        _logger.Collector.GetSnapshot()
            .ShouldContain(r => r.Level == LogLevel.Warning && r.Message.Contains("Polling check failed"));
    }

    [Fact]
    public async Task ExecuteAsync_CancelledMidRound_SkipsRemainingTables()
    {
        CancelOnTopologyCall(callNumber: 1);
        var sut = CreateSut(
            Config(typeof(string), RefreshStrategy.Polling),
            Config(typeof(int), RefreshStrategy.Polling));

        await RunUntilCancelledAsync(sut);

        _topologyProvider.Received(1).GetTopology();
    }

    [Fact]
    public async Task ExecuteAsync_TableNotYetDue_IsSkippedOnLaterRounds()
    {
        // Round 1 polls both tables (calls 1 and 2); round 2 skips the hourly one and polls only the
        // short-interval one (call 3).
        CancelOnTopologyCall(callNumber: 3);
        var sut = CreateSut(
            Config(typeof(int), RefreshStrategy.Polling, interval: TimeSpan.FromHours(1)),
            Config(typeof(string), RefreshStrategy.Polling, interval: TimeSpan.FromMilliseconds(1)));

        await RunUntilCancelledAsync(sut);

        _topologyProvider.Received(3).GetTopology();
    }

    [Fact]
    public async Task ExecuteAsync_PollingThrows_LogsRedactedExceptionAndBacksOff()
    {
        _topologyProvider.GetTopology().Returns(_ =>
        {
            _cts.Cancel();
            throw new InvalidOperationException(Sentinel);
        });
        var sut = CreateSut(Config(typeof(string), RefreshStrategy.Polling));

        // The back-off delay observes the cancelled token and ends the loop.
        await RunUntilCancelledAsync(sut);

        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
    }
}
