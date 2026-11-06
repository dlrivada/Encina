using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Purge;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Messaging;

/// <summary>
/// Behavioral contract of the inbox purge (#3013, #3166): the options keep their defaults and validation, the
/// service is a hosted service registered only when the inbox purge is on, and the purge cycle honours the
/// <see cref="IInboxStore"/> contract (fetch with the batch size, remove exactly the ids it read, save after
/// every remove, stop at the first <c>Left</c>).
/// </summary>
[Trait("Category", "Contract")]
public sealed class MessagePurgeContractTests
{
    [Fact]
    public void MessagePurgeService_IsASealedHostedService()
    {
        typeof(MessagePurgeService).IsSealed.ShouldBeTrue();
        typeof(IHostedService).IsAssignableFrom(typeof(MessagePurgeService)).ShouldBeTrue();
        typeof(BackgroundService).IsAssignableFrom(typeof(MessagePurgeService)).ShouldBeTrue();
    }

    [Fact]
    public void InboxOptions_PurgeDefaults_AreThirtyDaysDailyBatchesOfOneHundredOn()
    {
        var options = new InboxOptions();

        options.MessageRetentionPeriod.ShouldBe(TimeSpan.FromDays(30));
        options.PurgeInterval.ShouldBe(TimeSpan.FromHours(24));
        options.PurgeBatchSize.ShouldBe(100);
        options.EnableAutomaticPurge.ShouldBeTrue();
    }

    [Fact]
    public void InboxOptions_PurgeSetters_RejectNonPositiveValues()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new InboxOptions { PurgeBatchSize = 0 });
        Should.Throw<ArgumentOutOfRangeException>(() => new InboxOptions { PurgeInterval = TimeSpan.Zero });
        Should.Throw<ArgumentOutOfRangeException>(() => new InboxOptions { MessageRetentionPeriod = TimeSpan.Zero });
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void SharedRegistration_RegistersThePurgeServiceOnlyWithTheInboxAndItsPurgeOn(
        bool useInbox, bool automaticPurge, bool expected)
    {
        var config = new MessagingConfiguration { UseInbox = useInbox };
        config.InboxOptions.EnableAutomaticPurge = automaticPurge;
        var services = new ServiceCollection();

        services.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory, FakeInboxStore, StubInboxFactory, StubProcessor>(config);

        services.Any(d => d.ImplementationType == typeof(MessagePurgeService)).ShouldBe(expected);
    }

    [Fact]
    public async Task Cycle_FetchesWithTheBatchSize_RemovesExactlyTheIdsItRead_AndSavesAfterTheRemove()
    {
        var store = Substitute.For<IInboxStore>();
        Fetches(store, Batch("a", "b"), Batch("c"));
        Succeeds(store);

        var result = await MessagePurgeCycle.PurgeInboxAsync(store, 2, NullLogger.Instance, CancellationToken.None);

        ((int)result).ShouldBe(3);
        Received.InOrder(() =>
        {
            store.GetExpiredMessagesAsync(2, Arg.Any<CancellationToken>());
            store.RemoveExpiredMessagesAsync(Arg.Is<IEnumerable<string>>(ids => string.Join(',', ids) == "a,b"), Arg.Any<CancellationToken>());
            store.SaveChangesAsync(Arg.Any<CancellationToken>());
            store.GetExpiredMessagesAsync(2, Arg.Any<CancellationToken>());
            store.RemoveExpiredMessagesAsync(Arg.Is<IEnumerable<string>>(ids => string.Join(',', ids) == "c"), Arg.Any<CancellationToken>());
            store.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Cycle_NothingExpired_ReturnsZeroWithoutRemovingOrSaving()
    {
        var store = Substitute.For<IInboxStore>();
        Fetches(store, Batch());
        Succeeds(store);

        var result = await MessagePurgeCycle.PurgeInboxAsync(store, 5, NullLogger.Instance, CancellationToken.None);

        ((int)result).ShouldBe(0);
        await store.DidNotReceiveWithAnyArgs().RemoveExpiredMessagesAsync(default!, default);
        await store.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Cycle_LeftFromTheFetch_IsReturnedAndNothingIsRemoved()
    {
        var store = Substitute.For<IInboxStore>();
        store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IEnumerable<IInboxMessage>>(EncinaErrors.Create("inbox.fetch", "x")));

        var result = await MessagePurgeCycle.PurgeInboxAsync(store, 2, NullLogger.Instance, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await store.DidNotReceiveWithAnyArgs().RemoveExpiredMessagesAsync(default!, default);
        await store.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Cycle_LeftFromTheRemove_IsReturnedAndNothingIsSaved()
    {
        var store = Substitute.For<IInboxStore>();
        Fetches(store, Batch("a"));
        store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("inbox.remove", "x")));

        var result = await MessagePurgeCycle.PurgeInboxAsync(store, 2, NullLogger.Instance, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await store.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Cycle_LeftFromTheSave_IsReturnedAndTheCycleStops()
    {
        var store = Substitute.For<IInboxStore>();
        Fetches(store, Batch("a", "b"), Batch("c", "d"));
        store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("inbox.save", "x")));

        var result = await MessagePurgeCycle.PurgeInboxAsync(store, 2, NullLogger.Instance, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await store.Received(1).GetExpiredMessagesAsync(2, Arg.Any<CancellationToken>());
    }

    private static void Succeeds(IInboxStore store)
    {
        store.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
    }

    // The batches come back in order; after the last one the store reports nothing expired.
    private static void Fetches(IInboxStore store, params IReadOnlyList<IInboxMessage>[] batches)
    {
        var queue = new Queue<IReadOnlyList<IInboxMessage>>(batches);
        store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Right<EncinaError, IEnumerable<IInboxMessage>>(
                queue.Count > 0 ? queue.Dequeue() : [])));
    }

    private static IReadOnlyList<IInboxMessage> Batch(params string[] ids)
        => [.. ids.Select(id =>
        {
            var message = Substitute.For<IInboxMessage>();
            message.MessageId.Returns(id);
            return message;
        })];

    private sealed class StubInboxFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => Substitute.For<IInboxMessage>();
    }

    private sealed class StubProcessor : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
