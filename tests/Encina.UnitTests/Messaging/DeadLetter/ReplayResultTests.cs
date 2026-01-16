using Encina.Messaging.DeadLetter;
using Shouldly;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Unit tests for <see cref="ReplayResult"/> and <see cref="BatchReplayResult"/>.
/// </summary>
public sealed class ReplayResultTests
{
    #region ReplayResult Tests

    [Fact]
    public void Succeeded_ReturnsSuccessfulResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        var result = ReplayResult.Succeeded(messageId);

        // Assert
        result.MessageId.ShouldBe(messageId);
        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
        result.ReplayedAtUtc.ShouldBeInRange(
            DateTime.UtcNow.AddSeconds(-5),
            DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void Failed_ReturnsFailedResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        const string errorMessage = "Something went wrong";

        // Act
        var result = ReplayResult.Failed(messageId, errorMessage);

        // Assert
        result.MessageId.ShouldBe(messageId);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(errorMessage);
        result.ReplayedAtUtc.ShouldBeInRange(
            DateTime.UtcNow.AddSeconds(-5),
            DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void ReplayResult_RequiredProperties_CanBeSet()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var replayedAt = DateTime.UtcNow;

        // Act
        var result = new ReplayResult
        {
            MessageId = messageId,
            Success = true,
            ReplayedAtUtc = replayedAt,
            ErrorMessage = null
        };

        // Assert
        result.MessageId.ShouldBe(messageId);
        result.Success.ShouldBeTrue();
        result.ReplayedAtUtc.ShouldBe(replayedAt);
        result.ErrorMessage.ShouldBeNull();
    }

    #endregion

    #region BatchReplayResult Tests

    [Fact]
    public void BatchReplayResult_AllSucceeded_ReturnsTrueWhenNoFailures()
    {
        // Arrange
        var results = new[]
        {
            ReplayResult.Succeeded(Guid.NewGuid()),
            ReplayResult.Succeeded(Guid.NewGuid()),
            ReplayResult.Succeeded(Guid.NewGuid())
        };

        // Act
        var batchResult = new BatchReplayResult
        {
            TotalProcessed = 3,
            SuccessCount = 3,
            FailureCount = 0,
            Results = results
        };

        // Assert
        batchResult.AllSucceeded.ShouldBeTrue();
    }

    [Fact]
    public void BatchReplayResult_AllSucceeded_ReturnsFalseWhenFailuresExist()
    {
        // Arrange
        var results = new[]
        {
            ReplayResult.Succeeded(Guid.NewGuid()),
            ReplayResult.Failed(Guid.NewGuid(), "Error"),
            ReplayResult.Succeeded(Guid.NewGuid())
        };

        // Act
        var batchResult = new BatchReplayResult
        {
            TotalProcessed = 3,
            SuccessCount = 2,
            FailureCount = 1,
            Results = results
        };

        // Assert
        batchResult.AllSucceeded.ShouldBeFalse();
    }

    [Fact]
    public void BatchReplayResult_AllSucceeded_ReturnsFalseWhenNoProcessed()
    {
        // Arrange & Act
        var batchResult = new BatchReplayResult
        {
            TotalProcessed = 0,
            SuccessCount = 0,
            FailureCount = 0,
            Results = Array.Empty<ReplayResult>()
        };

        // Assert
        batchResult.AllSucceeded.ShouldBeFalse();
    }

    [Fact]
    public void BatchReplayResult_RequiredProperties_CanBeSet()
    {
        // Arrange
        var results = new[]
        {
            ReplayResult.Succeeded(Guid.NewGuid())
        };

        // Act
        var batchResult = new BatchReplayResult
        {
            TotalProcessed = 10,
            SuccessCount = 8,
            FailureCount = 2,
            Results = results
        };

        // Assert
        batchResult.TotalProcessed.ShouldBe(10);
        batchResult.SuccessCount.ShouldBe(8);
        batchResult.FailureCount.ShouldBe(2);
        batchResult.Results.ShouldBe(results);
    }

    #endregion
}
