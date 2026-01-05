using Encina.Messaging.Inbox;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.Messaging.PropertyTests.Inbox;

/// <summary>
/// Property-based tests for Inbox Message invariants.
/// </summary>
public sealed class InboxMessageInvariantTests
{
    #region IInboxMessage IsProcessed Invariants

    /// <summary>
    /// IsProcessed should be true if and only if ProcessedAtUtc has a value.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsProcessed_TrueIfAndOnlyIfProcessedAtUtcHasValue(DateTime? processedAt)
    {
        var utcDate = processedAt.HasValue
            ? DateTime.SpecifyKind(processedAt.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var message = new TestInboxMessage
        {
            ProcessedAtUtc = utcDate
        };

        return message.IsProcessed == utcDate.HasValue;
    }

    /// <summary>
    /// Processed messages should always have IsProcessed = true.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsProcessed_AlwaysTrueWhenProcessedAtUtcIsSet(DateTime processedAt)
    {
        var utcDate = DateTime.SpecifyKind(processedAt, DateTimeKind.Utc);
        var message = new TestInboxMessage
        {
            ProcessedAtUtc = utcDate
        };

        return message.IsProcessed;
    }

    /// <summary>
    /// Unprocessed messages should always have IsProcessed = false.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsProcessed_AlwaysFalseWhenProcessedAtUtcIsNull(NonEmptyString messageId)
    {
        var message = new TestInboxMessage
        {
            MessageId = messageId.Get,
            ProcessedAtUtc = null
        };

        return !message.IsProcessed;
    }

    #endregion

    #region IInboxMessage IsExpired Invariants

    private static readonly DateTime ReferenceTime = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// IsExpired should be true when ExpiresAtUtc is in the past.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsExpired_TrueWhenExpiresAtUtcInPast(PositiveInt minutesAgo)
    {
        var expiresAt = ReferenceTime.AddMinutes(-minutesAgo.Get);
        var message = new TestInboxMessage
        {
            ExpiresAtUtc = expiresAt
        };

        return message.IsExpired(ReferenceTime);
    }

    /// <summary>
    /// IsExpired should be false when ExpiresAtUtc is in the future.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsExpired_FalseWhenExpiresAtUtcInFuture(PositiveInt minutesAhead)
    {
        var expiresAt = ReferenceTime.AddMinutes(minutesAhead.Get + 1); // +1 to avoid edge cases
        var message = new TestInboxMessage
        {
            ExpiresAtUtc = expiresAt
        };

        return !message.IsExpired(ReferenceTime);
    }

    /// <summary>
    /// IsExpired should be false when ExpiresAtUtc equals the current time exactly.
    /// This verifies the boundary condition: expiration uses greater-than (>) not greater-than-or-equal (>=).
    /// A message expires only AFTER its ExpiresAtUtc, not AT that exact moment.
    /// </summary>
    [Fact]
    public void IsExpired_FalseWhenExpiresAtUtcEqualsNow_BoundaryCondition()
    {
        // Arrange: ExpiresAtUtc exactly equals the reference time
        var message = new TestInboxMessage
        {
            ExpiresAtUtc = ReferenceTime
        };

        // Act & Assert: Message should NOT be expired when now == ExpiresAtUtc
        message.IsExpired(ReferenceTime).ShouldBeFalse(
            "Message should not be expired when current time exactly equals ExpiresAtUtc (uses > not >=)");
    }

    #endregion

    #region IInboxMessage Timestamp Invariants

    /// <summary>
    /// ReceivedAtUtc should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ReceivedAtUtc_IsPreserved(DateTime receivedAt)
    {
        var utcDate = DateTime.SpecifyKind(receivedAt, DateTimeKind.Utc);
        var message = new TestInboxMessage
        {
            ReceivedAtUtc = utcDate
        };

        return message.ReceivedAtUtc == utcDate;
    }

    /// <summary>
    /// ExpiresAtUtc should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExpiresAtUtc_IsPreserved(DateTime expiresAt)
    {
        var utcDate = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);
        var message = new TestInboxMessage
        {
            ExpiresAtUtc = utcDate
        };

        return message.ExpiresAtUtc == utcDate;
    }

    /// <summary>
    /// ProcessedAtUtc should be strictly after ReceivedAtUtc when both are set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ProcessedAtUtc_ShouldBeStrictlyAfterReceivedAtUtc_WhenBothSet(PositiveInt minutesAfter)
    {
        var receivedAt = ReferenceTime;
        var processedAt = receivedAt.AddMinutes(minutesAfter.Get);

        var message = new TestInboxMessage
        {
            ReceivedAtUtc = receivedAt,
            ProcessedAtUtc = processedAt
        };

        return message.ProcessedAtUtc.HasValue && message.ProcessedAtUtc.Value > message.ReceivedAtUtc;
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

        var message = new TestInboxMessage
        {
            NextRetryAtUtc = utcDate
        };

        return message.NextRetryAtUtc == utcDate;
    }

    #endregion

    #region IInboxMessage Content Invariants

    /// <summary>
    /// MessageId should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool MessageId_IsPreserved(NonEmptyString messageId)
    {
        var message = new TestInboxMessage
        {
            MessageId = messageId.Get
        };

        return message.MessageId == messageId.Get;
    }

    /// <summary>
    /// RequestType should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RequestType_IsPreserved(NonEmptyString requestType)
    {
        var message = new TestInboxMessage
        {
            RequestType = requestType.Get
        };

        return message.RequestType == requestType.Get;
    }

    /// <summary>
    /// Response should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Response_IsPreserved(string? response)
    {
        var message = new TestInboxMessage
        {
            Response = response
        };

        return message.Response == response;
    }

    /// <summary>
    /// ErrorMessage should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ErrorMessage_IsPreserved(string? errorMessage)
    {
        var message = new TestInboxMessage
        {
            ErrorMessage = errorMessage
        };

        return message.ErrorMessage == errorMessage;
    }

    #endregion

    #region IInboxMessage Retry Invariants

    /// <summary>
    /// RetryCount should reject negative values with ArgumentOutOfRangeException.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RetryCount_ShouldRejectNegativeValues(NegativeInt negativeCount)
    {
        var message = new TestInboxMessage();

        var exception = Record.Exception(() => message.RetryCount = negativeCount.Get);

        return exception is ArgumentOutOfRangeException;
    }

    /// <summary>
    /// RetryCount should accept non-negative values.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RetryCount_ShouldAcceptNonNegativeValues(NonNegativeInt retryCount)
    {
        var message = new TestInboxMessage
        {
            RetryCount = retryCount.Get
        };

        return message.RetryCount == retryCount.Get;
    }

    /// <summary>
    /// RetryCount should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RetryCount_IsPreserved(PositiveInt retryCount)
    {
        var message = new TestInboxMessage
        {
            RetryCount = retryCount.Get
        };

        return message.RetryCount == retryCount.Get;
    }

    #endregion

    #region InboxOptions Invariants

    /// <summary>
    /// InboxOptions.MaxRetries should preserve value when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool InboxOptions_MaxRetries_PreservesValue(PositiveInt maxRetries)
    {
        var options = new InboxOptions
        {
            MaxRetries = maxRetries.Get
        };

        return options.MaxRetries == maxRetries.Get;
    }

    /// <summary>
    /// InboxOptions.MessageRetentionPeriod should preserve value when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool InboxOptions_MessageRetentionPeriod_PreservesValue(PositiveInt days)
    {
        var retention = TimeSpan.FromDays(days.Get);
        var options = new InboxOptions
        {
            MessageRetentionPeriod = retention
        };

        return options.MessageRetentionPeriod == retention;
    }

    #endregion

    #region InboxMetadata Invariants

    /// <summary>
    /// InboxMetadata.CorrelationId should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool InboxMetadata_CorrelationId_PreservesValue(NonEmptyString correlationId)
    {
        var metadata = new InboxMetadata
        {
            CorrelationId = correlationId.Get
        };

        return metadata.CorrelationId == correlationId.Get;
    }

    /// <summary>
    /// InboxMetadata.UserId should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool InboxMetadata_UserId_PreservesValue(NonEmptyString userId)
    {
        var metadata = new InboxMetadata
        {
            UserId = userId.Get
        };

        return metadata.UserId == userId.Get;
    }

    /// <summary>
    /// InboxMetadata.TenantId should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool InboxMetadata_TenantId_PreservesValue(NonEmptyString tenantId)
    {
        var metadata = new InboxMetadata
        {
            TenantId = tenantId.Get
        };

        return metadata.TenantId == tenantId.Get;
    }

    /// <summary>
    /// InboxMetadata properties should be independently settable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool InboxMetadata_PropertiesAreIndependent(
        NonEmptyString correlationId,
        NonEmptyString userId,
        NonEmptyString tenantId)
    {
        var metadata = new InboxMetadata
        {
            CorrelationId = correlationId.Get,
            UserId = userId.Get,
            TenantId = tenantId.Get
        };

        return metadata.CorrelationId == correlationId.Get
               && metadata.UserId == userId.Get
               && metadata.TenantId == tenantId.Get;
    }

    #endregion

    #region Test Implementation

    private sealed class TestInboxMessage : IInboxMessage
    {
        private int _retryCount;

        public string MessageId { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ReceivedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

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
        public bool IsExpired() => IsExpired(DateTime.UtcNow);
        public bool IsExpired(DateTime now) => now > ExpiresAtUtc;
    }

    #endregion
}
