using System.Diagnostics;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.Enrichers;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="MessagingActivityEnricher"/> to verify null parameter handling.
/// </summary>
public sealed class MessagingActivityEnricherGuardTests : IDisposable
{
    private readonly ActivitySource _source;
    private readonly ActivityListener _listener;

    public MessagingActivityEnricherGuardTests()
    {
        _source = new ActivitySource("TestGuard.Messaging");
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "TestGuard.Messaging",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    [Fact]
    public void EnrichWithOutboxMessage_NullActivity_DoesNotThrow()
    {
        var message = Substitute.For<IOutboxMessage>();
        Should.NotThrow(() => MessagingActivityEnricher.EnrichWithOutboxMessage(null, message));
    }

    [Fact]
    public void EnrichWithOutboxMessage_NullMessage_DoesNotThrow()
    {
        using var activity = _source.StartActivity("test");
        Should.NotThrow(() => MessagingActivityEnricher.EnrichWithOutboxMessage(activity, null!));
    }

    [Fact]
    public void EnrichWithInboxMessage_NullActivity_DoesNotThrow()
    {
        var message = Substitute.For<IInboxMessage>();
        Should.NotThrow(() => MessagingActivityEnricher.EnrichWithInboxMessage(null, message));
    }

    [Fact]
    public void EnrichWithInboxMessage_NullMessage_DoesNotThrow()
    {
        using var activity = _source.StartActivity("test");
        Should.NotThrow(() => MessagingActivityEnricher.EnrichWithInboxMessage(activity, null!));
    }

    [Fact]
    public void EnrichWithSagaState_NullActivity_DoesNotThrow()
    {
        var saga = Substitute.For<ISagaState>();
        Should.NotThrow(() => MessagingActivityEnricher.EnrichWithSagaState(null, saga));
    }

    [Fact]
    public void EnrichWithSagaState_NullSagaState_DoesNotThrow()
    {
        using var activity = _source.StartActivity("test");
        Should.NotThrow(() => MessagingActivityEnricher.EnrichWithSagaState(activity, null!));
    }

    [Fact]
    public void EnrichWithScheduledMessage_NullActivity_DoesNotThrow()
    {
        var message = Substitute.For<IScheduledMessage>();
        Should.NotThrow(() => MessagingActivityEnricher.EnrichWithScheduledMessage(null, message));
    }

    [Fact]
    public void EnrichWithScheduledMessage_NullMessage_DoesNotThrow()
    {
        using var activity = _source.StartActivity("test");
        Should.NotThrow(() => MessagingActivityEnricher.EnrichWithScheduledMessage(activity, null!));
    }
}
