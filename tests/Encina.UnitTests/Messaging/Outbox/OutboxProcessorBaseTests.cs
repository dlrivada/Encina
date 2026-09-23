using System.Diagnostics.Metrics;
using System.Text.Json;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.Messaging.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxProcessorBase"/>: the handling of the result of
/// <see cref="IOutboxStore.SaveChangesAsync"/>, options validation and cancellation (#1167 review).
/// </summary>
public sealed class OutboxProcessorBaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Processor_SaveChangesReturnsLeft_LogsTheErrorAndNotTheBatchSummary()
    {
        var harness = new Harness(Right<EncinaError, Unit>(Unit.Default));
        harness.Store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("outbox.save_failed", "Deadlock victim")));
        using var unsaved = new OutcomeListener("unsaved");

        await harness.RunUntilLoggedAsync(2960);

        var entry = harness.Logger.Entries.Single(e => e.EventId == 2960);
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Message.ShouldContain("Deadlock victim");
        harness.Logger.EventIds.ShouldNotContain(2833);
        unsaved.Total.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Processor_SaveChangesReturnsRight_LogsTheBatchSummary()
    {
        var harness = new Harness(Right<EncinaError, Unit>(Unit.Default));

        await harness.RunUntilLoggedAsync(2833);

        harness.Logger.EventIds.ShouldNotContain(2960);
        await harness.Store.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Processor_PublishCancelledByTheDispatcher_ConsumesNoRetry()
    {
        var cancelled = EncinaErrors.Create(EncinaErrorCodes.NotificationCancelled, "Notification handler was cancelled");
        var harness = new Harness(Left<EncinaError, Unit>(cancelled));

        await harness.RunUntilLoggedAsync(2962);

        await harness.Store.DidNotReceiveWithAnyArgs().MarkAsFailedAsync(default, default!, default, default);
        await harness.Store.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default);
    }

    [Fact]
    public void Constructor_MaxRetryDelayBelowBaseRetryDelay_Throws()
    {
        var options = new OutboxOptions
        {
            BaseRetryDelay = TimeSpan.FromMinutes(20),
            MaxRetryDelay = TimeSpan.FromMinutes(10)
        };

        var exception = Should.Throw<ArgumentException>(
            () => new TestOutboxProcessor(Substitute.For<IServiceProvider>(), new CapturingLogger(), options));

        exception.ParamName.ShouldBe("options");
        exception.Message.ShouldContain(nameof(OutboxOptions.MaxRetryDelay));
    }

    private static TestOutboxMessage CreateMessage() => new()
    {
        Id = Guid.NewGuid(),
        NotificationType = typeof(ProviderOutboxNotification).AssemblyQualifiedName!,
        Content = JsonSerializer.Serialize(new ProviderOutboxNotification("value")),
        CreatedAtUtc = Now.UtcDateTime,
        RetryCount = 0
    };

    private sealed class Harness
    {
        private readonly IServiceProvider _services;

        public Harness(Either<EncinaError, Unit> publishResult)
        {
            var message = CreateMessage();
            var returned = false;
            Store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    IEnumerable<IOutboxMessage> batch = returned ? [] : [message];
                    returned = true;
                    return Right<EncinaError, IEnumerable<IOutboxMessage>>(batch);
                });
            Store.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(Unit.Default));
            Store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(Unit.Default));
            Store.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(Unit.Default));

            var encina = Substitute.For<IEncina>();
            encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
                .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(publishResult));

            var scopedServices = Substitute.For<IServiceProvider>();
            scopedServices.GetService(typeof(IOutboxStore)).Returns(Store);
            scopedServices.GetService(typeof(IEncina)).Returns(encina);
            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(scopedServices);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);
            var services = Substitute.For<IServiceProvider>();
            services.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
            _services = services;
        }

        public IOutboxStore Store { get; } = Substitute.For<IOutboxStore>();

        public CapturingLogger Logger { get; } = new();

        public async Task RunUntilLoggedAsync(int eventId)
        {
            var options = new OutboxOptions
            {
                EnableProcessor = true,
                ProcessingInterval = TimeSpan.FromMilliseconds(20),
                MaxRetries = 3,
                RetryJitterRatio = 0
            };
            var processor = new TestOutboxProcessor(_services, Logger, options, new FakeTimeProvider(Now));
            using var cts = new CancellationTokenSource();

            await processor.StartAsync(cts.Token);
            try
            {
                await Logger.WaitForAsync(eventId).WaitAsync(TimeSpan.FromSeconds(10));
            }
            finally
            {
                await cts.CancelAsync();
                await processor.StopAsync(CancellationToken.None);
            }
        }
    }

    private sealed class TestOutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger logger,
        OutboxOptions options,
        TimeProvider? timeProvider = null)
        : OutboxProcessorBase(serviceProvider, logger, options, timeProvider);

    private sealed class OutcomeListener : IDisposable
    {
        private readonly MeterListener _listener = new();
        private long _total;

        public OutcomeListener(string outcome)
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Encina" && instrument.Name == "encina.outbox.processor.messages_total")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "outcome" && Equals(tag.Value, outcome))
                    {
                        Interlocked.Add(ref _total, value);
                    }
                }
            });
            _listener.Start();
        }

        public long Total => Interlocked.Read(ref _total);

        public void Dispose() => _listener.Dispose();
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly object _gate = new();
        private readonly List<(int EventId, LogLevel Level, string Message)> _entries = [];
        private readonly Dictionary<int, TaskCompletionSource> _waiters = [];

        public IReadOnlyList<(int EventId, LogLevel Level, string Message)> Entries
        {
            get
            {
                lock (_gate)
                {
                    return [.. _entries];
                }
            }
        }

        public IEnumerable<int> EventIds => Entries.Select(e => e.EventId);

        public Task WaitForAsync(int eventId)
        {
            lock (_gate)
            {
                if (_entries.Any(e => e.EventId == eventId))
                {
                    return Task.CompletedTask;
                }

                if (!_waiters.TryGetValue(eventId, out var waiter))
                {
                    waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _waiters[eventId] = waiter;
                }

                return waiter.Task;
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_gate)
            {
                _entries.Add((eventId.Id, logLevel, formatter(state, exception)));
                if (_waiters.Remove(eventId.Id, out var waiter))
                {
                    waiter.TrySetResult();
                }
            }
        }
    }
}
