using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using Encina.OpenTelemetry.MessagingStores;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Shouldly;

using LanguageExt;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.UnitTests.OpenTelemetry.MessagingStores;

/// <summary>
/// One add, replay and cleanup of a dead letter, through the instrumented store, the orchestrator and the
/// manager: the expected activities and counters are emitted, and no tag, metric dimension or log carries
/// the payload, the tenant id, an <see cref="EncinaError"/> message or an exception message (SPEC-002 REQ-062).
/// </summary>
public sealed class DeadLetterTelemetryTests
{
    private const string Payload = "payload-secret-7f3a";
    private const string TenantId = "tenant-secret-42";
    private const string ErrorText = "patient Jane Doe 123 not found";

    public sealed record SecretCommand(string Secret) : IRequest<int>;

    [Fact]
    public async Task AddReplayAndCleanup_EmitSpansAndCountersAndLeakNothing()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
        var pattern = $"telemetry-{Guid.NewGuid():N}";
        var activities = new List<Activity>();
        var gate = new object();
        var measurements = new List<(string Instrument, Dictionary<string, object?> Tags)>();

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Messaging.DeadLetter",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (gate)
                {
                    activities.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(activityListener);

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Encina" && instrument.Name.StartsWith("encina.dlq.", StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            var copy = tags.ToArray().ToDictionary(t => t.Key, t => t.Value);
            lock (gate)
            {
                measurements.Add((instrument.Name, copy));
            }
        });
        meterListener.Start();

        var inner = new FakeDeadLetterStore(clock);
        var store = new InstrumentedDeadLetterStore(inner);
        var orchestratorLogger = new FakeLogger<DeadLetterOrchestrator>();
        var managerLogger = new FakeLogger<DeadLetterManager>();
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(TenantId);
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        var factory = Substitute.For<IDeadLetterMessageFactory>();
        factory.Create(Arg.Any<DeadLetterData>()).Returns(call =>
        {
            var d = call.Arg<DeadLetterData>();
            return new FakeDeadLetterMessage
            {
                Id = d.Id,
                RequestType = d.RequestType,
                RequestContent = d.RequestContent,
                ErrorCode = d.ErrorCode,
                SourcePattern = d.SourcePattern,
                SourceMessageId = d.SourceMessageId,
                TenantId = d.TenantId,
                TotalRetryAttempts = d.TotalRetryAttempts,
                FirstFailedAtUtc = d.FirstFailedAtUtc,
                DeadLetteredAtUtc = d.DeadLetteredAtUtc,
                ExpiresAtUtc = d.ExpiresAtUtc
            };
        });
        var serializer = new JsonMessageSerializer();
        var orchestrator = new DeadLetterOrchestrator(
            store, factory, new DeadLetterOptions(), orchestratorLogger, serializer, accessor, clock);
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Left<EncinaError, int>(EncinaErrors.Create("telemetry.handler_failed", ErrorText))));
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEncina)).Returns(encina);
        var manager = new DeadLetterManager(
            store, orchestrator, services, managerLogger, serializer, new DeadLetterOptions(), accessor, clock);

        var added = (await orchestrator.AddAsync(
            new SecretCommand(Payload),
            new DeadLetterContext(EncinaErrors.Create("telemetry.failed", ErrorText), new InvalidOperationException(ErrorText), pattern, 3, clock.GetUtcNow().UtcDateTime))).ShouldBeRight();
        var replay = (await manager.ReplayAsync(added.Id)).ShouldBeRight();
        clock.Advance(TimeSpan.FromDays(30));
        (await manager.CleanupExpiredAsync()).ShouldBeRight().ShouldBe(1);

        replay.Success.ShouldBeFalse();
        List<Activity> spans;
        lock (gate)
        {
            spans = [.. activities];
        }

        spans.Select(a => a.OperationName).ShouldBeSubsetOf(
            ["encina.dlq.add", "encina.dlq.query", "encina.dlq.count", "encina.dlq.replay_claim", "encina.dlq.replay_mark", "encina.dlq.delete", "encina.dlq.delete_many", "encina.dlq.delete_expired"]);
        spans.ShouldContain(a => a.OperationName == "encina.dlq.add" && (string?)a.GetTagItem("dlq.source_pattern") == pattern);
        spans.ShouldContain(a => a.OperationName == "encina.dlq.replay_claim");
        spans.ShouldContain(a => a.OperationName == "encina.dlq.replay_mark");
        spans.ShouldContain(a => a.OperationName == "encina.dlq.delete_expired" && (int?)a.GetTagItem("dlq.count") == 1);

        List<(string Instrument, Dictionary<string, object?> Tags)> counters;
        lock (gate)
        {
            counters = [.. measurements];
        }

        counters.ShouldContain(m => m.Instrument == "encina.dlq.messages_added_total" && (string?)m.Tags["source_pattern"] == pattern);
        counters.ShouldContain(m => m.Instrument == "encina.dlq.messages_replayed_total" && (string?)m.Tags["outcome"] == "failed");
        counters.ShouldContain(m => m.Instrument == "encina.dlq.messages_deleted_total" && (string?)m.Tags["reason"] == "expired");

        string[] forbidden = [Payload, TenantId, ErrorText];
        var observed = new List<string>();
        foreach (var span in spans)
        {
            observed.AddRange(span.TagObjects.Select(t => t.Value?.ToString() ?? string.Empty));
            observed.Add(span.StatusDescription ?? string.Empty);
        }

        foreach (var counter in counters)
        {
            observed.AddRange(counter.Tags.Select(t => t.Value?.ToString() ?? string.Empty));
            counter.Tags.Keys.ShouldNotContain("tenant_id");
        }

        foreach (var record in orchestratorLogger.Collector.GetSnapshot().Concat(managerLogger.Collector.GetSnapshot()))
        {
            observed.Add(record.Message);
            observed.Add(record.Exception?.Message ?? string.Empty);
        }

        foreach (var value in observed)
        {
            foreach (var secret in forbidden)
            {
                value.ShouldNotContain(secret);
            }
        }
    }
}
