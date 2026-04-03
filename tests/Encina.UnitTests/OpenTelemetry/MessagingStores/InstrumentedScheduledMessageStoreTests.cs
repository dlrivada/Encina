using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.MessagingStores;
using Encina.Testing;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.MessagingStores;

/// <summary>
/// Unit tests for <see cref="InstrumentedScheduledMessageStore"/>.
/// </summary>
public sealed class InstrumentedScheduledMessageStoreTests
{
    private readonly IScheduledMessageStore _inner;
    private readonly InstrumentedScheduledMessageStore _sut;

    public InstrumentedScheduledMessageStoreTests()
    {
        _inner = Substitute.For<IScheduledMessageStore>();
        _sut = new InstrumentedScheduledMessageStore(_inner);
    }

    [Fact]
    public async Task AddAsync_DelegatesToInner()
    {
        var message = Substitute.For<IScheduledMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.RequestType.Returns("TestCmd");
        message.ScheduledAtUtc.Returns(DateTime.UtcNow.AddHours(1));
        _inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.AddAsync(message);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task GetDueMessagesAsync_DelegatesToInner()
    {
        var messages = new List<IScheduledMessage>();
        _inner.GetDueMessagesAsync(10, 3, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IScheduledMessage>>.Right(messages));

        var result = await _sut.GetDueMessagesAsync(10, 3);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_DelegatesToInner()
    {
        var id = Guid.NewGuid();
        _inner.MarkAsProcessedAsync(id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.MarkAsProcessedAsync(id);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task MarkAsFailedAsync_DelegatesToInner()
    {
        var id = Guid.NewGuid();
        _inner.MarkAsFailedAsync(id, "err", null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.MarkAsFailedAsync(id, "err", null);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_DelegatesToInner()
    {
        var id = Guid.NewGuid();
        var nextAt = DateTime.UtcNow.AddHours(2);
        _inner.RescheduleRecurringMessageAsync(id, nextAt, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.RescheduleRecurringMessageAsync(id, nextAt);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task CancelAsync_DelegatesToInner()
    {
        var id = Guid.NewGuid();
        _inner.CancelAsync(id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.CancelAsync(id);

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
