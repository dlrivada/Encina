using Encina.Messaging.Inbox;
using Encina.Messaging.Purge;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Purge;

/// <summary>
/// The purge loop of <see cref="MessagePurgeService"/> (#3013): a cycle at start-up and one per interval, batches
/// until a short one, and a failed cycle that is logged without stopping the loop. Time is a fake clock; the
/// tests wait for the loop to register its next delay instead of sleeping.
/// </summary>
public sealed class MessagePurgeServiceTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int BatchSize = 2;

    private readonly IInboxStore _store = Substitute.For<IInboxStore>();
    private readonly TimerCountingTimeProvider _clock = new();
    private readonly FakeLogger<MessagePurgeService> _logger = new();

    public MessagePurgeServiceTests()
    {
        _store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        _store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        Expired();
    }

    [Fact]
    public async Task NoEntryEnabled_DoesNothingAndLogsIt()
    {
        using var sut = CreateService(new MessagePurgeSchedule(null));

        await sut.StartAsync(CancellationToken.None);
        await sut.ExecuteTask!;
        await sut.StopAsync(CancellationToken.None);

        await _store.DidNotReceiveWithAnyArgs().GetExpiredMessagesAsync(default, default);
        _logger.Collector.GetSnapshot().Select(r => r.Id.Id).ShouldContain(2108);
    }

    [Fact]
    public async Task EnabledEntry_RunsOneCycleAfterTheStartupJitterAndOneEveryInterval()
    {
        using var sut = CreateService();
        await sut.StartAsync(CancellationToken.None);
        await _clock.WaitForTimersAsync(1);
        await _store.DidNotReceiveWithAnyArgs().GetExpiredMessagesAsync(default, default);

        _clock.Advance(MessagePurgeService.MaxStartupJitter);
        await _clock.WaitForTimersAsync(2);
        await _store.Received(1).GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>());

        _clock.Advance(Interval);
        await _clock.WaitForTimersAsync(3);
        await _store.Received(2).GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Cycle_DrainsFullBatchesUntilAShortOneAndSavesAfterEachRemove()
    {
        Expired(["a", "b"], ["c", "d"], ["e"]);
        using var sut = CreateService();

        await RunFirstCycleAsync(sut);

        Received.InOrder(() =>
        {
            _store.GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>());
            _store.RemoveExpiredMessagesAsync(Arg.Is<IEnumerable<string>>(ids => HasIds(ids, "a", "b")), Arg.Any<CancellationToken>());
            _store.SaveChangesAsync(Arg.Any<CancellationToken>());
            _store.GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>());
            _store.RemoveExpiredMessagesAsync(Arg.Is<IEnumerable<string>>(ids => HasIds(ids, "c", "d")), Arg.Any<CancellationToken>());
            _store.SaveChangesAsync(Arg.Any<CancellationToken>());
            _store.GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>());
            _store.RemoveExpiredMessagesAsync(Arg.Is<IEnumerable<string>>(ids => HasIds(ids, "e")), Arg.Any<CancellationToken>());
            _store.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
        await _store.Received(3).GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>());
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 2109 && r.Message.Contains('5'));

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task LeftInOneCycle_IsLoggedByErrorCodeOnly_AndTheNextCycleStillRuns()
    {
        var failure = Left<EncinaError, IEnumerable<IInboxMessage>>(EncinaErrors.Create("inbox.purge_failed", "secret patient name"));
        var next = Right<EncinaError, IEnumerable<IInboxMessage>>(Messages("x"));
        _store.GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>())
            .Returns(failure, next);
        using var sut = CreateService();

        await RunFirstCycleAsync(sut);
        _clock.Advance(Interval);
        await _clock.WaitForTimersAsync(3);

        var records = _logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 2110 && r.Message.Contains("inbox.purge_failed"));
        records.ShouldNotContain(r => r.Message.Contains("secret patient name"));
        await _store.Received(2).GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>());
        await _store.Received(1).RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExceptionInOneCycle_IsLoggedAndTheNextCycleStillRuns()
    {
        var calls = 0;
        _store.GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>())
            .Returns(_ => ++calls == 1
                ? throw new InvalidOperationException("boom with patient data")
                : Task.FromResult(Right<EncinaError, IEnumerable<IInboxMessage>>(Messages("x"))));
        using var sut = CreateService();

        await RunFirstCycleAsync(sut);
        _clock.Advance(Interval);
        await _clock.WaitForTimersAsync(3);

        var records = _logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 2111);
        records.ShouldNotContain(r => r.Message.Contains("boom with patient data"));
        calls.ShouldBe(2);

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Stop_DuringTheIntervalDelay_EndsTheLoopWithoutAFurtherCycle()
    {
        using var sut = CreateService();
        await RunFirstCycleAsync(sut);

        await sut.StopAsync(CancellationToken.None);
        _clock.Advance(Interval);

        await _store.Received(1).GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>());
        sut.ExecuteTask!.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Stop_DuringTheStartupJitter_RunsNoCycle()
    {
        using var sut = CreateService();
        await sut.StartAsync(CancellationToken.None);
        await _clock.WaitForTimersAsync(1);

        await sut.StopAsync(CancellationToken.None);

        await _store.DidNotReceiveWithAnyArgs().GetExpiredMessagesAsync(default, default);
    }

    [Fact]
    public async Task MissingStoreRegistration_FailsTheCycleWithoutStoppingTheLoop()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        using var sut = new MessagePurgeService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new MessagePurgeSchedule(new PurgeEntry(Interval, BatchSize)),
            _logger,
            _clock);

        await sut.StartAsync(CancellationToken.None);
        await _clock.WaitForTimersAsync(1);
        _clock.Advance(MessagePurgeService.MaxStartupJitter);
        await _clock.WaitForTimersAsync(2);

        _logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 2111);
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var schedule = new MessagePurgeSchedule(null);

        Should.Throw<ArgumentNullException>(() => new MessagePurgeService(null!, schedule, _logger, _clock))
            .ParamName.ShouldBe("scopeFactory");
        Should.Throw<ArgumentNullException>(() => new MessagePurgeService(scopeFactory, null!, _logger, _clock))
            .ParamName.ShouldBe("schedule");
        Should.Throw<ArgumentNullException>(() => new MessagePurgeService(scopeFactory, schedule, null!, _clock))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithoutTimeProvider_UsesTheSystemClock()
    {
        using var sut = new MessagePurgeService(
            Substitute.For<IServiceScopeFactory>(), new MessagePurgeSchedule(null), _logger);

        sut.ShouldNotBeNull();
    }

    private async Task RunFirstCycleAsync(MessagePurgeService sut)
    {
        await sut.StartAsync(CancellationToken.None);
        await _clock.WaitForTimersAsync(1);
        _clock.Advance(MessagePurgeService.MaxStartupJitter);
        await _clock.WaitForTimersAsync(2);
    }

    private MessagePurgeService CreateService(MessagePurgeSchedule? schedule = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _store);
        var provider = services.BuildServiceProvider();
        return new MessagePurgeService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            schedule ?? new MessagePurgeSchedule(new PurgeEntry(Interval, BatchSize)),
            _logger,
            _clock);
    }

    private void Expired(params string[][] batches)
    {
        var results = batches
            .Select(ids => Right<EncinaError, IEnumerable<IInboxMessage>>(Messages(ids)))
            .Append(Right<EncinaError, IEnumerable<IInboxMessage>>(Messages()))
            .ToArray();
        _store.GetExpiredMessagesAsync(BatchSize, Arg.Any<CancellationToken>())
            .Returns(results[0], results.Skip(1).ToArray());
    }

    private static bool HasIds(IEnumerable<string> actual, params string[] expected)
        => actual.SequenceEqual(expected);

    private static List<IInboxMessage> Messages(params string[] ids)
        => ids.Select(id =>
        {
            var message = Substitute.For<IInboxMessage>();
            message.MessageId.Returns(id);
            return message;
        }).ToList();

    /// <summary>A fake clock that counts the timers created, so a test waits for the loop to reach its next delay.</summary>
    private sealed class TimerCountingTimeProvider : FakeTimeProvider
    {
        private int _created;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            Interlocked.Increment(ref _created);
            return timer;
        }

        public async Task WaitForTimersAsync(int count)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (Volatile.Read(ref _created) < count)
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException($"The loop created {_created} timers; expected {count}.");
                }

                await Task.Yield();
            }
        }
    }
}
