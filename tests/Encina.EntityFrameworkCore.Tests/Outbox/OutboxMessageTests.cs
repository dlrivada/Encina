using Encina.EntityFrameworkCore.Outbox;
using Shouldly;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxMessage"/> domain behavior.
/// </summary>
public class OutboxMessageTests
{
    #region IsProcessed Tests

    /// <summary>
    /// Verifies that IsProcessed ALWAYS reflects ProcessedAtUtc and ErrorMessage state correctly.
    /// </summary>
    [Theory]
    [MemberData(nameof(IsProcessedTestCases))]
    public void IsProcessed_ReflectsProcessedAtUtcAndErrorMessageState(
        DateTime? processedAt,
        string? errorMessage,
        bool expected)
    {
        // Arrange
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestIsProcessed",
            Content = "{}",
            CreatedAtUtc = baseTime,
            ProcessedAtUtc = processedAt,
            ErrorMessage = errorMessage,
            RetryCount = 0
        };

        // Act & Assert
        message.IsProcessed.ShouldBe(expected,
            $"IsProcessed with ProcessedAt={processedAt}, Error={errorMessage} must be {expected}");
    }

    public static TheoryData<DateTime?, string?, bool> IsProcessedTestCases =>
        new()
        {
            // ProcessedAt is null, no error -> not processed
            { null, null, false },
            // ProcessedAt is null, has error -> not processed
            { null, "Error", false },
            // ProcessedAt is set, no error -> processed
            { new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), null, true },
            // ProcessedAt is set, has error -> not processed (error takes precedence)
            { new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), "Error", false }
        };

    #endregion

    #region IsDeadLettered Tests

    [Fact]
    public void IsDeadLettered_WhenRetryCountExceedsMaxAndNotProcessed_ReturnsTrue()
    {
        // Arrange
        var message = new OutboxMessage
        {
            NotificationType = "TestNotification",
            Content = "{}",
            RetryCount = 5,
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountEqualsMaxAndNotProcessed_ReturnsTrue()
    {
        // Arrange
        var message = new OutboxMessage
        {
            NotificationType = "TestNotification",
            Content = "{}",
            RetryCount = 3,
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_WhenRetryCountBelowMax_ReturnsFalse()
    {
        // Arrange
        var message = new OutboxMessage
        {
            NotificationType = "TestNotification",
            Content = "{}",
            RetryCount = 2,
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }

    [Fact]
    public void IsDeadLettered_WhenProcessedSuccessfully_ReturnsFalse()
    {
        // Arrange
        var message = new OutboxMessage
        {
            NotificationType = "TestNotification",
            Content = "{}",
            RetryCount = 5,
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeFalse();
    }

    [Fact]
    public void IsDeadLettered_WhenProcessedWithError_ReturnsTrue()
    {
        // Arrange
        var message = new OutboxMessage
        {
            NotificationType = "TestNotification",
            Content = "{}",
            RetryCount = 5,
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Processing error"
        };

        // Act & Assert - ProcessedAtUtc with error means IsProcessed is false, so IsDeadLettered is true
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 3, false)]
    [InlineData(1, 3, false)]
    [InlineData(2, 3, false)]
    [InlineData(3, 3, true)]
    [InlineData(4, 3, true)]
    [InlineData(10, 3, true)]
    public void IsDeadLettered_WithVariousRetryCounts_ReturnsExpected(int retryCount, int maxRetries, bool expected)
    {
        // Arrange
        var message = new OutboxMessage
        {
            NotificationType = "TestNotification",
            Content = "{}",
            RetryCount = retryCount,
            ProcessedAtUtc = null
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries).ShouldBe(expected);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_SetAndGetCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var processedAt = DateTime.UtcNow.AddMinutes(5);
        var nextRetry = DateTime.UtcNow.AddMinutes(10);

        // Act
        var message = new OutboxMessage
        {
            Id = id,
            NotificationType = "OrderPlacedNotification",
            Content = "{\"orderId\":123}",
            CreatedAtUtc = createdAt,
            ProcessedAtUtc = processedAt,
            ErrorMessage = null,
            RetryCount = 2,
            NextRetryAtUtc = nextRetry
        };

        // Assert
        message.Id.ShouldBe(id);
        message.NotificationType.ShouldBe("OrderPlacedNotification");
        message.Content.ShouldBe("{\"orderId\":123}");
        message.CreatedAtUtc.ShouldBe(createdAt);
        message.ProcessedAtUtc.ShouldBe(processedAt);
        message.ErrorMessage.ShouldBeNull();
        message.RetryCount.ShouldBe(2);
        message.NextRetryAtUtc.ShouldBe(nextRetry);
    }

    [Fact]
    public void NullableProperties_CanBeSetToNull()
    {
        // Arrange & Act
        var message = new OutboxMessage
        {
            NotificationType = "Test",
            Content = "{}",
            ProcessedAtUtc = null,
            ErrorMessage = null,
            NextRetryAtUtc = null
        };

        // Assert
        message.ProcessedAtUtc.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        message.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public void DefaultRetryCount_IsZero()
    {
        // Arrange & Act
        var message = new OutboxMessage
        {
            NotificationType = "Test",
            Content = "{}"
        };

        // Assert
        message.RetryCount.ShouldBe(0);
    }

    #endregion
}
