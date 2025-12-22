using System.Diagnostics;
using FluentAssertions;
using NSubstitute;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.Enrichers;
using Xunit;

namespace Encina.OpenTelemetry.Tests.Guards;

/// <summary>
/// Guard clause tests for <see cref="MessagingActivityEnricher"/>.
/// </summary>
public sealed class MessagingActivityEnricherGuardTests
{
    [Fact]
    public void EnrichWithOutboxMessage_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var message = Substitute.For<IOutboxMessage>();

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithOutboxMessage(null, message);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithOutboxMessage_WithNullMessage_ShouldNotThrow()
    {
        // Arrange
        using var activity = new Activity("test");

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithOutboxMessage(activity, null!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithInboxMessage_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var message = Substitute.For<IInboxMessage>();

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithInboxMessage(null, message);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithInboxMessage_WithNullMessage_ShouldNotThrow()
    {
        // Arrange
        using var activity = new Activity("test");

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithInboxMessage(activity, null!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithSagaState_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var sagaState = Substitute.For<ISagaState>();

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithSagaState(null, sagaState);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithSagaState_WithNullSagaState_ShouldNotThrow()
    {
        // Arrange
        using var activity = new Activity("test");

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithSagaState(activity, null!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithScheduledMessage_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var message = Substitute.For<IScheduledMessage>();

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithScheduledMessage(null, message);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithScheduledMessage_WithNullMessage_ShouldNotThrow()
    {
        // Arrange
        using var activity = new Activity("test");

        // Act
        var act = () => MessagingActivityEnricher.EnrichWithScheduledMessage(activity, null!);

        // Assert
        act.Should().NotThrow();
    }
}
