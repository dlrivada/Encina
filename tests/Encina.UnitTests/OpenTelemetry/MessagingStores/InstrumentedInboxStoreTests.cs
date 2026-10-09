using Encina.Messaging.Inbox;
using Encina.OpenTelemetry.MessagingStores;
using Encina.Testing;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.MessagingStores;

/// <summary>
/// Unit tests for <see cref="InstrumentedInboxStore"/>.
/// </summary>
public sealed class InstrumentedInboxStoreTests
{
    private readonly IInboxStore _inner;
    private readonly InstrumentedInboxStore _sut;

    public InstrumentedInboxStoreTests()
    {
        _inner = Substitute.For<IInboxStore>();
        _sut = new InstrumentedInboxStore(_inner);
    }

    [Fact]
    public async Task EveryFailedStoreCall_RecordsOnlyTheErrorCodeInTheActivityStatus()
    {
        const string secret = "duplicate key value (patient-4711)";
        var failure = Either<EncinaError, Unit>.Left(EncinaErrors.Create("inbox.test_failure", secret));
        var failedGet = Either<EncinaError, Option<IInboxMessage>>.Left(EncinaErrors.Create("inbox.test_failure", secret));
        var failedList = Either<EncinaError, IEnumerable<IInboxMessage>>.Left(EncinaErrors.Create("inbox.test_failure", secret));
        var message = Substitute.For<IInboxMessage>();
        message.MessageId.Returns("msg-1");

        _inner.GetMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(failedGet);
        _inner.AddAsync(Arg.Any<IInboxMessage>(), Arg.Any<CancellationToken>()).Returns(failure);
        _inner.MarkAsProcessedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(failure);
        _inner.CacheHandlerErrorAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(failure);
        _inner.MarkAsFailedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(failure);
        _inner.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(failedList);
        _inner.RemoveExpiredMessagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns(failure);

        var statuses = new List<string?>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Messaging.Inbox",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                lock (statuses)
                {
                    statuses.Add(activity.StatusDescription);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        await _sut.GetMessageAsync("msg-1");
        await _sut.AddAsync(message);
        await _sut.MarkAsProcessedAsync("msg-1", "r");
        await _sut.CacheHandlerErrorAsync("msg-1", "r");
        await _sut.MarkAsFailedAsync("msg-1", "e", null);
        await _sut.GetExpiredMessagesAsync(10);
        await _sut.RemoveExpiredMessagesAsync(["msg-1"]);

        statuses.Count.ShouldBe(7);
        statuses.ShouldAllBe(s => s == "inbox.test_failure");
    }

    [Fact]
    public async Task GetMessageAsync_DelegatesToInner()
    {
        _inner.GetMessageAsync("msg-1", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Option<IInboxMessage>>.Right(Option<IInboxMessage>.None));

        var result = await _sut.GetMessageAsync("msg-1");

        result.ShouldBeSuccess();
        await _inner.Received(1).GetMessageAsync("msg-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_DelegatesToInner()
    {
        var message = Substitute.For<IInboxMessage>();
        message.MessageId.Returns("msg-1");
        message.RequestType.Returns("TestCmd");
        _inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.AddAsync(message);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task CacheHandlerErrorAsync_DelegatesToInner()
    {
        _inner.CacheHandlerErrorAsync("msg-1", "err", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.CacheHandlerErrorAsync("msg-1", "err");

        result.ShouldBeSuccess();
        await _inner.Received(1).CacheHandlerErrorAsync("msg-1", "err", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsProcessedAsync_DelegatesToInner()
    {
        _inner.MarkAsProcessedAsync("msg-1", "ok", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.MarkAsProcessedAsync("msg-1", "ok");

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task MarkAsFailedAsync_DelegatesToInner()
    {
        _inner.MarkAsFailedAsync("msg-1", "err", null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.MarkAsFailedAsync("msg-1", "err", null);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_DelegatesToInner()
    {
        var messages = new List<IInboxMessage>();
        _inner.GetExpiredMessagesAsync(10, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IInboxMessage>>.Right(messages));

        var result = await _sut.GetExpiredMessagesAsync(10);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_DelegatesToInner()
    {
        var ids = new[] { "msg-1", "msg-2" };
        _inner.RemoveExpiredMessagesAsync(ids, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.RemoveExpiredMessagesAsync(ids);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToInner()
    {
        _inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.SaveChangesAsync();

        result.ShouldBeSuccess();
    }
}
