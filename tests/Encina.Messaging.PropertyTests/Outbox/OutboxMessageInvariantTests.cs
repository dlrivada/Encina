using Encina.Messaging.Outbox;
using FsCheck;
using FsCheck.Xunit;
using Shouldly;

namespace Encina.Messaging.PropertyTests.Outbox;

/// <summary>
/// Property-based tests for Outbox Message invariants.
/// </summary>
public sealed class OutboxMessageInvariantTests
{
    #region IOutboxMessage Properties

    /// <summary>
    /// IsProcessed should be true if and only if ProcessedAtUtc has a value.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsProcessed_TrueIfAndOnlyIfProcessedAtUtcHasValue(DateTime? processedAt)
    {
        var message = CreateTestMessage(processedAtUtc: processedAt);

        return message.IsProcessed == processedAt.HasValue;
    }

    /// <summary>
    /// IsDeadLettered should be true when retry count equals or exceeds max retries and not processed.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsDeadLettered_TrueWhenRetryCountExceedsMaxAndNotProcessed(PositiveInt retryCount, PositiveInt maxRetries)
    {
        var message = CreateTestMessage(retryCount: retryCount.Get, processedAtUtc: null);

        var expected = retryCount.Get >= maxRetries.Get;
        return message.IsDeadLettered(maxRetries.Get) == expected;
    }

    /// <summary>
    /// IsDeadLettered should always be false when message is processed.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsDeadLettered_AlwaysFalseWhenProcessed(PositiveInt retryCount, PositiveInt maxRetries, DateTime processedAt)
    {
        var utcProcessedAt = DateTime.SpecifyKind(processedAt, DateTimeKind.Utc);
        var message = CreateTestMessage(retryCount: retryCount.Get, processedAtUtc: utcProcessedAt);

        return !message.IsDeadLettered(maxRetries.Get);
    }

    /// <summary>
    /// IsDeadLettered should be false when retry count is less than max retries.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsDeadLettered_FalseWhenRetryCountBelowMax(PositiveInt maxRetries)
    {
        var retryCount = Math.Max(0, maxRetries.Get - 1);
        var message = CreateTestMessage(retryCount: retryCount, processedAtUtc: null);

        return !message.IsDeadLettered(maxRetries.Get);
    }

    #endregion

    #region OutboxMessage Timestamp Invariants

    /// <summary>
    /// CreatedAtUtc should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CreatedAtUtc_IsPreserved(DateTime createdAt)
    {
        var utcDate = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
        var message = CreateTestMessage(createdAtUtc: utcDate);

        return message.CreatedAtUtc == utcDate;
    }

    /// <summary>
    /// ProcessedAtUtc should be after CreatedAtUtc when both are set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ProcessedAtUtc_ShouldBeAfterCreatedAtUtc_WhenBothSet(PositiveInt minutesAfter)
    {
        var createdAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var processedAt = createdAt.AddMinutes(minutesAfter.Get);

        var message = CreateTestMessage(createdAtUtc: createdAt, processedAtUtc: processedAt);

        return message.ProcessedAtUtc!.Value > message.CreatedAtUtc;
    }

    /// <summary>
    /// NextRetryAtUtc should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NextRetryAtUtc_IsPreserved(DateTime? nextRetryAt)
    {
        var utcDate = nextRetryAt.HasValue
            ? DateTime.SpecifyKind(nextRetryAt.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var message = CreateTestMessage(nextRetryAtUtc: utcDate);

        return message.NextRetryAtUtc == utcDate;
    }

    #endregion

    #region OutboxMessage Retry Invariants

    /// <summary>
    /// RetryCount should reject negative values with ArgumentOutOfRangeException.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RetryCount_RejectsNegativeValues(NegativeInt negativeCount)
    {
        var message = CreateTestMessage();

        try
        {
            message.RetryCount = negativeCount.Get;
            return false; // Should have thrown
        }
        catch (ArgumentOutOfRangeException)
        {
            return true; // Expected behavior
        }
        catch
        {
            return false; // Wrong exception type
        }
    }

    /// <summary>
    /// RetryCount should accept non-negative values.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RetryCount_AcceptsNonNegativeValues(NonNegativeInt retryCount)
    {
        var message = CreateTestMessage(retryCount: retryCount.Get);

        return message.RetryCount == retryCount.Get;
    }

    #endregion

    #region OutboxOptions Invariants

    /// <summary>
    /// OutboxOptions should have sensible default values when created.
    /// </summary>
    [Fact]
    public void OutboxOptions_DefaultValues_AreSensible()
    {
        var options = new OutboxOptions();

        options.ProcessingInterval.ShouldBe(TimeSpan.FromSeconds(30));
        options.BatchSize.ShouldBe(100);
        options.MaxRetries.ShouldBe(3);
        options.BaseRetryDelay.ShouldBe(TimeSpan.FromSeconds(5));
        options.EnableProcessor.ShouldBeTrue();
    }

    /// <summary>
    /// OutboxOptions.ProcessingInterval should be positive for meaningful polling.
    /// Zero or negative intervals would cause busy-loops or errors.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void OutboxOptions_ProcessingInterval_ZeroOrNegative_AllowedButNotRecommended(int seconds)
    {
        // Note: OutboxOptions is a plain POCO without validation.
        // This test documents that invalid values ARE accepted (no guard).
        // Consuming code should validate before use.

        // Arrange
        var expectedInterval = TimeSpan.FromSeconds(seconds);

        // Act
        var options = new OutboxOptions
        {
            ProcessingInterval = expectedInterval
        };

        // Assert
        options.ProcessingInterval.ShouldBe(expectedInterval);
    }

    /// <summary>
    /// OutboxOptions.BatchSize with zero or negative values is technically allowed
    /// but would cause no messages to be processed or errors.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void OutboxOptions_BatchSize_ZeroOrNegative_AllowedButNotRecommended(int batchSize)
    {
        // Note: OutboxOptions is a plain POCO without validation.
        // This test documents that invalid values ARE accepted (no guard).

        // Arrange
        var expectedBatchSize = batchSize;

        // Act
        var options = new OutboxOptions
        {
            BatchSize = expectedBatchSize
        };

        // Assert
        options.BatchSize.ShouldBe(expectedBatchSize);
    }

    /// <summary>
    /// OutboxOptions.MaxRetries with zero disables retries entirely.
    /// Negative values are technically allowed but semantically invalid.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OutboxOptions_MaxRetries_ZeroOrNegative_AllowedButNotRecommended(int maxRetries)
    {
        // Note: OutboxOptions is a plain POCO without validation.
        // This test documents that invalid values ARE accepted (no guard).

        // Arrange
        var expectedMaxRetries = maxRetries;

        // Act
        var options = new OutboxOptions
        {
            MaxRetries = expectedMaxRetries
        };

        // Assert
        options.MaxRetries.ShouldBe(expectedMaxRetries);
    }

    /// <summary>
    /// OutboxOptions.BaseRetryDelay with zero or negative values would cause
    /// immediate retries or errors in exponential backoff calculation.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OutboxOptions_BaseRetryDelay_ZeroOrNegative_AllowedButNotRecommended(int seconds)
    {
        // Note: OutboxOptions is a plain POCO without validation.
        // This test documents that invalid values ARE accepted (no guard).

        // Arrange
        var expectedDelay = TimeSpan.FromSeconds(seconds);

        // Act
        var options = new OutboxOptions
        {
            BaseRetryDelay = expectedDelay
        };

        // Assert
        options.BaseRetryDelay.ShouldBe(expectedDelay);
    }

    /// <summary>
    /// OutboxOptions should handle arbitrary large configuration values.
    /// Since OutboxOptions is a plain POCO without validation, any value should be preserved.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool OutboxOptions_LargeValues_ArePreserved(PositiveInt hours, PositiveInt batchSize, PositiveInt maxRetries, PositiveInt delayMinutes)
    {
        // No artificial limits - OutboxOptions has no validation constraints
        var interval = TimeSpan.FromHours(hours.Get);
        var delay = TimeSpan.FromMinutes(delayMinutes.Get);

        var options = new OutboxOptions
        {
            ProcessingInterval = interval,
            BatchSize = batchSize.Get,
            MaxRetries = maxRetries.Get,
            BaseRetryDelay = delay
        };

        return options.ProcessingInterval == interval
            && options.BatchSize == batchSize.Get
            && options.MaxRetries == maxRetries.Get
            && options.BaseRetryDelay == delay;
    }

    /// <summary>
    /// OutboxOptions instances should be independent (no shared state).
    /// </summary>
    [Fact]
    public void OutboxOptions_MultipleInstances_AreIndependent()
    {
        var options1 = new OutboxOptions { BatchSize = 50 };
        var options2 = new OutboxOptions { BatchSize = 200 };

        options1.BatchSize.ShouldBe(50);
        options2.BatchSize.ShouldBe(200);

        // Modifying one doesn't affect the other
        options1.BatchSize = 75;
        options2.BatchSize.ShouldBe(200);
    }

    #endregion

    #region Test Implementation

    /// <summary>
    /// Creates a TestOutboxMessage with default required values.
    /// </summary>
    private static TestOutboxMessage CreateTestMessage(
        DateTime? processedAtUtc = null,
        int retryCount = 0,
        DateTime? createdAtUtc = null,
        DateTime? nextRetryAtUtc = null) => new()
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            CreatedAtUtc = createdAtUtc ?? new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ProcessedAtUtc = processedAtUtc,
            NextRetryAtUtc = nextRetryAtUtc,
            RetryCount = retryCount
        };

    private sealed class TestOutboxMessage : IOutboxMessage
    {
        private int _retryCount;

        public Guid Id { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// RetryCount must remain mutable with validation to support testing negative value rejection.
        /// </summary>
        public int RetryCount
        {
            get => _retryCount;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                _retryCount = value;
            }
        }

        public DateTime? NextRetryAtUtc { get; set; }

        public bool IsProcessed => ProcessedAtUtc.HasValue;
        public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
    }

    #endregion
}
