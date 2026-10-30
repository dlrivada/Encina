using Encina.Messaging.Inbox;
using Encina.Messaging.Purge;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Purge;

/// <summary>
/// One inbox purge cycle (#3013): batches through the existing store methods, the first <c>Left</c> stops it.
/// </summary>
public sealed class MessagePurgeCycleTests
{
    private readonly IInboxStore _store = Substitute.For<IInboxStore>();

    public MessagePurgeCycleTests()
    {
        _store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        _store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
    }

    [Fact]
    public async Task EmptyFirstBatch_ReturnsZeroAndTouchesNothingElse()
    {
        Returns(Batch());

        var result = await MessagePurgeCycle.PurgeInboxAsync(_store, 3, CancellationToken.None);

        ((int)result).ShouldBe(0);
        await _store.DidNotReceiveWithAnyArgs().RemoveExpiredMessagesAsync(default!, default);
        await _store.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task FullBatchesThenAShortOne_AreAllRemovedAndTotalled()
    {
        Returns(Batch("a", "b"), Batch("c", "d"), Batch("e"));

        var result = await MessagePurgeCycle.PurgeInboxAsync(_store, 2, CancellationToken.None);

        ((int)result).ShouldBe(5);
        await _store.Received(3).GetExpiredMessagesAsync(2, Arg.Any<CancellationToken>());
        await _store.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FullBatchFollowedByAnEmptyOne_StopsOnTheEmptyBatch()
    {
        Returns(Batch("a", "b"), Batch());

        var result = await MessagePurgeCycle.PurgeInboxAsync(_store, 2, CancellationToken.None);

        ((int)result).ShouldBe(2);
        await _store.Received(2).GetExpiredMessagesAsync(2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeftFromTheFetch_IsReturnedAndNothingIsRemoved()
    {
        _store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IEnumerable<IInboxMessage>>(EncinaErrors.Create("inbox.fetch", "x")));

        var result = await MessagePurgeCycle.PurgeInboxAsync(_store, 2, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await _store.DidNotReceiveWithAnyArgs().RemoveExpiredMessagesAsync(default!, default);
    }

    [Fact]
    public async Task LeftFromTheRemove_IsReturnedAndTheChangesAreNotSaved()
    {
        Returns(Batch("a"));
        _store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("inbox.remove", "x")));

        var result = await MessagePurgeCycle.PurgeInboxAsync(_store, 2, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await _store.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task LeftFromTheSave_IsReturnedAndTheCycleStops()
    {
        Returns(Batch("a", "b"), Batch("c"));
        _store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("inbox.save", "x")));

        var result = await MessagePurgeCycle.PurgeInboxAsync(_store, 2, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await _store.Received(1).GetExpiredMessagesAsync(2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => MessagePurgeCycle.PurgeInboxAsync(_store, 2, cts.Token));
    }

    private void Returns(params IEnumerable<IInboxMessage>[] batches)
    {
        var results = batches.Select(b => Right<EncinaError, IEnumerable<IInboxMessage>>(b)).ToArray();
        _store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(results[0], results.Skip(1).ToArray());
    }

    private static List<IInboxMessage> Batch(params string[] ids)
        => ids.Select(id =>
        {
            var message = Substitute.For<IInboxMessage>();
            message.MessageId.Returns(id);
            return message;
        }).ToList();
}
