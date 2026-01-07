using Encina.Messaging.ScatterGather;
using LanguageExt;
using Shouldly;

namespace Encina.Messaging.Tests.ScatterGather;

/// <summary>
/// Unit tests for <see cref="ScatterExecutionResult"/> and <see cref="ScatterExecutionResult{TResponse}"/>.
/// </summary>
public sealed class ScatterExecutionResultTests
{
    private static readonly DateTime TestStartTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TestEndTime = new(2026, 1, 1, 12, 0, 0, 100, DateTimeKind.Utc);
    private static readonly TimeSpan TestDuration = TimeSpan.FromMilliseconds(100);

    #region Factory Method Tests

    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Arrange
        var response = "test response";

        // Act
        var result = ScatterExecutionResult.Success("Handler1", response, TestDuration, TestStartTime, TestEndTime);

        // Assert
        result.HandlerName.ShouldBe("Handler1");
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Duration.ShouldBe(TestDuration);
        result.StartedAtUtc.ShouldBe(TestStartTime);
        result.CompletedAtUtc.ShouldBe(TestEndTime);

        result.Result.Match(
            Right: r => r.ShouldBe(response),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");

        // Act
        var result = ScatterExecutionResult.Failure<string>("Handler1", error, TestDuration, TestStartTime, TestEndTime);

        // Assert
        result.HandlerName.ShouldBe("Handler1");
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Duration.ShouldBe(TestDuration);

        result.Result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.ShouldBe(error));
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithRightEither_CreatesSuccessfulResult()
    {
        // Arrange
        Either<EncinaError, int> either = 42;

        // Act
        var result = new ScatterExecutionResult<int>("TestHandler", either, TestDuration, TestStartTime, TestEndTime);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithLeftEither_CreatesFailedResult()
    {
        // Arrange
        var error = EncinaErrors.Create("err", "Error");
        Either<EncinaError, int> either = error;

        // Act
        var result = new ScatterExecutionResult<int>("TestHandler", either, TestDuration, TestStartTime, TestEndTime);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void HandlerName_IsPreserved()
    {
        // Arrange
        var handlerName = "MySpecialHandler";

        // Act
        var result = ScatterExecutionResult.Success(handlerName, "response", TestDuration, TestStartTime, TestEndTime);

        // Assert
        result.HandlerName.ShouldBe(handlerName);
    }

    [Fact]
    public void Duration_IsPreserved()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var result = ScatterExecutionResult.Success("Handler", "response", duration, TestStartTime, TestEndTime);

        // Assert
        result.Duration.ShouldBe(duration);
    }

    [Fact]
    public void Timestamps_ArePreserved()
    {
        // Arrange
        var start = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 15, 10, 30, 5, DateTimeKind.Utc);

        // Act
        var result = ScatterExecutionResult.Success("Handler", "response", TimeSpan.FromSeconds(5), start, end);

        // Assert
        result.StartedAtUtc.ShouldBe(start);
        result.CompletedAtUtc.ShouldBe(end);
    }

    #endregion

    #region Type Tests

    [Fact]
    public void Success_WithValueType_Works()
    {
        // Act
        var result = ScatterExecutionResult.Success("Handler", 123, TestDuration, TestStartTime, TestEndTime);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Result.Match(
            Right: r => r.ShouldBe(123),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void Success_WithComplexType_Works()
    {
        // Arrange
        var response = new TestResponse { Value = "test", Count = 5 };

        // Act
        var result = ScatterExecutionResult.Success("Handler", response, TestDuration, TestStartTime, TestEndTime);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Result.Match(
            Right: r =>
            {
                r.Value.ShouldBe("test");
                r.Count.ShouldBe(5);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    private sealed class TestResponse
    {
        public string Value { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
