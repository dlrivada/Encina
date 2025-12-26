using Encina.Messaging.DeadLetter;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.Messaging.PropertyTests.DeadLetter;

/// <summary>
/// Property-based tests for Dead Letter Queue invariants.
/// </summary>
public sealed class DeadLetterPropertyTests
{
    #region DeadLetterFilter Properties

    /// <summary>
    /// DeadLetterFilter.All should always return an empty filter.
    /// </summary>
    [Fact]
    public void DeadLetterFilter_All_ReturnsEmptyFilter()
    {
        var filter = DeadLetterFilter.All;

        Assert.Null(filter.SourcePattern);
        Assert.Null(filter.RequestType);
        Assert.Null(filter.CorrelationId);
        Assert.Null(filter.DeadLetteredAfterUtc);
        Assert.Null(filter.DeadLetteredBeforeUtc);
        Assert.Null(filter.ExcludeReplayed);
    }

    /// <summary>
    /// DeadLetterFilter.FromSource should always preserve the source pattern.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DeadLetterFilter_FromSource_PreservesSourcePattern(NonEmptyString source)
    {
        var filter = DeadLetterFilter.FromSource(source.Get);
        return filter.SourcePattern == source.Get && filter.ExcludeReplayed == true;
    }

    /// <summary>
    /// DeadLetterFilter.Since should always set ExcludeReplayed to true.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DeadLetterFilter_Since_SetsExcludeReplayed(DateTime dateTime)
    {
        var utcDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        var filter = DeadLetterFilter.Since(utcDate);
        return filter.DeadLetteredAfterUtc == utcDate && filter.ExcludeReplayed == true;
    }

    /// <summary>
    /// DeadLetterFilter.ByCorrelationId should preserve the correlation ID.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DeadLetterFilter_ByCorrelationId_PreservesCorrelationId(NonEmptyString correlationId)
    {
        var filter = DeadLetterFilter.ByCorrelationId(correlationId.Get);
        return filter.CorrelationId == correlationId.Get;
    }

    /// <summary>
    /// DeadLetterFilter properties should be independently settable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DeadLetterFilter_PropertiesAreIndependent(
        NonEmptyString source,
        NonEmptyString requestType,
        NonEmptyString correlationId,
        bool excludeReplayed)
    {
        var filter = new DeadLetterFilter
        {
            SourcePattern = source.Get,
            RequestType = requestType.Get,
            CorrelationId = correlationId.Get,
            ExcludeReplayed = excludeReplayed
        };

        return filter.SourcePattern == source.Get
               && filter.RequestType == requestType.Get
               && filter.CorrelationId == correlationId.Get
               && filter.ExcludeReplayed == excludeReplayed;
    }

    #endregion

    #region DeadLetterOptions Properties

    /// <summary>
    /// DeadLetterOptions.RetentionPeriod should preserve value when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DeadLetterOptions_RetentionPeriod_PreservesValue(PositiveInt days)
    {
        var retention = TimeSpan.FromDays(days.Get);
        var options = new DeadLetterOptions { RetentionPeriod = retention };
        return options.RetentionPeriod == retention;
    }

    /// <summary>
    /// DeadLetterOptions.CleanupInterval should preserve value when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DeadLetterOptions_CleanupInterval_PreservesValue(PositiveInt minutes)
    {
        var interval = TimeSpan.FromMinutes(minutes.Get);
        var options = new DeadLetterOptions { CleanupInterval = interval };
        return options.CleanupInterval == interval;
    }

    /// <summary>
    /// DeadLetterOptions.EnableAutomaticCleanup should toggle correctly.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DeadLetterOptions_EnableAutomaticCleanup_TogglesCorrectly(bool enabled)
    {
        var options = new DeadLetterOptions { EnableAutomaticCleanup = enabled };
        return options.EnableAutomaticCleanup == enabled;
    }

    /// <summary>
    /// DeadLetterOptions integration flags should be independently settable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DeadLetterOptions_IntegrationFlags_AreIndependent(
        bool fromRecoverability,
        bool fromOutbox,
        bool fromInbox,
        bool fromScheduling,
        bool fromSagas)
    {
        var options = new DeadLetterOptions
        {
            IntegrateWithRecoverability = fromRecoverability,
            IntegrateWithOutbox = fromOutbox,
            IntegrateWithInbox = fromInbox,
            IntegrateWithScheduling = fromScheduling,
            IntegrateWithSagas = fromSagas
        };

        return options.IntegrateWithRecoverability == fromRecoverability
               && options.IntegrateWithOutbox == fromOutbox
               && options.IntegrateWithInbox == fromInbox
               && options.IntegrateWithScheduling == fromScheduling
               && options.IntegrateWithSagas == fromSagas;
    }

    #endregion

    #region ReplayResult Properties

    /// <summary>
    /// ReplayResult.Succeeded should always have Success = true and null ErrorMessage.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ReplayResult_Succeeded_AlwaysHasSuccessTrue(Guid messageId)
    {
        var result = ReplayResult.Succeeded(messageId);
        return result.Success && result.MessageId == messageId && result.ErrorMessage == null;
    }

    /// <summary>
    /// ReplayResult.Failed should always have Success = false and non-null ErrorMessage.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ReplayResult_Failed_AlwaysHasSuccessFalse(Guid messageId, NonEmptyString errorMessage)
    {
        var result = ReplayResult.Failed(messageId, errorMessage.Get);
        return !result.Success && result.MessageId == messageId && result.ErrorMessage != null;
    }

    /// <summary>
    /// ReplayResult should preserve the message ID.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ReplayResult_PreservesMessageId(Guid messageId, bool success)
    {
        var result = success
            ? ReplayResult.Succeeded(messageId)
            : ReplayResult.Failed(messageId, "test error");

        return result.MessageId == messageId;
    }

    /// <summary>
    /// ReplayResult should always have a ReplayedAtUtc timestamp.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ReplayResult_AlwaysHasReplayedAtUtc(Guid messageId, bool success)
    {
        var before = DateTime.UtcNow;
        var result = success
            ? ReplayResult.Succeeded(messageId)
            : ReplayResult.Failed(messageId, "test error");
        var after = DateTime.UtcNow;

        return result.ReplayedAtUtc >= before && result.ReplayedAtUtc <= after;
    }

    #endregion

    #region BatchReplayResult Properties

    /// <summary>
    /// BatchReplayResult.AllSucceeded should be true when no failures.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BatchReplayResult_AllSucceeded_TrueWhenNoFailures(PositiveInt successCount)
    {
        var count = Math.Min(successCount.Get, 100); // Limit for performance
        var results = Enumerable.Range(0, count)
            .Select(_ => ReplayResult.Succeeded(Guid.NewGuid()))
            .ToList();

        var batch = new BatchReplayResult
        {
            TotalProcessed = count,
            SuccessCount = count,
            FailureCount = 0,
            Results = results
        };

        return batch.AllSucceeded == (count > 0);
    }

    /// <summary>
    /// BatchReplayResult.AllSucceeded should be false when any failures exist.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BatchReplayResult_AllSucceeded_FalseWhenAnyFailure(PositiveInt successCount)
    {
        var count = Math.Min(successCount.Get, 100);
        var successes = Enumerable.Range(0, count)
            .Select(_ => ReplayResult.Succeeded(Guid.NewGuid()))
            .ToList();
        var failure = ReplayResult.Failed(Guid.NewGuid(), "test error");

        var results = successes.Append(failure).ToList();
        var batch = new BatchReplayResult
        {
            TotalProcessed = count + 1,
            SuccessCount = count,
            FailureCount = 1,
            Results = results
        };

        return !batch.AllSucceeded;
    }

    /// <summary>
    /// BatchReplayResult counts should be consistent.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BatchReplayResult_CountsAreConsistent(PositiveInt successCount, PositiveInt failCount)
    {
        var sCount = Math.Min(successCount.Get, 50);
        var fCount = Math.Min(failCount.Get, 50);

        var successes = Enumerable.Range(0, sCount)
            .Select(_ => ReplayResult.Succeeded(Guid.NewGuid()));
        var failures = Enumerable.Range(0, fCount)
            .Select(_ => ReplayResult.Failed(Guid.NewGuid(), "test error"));

        var results = successes.Concat(failures).ToList();
        var batch = new BatchReplayResult
        {
            TotalProcessed = sCount + fCount,
            SuccessCount = sCount,
            FailureCount = fCount,
            Results = results
        };

        return batch.TotalProcessed == sCount + fCount
               && batch.SuccessCount == sCount
               && batch.FailureCount == fCount;
    }

    #endregion

    #region DeadLetterStatistics Properties

    /// <summary>
    /// DeadLetterStatistics should preserve all counts.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DeadLetterStatistics_PreservesCounts(
        PositiveInt total,
        PositiveInt pending,
        PositiveInt replayed,
        PositiveInt expired)
    {
        var stats = new DeadLetterStatistics
        {
            TotalCount = total.Get,
            PendingCount = pending.Get,
            ReplayedCount = replayed.Get,
            ExpiredCount = expired.Get,
            CountBySource = new Dictionary<string, int>()
        };

        return stats.TotalCount == total.Get
               && stats.PendingCount == pending.Get
               && stats.ReplayedCount == replayed.Get
               && stats.ExpiredCount == expired.Get;
    }

    /// <summary>
    /// DeadLetterStatistics.CountBySource should preserve entries.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DeadLetterStatistics_CountBySource_PreservesEntries(PositiveInt count)
    {
        var countValue = Math.Min(count.Get, 100);
        var dictionary = new Dictionary<string, int>
        {
            [DeadLetterSourcePatterns.Recoverability] = countValue,
            [DeadLetterSourcePatterns.Outbox] = countValue * 2
        };

        var stats = new DeadLetterStatistics
        {
            TotalCount = 0,
            PendingCount = 0,
            ReplayedCount = 0,
            ExpiredCount = 0,
            CountBySource = dictionary
        };

        return stats.CountBySource[DeadLetterSourcePatterns.Recoverability] == countValue
               && stats.CountBySource[DeadLetterSourcePatterns.Outbox] == countValue * 2;
    }

    #endregion

    #region DeadLetterSourcePatterns Properties

    /// <summary>
    /// All source patterns should be non-empty strings.
    /// </summary>
    [Fact]
    public void DeadLetterSourcePatterns_AllPatternsAreNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(DeadLetterSourcePatterns.Recoverability));
        Assert.False(string.IsNullOrWhiteSpace(DeadLetterSourcePatterns.Outbox));
        Assert.False(string.IsNullOrWhiteSpace(DeadLetterSourcePatterns.Inbox));
        Assert.False(string.IsNullOrWhiteSpace(DeadLetterSourcePatterns.Scheduling));
        Assert.False(string.IsNullOrWhiteSpace(DeadLetterSourcePatterns.Saga));
        Assert.False(string.IsNullOrWhiteSpace(DeadLetterSourcePatterns.Choreography));
    }

    /// <summary>
    /// All source patterns should be unique.
    /// </summary>
    [Fact]
    public void DeadLetterSourcePatterns_AllPatternsAreUnique()
    {
        var patterns = new[]
        {
            DeadLetterSourcePatterns.Recoverability,
            DeadLetterSourcePatterns.Outbox,
            DeadLetterSourcePatterns.Inbox,
            DeadLetterSourcePatterns.Scheduling,
            DeadLetterSourcePatterns.Saga,
            DeadLetterSourcePatterns.Choreography
        };

        Assert.Equal(patterns.Length, patterns.Distinct().Count());
    }

    #endregion

    #region DeadLetterErrorCodes Properties

    /// <summary>
    /// All error codes should start with "dlq." prefix.
    /// </summary>
    [Fact]
    public void DeadLetterErrorCodes_AllCodesHaveDlqPrefix()
    {
        Assert.StartsWith("dlq.", DeadLetterErrorCodes.NotFound);
        Assert.StartsWith("dlq.", DeadLetterErrorCodes.AlreadyReplayed);
        Assert.StartsWith("dlq.", DeadLetterErrorCodes.Expired);
        Assert.StartsWith("dlq.", DeadLetterErrorCodes.ReplayFailed);
        Assert.StartsWith("dlq.", DeadLetterErrorCodes.DeserializationFailed);
        Assert.StartsWith("dlq.", DeadLetterErrorCodes.StoreFailed);
    }

    /// <summary>
    /// All error codes should be unique.
    /// </summary>
    [Fact]
    public void DeadLetterErrorCodes_AllCodesAreUnique()
    {
        var codes = new[]
        {
            DeadLetterErrorCodes.NotFound,
            DeadLetterErrorCodes.AlreadyReplayed,
            DeadLetterErrorCodes.Expired,
            DeadLetterErrorCodes.ReplayFailed,
            DeadLetterErrorCodes.DeserializationFailed,
            DeadLetterErrorCodes.StoreFailed
        };

        Assert.Equal(codes.Length, codes.Distinct().Count());
    }

    #endregion
}
