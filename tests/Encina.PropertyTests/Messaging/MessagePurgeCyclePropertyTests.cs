using Encina.Messaging.Diagnostics;
using Encina.Messaging.Inbox;
using Encina.Messaging.Purge;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Messaging;

/// <summary>
/// Invariants of one inbox purge cycle (#3013, #3166), against a store that really hands out and deletes rows
/// or replays a scripted sequence of batches.
/// </summary>
[Trait("Category", "Property")]
public class MessagePurgeCyclePropertyTests
{
    /// <summary>The EventId of the no-progress warning (<c>MessagePurgeLog.NoProgress</c>).</summary>
    private const int NoProgressEventId = 2112;

    /// <summary>
    /// Invariant: whatever the backlog and the batch size, the cycle removes every expired message, returns
    /// their count, fetches <c>backlog / batchSize + 1</c> times (it stops on the first short batch) and saves
    /// once per non-empty batch.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Cycle_DrainsTheWholeBacklogInTheExpectedNumberOfBatches()
    {
        var backlogGen = Arb.From(Gen.Choose(0, 60));
        var batchGen = Arb.From(Gen.Choose(1, 20));

        return Prop.ForAll(backlogGen, batchGen, (backlog, batchSize) =>
        {
            var remaining = new Queue<string>(Enumerable.Range(0, backlog).Select(i => $"m{i}"));
            var fetches = 0;
            var saves = 0;
            var store = Substitute.For<IInboxStore>();
            store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    fetches++;
                    var batch = remaining.Take(call.ArgAt<int>(0)).Select(Message).ToList();
                    return Task.FromResult(Right<EncinaError, IEnumerable<IInboxMessage>>(batch));
                });
            store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    foreach (var _ in call.Arg<IEnumerable<string>>())
                    {
                        remaining.Dequeue();
                    }

                    return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
                });
            store.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    saves++;
                    return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
                });

            var result = MessagePurgeCycle.PurgeInboxAsync(store, batchSize, new FakeLogger(), CancellationToken.None)
                .GetAwaiter().GetResult();

            var expectedSaves = (backlog + batchSize - 1) / batchSize;
            return ((int)result == backlog && remaining.Count == 0 && fetches == (backlog / batchSize) + 1 && saves == expectedSaves)
                .ToProperty();
        });
    }

    /// <summary>
    /// Invariant of the no-progress guard: for any sequence of full batches (ids drawn from a small pool, so
    /// repeats are common), the cycle stops on the first batch whose ids were all tried earlier in the cycle
    /// and logs the 2112 warning exactly then, and it never stops while a batch has a new id: every earlier
    /// batch is removed and saved, and a sequence that never repeats is drained up to its closing empty batch.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Cycle_StopsOnTheFirstBatchOfAlreadyTriedIds_AndNeverWhileABatchHasANewId()
    {
        var scenario =
            from batchSize in Gen.Choose(1, 4)
            from batches in Gen.ListOf(
                Gen.Select(Gen.Shuffle(Enumerable.Range(0, 8).Select(i => $"m{i}")), ids => ids.Take(batchSize).ToArray()))
            select (batchSize, batches: batches.ToList());

        return Prop.ForAll(Arb.From(scenario), scripted =>
        {
            var (batchSize, batches) = scripted;

            // The model: the batches before the first one made only of ids already tried.
            var tried = new System.Collections.Generic.HashSet<string>();
            var processed = new List<string[]>();
            var stoppedByGuard = false;
            foreach (var batch in batches)
            {
                if (batch.All(tried.Contains))
                {
                    stoppedByGuard = true;
                    break;
                }

                tried.UnionWith(batch);
                processed.Add(batch);
            }

            var removedBatches = new List<string[]>();
            var saves = 0;
            var fetches = 0;
            var store = Substitute.For<IInboxStore>();
            store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    var next = fetches < batches.Count ? batches[fetches] : [];
                    fetches++;
                    return Task.FromResult(Right<EncinaError, IEnumerable<IInboxMessage>>(next.Select(Message).ToList()));
                });
            store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    removedBatches.Add([.. call.Arg<IEnumerable<string>>()]);
                    return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
                });
            store.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    saves++;
                    return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
                });
            var logger = new FakeLogger();

            var result = MessagePurgeCycle.PurgeInboxAsync(store, batchSize, logger, CancellationToken.None)
                .GetAwaiter().GetResult();

            var warned = logger.Collector.GetSnapshot().Count(r => r.Id.Id == NoProgressEventId);
            var expectedFetches = stoppedByGuard ? processed.Count + 1 : batches.Count + 1;

            return (result.IsRight
                    && (int)result == processed.Count * batchSize
                    && removedBatches.Count == processed.Count
                    && removedBatches.Zip(processed).All(p => p.Item1.SequenceEqual(p.Item2))
                    && saves == processed.Count
                    && fetches == expectedFetches
                    && warned == (stoppedByGuard ? 1 : 0))
                .ToProperty();
        });
    }

    /// <summary>
    /// Invariant: whichever store call fails (fetch, remove or save) on whichever batch, the cycle returns that
    /// error, never reports success, and does not touch the store again.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Cycle_StopsAtTheFirstLeftOfTheStore_AndReturnsIt()
    {
        var scenario =
            from stage in Gen.Choose(0, 2)
            from failingBatch in Gen.Choose(0, 4)
            from batchSize in Gen.Choose(1, 5)
            select (stage, failingBatch, batchSize);

        return Prop.ForAll(Arb.From(scenario), s =>
        {
            var (stage, failingBatch, batchSize) = s;
            var error = EncinaErrors.Create("test.purge.failure", "failure");
            var fetches = 0;
            var removes = 0;
            var saves = 0;
            var store = Substitute.For<IInboxStore>();
            store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    var index = fetches++;
                    if (stage == 0 && index == failingBatch)
                    {
                        return Task.FromResult(Left<EncinaError, IEnumerable<IInboxMessage>>(error));
                    }

                    // Always a full batch of fresh ids, so only the scripted failure can end the cycle.
                    var batch = Enumerable.Range(0, batchSize).Select(i => Message($"b{index}-{i}")).ToList();
                    return Task.FromResult(Right<EncinaError, IEnumerable<IInboxMessage>>(batch));
                });
            store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    var index = removes++;
                    return Task.FromResult(stage == 1 && index == failingBatch
                        ? Left<EncinaError, Unit>(error)
                        : Right<EncinaError, Unit>(Unit.Default));
                });
            store.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    var index = saves++;
                    return Task.FromResult(stage == 2 && index == failingBatch
                        ? Left<EncinaError, Unit>(error)
                        : Right<EncinaError, Unit>(Unit.Default));
                });

            var result = MessagePurgeCycle.PurgeInboxAsync(store, batchSize, new FakeLogger(), CancellationToken.None)
                .GetAwaiter().GetResult();

            var expectedFetches = failingBatch + 1;
            var expectedRemoves = stage == 0 ? failingBatch : failingBatch + 1;
            var expectedSaves = stage == 2 ? failingBatch + 1 : failingBatch;

            var returnedCode = result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty));

            return (returnedCode == "test.purge.failure"
                    && fetches == expectedFetches
                    && removes == expectedRemoves
                    && saves == expectedSaves)
                .ToProperty();
        });
    }

    /// <summary>
    /// Invariant: recording a purge counter never throws, whatever the pattern, count or error code.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Metrics_RecordingNeverThrows(NonEmptyString pattern, NonEmptyString errorCode, int count)
    {
        var thrown = Record.Exception(() =>
        {
            MessagePurgeMetrics.RecordDeleted(pattern.Get, count);
            MessagePurgeMetrics.RecordCycleFailure(pattern.Get, errorCode.Get);
        });

        return thrown is null;
    }

    private static IInboxMessage Message(string id)
    {
        var message = Substitute.For<IInboxMessage>();
        message.MessageId.Returns(id);
        return message;
    }
}
