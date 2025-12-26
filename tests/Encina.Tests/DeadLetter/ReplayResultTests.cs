using Encina.Messaging.DeadLetter;
using Shouldly;

namespace Encina.Tests.DeadLetter;

public sealed class ReplayResultTests
{
    [Fact]
    public void Succeeded_CreatesSuccessfulResult()
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
            DateTime.UtcNow.AddSeconds(-1),
            DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Failed_CreatesFailedResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var errorMessage = "Something went wrong";

        // Act
        var result = ReplayResult.Failed(messageId, errorMessage);

        // Assert
        result.MessageId.ShouldBe(messageId);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(errorMessage);
        result.ReplayedAtUtc.ShouldBeInRange(
            DateTime.UtcNow.AddSeconds(-1),
            DateTime.UtcNow.AddSeconds(1));
    }
}

public sealed class BatchReplayResultTests
{
    [Fact]
    public void AllSucceeded_ReturnsTrueWhenNoFailures()
    {
        // Arrange
        var results = new List<ReplayResult>
        {
            ReplayResult.Succeeded(Guid.NewGuid()),
            ReplayResult.Succeeded(Guid.NewGuid())
        };

        var batchResult = new BatchReplayResult
        {
            TotalProcessed = 2,
            SuccessCount = 2,
            FailureCount = 0,
            Results = results
        };

        // Assert
        batchResult.AllSucceeded.ShouldBeTrue();
    }

    [Fact]
    public void AllSucceeded_ReturnsFalseWhenHasFailures()
    {
        // Arrange
        var results = new List<ReplayResult>
        {
            ReplayResult.Succeeded(Guid.NewGuid()),
            ReplayResult.Failed(Guid.NewGuid(), "Error")
        };

        var batchResult = new BatchReplayResult
        {
            TotalProcessed = 2,
            SuccessCount = 1,
            FailureCount = 1,
            Results = results
        };

        // Assert
        batchResult.AllSucceeded.ShouldBeFalse();
    }

    [Fact]
    public void AllSucceeded_ReturnsFalseWhenNoProcessed()
    {
        // Arrange
        var batchResult = new BatchReplayResult
        {
            TotalProcessed = 0,
            SuccessCount = 0,
            FailureCount = 0,
            Results = []
        };

        // Assert
        batchResult.AllSucceeded.ShouldBeFalse();
    }
}

public sealed class DeadLetterStatisticsTests
{
    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Arrange
        var countBySource = new Dictionary<string, int>
        {
            ["Recoverability"] = 5,
            ["Outbox"] = 3
        };

        // Act
        var stats = new DeadLetterStatistics
        {
            TotalCount = 10,
            PendingCount = 8,
            ReplayedCount = 2,
            ExpiredCount = 1,
            CountBySource = countBySource,
            OldestPendingAtUtc = DateTime.UtcNow.AddDays(-7),
            NewestPendingAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        // Assert
        stats.TotalCount.ShouldBe(10);
        stats.PendingCount.ShouldBe(8);
        stats.ReplayedCount.ShouldBe(2);
        stats.ExpiredCount.ShouldBe(1);
        stats.CountBySource.ShouldContainKey("Recoverability");
        stats.CountBySource["Recoverability"].ShouldBe(5);
        stats.OldestPendingAtUtc.ShouldNotBeNull();
        stats.NewestPendingAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void NullableTimestamps_CanBeNull()
    {
        // Act
        var stats = new DeadLetterStatistics
        {
            TotalCount = 0,
            PendingCount = 0,
            ReplayedCount = 0,
            ExpiredCount = 0,
            CountBySource = new Dictionary<string, int>(),
            OldestPendingAtUtc = null,
            NewestPendingAtUtc = null
        };

        // Assert
        stats.OldestPendingAtUtc.ShouldBeNull();
        stats.NewestPendingAtUtc.ShouldBeNull();
    }
}
