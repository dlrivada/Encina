using Encina.Messaging.Recoverability;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// Unit tests for <see cref="RecoverabilityContext"/>.
/// </summary>
public sealed class RecoverabilityContextTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var context = new RecoverabilityContext();

        // Assert
        context.Id.ShouldNotBe(Guid.Empty);
        context.StartedAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
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
    public void Constructor_WithProperties_SetsCorrectly()
    {
        // Arrange & Act
        var context = new RecoverabilityContext
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456",
            RequestTypeName = "TestRequest"
        };

        // Assert
        context.CorrelationId.ShouldBe("corr-123");
        context.IdempotencyKey.ShouldBe("idem-456");
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

        // Assert
        context.ImmediateRetryCount.ShouldBe(2);
        context.TotalAttempts.ShouldBe(3); // 1 initial + 2 retries
        context.IsInDelayedRetryPhase.ShouldBeFalse();
    }

    [Fact]
    public void IncrementDelayedRetry_IncrementsCounterAndSetsPhase()
    {
        // Arrange
        var context = new RecoverabilityContext();

        // Act
        context.IncrementDelayedRetry();

        // Assert
        context.DelayedRetryCount.ShouldBe(1);
        context.IsInDelayedRetryPhase.ShouldBeTrue();
    }

    [Fact]
    public void TransitionToDelayedPhase_SetsPhaseWithoutIncrement()
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
    public void RecordFailedAttempt_AddsToHistory()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new InvalidOperationException("Test");

        // Act
        context.RecordFailedAttempt(error, exception, ErrorClassification.Transient, TimeSpan.FromMilliseconds(100));

        // Assert
        context.LastError.ShouldBe(error);
        context.LastException.ShouldBe(exception);
        context.LastClassification.ShouldBe(ErrorClassification.Transient);
        context.RetryHistory.Count.ShouldBe(1);

        var attempt = context.RetryHistory[0];
        attempt.AttemptNumber.ShouldBe(1);
        attempt.IsImmediate.ShouldBeTrue();
        attempt.Error.ShouldBe(error);
        attempt.Exception.ShouldBe(exception);
        attempt.Classification.ShouldBe(ErrorClassification.Transient);
        attempt.Duration.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void RecordFailedAttempt_InDelayedPhase_MarksAsNotImmediate()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var error = EncinaErrors.Create("test.error", "Test error");
        context.TransitionToDelayedPhase();

        // Act
        context.RecordFailedAttempt(error, null, ErrorClassification.Transient);

        // Assert
        context.RetryHistory[0].IsImmediate.ShouldBeFalse();
    }

    [Fact]
    public void RecordFailedAttempt_MultipleAttempts_TracksAll()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var error1 = EncinaErrors.Create("error1", "Error 1");
        var error2 = EncinaErrors.Create("error2", "Error 2");

        // Act
        context.RecordFailedAttempt(error1, null, ErrorClassification.Transient);
        context.IncrementImmediateRetry();
        context.RecordFailedAttempt(error2, null, ErrorClassification.Permanent);

        // Assert
        context.RetryHistory.Count.ShouldBe(2);
        context.RetryHistory[0].Error.ShouldBe(error1);
        context.RetryHistory[1].Error.ShouldBe(error2);
        context.LastError.ShouldBe(error2);
        context.LastClassification.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void CreateFailedMessage_ReturnsCorrectFailedMessage()
    {
        // Arrange
        var context = new RecoverabilityContext
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456"
        };

        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new InvalidOperationException("Test");

        context.RecordFailedAttempt(error, exception, ErrorClassification.Permanent);
        context.IncrementImmediateRetry();
        context.IncrementImmediateRetry();
        context.IncrementDelayedRetry();

        var request = new TestRequest { Value = "test" };

        // Act
        var failedMessage = context.CreateFailedMessage(request);

        // Assert
        failedMessage.Id.ShouldBe(context.Id);
        failedMessage.Request.ShouldBe(request);
        failedMessage.RequestType.ShouldContain("TestRequest");
        failedMessage.Error.ShouldBe(error);
        failedMessage.Exception.ShouldBe(exception);
        failedMessage.CorrelationId.ShouldBe("corr-123");
        failedMessage.IdempotencyKey.ShouldBe("idem-456");
        failedMessage.TotalAttempts.ShouldBe(4); // 1 initial + 2 immediate + 1 delayed
        failedMessage.ImmediateRetryAttempts.ShouldBe(2);
        failedMessage.DelayedRetryAttempts.ShouldBe(1);
        failedMessage.FirstAttemptAtUtc.ShouldBe(context.StartedAtUtc);
        failedMessage.FailedAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        failedMessage.RetryHistory.ShouldBe(context.RetryHistory);
    }

    [Fact]
    public void CreateFailedMessage_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new RecoverabilityContext();

        // Act
        var act = () => context.CreateFailedMessage(null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("request");
    }

    [Fact]
    public void CreateFailedMessage_NoError_UsesDefaultError()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var request = new TestRequest { Value = "test" };

        // Act
        var failedMessage = context.CreateFailedMessage(request);

        // Assert
        failedMessage.Error.Message.ShouldContain("Unknown error");
    }

    [Fact]
    public void TotalAttempts_CalculatesCorrectly()
    {
        // Arrange
        var context = new RecoverabilityContext();

        // Initial: 1 attempt
        context.TotalAttempts.ShouldBe(1);

        // After 3 immediate retries: 4 attempts
        context.IncrementImmediateRetry();
        context.IncrementImmediateRetry();
        context.IncrementImmediateRetry();
        context.TotalAttempts.ShouldBe(4);

        // After 2 delayed retries: 6 attempts
        context.IncrementDelayedRetry();
        context.IncrementDelayedRetry();
        context.TotalAttempts.ShouldBe(6);
    }

    [Fact]
    public void RetryHistory_IsThreadSafe()
    {
        // Arrange
        var context = new RecoverabilityContext();
        var error = EncinaErrors.Create("test.error", "Test error");

        // Act - Simulate concurrent access
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            context.RecordFailedAttempt(error, null, ErrorClassification.Transient);
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert - Should not throw and should have all entries
        context.RetryHistory.Count.ShouldBe(100);
    }

    private sealed class TestRequest
    {
        public string Value { get; set; } = string.Empty;
    }
}
