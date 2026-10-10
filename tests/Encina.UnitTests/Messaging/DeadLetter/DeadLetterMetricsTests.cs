using System.Diagnostics.Metrics;

using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using Encina.Testing.Shouldly;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Tests the dead letter counters (<c>encina.dlq.*</c>) as the orchestrator and the manager emit them, and
/// that no dimension carries a tenant id or error text (SPEC-002 REQ-062).
/// </summary>
/// <remarks>
/// The meter is process-wide, so counters that other tests also increment are asserted with
/// <c>ShouldBeGreaterThanOrEqualTo</c>; counters scoped by a unique source pattern or error code are exact.
/// </remarks>
public sealed class DeadLetterMetricsTests : IDisposable
{
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly object _gate = new();
    private readonly List<(string Instrument, long Value, Dictionary<string, object?> Tags)> _measurements = [];
    private readonly MeterListener _listener = new();

    public DeadLetterMetricsTests()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Encina" && instrument.Name.StartsWith("encina.dlq.", StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            var copy = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                copy[tag.Key] = tag.Value;
            }

            lock (_gate)
            {
                _measurements.Add((instrument.Name, value, copy));
            }
        });
        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public async Task AddAsync_WhenStored_CountsOneAddedMessageBySourcePattern()
    {
        var pattern = $"metrics-add-{Guid.NewGuid():N}";
        var (orchestrator, store) = CreateOrchestrator();
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));

        (await orchestrator.AddAsync(new TestRequest(1), Context(pattern))).ShouldBeRight();

        SumFor("encina.dlq.messages_added_total", "source_pattern", pattern).ShouldBe(1);
        SumFor("encina.dlq.duplicates_ignored_total", "source_pattern", pattern).ShouldBe(0);
    }

    [Fact]
    public async Task AddAsync_WhenTheSourceWasAlreadyCaptured_CountsADuplicateAndNoAddedMessage()
    {
        var pattern = $"metrics-dup-{Guid.NewGuid():N}";
        var (orchestrator, store) = CreateOrchestrator();
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(false));
        var existing = Substitute.For<IDeadLetterMessage>();
        existing.Id.Returns(Guid.NewGuid());
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), 0, 1, false, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(new[] { existing }));

        (await orchestrator.AddAsync(new TestRequest(1), Context(pattern))).ShouldBeRight();

        SumFor("encina.dlq.duplicates_ignored_total", "source_pattern", pattern).ShouldBe(1);
        SumFor("encina.dlq.messages_added_total", "source_pattern", pattern).ShouldBe(0);
    }

    [Fact]
    public async Task AddAsync_WhenTheStoreFails_CountsAStoreFailureByOperationAndErrorCodeOnly()
    {
        var code = $"metrics.failure.{Guid.NewGuid():N}";
        var (orchestrator, store) = CreateOrchestrator();
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create(code, "patient Jane Doe 123")));

        (await orchestrator.AddAsync(new TestRequest(1), Context("metrics-fail"))).ShouldBeErrorWithCode(code);

        var failures = Measurements("encina.dlq.store_failures_total").Where(m => (string?)m.Tags["error_code"] == code).ToList();
        failures.Count.ShouldBe(1);
        failures[0].Tags["operation"].ShouldBe("add");
        failures[0].Tags.Keys.OrderBy(k => k).ShouldBe(["error_code", "operation"]);
    }

    [Fact]
    public async Task SourceCapture_RejectedAndThrown_AreCountedUnderTheCaptureOperation()
    {
        var throwing = Substitute.For<IDeadLetterStore>();
        throwing.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, bool>>>(_ => throw new InvalidOperationException("connection lost"));
        using var rejectingHost = DeadLetterCaptureHost.Create();
        using var throwingHost = DeadLetterCaptureHost.Create(store: throwing);

        (await rejectingHost.Capture.CaptureAsync(new TestRequest(1), Context("metrics-rejected") with { SourcePattern = "Custom" }))
            .ShouldBeErrorWithCode(DeadLetterErrorCodes.CaptureRejected);
        (await throwingHost.Capture.CaptureAsync(new TestRequest(1), Context("metrics-thrown") with { SourcePattern = DeadLetterSourcePatterns.Inbox }))
            .ShouldBeErrorWithCode(DeadLetterErrorCodes.CaptureFailed);

        var capture = Measurements("encina.dlq.store_failures_total").Where(m => (string?)m.Tags["operation"] == "capture").ToList();
        capture.ShouldContain(m => (string?)m.Tags["error_code"] == DeadLetterErrorCodes.CaptureRejected);
        capture.ShouldContain(m => (string?)m.Tags["error_code"] == DeadLetterErrorCodes.CaptureFailed);
    }

    [Fact]
    public async Task AddAsync_WithATenant_NeverUsesTheTenantOrAnyErrorTextAsADimension()
    {
        var (orchestrator, store) = CreateOrchestrator(tenantId: "tenant-secret-42");
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));

        await orchestrator.AddAsync(new TestRequest(1), Context($"metrics-tenant-{Guid.NewGuid():N}"));

        foreach (var measurement in AllMeasurements())
        {
            measurement.Tags.Keys.ShouldNotContain("tenant_id");
            measurement.Tags.Values.ShouldNotContain("tenant-secret-42");
        }
    }

    [Fact]
    public async Task CleanupExpiredAsync_CountsTheDeletedMessagesAsExpired()
    {
        var (orchestrator, store) = CreateOrchestrator();
        store.DeleteExpiredAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, int>(3));

        (await orchestrator.CleanupExpiredAsync()).ShouldBeRight().ShouldBe(3);

        SumFor("encina.dlq.messages_deleted_total", "reason", "expired").ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task DeleteAsync_AndDeleteAllAsync_CountTheDeletedMessagesAsManual()
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));
        store.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, int>(4));
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));
        var manager = NewManager(store);

        (await manager.DeleteAsync(Guid.NewGuid())).ShouldBeRight();
        (await manager.DeleteAllAsync(DeadLetterFilter.All)).ShouldBeRight().ShouldBe(4);

        SumFor("encina.dlq.messages_deleted_total", "reason", "manual").ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task ReplayAsync_CountsSucceededAndFailedOutcomes()
    {
        var store = Substitute.For<IDeadLetterStore>();
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.RequestType.Returns("NonExistent.Type, NonExistent.Assembly");
        message.IsExpiredAt(Arg.Any<DateTime>()).Returns(false);
        store.GetAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.Some(message)));
        store.TryClaimForReplayAsync(message.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));
        store.MarkAsReplayedAsync(message.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));
        var manager = NewManager(store);

        (await manager.ReplayAsync(message.Id)).IsLeft.ShouldBeTrue();

        SumFor("encina.dlq.messages_replayed_total", "outcome", "failed").ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ReplayAsync_WhenRecordingTheOutcomeFails_CountsAStoreFailureByOperationAndCode()
    {
        var code = $"metrics.mark.{Guid.NewGuid():N}";
        var store = Substitute.For<IDeadLetterStore>();
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.RequestType.Returns("NonExistent.Type, NonExistent.Assembly");
        message.IsExpiredAt(Arg.Any<DateTime>()).Returns(false);
        store.GetAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.Some(message)));
        store.TryClaimForReplayAsync(message.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));
        store.MarkAsReplayedAsync(message.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create(code, "down")));
        var manager = NewManager(store);

        (await manager.ReplayAsync(message.Id)).ShouldBeErrorWithCode(code);

        var failures = Measurements("encina.dlq.store_failures_total").Where(m => (string?)m.Tags["error_code"] == code).ToList();
        failures.Count.ShouldBe(1);
        failures[0].Tags["operation"].ShouldBe("mark_replayed");
    }

    [Fact]
    public async Task AddAsync_OversizedIdentityValue_ThrowsBeforeAnyStoreCall()
    {
        var (orchestrator, store) = CreateOrchestrator();
        var context = Context("limits") with { SourceMessageId = new string('x', DeadLetterStoreLimits.SourceMessageIdMaxLength + 1) };

        await Should.ThrowAsync<ArgumentException>(() => orchestrator.AddAsync(new TestRequest(1), context));

        await store.DidNotReceive().AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_OversizedDiagnosticValues_AreCutToTheColumnSize()
    {
        var created = new List<DeadLetterData>();
        var (orchestrator, store) = CreateOrchestrator(created: created);
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));
        var context = Context("limits-cut") with
        {
            CorrelationId = new string('c', 1000),
            Exception = new InvalidOperationException()
        };

        (await orchestrator.AddAsync(new TestRequest(1), context)).ShouldBeRight();

        created.Single().CorrelationId!.Length.ShouldBe(DeadLetterStoreLimits.CorrelationIdMaxLength);
    }

    private static DeadLetterContext Context(string sourcePattern)
        => new(EncinaErrors.Create("test.error", "boom"), null, sourcePattern, 3, FixedUtcNow);

    private static (DeadLetterOrchestrator Orchestrator, IDeadLetterStore Store) CreateOrchestrator(string? tenantId = null, List<DeadLetterData>? created = null)
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));
        var factory = Substitute.For<IDeadLetterMessageFactory>();
        factory.Create(Arg.Any<DeadLetterData>()).Returns(call =>
        {
            created?.Add(call.Arg<DeadLetterData>());
            var message = Substitute.For<IDeadLetterMessage>();
            message.Id.Returns(call.Arg<DeadLetterData>().Id);
            return message;
        });

        var orchestrator = new DeadLetterOrchestrator(
            store,
            factory,
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            Accessor(tenantId),
            new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)));

        return (orchestrator, store);
    }

    private static DeadLetterManager NewManager(IDeadLetterStore store)
    {
        var accessor = Accessor(null);
        var orchestrator = new DeadLetterOrchestrator(
            store,
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            accessor);

        return new DeadLetterManager(
            store,
            orchestrator,
            Substitute.For<IServiceProvider>(),
            NullLogger<DeadLetterManager>.Instance,
            new JsonMessageSerializer(),
            new DeadLetterOptions(),
            accessor,
            new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)));
    }

    private static IRequestContextAccessor Accessor(string? tenantId)
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        if (tenantId is not null)
        {
            var context = Substitute.For<IRequestContext>();
            context.TenantId.Returns(tenantId);
            accessor.RequestContext.Returns(context);
        }

        return accessor;
    }

    private List<(string Instrument, long Value, Dictionary<string, object?> Tags)> AllMeasurements()
    {
        lock (_gate)
        {
            return [.. _measurements];
        }
    }

    private List<(string Instrument, long Value, Dictionary<string, object?> Tags)> Measurements(string instrument)
        => AllMeasurements().Where(m => m.Instrument == instrument).ToList();

    private long SumFor(string instrument, string tag, string value)
        => Measurements(instrument)
            .Where(m => m.Tags.TryGetValue(tag, out var v) && string.Equals(v as string, value, StringComparison.Ordinal))
            .Sum(m => m.Value);

    private sealed record TestRequest(int Value);
}
