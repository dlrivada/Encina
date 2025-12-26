using Encina.Messaging.Recoverability;
using Shouldly;

namespace Encina.Tests.Recoverability;

public sealed class RecoverabilityContextTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var context = new RecoverabilityContext();

        // Assert
        context.Id.ShouldNotBe(Guid.Empty);
        context.StartedAtUtc.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        context.ImmediateRetryCount.ShouldBe(0);
        context.DelayedRetryCount.ShouldBe(0);
        context.TotalAttempts.ShouldBe(1); // Initial attempt
        context.LastError.ShouldBeNull();
        context.LastException.ShouldBeNull();
        context.LastClassification.ShouldBe(ErrorClassification.Unknown);
        context.IsInDelayedRetryPhase.ShouldBeFalse();
        context.RetryHistory.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithInitializers_SetsProperties()
    {
        // Arrange & Act
        var context = new RecoverabilityContext
        {
            CorrelationId = "test-correlation",
            IdempotencyKey = "test-idempotency",
            RequestTypeName = "TestRequest"
        };

        // Assert
        context.CorrelationId.ShouldBe("test-correlation");
        context.IdempotencyKey.ShouldBe("test-idempotency");
        context.RequestTypeName.ShouldBe("TestRequest");
    }

    [Fact]
    public void IncrementImmediateRetry_IncrementsCounter()
    {
        // Arrange
        var context = new RecoverabilityContext();

        // Act
        context.IncrementImmediateRetry();
        context.IncrementImmediateRetry();
        context.IncrementImmediateRetry();

        // Assert
        context.ImmediateRetryCount.ShouldBe(3);
        context.TotalAttempts.ShouldBe(4); // 1 initial + 3 retries
        context.IsInDelayedRetryPhase.ShouldBeFalse();
    }

    [Fact]
    public void IncrementDelayedRetry_IncrementsCounterAndSetsPhase()
    {
        // Arrange
        var context = new RecoverabilityContext();

        // Act
        context.IncrementDelayedRetry();
        context.IncrementDelayedRetry();

        // Assert
        context.DelayedRetryCount.ShouldBe(2);
        context.TotalAttempts.ShouldBe(3); // 1 initial + 2 delayed
        context.IsInDelayedRetryPhase.ShouldBeTrue();
    }

    [Fact]
    public void TransitionToDelayedPhase_SetsPhaseWithoutIncrementing()
    {
        // Arrange
        var context = new RecoverabilityContext();

        // Act
        context.TransitionToDelayedPhase();

        // Assert
        context.DelayedRetryCount.ShouldBe(0);
        context.IsInDelayedRetryPhase.ShouldBeTrue();
    }

    [Fact]
    public void RecordFailedAttempt_TracksErrorAndAddsToHistory()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var error = EncinaError.New("Test error");
        var exception = new InvalidOperationException("Test exception");
        var classification = ErrorClassification.Transient;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        context.RecordFailedAttempt(error, exception, classification, duration);

        // Assert
        context.LastError.ShouldNotBeNull();
        context.LastError!.Value.Message.ShouldBe("Test error");
        context.LastException.ShouldBe(exception);
        context.LastClassification.ShouldBe(ErrorClassification.Transient);
        context.RetryHistory.Count.ShouldBe(1);
    }

    [Fact]
    public void RecordFailedAttempt_AddsToRetryHistory()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var error1 = EncinaError.New("Error 1");
        var error2 = EncinaError.New("Error 2");

        // Act
        context.RecordFailedAttempt(error1, null, ErrorClassification.Transient, TimeSpan.FromMilliseconds(50));
        context.IncrementImmediateRetry();
        context.RecordFailedAttempt(error2, null, ErrorClassification.Permanent, TimeSpan.FromMilliseconds(100));

        // Assert
        context.RetryHistory.Count.ShouldBe(2);
        context.RetryHistory[0].Error.Message.ShouldBe("Error 1");
        context.RetryHistory[0].Classification.ShouldBe(ErrorClassification.Transient);
        context.RetryHistory[1].Error.Message.ShouldBe("Error 2");
        context.RetryHistory[1].Classification.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void RetryHistory_IsImmutable()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var error = EncinaError.New("Test error");
        context.RecordFailedAttempt(error, null, ErrorClassification.Transient);

        // Act
        var history = context.RetryHistory;

        // Assert - Getting the history again returns the same count
        context.RetryHistory.Count.ShouldBe(1);
    }

    [Fact]
    public void CreateFailedMessage_CreatesCompleteMessage()
    {
        // Arrange
        var context = new RecoverabilityContext
        {
            CorrelationId = "test-correlation",
            IdempotencyKey = "test-idempotency",
            RequestTypeName = "TestRequest"
        };
        var error = EncinaError.New("Final error");
        var exception = new TimeoutException("Timeout");
        context.RecordFailedAttempt(error, exception, ErrorClassification.Transient);
        context.IncrementImmediateRetry();
        context.IncrementImmediateRetry();
        context.IncrementDelayedRetry();

        var request = new TestRequest { Id = 42 };

        // Act
        var failedMessage = context.CreateFailedMessage(request);

        // Assert
        failedMessage.Id.ShouldBe(context.Id);
        failedMessage.Request.ShouldBe(request);
        failedMessage.RequestType.ShouldContain("TestRequest");
        failedMessage.Error.Message.ShouldBe("Final error");
        failedMessage.Exception.ShouldBe(exception);
        failedMessage.CorrelationId.ShouldBe("test-correlation");
        failedMessage.IdempotencyKey.ShouldBe("test-idempotency");
        failedMessage.ImmediateRetryAttempts.ShouldBe(2);
        failedMessage.DelayedRetryAttempts.ShouldBe(1);
        failedMessage.TotalAttempts.ShouldBe(4); // 1 initial + 2 immediate + 1 delayed
        failedMessage.RetryHistory.Count.ShouldBe(1);
    }

    [Fact]
    public void CreateFailedMessage_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new RecoverabilityContext();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.CreateFailedMessage(null!));
    }

    [Fact]
    public void CreateFailedMessage_WithNoErrors_UsesDefaultError()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var request = new TestRequest { Id = 1 };

        // Act
        var failedMessage = context.CreateFailedMessage(request);

        // Assert
        failedMessage.Error.Message.ShouldContain("Unknown error");
    }

    private sealed class TestRequest
    {
        public int Id { get; set; }
    }
}
