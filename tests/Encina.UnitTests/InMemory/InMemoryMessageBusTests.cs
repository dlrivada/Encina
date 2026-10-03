using Encina.Diagnostics;
using Encina.InMemory;
using Encina.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.InMemory;

/// <summary>
/// Unit tests for <see cref="InMemoryMessageBus"/>: synchronous publishing, queued processing by the
/// background workers and the failure paths. Queued processing is awaited through signals raised by
/// the handlers or the logger, never through elapsed time.
/// </summary>
public sealed class InMemoryMessageBusTests
{
    private const string SentinelMessage = "sentinel-secret-message";
    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PublishAsync_InvokesEverySubscriberOfTheMessageType()
    {
        // Arrange
        using var bus = CreateBus(new FakeLogger<InMemoryMessageBus>());
        var received = new List<string>();
        bus.Subscribe<TestMessage>(m => { received.Add("a:" + m.Text); return ValueTask.CompletedTask; });
        bus.Subscribe<TestMessage>(m => { received.Add("b:" + m.Text); return ValueTask.CompletedTask; });

        // Act
        var result = await bus.PublishAsync(new TestMessage("hi"));

        // Assert
        result.ShouldBeSuccess();
        received.ShouldBe(["a:hi", "b:hi"]);
        bus.SubscriberCount.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAsync_WithoutSubscribers_Succeeds()
    {
        // Arrange
        using var bus = CreateBus(new FakeLogger<InMemoryMessageBus>());

        // Act
        var result = await bus.PublishAsync(new TestMessage("hi"));

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishAsync_WhenAHandlerThrows_ReturnsPublishFailedAndLogsARedactedException()
    {
        // Arrange
        var logger = new FakeLogger<InMemoryMessageBus>();
        using var bus = CreateBus(logger);
        bus.Subscribe<TestMessage>(_ => throw new InvalidOperationException(SentinelMessage));

        // Act
        var result = await bus.PublishAsync(new TestMessage("hi"));

        // Assert
        result.ShouldBeErrorWithCode("INMEMORY_PUBLISH_FAILED");
        AssertSingleRedactedLog(logger);
    }

    [Fact]
    public async Task Subscribe_DisposingTheSubscription_StopsDelivery()
    {
        // Arrange
        using var bus = CreateBus(new FakeLogger<InMemoryMessageBus>());
        var calls = 0;
        var subscription = bus.Subscribe<TestMessage>(_ => { calls++; return ValueTask.CompletedTask; });

        // Act
        subscription.Dispose();
        await bus.PublishAsync(new TestMessage("hi"));

        // Assert
        calls.ShouldBe(0);
        bus.SubscriberCount.ShouldBe(0);
    }

    [Fact]
    public async Task EnqueueAsync_DeliversTheMessageToSubscribersOnAWorker()
    {
        // Arrange
        using var bus = CreateBus(new FakeLogger<InMemoryMessageBus>());
        var delivered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        bus.Subscribe<TestMessage>(m => { delivered.TrySetResult(m.Text); return ValueTask.CompletedTask; });

        // Act
        var result = await bus.EnqueueAsync(new TestMessage("queued"));

        // Assert
        result.ShouldBeSuccess();
        (await delivered.Task.WaitAsync(SafetyTimeout)).ShouldBe("queued");
    }

    [Fact]
    public async Task EnqueueAsync_WithoutSubscribers_DropsTheMessageAndKeepsWorking()
    {
        // Arrange
        using var bus = CreateBus(new FakeLogger<InMemoryMessageBus>());
        var delivered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act: the first message has no subscriber, the second one does
        await bus.EnqueueAsync(new OtherMessage());
        bus.Subscribe<TestMessage>(m => { delivered.TrySetResult(m.Text); return ValueTask.CompletedTask; });
        await bus.EnqueueAsync(new TestMessage("after"));

        // Assert
        (await delivered.Task.WaitAsync(SafetyTimeout)).ShouldBe("after");
    }

    [Fact]
    public async Task EnqueueAsync_WhenAQueuedHandlerThrows_LogsARedactedExceptionAndKeepsProcessing()
    {
        // Arrange
        var logged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var collector = FakeLogCollector.Create(new FakeLogCollectorOptions
        {
            OutputSink = text => { if (text.Contains("Error", StringComparison.OrdinalIgnoreCase)) { logged.TrySetResult(); } }
        });
        var logger = new FakeLogger<InMemoryMessageBus>(collector);
        using var bus = CreateBus(logger);
        var delivered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        bus.Subscribe<TestMessage>(_ => throw new InvalidOperationException(SentinelMessage));
        bus.Subscribe<TestMessage>(m => { delivered.TrySetResult(m.Text); return ValueTask.CompletedTask; });

        // Act
        await bus.EnqueueAsync(new TestMessage("queued"));
        await logged.Task.WaitAsync(SafetyTimeout);
        await delivered.Task.WaitAsync(SafetyTimeout);

        // Assert: the failing handler is logged redacted and the next handler still ran
        var entry = collector.GetSnapshot().First(r => r.Level >= LogLevel.Error);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    [Fact]
    public async Task EnqueueAsync_AfterDispose_ReturnsEnqueueFailedAndLogsARedactedException()
    {
        // Arrange
        var logger = new FakeLogger<InMemoryMessageBus>();
        var bus = CreateBus(logger);
        bus.Dispose();

        // Act
        var result = await bus.EnqueueAsync(new TestMessage("late"));

        // Assert
        result.ShouldBeErrorWithCode("INMEMORY_ENQUEUE_FAILED");
        logger.Collector.GetSnapshot().Any(r => r.Exception is RedactedException).ShouldBeTrue();
    }

    private static void AssertSingleRedactedLog(FakeLogger<InMemoryMessageBus> logger)
    {
        var entry = logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    private static InMemoryMessageBus CreateBus(FakeLogger<InMemoryMessageBus> logger) =>
        new(logger, Options.Create(new EncinaInMemoryOptions { WorkerCount = 1, UseUnboundedChannel = true }));

    private sealed record TestMessage(string Text);

    private sealed record OtherMessage;
}
