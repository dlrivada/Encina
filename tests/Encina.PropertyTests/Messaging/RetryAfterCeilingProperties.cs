using System.Text.Json;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using FsCheck;
using FsCheck.Fluent;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Messaging;

/// <summary>
/// Invariant of the outbox for a <c>Retry-After</c> and its ceiling (#2588): at or below the ceiling the next
/// attempt is at now + max(backoff, Retry-After); above it nothing is scheduled.
/// </summary>
public class RetryAfterCeilingProperties
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Backoff = TimeSpan.FromSeconds(5);

    public sealed class Note
    {
        public string Text { get; set; } = "note";
    }

    private sealed class PendingMessage : IOutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NotificationType { get; set; } = typeof(Note).AssemblyQualifiedName!;
        public string Content { get; set; } = JsonSerializer.Serialize(new Note());
        public DateTime CreatedAtUtc { get; set; } = Now.UtcDateTime;
        public DateTime? ProcessedAtUtc { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsProcessed => ProcessedAtUtc.HasValue;
        public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
    }

    // Marked: the store recorded a retry time; Exhausted: the store recorded the exhausted state instead.
    private static (DateTime? NextRetryAtUtc, bool Marked, bool Exhausted) Run(TimeSpan retryAfter, TimeSpan ceiling)
    {
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IOutboxMessage>>(new List<IOutboxMessage> { new PendingMessage() }));

        DateTime? captured = null;
        var marked = false;
        store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.ArgAt<DateTime?>(2);
                marked = true;
                return Right<EncinaError, Unit>(Unit.Default);
            });

        var exhausted = false;
        store.MarkAsExhaustedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                exhausted = true;
                return Right<EncinaError, Unit>(Unit.Default);
            });

        var options = new OutboxOptions
        {
            MaxRetries = 10,
            BaseRetryDelay = Backoff,
            MaxRetryDelay = TimeSpan.FromHours(1),
            RetryJitterRatio = 0,
            MaxRetryAfter = ceiling
        };
        var processor = new OutboxBatchProcessor(store, options, NullLogger.Instance, serializer: null, new FakeTimeProvider(Now));
        var error = EncinaErrors.Create(
            "encina.outbound.rate_limited",
            "Rate limited",
            details: new Dictionary<string, object?> { ["transient"] = true, ["http.retry_after"] = retryAfter });

        processor.ProcessAsync((_, _, _) => ValueTask.FromResult(Left<EncinaError, Unit>(error)), CancellationToken.None)
            .GetAwaiter().GetResult();

        return (captured, marked, exhausted);
    }

    [FsCheck.Xunit.Property]
    public FsCheck.Property Outbox_RetryAfterAtOrBelowTheCeiling_SchedulesAtNowPlusTheLongerOfBackoffAndHeader() =>
        Prop.ForAll<NonNegativeInt, PositiveInt>((seconds, ceilingSeconds) =>
        {
            var retryAfter = TimeSpan.FromSeconds(seconds.Get);
            var ceiling = TimeSpan.FromSeconds(ceilingSeconds.Get);
            var (next, marked, exhausted) = Run(retryAfter, ceiling);

            if (retryAfter > ceiling)
            {
                // Nothing is scheduled: no retry time is recorded, the exhausted state is.
                return (exhausted && !marked && next is null).ToProperty();
            }

            var expected = Now.UtcDateTime + (retryAfter > Backoff ? retryAfter : Backoff);
            return (marked && !exhausted && next == expected).ToProperty();
        });
}
