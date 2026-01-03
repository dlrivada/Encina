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
/// Unit tests for <see cref="EncinaProperties"/>.
/// </summary>
public class EncinaPropertiesTests : PropertyTestBase
{
    #region Either Properties Tests

    [EncinaProperty]
    public Property EitherIsExclusive_ReturnsTrue_ForAllEithers(Either<EncinaError, int> either)
    {
        return EncinaProperties.EitherIsExclusive(either);
    }

    [EncinaProperty]
    public Property MapPreservesRightState_WorksCorrectly(Either<EncinaError, int> either)
    {
        return EncinaProperties.MapPreservesRightState(either, x => x * 2);
    }

    [EncinaProperty]
    public Property MapPreservesLeftError_WorksCorrectly(Either<EncinaError, int> either)
    {
        return EncinaProperties.MapPreservesLeftError(either, x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void BindToFailureProducesLeft_ReturnsTrue_ForRightValue()
    {
        // Arrange
        var either = Either<EncinaError, int>.Right(42);
        var error = EncinaError.New("Test error");

        // Act
        var result = EncinaProperties.BindToFailureProducesLeft<int, string>(either, error);

        // Assert
        result.QuickCheckThrowOnFailure();
    }

    #endregion

    #region EncinaError Properties Tests

    [EncinaProperty]
    public Property ErrorHasNonEmptyMessage_ReturnsTrue_ForAllErrors(EncinaError error)
    {
        return EncinaProperties.ErrorHasNonEmptyMessage(error);
    }

    [Property]
    public Property ErrorFromStringPreservesMessage_WorksCorrectly(NonEmptyString message)
    {
        return EncinaProperties.ErrorFromStringPreservesMessage(message);
    }

    #endregion

    #region RequestContext Properties Tests

    [EncinaProperty]
    public Property ContextHasCorrelationId_ReturnsTrue_ForAllContexts(IRequestContext context)
    {
        return EncinaProperties.ContextHasCorrelationId(context);
    }

    [Property]
    public Property WithMetadataIsImmutable_WorksCorrectly(NonEmptyString key)
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act & Assert
        return EncinaProperties.WithMetadataIsImmutable(context, key, 42);
    }

    [Property]
    public Property WithUserIdCreatesNewContext_WorksCorrectly(NonEmptyString userId)
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act & Assert
        return EncinaProperties.WithUserIdCreatesNewContext(context, userId);
    }

    #endregion

    #region Outbox Properties Tests

    [EncinaProperty]
    public Property OutboxProcessedStateIsConsistent_ReturnsTrue(IOutboxMessage message)
    {
        return EncinaProperties.OutboxProcessedStateIsConsistent(message);
    }

    [Property]
    public Property OutboxDeadLetterIsConsistent_WorksCorrectly(PositiveInt maxRetries)
    {
        // Arrange
        var message = new ArbitraryOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = maxRetries.Get + 1
        };

        // Act & Assert
        return EncinaProperties.OutboxDeadLetterIsConsistent(message, maxRetries);
    }

    [EncinaProperty]
    public Property OutboxHasRequiredFields_ReturnsTrue(IOutboxMessage message)
    {
        return EncinaProperties.OutboxHasRequiredFields(message);
    }

    #endregion

    #region Inbox Properties Tests

    [EncinaProperty]
    public Property InboxProcessedStateIsConsistent_ReturnsTrue(IInboxMessage message)
    {
        return EncinaProperties.InboxProcessedStateIsConsistent(message);
    }

    [EncinaProperty]
    public Property InboxHasRequiredFields_ReturnsTrue(IInboxMessage message)
    {
        return EncinaProperties.InboxHasRequiredFields(message);
    }

    #endregion

    #region Saga Properties Tests

    [EncinaProperty]
    public Property SagaStatusIsValid_ReturnsTrue(ISagaState state)
    {
        return EncinaProperties.SagaStatusIsValid(state);
    }

    [EncinaProperty]
    public Property SagaHasRequiredFields_ReturnsTrue(ISagaState state)
    {
        return EncinaProperties.SagaHasRequiredFields(state);
    }

    [EncinaProperty]
    public Property SagaCurrentStepIsNonNegative_ReturnsTrue(ISagaState state)
    {
        return EncinaProperties.SagaCurrentStepIsNonNegative(state);
    }

    #endregion

    #region ScheduledMessage Properties Tests

    [EncinaProperty]
    public Property RecurringHasCronExpression_WorksCorrectly(IScheduledMessage message)
    {
        return EncinaProperties.RecurringHasCronExpression(message);
    }

    [EncinaProperty]
    public Property ScheduledHasRequiredFields_ReturnsTrue(IScheduledMessage message)
    {
        return EncinaProperties.ScheduledHasRequiredFields(message);
    }

    #endregion

    #region Handler Properties Tests

    [Fact]
    public void HandlerIsDeterministic_ReturnsTrue_ForPureHandler()
    {
        // Arrange
        static Either<EncinaError, int> PureHandler(int request) => request * 2;

        // Act
        var property = EncinaProperties.HandlerIsDeterministic(PureHandler, 21);

        // Assert
        property.QuickCheckThrowOnFailure();
    }

    [Fact]
    public void AsyncHandlerIsDeterministic_ReturnsTrue_ForPureHandler()
    {
        // Arrange
        static Task<Either<EncinaError, int>> AsyncPureHandler(int request, CancellationToken _)
            => Task.FromResult(Either<EncinaError, int>.Right(request * 2));

        // Act
        var property = EncinaProperties.AsyncHandlerIsDeterministic(AsyncPureHandler, 21);

        // Assert
        property.QuickCheckThrowOnFailure();
    }

    #endregion
}
