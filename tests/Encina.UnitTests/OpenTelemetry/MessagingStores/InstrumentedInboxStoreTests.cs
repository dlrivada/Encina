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
    public async Task IncrementRetryCountAsync_DelegatesToInner()
    {
        _inner.IncrementRetryCountAsync("msg-1", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.IncrementRetryCountAsync("msg-1");

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
