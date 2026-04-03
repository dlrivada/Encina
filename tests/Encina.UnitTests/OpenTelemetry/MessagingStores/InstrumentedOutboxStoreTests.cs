using Encina.Messaging.Outbox;
using Encina.OpenTelemetry.MessagingStores;
using Encina.Testing;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.MessagingStores;

/// <summary>
/// Unit tests for <see cref="InstrumentedOutboxStore"/>.
/// </summary>
public sealed class InstrumentedOutboxStoreTests
{
    private readonly IOutboxStore _inner;
    private readonly InstrumentedOutboxStore _sut;

    public InstrumentedOutboxStoreTests()
    {
        _inner = Substitute.For<IOutboxStore>();
        _sut = new InstrumentedOutboxStore(_inner);
    }

    [Fact]
    public async Task AddAsync_DelegatesToInner()
    {
        var message = Substitute.For<IOutboxMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.NotificationType.Returns("TestEvent");
        _inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.AddAsync(message);

        result.ShouldBeSuccess();
        await _inner.Received(1).AddAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPendingMessagesAsync_DelegatesToInner()
    {
        var messages = new List<IOutboxMessage>();
        _inner.GetPendingMessagesAsync(10, 3, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(messages));

        var result = await _sut.GetPendingMessagesAsync(10, 3);

        result.ShouldBeSuccess();
        await _inner.Received(1).GetPendingMessagesAsync(10, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsProcessedAsync_DelegatesToInner()
    {
        var id = Guid.NewGuid();
        _inner.MarkAsProcessedAsync(id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.MarkAsProcessedAsync(id);

        result.ShouldBeSuccess();
        await _inner.Received(1).MarkAsProcessedAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsFailedAsync_DelegatesToInner()
    {
        var id = Guid.NewGuid();
        _inner.MarkAsFailedAsync(id, "error", null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.MarkAsFailedAsync(id, "error", null);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToInner()
    {
        _inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.SaveChangesAsync();

        result.ShouldBeSuccess();
        await _inner.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WhenInnerFails_ReturnsError()
    {
        var message = Substitute.For<IOutboxMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.NotificationType.Returns("TestEvent");
        var error = EncinaError.New("store error");
        _inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(error));

        var result = await _sut.AddAsync(message);

        result.ShouldBeError();
    }
}
