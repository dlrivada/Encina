using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Shouldly;

namespace Encina.Testing.FsCheck.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaArbitraries"/>.
/// </summary>
public class EncinaArbitrariesTests
{
    // FsCheck 3.x auto-discovers arbitraries via EncinaArbitraryProvider

    #region EncinaError Tests

    [Fact]
    public void EncinaError_GeneratesNonEmptyMessages()
    {
        // Arrange
        var arb = EncinaArbitraries.EncinaError();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(e => !string.IsNullOrEmpty(e.Message));
    }

    [Fact]
    public void EncinaErrorWithException_IncludesExceptionMetadata()
    {
        // Arrange
        var arb = EncinaArbitraries.EncinaErrorWithException();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(e => e.Exception.IsSome);
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public bool EncinaError_MessageIsNeverNull(EncinaError error)
    {
        return error.Message != null;
    }

    #endregion

    #region RequestContext Tests

    [Fact]
    public void RequestContext_GeneratesValidContexts()
    {
        // Arrange
        var arb = EncinaArbitraries.RequestContext();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(ctx => !string.IsNullOrEmpty(ctx.CorrelationId));
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public Property RequestContext_AlwaysHasCorrelationId()
    {
        return Prop.ForAll(EncinaArbitraries.RequestContext(), ctx =>
            !string.IsNullOrEmpty(ctx.CorrelationId));
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public Property RequestContext_HasValidTimestamp()
    {
        return Prop.ForAll(EncinaArbitraries.RequestContext(), ctx =>
            ctx.Timestamp != default);
    }

    #endregion

    #region Either Tests

    [Fact]
    public void EitherOf_GeneratesBothLeftAndRight()
    {
        // Arrange
        var arb = EncinaArbitraries.EitherOf<int>();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 100).ToList();

        // Assert
        samples.Any(e => e.IsLeft).ShouldBeTrue("Should generate at least one Left");
        samples.Any(e => e.IsRight).ShouldBeTrue("Should generate at least one Right");
    }

    [Fact]
    public void SuccessEither_GeneratesOnlyRight()
    {
        // Arrange
        var arb = EncinaArbitraries.SuccessEither<int>();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(e => e.IsRight);
    }

    [Fact]
    public void FailureEither_GeneratesOnlyLeft()
    {
        // Arrange
        var arb = EncinaArbitraries.FailureEither<int>();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(e => e.IsLeft);
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public Property Either_IsExclusivelyLeftOrRight()
    {
        return Prop.ForAll(EncinaArbitraries.EitherOf<int>(), either =>
            either.IsLeft ^ either.IsRight);
    }

    #endregion

    #region OutboxMessage Tests

    [Fact]
    public void OutboxMessage_GeneratesValidMessages()
    {
        // Arrange
        var arb = EncinaArbitraries.OutboxMessage();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(m => m.Id != Guid.Empty);
        samples.ShouldAllBe(m => !string.IsNullOrEmpty(m.NotificationType));
        samples.ShouldAllBe(m => !string.IsNullOrEmpty(m.Content));
    }

    [Fact]
    public void PendingOutboxMessage_GeneratesOnlyPendingMessages()
    {
        // Arrange
        var arb = EncinaArbitraries.PendingOutboxMessage();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(m => !m.IsProcessed);
        samples.ShouldAllBe(m => m.ProcessedAtUtc == null);
    }

    [Fact]
    public void FailedOutboxMessage_HasErrorInformation()
    {
        // Arrange
        var arb = EncinaArbitraries.FailedOutboxMessage();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(m => m.RetryCount > 0);
        samples.ShouldAllBe(m => !string.IsNullOrEmpty(m.ErrorMessage));
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public Property OutboxMessage_ProcessedStateIsConsistent()
    {
        return Prop.ForAll(EncinaArbitraries.OutboxMessage(), message =>
            message.IsProcessed == message.ProcessedAtUtc.HasValue);
    }

    #endregion

    #region InboxMessage Tests

    [Fact]
    public void InboxMessage_GeneratesValidMessages()
    {
        // Arrange
        var arb = EncinaArbitraries.InboxMessage();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(m => !string.IsNullOrEmpty(m.MessageId));
        samples.ShouldAllBe(m => !string.IsNullOrEmpty(m.RequestType));
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public Property InboxMessage_ProcessedStateIsConsistent()
    {
        return Prop.ForAll(EncinaArbitraries.InboxMessage(), message =>
            message.IsProcessed == message.ProcessedAtUtc.HasValue);
    }

    #endregion

    #region SagaState Tests

    [Fact]
    public void SagaState_GeneratesValidStates()
    {
        // Arrange
        var arb = EncinaArbitraries.SagaState();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(s => s.SagaId != Guid.Empty);
        samples.ShouldAllBe(s => !string.IsNullOrEmpty(s.SagaType));
        samples.ShouldAllBe(s => !string.IsNullOrEmpty(s.Status));
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public Property SagaState_HasValidStatus()
    {
        return Prop.ForAll(EncinaArbitraries.SagaState(), state =>
            EncinaArbitraries.SagaStatuses.Contains(state.Status));
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public Property SagaState_CurrentStepIsNonNegative()
    {
        return Prop.ForAll(EncinaArbitraries.SagaState(), state =>
            state.CurrentStep >= 0);
    }

    #endregion

    #region ScheduledMessage Tests

    [Fact]
    public void ScheduledMessage_GeneratesValidMessages()
    {
        // Arrange
        var arb = EncinaArbitraries.ScheduledMessage();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(m => m.Id != Guid.Empty);
        samples.ShouldAllBe(m => !string.IsNullOrEmpty(m.RequestType));
        samples.ShouldAllBe(m => !string.IsNullOrEmpty(m.Content));
    }

    [Fact]
    public void RecurringScheduledMessage_HasCronExpression()
    {
        // Arrange
        var arb = EncinaArbitraries.RecurringScheduledMessage();
        var gen = arb.Generator;

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(m => m.IsRecurring);
        samples.ShouldAllBe(m => !string.IsNullOrEmpty(m.CronExpression));
    }

    [Property(Arbitrary = [typeof(EncinaArbitraryProvider)])]
    public Property ScheduledMessage_ProcessedStateIsConsistent()
    {
        return Prop.ForAll(EncinaArbitraries.ScheduledMessage(), message =>
            message.IsProcessed == message.ProcessedAtUtc.HasValue);
    }

    #endregion

    #region Registration Tests

    [Fact]
    public void Register_CanGenerateEncinaTypes()
    {
        // Arrange & Act - Generate directly using the arbitraries
        var errorArb = EncinaArbitraries.EncinaError();
        var contextArb = EncinaArbitraries.RequestContext();

        var errorSamples = Gen.Sample(errorArb.Generator, 10, 10).ToList();
        var contextSamples = Gen.Sample(contextArb.Generator, 10, 10).ToList();

        // Assert
        errorSamples.ShouldNotBeEmpty();
        contextSamples.ShouldNotBeEmpty();
    }

    #endregion
}
