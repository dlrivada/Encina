using System.Diagnostics;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.Enrichers;
using Shouldly;
using NSubstitute;
using Xunit;

namespace Encina.OpenTelemetry.Tests.Contracts;

/// <summary>
/// Contract tests for <see cref="MessagingActivityEnricher"/> to verify
/// compliance with OpenTelemetry semantic conventions for messaging.
/// </summary>
/// <remarks>
/// These tests verify adherence to OpenTelemetry Semantic Conventions v1.28.0+
/// for messaging systems: https://opentelemetry.io/docs/specs/semconv/messaging/
/// </remarks>
public sealed class MessagingActivityEnricherContractTests
{
    private static (ActivitySource Source, ActivityListener Listener) CreateActivityContext()
    {
        var source = new ActivitySource($"Test-{Guid.NewGuid()}");
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return (source, listener);
    }

    [Fact]
    public void EnrichWithOutboxMessage_ShouldFollowMessagingSemanticConventions()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var _ = listener;
        using var __ = source;
        using var activity = source.StartActivity("TestActivity");

        var message = Substitute.For<IOutboxMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.NotificationType.Returns("OrderCreated");
        message.CreatedAtUtc.Returns(DateTime.UtcNow);
        message.IsProcessed.Returns(false);

        // Act
        MessagingActivityEnricher.EnrichWithOutboxMessage(activity, message);

        // Assert - Verify OpenTelemetry semantic conventions
        activity!.Tags.ShouldContain(tag => tag.Key == "messaging.system", "messaging.system is required per semantic conventions");
        activity.Tags.ShouldContain(tag => tag.Key == "messaging.message.id", "messaging.message.id identifies the message");
        activity.Tags.ShouldContain(tag => tag.Key == "messaging.operation.name" && tag.Value == "publish");
    }

    [Fact]
    public void EnrichWithInboxMessage_ShouldFollowMessagingSemanticConventions()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        var message = Substitute.For<IInboxMessage>();
        message.MessageId.Returns(Guid.NewGuid().ToString());
        message.RequestType.Returns("PaymentProcessed");
        message.ReceivedAtUtc.Returns(DateTime.UtcNow);
        message.ProcessedAtUtc.Returns(DateTime.UtcNow.AddSeconds(1));
        message.IsProcessed.Returns(true);

        // Act
        MessagingActivityEnricher.EnrichWithInboxMessage(activity, message);

        // Assert - Verify OpenTelemetry semantic conventions
        activity!.Tags.ShouldContain(tag => tag.Key == "messaging.system", "messaging.system is required per semantic conventions");
        activity.Tags.ShouldContain(tag => tag.Key == "messaging.message.id", "messaging.message.id identifies the message");
        activity.Tags.ShouldContain(tag => tag.Key == "messaging.operation.name" && tag.Value == "receive");
    }

    [Fact]
    public void EnrichWithSagaState_ShouldAddSagaContextTags()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        var sagaState = Substitute.For<ISagaState>();
        sagaState.SagaId.Returns(Guid.NewGuid());
        sagaState.SagaType.Returns("OrderFulfillmentSaga");
        sagaState.Status.Returns("Running");
        sagaState.CurrentStep.Returns(2);
        sagaState.StartedAtUtc.Returns(DateTime.UtcNow.AddMinutes(-5));

        // Act
        MessagingActivityEnricher.EnrichWithSagaState(activity, sagaState);

        // Assert - Verify saga-specific tags
        activity!.Tags.ShouldContain(tag => tag.Key == "saga.id", "saga.id identifies the saga instance");
        activity.Tags.ShouldContain(tag => tag.Key == "saga.type", "saga.type specifies the saga class");
        activity.Tags.ShouldContain(tag => tag.Key == "saga.status", "saga.status tracks saga state");
        // Note: saga.current_step is always added by the enricher, even if CurrentStep is 0
    }

    [Fact]
    public void EnrichWithScheduledMessage_ShouldAddSchedulingContextTags()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        var message = Substitute.For<IScheduledMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.RequestType.Returns("SendReminderCommand");
        message.ScheduledAtUtc.Returns(DateTime.UtcNow.AddHours(24));
        message.CreatedAtUtc.Returns(DateTime.UtcNow);
        message.IsProcessed.Returns(false);
        message.IsRecurring.Returns(false);

        // Act
        MessagingActivityEnricher.EnrichWithScheduledMessage(activity, message);

        // Assert - Verify scheduling-specific tags
        activity!.Tags.ShouldContain(tag => tag.Key == "messaging.system", "messaging.system identifies the scheduling system");
        activity.Tags.ShouldContain(tag => tag.Key == "messaging.message.id", "messaging.message.id identifies the scheduled message");
        activity.Tags.ShouldContain(tag => tag.Key == "messaging.operation.name" && tag.Value == "schedule");
        activity.Tags.ShouldContain(tag => tag.Key == "messaging.message.scheduled_at", "scheduled_at specifies execution time");
    }

    [Fact]
    public void EnrichWithOutboxMessage_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var message = Substitute.For<IOutboxMessage>();

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithOutboxMessage(null, message);

        // Assert
        Should.NotThrow(act, "enricher should handle null activity gracefully");
    }

    [Fact]
    public void EnrichWithOutboxMessage_WithNullMessage_ShouldNotThrow()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithOutboxMessage(activity, null!);

        // Assert
        Should.NotThrow(act, "enricher should handle null message gracefully");
    }

    [Fact]
    public void AllEnricherMethods_ShouldNotModifyActivityWhenBothNull()
    {
        // Act & Assert - All enricher methods should be null-safe
        var act1 = () => MessagingActivityEnricher.EnrichWithOutboxMessage(null, null!);
        var act2 = () => MessagingActivityEnricher.EnrichWithInboxMessage(null, null!);
        var act3 = () => MessagingActivityEnricher.EnrichWithSagaState(null, null!);
        var act4 = () => MessagingActivityEnricher.EnrichWithScheduledMessage(null, null!);

        Should.NotThrow(act1);
        Should.NotThrow(act2);
        Should.NotThrow(act3);
        Should.NotThrow(act4);
    }

    [Fact]
    public void EnrichWithOutboxMessage_ShouldUseConsistentTagNaming()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        var message = Substitute.For<IOutboxMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.NotificationType.Returns("TestNotification");
        message.IsProcessed.Returns(false);

        // Act
        MessagingActivityEnricher.EnrichWithOutboxMessage(activity, message);

        // Assert - Verify consistent naming conventions (messaging.* tags use dot-separated segments)
        foreach (var tag in activity!.Tags.Where(t => t.Key.StartsWith("messaging", StringComparison.Ordinal)))
        {
            tag.Key.ShouldMatch("^messaging\\.(system|message|operation)(\\.[a-z_]+)*$",
                "OpenTelemetry standard tags should follow semantic conventions with dot-separated segments (allowing underscores)");
        }
    }

    [Fact]
    public void EnrichWithSagaState_WhenCompletedAtUtcIsSet_ShouldIncludeCompletedTag()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        var sagaState = Substitute.For<ISagaState>();
        sagaState.SagaId.Returns(Guid.NewGuid());
        sagaState.Status.Returns("Completed");
        sagaState.CompletedAtUtc.Returns(DateTime.UtcNow);

        // Act
        MessagingActivityEnricher.EnrichWithSagaState(activity, sagaState);

        // Assert
        activity!.Tags.ShouldContain(tag => tag.Key == "saga.completed_at",
            "completed_at should be included when saga is completed");
    }

    [Fact]
    public void EnrichWithInboxMessage_WhenProcessedAtUtcIsSet_ShouldIncludeProcessedTag()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        var message = Substitute.For<IInboxMessage>();
        message.MessageId.Returns(Guid.NewGuid().ToString());
        message.ProcessedAtUtc.Returns(DateTime.UtcNow);
        message.IsProcessed.Returns(true);

        // Act
        MessagingActivityEnricher.EnrichWithInboxMessage(activity, message);

        // Assert
        activity!.Tags.ShouldContain(tag => tag.Key == "messaging.message.processed_at",
            "processed_at should be included when message is processed");
    }

    [Fact]
    public void AllEnrichMethods_ShouldSetTagValuesAsStrings()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        var outboxMessage = Substitute.For<IOutboxMessage>();
        outboxMessage.Id.Returns(Guid.NewGuid());
        outboxMessage.IsProcessed.Returns(false);

        // Act
        MessagingActivityEnricher.EnrichWithOutboxMessage(activity, outboxMessage);

        // Assert - OpenTelemetry requires tag values to be strings for compatibility
        foreach (var tag in activity!.Tags.Where(t => t.Key.StartsWith("messaging", StringComparison.Ordinal)))
        {
            tag.Value.ShouldBeOfType<string>($"tag '{tag.Key}' should have string value for OpenTelemetry compatibility");
        }
    }

    [Fact]
    public void EnrichWithScheduledMessage_WhenRecurring_ShouldIncludeCronExpression()
    {
        // Arrange
        var (source, listener) = CreateActivityContext();
        using var listenerDisposal = listener;
        using var sourceDisposal = source;
        using var activity = source.StartActivity("TestActivity");

        var message = Substitute.For<IScheduledMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.IsRecurring.Returns(true);
        message.CronExpression.Returns("0 0 * * *");
        message.IsProcessed.Returns(false);

        // Act
        MessagingActivityEnricher.EnrichWithScheduledMessage(activity, message);

        // Assert
        activity!.Tags.ShouldContain(tag => tag.Key == "messaging.message.cron_expression" && tag.Value == "0 0 * * *",
            "cron_expression should be included when message is recurring");
    }

}
