using Encina.Messaging.DeadLetter;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;

namespace Encina.PropertyTests.Messaging.DeadLetter;

/// <summary>
/// Property-based tests of the <see cref="IDeadLetterStore"/> invariants, run over random sequences of add,
/// replay mark, delete and clock advance against <see cref="FakeDeadLetterStore"/> (the contract holds the
/// persistent stores to the same behavior).
/// </summary>
[Trait("Category", "Property")]
public sealed class DeadLetterStorePropertyTests
{
    private static readonly DateTime Start = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 60)]
    public bool Property_Pages_AreSortedAndDisjoint_AndTheirUnionEqualsTheFilteredCount(byte[] script, byte pageSeed, bool newestFirst, byte patternSeed)
    {
        var (store, _) = Run(script);
        var filter = new DeadLetterFilter { SourcePattern = Pattern(patternSeed) };
        var pageSize = (pageSeed % 7) + 1;

        var rows = new List<IDeadLetterMessage>();
        for (var skip = 0; ; skip += pageSize)
        {
            var page = Rows(store.GetMessagesAsync(filter, skip, pageSize, newestFirst).GetAwaiter().GetResult());
            rows.AddRange(page);
            if (page.Count < pageSize)
            {
                break;
            }
        }

        var count = Value(store.GetCountAsync(filter).GetAwaiter().GetResult());
        var ordered = newestFirst
            ? rows.OrderByDescending(r => r.DeadLetteredAtUtc).ToList()
            : rows.OrderBy(r => r.DeadLetteredAtUtc).ToList();

        return rows.Count == count
            && rows.Select(r => r.Id).Distinct().Count() == rows.Count
            && ordered.Select(r => r.DeadLetteredAtUtc).SequenceEqual(rows.Select(r => r.DeadLetteredAtUtc));
    }

    [Property(MaxTest = 60)]
    public bool Property_DeleteExpired_NeverRemovesARowThatIsNotYetExpired(byte[] script)
    {
        var (store, clock) = Run(script);
        var now = clock.GetUtcNow().UtcDateTime;
        var survivorsBefore = store.GetMessages()
            .Where(m => m.ExpiresAtUtc is null || m.ExpiresAtUtc > now)
            .Select(m => m.Id)
            .ToHashSet();
        var expiredBefore = store.GetMessages().Count(m => m.ExpiresAtUtc is not null && m.ExpiresAtUtc <= now);

        var deleted = Value(store.DeleteExpiredAsync().GetAwaiter().GetResult());

        var remaining = store.GetMessages().Select(m => m.Id).ToHashSet();
        return deleted == expiredBefore && survivorsBefore.IsSubsetOf(remaining) && remaining.Count == survivorsBefore.Count;
    }

    [Property(MaxTest = 60)]
    public bool Property_AddingTheSameSourceKeyTwice_IsIdempotent(byte patternSeed, byte sourceSeed)
    {
        var clock = new StepClock(Start);
        var store = new FakeDeadLetterStore(clock);
        var pattern = Pattern(patternSeed);
        var source = $"source-{sourceSeed}";

        var first = Value(store.AddAsync(Message(pattern, source, Start, null)).GetAwaiter().GetResult());
        var second = Value(store.AddAsync(Message(pattern, source, Start.AddSeconds(5), null)).GetAwaiter().GetResult());

        return first
            && !second
            && Value(store.GetCountAsync(DeadLetterFilter.All).GetAwaiter().GetResult()) == 1;
    }

    [Property(MaxTest = 60)]
    public bool Property_MarkAsReplayed_SucceedsOncePerRow(byte[] script)
    {
        var (store, _) = Run(script);
        var ids = store.GetMessages().Select(m => m.Id).ToList();

        var firstPass = ids.Select(id => Value(store.MarkAsReplayedAsync(id, "replay.succeeded").GetAwaiter().GetResult())).ToList();
        var secondPass = ids.Select(id => Value(store.MarkAsReplayedAsync(id, "replay.failed").GetAwaiter().GetResult())).ToList();

        // A row replayed by the script already answered false on the first pass; none answers true twice.
        return secondPass.All(replayed => !replayed) && firstPass.Count == secondPass.Count;
    }

    // A script is a sequence of operations read two bytes at a time: (operation, argument).
    private static (FakeDeadLetterStore Store, StepClock Clock) Run(byte[]? script)
    {
        var clock = new StepClock(Start);
        var store = new FakeDeadLetterStore(clock);
        var ids = new List<Guid>();
        var counter = 0;

        var bytes = script ?? [];
        for (var i = 0; i + 1 < bytes.Length; i += 2)
        {
            var argument = bytes[i + 1];
            switch (bytes[i] % 5)
            {
                case 0:
                case 1:
                    var expires = argument % 3 == 0 ? (DateTime?)null : clock.GetUtcNow().UtcDateTime.AddSeconds((argument % 20) - 5);
                    var message = Message(Pattern(argument), $"source-{counter++}-{argument % 4}", clock.GetUtcNow().UtcDateTime, expires);
                    if (Value(store.AddAsync(message).GetAwaiter().GetResult()))
                    {
                        ids.Add(message.Id);
                    }

                    break;
                case 2:
                    if (ids.Count > 0)
                    {
                        store.MarkAsReplayedAsync(ids[argument % ids.Count], "replay.succeeded").GetAwaiter().GetResult();
                    }

                    break;
                case 3:
                    if (ids.Count > 0)
                    {
                        store.DeleteAsync(ids[argument % ids.Count]).GetAwaiter().GetResult();
                    }

                    break;
                default:
                    clock.Advance(TimeSpan.FromSeconds((argument % 10) + 1));
                    break;
            }
        }

        return (store, clock);
    }

    private static string Pattern(byte seed) => (seed % 3) switch { 0 => "Outbox", 1 => "Inbox", _ => "Saga" };

    private static FakeDeadLetterMessage Message(string pattern, string sourceId, DateTime deadLetteredAtUtc, DateTime? expiresAtUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            RequestType = "T",
            RequestContent = "{}",
            ErrorCode = "e",
            SourcePattern = pattern,
            SourceMessageId = sourceId,
            TotalRetryAttempts = 1,
            FirstFailedAtUtc = deadLetteredAtUtc,
            DeadLetteredAtUtc = deadLetteredAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };

    private static T Value<T>(Either<EncinaError, T> result)
        => result.Match(Right: value => value, Left: error => throw new InvalidOperationException(error.GetCode().IfNone("unknown")));

    private static List<IDeadLetterMessage> Rows(Either<EncinaError, IEnumerable<IDeadLetterMessage>> result)
        => Value(result).ToList();

    private sealed class StepClock(DateTime start) : TimeProvider
    {
        private DateTime _utcNow = start;

        public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);

        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
