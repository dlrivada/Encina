using Encina.Messaging.Recoverability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// Additional tests for <see cref="DelayedRetryScheduler"/> covering scheduling and cancellation paths.
/// </summary>
public sealed class DelayedRetrySchedulerAdditionalTests
{
    private readonly IDelayedRetryStore _store = Substitute.For<IDelayedRetryStore>();
    private readonly IDelayedRetryMessageFactory _messageFactory = Substitute.For<IDelayedRetryMessageFactory>();

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DelayedRetryScheduler(
            null!,
            _messageFactory,
            NullLogger<DelayedRetryScheduler>.Instance));
    }

    [Fact]
    public void Constructor_WithNullFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DelayedRetryScheduler(
            _store,
            null!,
            NullLogger<DelayedRetryScheduler>.Instance));
    }

    [Fact]
    public async Task ScheduleRetryAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        var sut = new DelayedRetryScheduler(_store, _messageFactory, NullLogger<DelayedRetryScheduler>.Instance);
        var context = new RecoverabilityContext();

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ScheduleRetryAsync<object>(null!, context, TimeSpan.FromMinutes(1), 0));
    }

    [Fact]
    public async Task ScheduleRetryAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var sut = new DelayedRetryScheduler(_store, _messageFactory, NullLogger<DelayedRetryScheduler>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ScheduleRetryAsync("request", null!, TimeSpan.FromMinutes(1), 0));
    }

    [Fact]
    public async Task ScheduleRetryAsync_CreatesMessageAndPersists()
    {
        var message = Substitute.For<IDelayedRetryMessage>();
        _messageFactory.Create(Arg.Any<DelayedRetryMessageData>()).Returns(message);

        var sut = new DelayedRetryScheduler(_store, _messageFactory, NullLogger<DelayedRetryScheduler>.Instance);
        var context = new RecoverabilityContext { CorrelationId = "corr-1" };

        var result = await sut.ScheduleRetryAsync("test-request", context, TimeSpan.FromMinutes(5), 0);

        result.IsRight.ShouldBeTrue();
        _messageFactory.Received(1).Create(Arg.Any<DelayedRetryMessageData>());
        await _store.Received(1).AddAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelScheduledRetryAsync_DeletesByContextId()
    {
        var contextId = Guid.NewGuid();
        _store.DeleteByContextIdAsync(contextId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new DelayedRetryScheduler(_store, _messageFactory, NullLogger<DelayedRetryScheduler>.Instance);
        var result = await sut.CancelScheduledRetryAsync(contextId);

        result.IsRight.ShouldBeTrue();
        await _store.Received(1).DeleteByContextIdAsync(contextId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelScheduledRetryAsync_WhenNotFound_StillReturnsUnit()
    {
        var contextId = Guid.NewGuid();
        _store.DeleteByContextIdAsync(contextId, Arg.Any<CancellationToken>()).Returns(false);

        var sut = new DelayedRetryScheduler(_store, _messageFactory, NullLogger<DelayedRetryScheduler>.Instance);
        var result = await sut.CancelScheduledRetryAsync(contextId);

        result.IsRight.ShouldBeTrue();
    }
}
