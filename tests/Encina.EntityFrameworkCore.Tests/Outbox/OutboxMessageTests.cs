using Encina.EntityFrameworkCore.Outbox;
using Shouldly;

namespace Encina.EntityFrameworkCore.Tests.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxMessage"/> domain behavior.
/// </summary>
public class OutboxMessageTests
{
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
}
