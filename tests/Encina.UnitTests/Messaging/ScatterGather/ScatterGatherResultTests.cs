using Encina.Messaging.ScatterGather;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Messaging.ScatterGather;

/// <summary>
/// Unit tests for <see cref="ScatterGatherResult{TResponse}"/>.
/// </summary>
public sealed class ScatterGatherResultTests
{
    private static readonly DateTime TestStartTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TestEndTime = new(2026, 1, 1, 12, 0, 1, DateTimeKind.Utc);
    private static readonly TimeSpan TestDuration = TimeSpan.FromSeconds(1);

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var operationId = Guid.NewGuid();
        var response = "aggregated result";
        var scatterResults = CreateMixedScatterResults();
        var strategy = GatherStrategy.WaitForAll;
        var completedAt = DateTime.UtcNow;

        // Act
        var result = new ScatterGatherResult<string>(
            operationId,
            response,
            scatterResults,
            strategy,
            TestDuration,
            cancelledCount: 0,
            completedAt);

        // Assert
        result.OperationId.ShouldBe(operationId);
        result.Response.ShouldBe(response);
        result.ScatterResults.ShouldBe(scatterResults);
        result.Strategy.ShouldBe(strategy);
        result.TotalDuration.ShouldBe(TestDuration);
        result.CompletedAtUtc.ShouldBe(completedAt);
    }

    #endregion

    #region Count Properties Tests

    [Fact]
    public void ScatterCount_ReturnsCorrectCount()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1),
            CreateSuccessResult(2),
            CreateFailureResult()
        };

        // Act
        var result = CreateResult(scatterResults);

        // Assert
        result.ScatterCount.ShouldBe(3);
    }

    [Fact]
    public void SuccessCount_ReturnsCorrectCount()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1),
            CreateSuccessResult(2),
            CreateFailureResult()
        };

        // Act
        var result = CreateResult(scatterResults);

        // Assert
        result.SuccessCount.ShouldBe(2);
    }

    [Fact]
    public void FailureCount_ReturnsCorrectCount()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1),
            CreateFailureResult(),
            CreateFailureResult()
        };

        // Act
        var result = CreateResult(scatterResults);

        // Assert
        result.FailureCount.ShouldBe(2);
    }

    [Fact]
    public void CancelledCount_ReturnsProvidedValue()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1)
        };

        // Act
        var result = new ScatterGatherResult<int>(
            Guid.NewGuid(),
            100,
            scatterResults,
            GatherStrategy.WaitForFirst,
            TestDuration,
            cancelledCount: 5,
            DateTime.UtcNow);

        // Assert
        result.CancelledCount.ShouldBe(5);
    }

    #endregion

    #region Boolean Properties Tests

    [Fact]
    public void AllSucceeded_WhenAllSuccess_ReturnsTrue()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1),
            CreateSuccessResult(2),
            CreateSuccessResult(3)
        };

        // Act
        var result = CreateResult(scatterResults, cancelledCount: 0);

        // Assert
        result.AllSucceeded.ShouldBeTrue();
    }

    [Fact]
    public void AllSucceeded_WhenHasFailures_ReturnsFalse()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1),
            CreateFailureResult()
        };

        // Act
        var result = CreateResult(scatterResults);

        // Assert
        result.AllSucceeded.ShouldBeFalse();
    }

    [Fact]
    public void AllSucceeded_WhenHasCancelled_ReturnsFalse()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1)
        };

        // Act
        var result = CreateResult(scatterResults, cancelledCount: 1);

        // Assert
        result.AllSucceeded.ShouldBeFalse();
    }

    [Fact]
    public void HasPartialFailures_WhenNoFailures_ReturnsFalse()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1),
            CreateSuccessResult(2)
        };

        // Act
        var result = CreateResult(scatterResults);

        // Assert
        result.HasPartialFailures.ShouldBeFalse();
    }

    [Fact]
    public void HasPartialFailures_WhenHasFailures_ReturnsTrue()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(1),
            CreateFailureResult()
        };

        // Act
        var result = CreateResult(scatterResults);

        // Assert
        result.HasPartialFailures.ShouldBeTrue();
    }

    #endregion

    #region Response Enumeration Tests

    [Fact]
    public void SuccessfulResponses_ReturnsOnlySuccessful()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(10),
            CreateFailureResult(),
            CreateSuccessResult(20),
            CreateFailureResult(),
            CreateSuccessResult(30)
        };

        // Act
        var result = CreateResult(scatterResults);
        var successfulResponses = result.SuccessfulResponses.ToList();

        // Assert
        successfulResponses.Count.ShouldBe(3);
        successfulResponses.ShouldContain(10);
        successfulResponses.ShouldContain(20);
        successfulResponses.ShouldContain(30);
    }

    [Fact]
    public void SuccessfulResponses_WhenEmpty_ReturnsEmpty()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateFailureResult(),
            CreateFailureResult()
        };

        // Act
        var result = CreateResult(scatterResults);

        // Assert
        result.SuccessfulResponses.ShouldBeEmpty();
    }

    [Fact]
    public void Errors_ReturnsOnlyErrors()
    {
        // Arrange
        var error1 = EncinaErrors.Create("err1", "Error 1");
        var error2 = EncinaErrors.Create("err2", "Error 2");

        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(10),
            CreateFailureResult(error1),
            CreateSuccessResult(20),
            CreateFailureResult(error2)
        };

        // Act
        var result = CreateResult(scatterResults);
        var errors = result.Errors.ToList();

        // Assert
        errors.Count.ShouldBe(2);
        errors.ShouldContain(error1);
        errors.ShouldContain(error2);
    }

    [Fact]
    public void Errors_WhenNoErrors_ReturnsEmpty()
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>>
        {
            CreateSuccessResult(10),
            CreateSuccessResult(20)
        };

        // Act
        var result = CreateResult(scatterResults);

        // Assert
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Strategy Tests

    [Theory]
    [InlineData(GatherStrategy.WaitForAll)]
    [InlineData(GatherStrategy.WaitForFirst)]
    [InlineData(GatherStrategy.WaitForQuorum)]
    public void Strategy_PreservesValue(GatherStrategy strategy)
    {
        // Arrange
        var scatterResults = new List<ScatterExecutionResult<int>> { CreateSuccessResult(1) };

        // Act
        var result = new ScatterGatherResult<int>(
            Guid.NewGuid(),
            100,
            scatterResults,
            strategy,
            TestDuration,
            0,
            DateTime.UtcNow);

        // Assert
        result.Strategy.ShouldBe(strategy);
    }

    #endregion

    #region Helper Methods

    private static ScatterExecutionResult<int> CreateSuccessResult(int value)
    {
        return ScatterExecutionResult.Success($"Handler{value}", value, TestDuration, TestStartTime, TestEndTime);
    }

    private static ScatterExecutionResult<int> CreateFailureResult(EncinaError? error = null)
    {
        var errorToUse = error ?? EncinaErrors.Create("test.error", "Test error");
        return ScatterExecutionResult.Failure<int>("FailedHandler", errorToUse, TestDuration, TestStartTime, TestEndTime);
    }

    private static ScatterGatherResult<int> CreateResult(
        IReadOnlyList<ScatterExecutionResult<int>> scatterResults,
        int cancelledCount = 0)
    {
        return new ScatterGatherResult<int>(
            Guid.NewGuid(),
            scatterResults.Where(r => r.IsSuccess).Count(),
            scatterResults,
            GatherStrategy.WaitForAll,
            TestDuration,
            cancelledCount,
            DateTime.UtcNow);
    }

    private static List<ScatterExecutionResult<string>> CreateMixedScatterResults()
    {
        return
        [
            ScatterExecutionResult.Success("Handler1", "result1", TestDuration, TestStartTime, TestEndTime),
            ScatterExecutionResult.Failure<string>("Handler2", EncinaErrors.Create("err", "Error"), TestDuration, TestStartTime, TestEndTime)
        ];
    }

    #endregion
}
