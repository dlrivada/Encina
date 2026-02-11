using Encina.Dapper.Sqlite.Inbox;
using Encina.Dapper.Sqlite.Outbox;
using Encina.Dapper.Sqlite.Sagas;
using Encina.Dapper.Sqlite.Scheduling;
using Xunit;

namespace Encina.TestInfrastructure.Extensions;

/// <summary>
/// Extension methods for common test assertions.
/// Provides fluent API for asserting messaging entity states.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Asserts that an OutboxMessage is in pending state (not processed, no error).
    /// </summary>
    public static void ShouldBePending(this OutboxMessage message)
    {
        Assert.NotNull(message);
        Assert.Null(message.ProcessedAtUtc);
        Assert.Null(message.ErrorMessage);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.NextRetryAtUtc);
    }

    /// <summary>
    /// Asserts that an OutboxMessage is processed successfully.
    /// </summary>
    public static void ShouldBeProcessed(this OutboxMessage message)
    {
        Assert.NotNull(message);
        Assert.NotNull(message.ProcessedAtUtc);
        Assert.Null(message.ErrorMessage);
    }

    /// <summary>
    /// Asserts that an OutboxMessage has failed with an error.
    /// </summary>
    public static void ShouldHaveFailed(this OutboxMessage message, string? expectedError = null)
    {
        Assert.NotNull(message);
        Assert.Null(message.ProcessedAtUtc);
        Assert.NotNull(message.ErrorMessage);
        Assert.True(message.RetryCount > 0);

        if (expectedError is not null)
        {
            Assert.Contains(expectedError, message.ErrorMessage);
        }
    }

    /// <summary>
    /// Asserts that an InboxMessage is in pending state (not processed, no error).
    /// </summary>
    public static void ShouldBePending(this InboxMessage message)
    {
        Assert.NotNull(message);
        Assert.Null(message.ProcessedAtUtc);
        Assert.Null(message.Response);
        Assert.Null(message.ErrorMessage);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.NextRetryAtUtc);
    }

    /// <summary>
    /// Asserts that an InboxMessage is processed successfully.
    /// </summary>
    public static void ShouldBeProcessed(this InboxMessage message)
    {
        Assert.NotNull(message);
        Assert.NotNull(message.ProcessedAtUtc);
        Assert.NotNull(message.Response);
        Assert.Null(message.ErrorMessage);
    }

    /// <summary>
    /// Asserts that an InboxMessage has failed with an error.
    /// </summary>
    public static void ShouldHaveFailed(this InboxMessage message, string? expectedError = null)
    {
        Assert.NotNull(message);
        Assert.Null(message.ProcessedAtUtc);
        Assert.NotNull(message.ErrorMessage);
        Assert.True(message.RetryCount > 0);

        if (expectedError is not null)
        {
            Assert.Contains(expectedError, message.ErrorMessage);
        }
    }

    /// <summary>
    /// Asserts that an InboxMessage is expired.
    /// </summary>
    public static void ShouldBeExpired(this InboxMessage message)
    {
        Assert.NotNull(message);
        Assert.True(message.ExpiresAtUtc < DateTime.UtcNow);
    }

    /// <summary>
    /// Asserts that a SagaState is in running state.
    /// </summary>
    public static void ShouldBeRunning(this SagaState saga)
    {
        Assert.NotNull(saga);
        Assert.Equal("Running", saga.Status);
        Assert.Null(saga.CompletedAtUtc);
        Assert.Null(saga.ErrorMessage);
    }

    /// <summary>
    /// Asserts that a SagaState is completed successfully.
    /// </summary>
    public static void ShouldBeCompleted(this SagaState saga)
    {
        Assert.NotNull(saga);
        Assert.Equal("Completed", saga.Status);
        Assert.NotNull(saga.CompletedAtUtc);
        Assert.Null(saga.ErrorMessage);
    }

    /// <summary>
    /// Asserts that a SagaState is compensating.
    /// </summary>
    public static void ShouldBeCompensating(this SagaState saga)
    {
        Assert.NotNull(saga);
        Assert.Equal("Compensating", saga.Status);
    }

    /// <summary>
    /// Asserts that a SagaState is compensated.
    /// </summary>
    public static void ShouldBeCompensated(this SagaState saga)
    {
        Assert.NotNull(saga);
        Assert.Equal("Compensated", saga.Status);
        Assert.NotNull(saga.CompletedAtUtc);
    }

    /// <summary>
    /// Asserts that a SagaState has failed.
    /// </summary>
    public static void ShouldHaveFailed(this SagaState saga, string? expectedError = null)
    {
        Assert.NotNull(saga);
        Assert.Equal("Failed", saga.Status);
        Assert.NotNull(saga.ErrorMessage);
        Assert.NotNull(saga.CompletedAtUtc);

        if (expectedError is not null)
        {
            Assert.Contains(expectedError, saga.ErrorMessage);
        }
    }

    /// <summary>
    /// Asserts that a ScheduledMessage is pending execution.
    /// </summary>
    public static void ShouldBePending(this ScheduledMessage message)
    {
        Assert.NotNull(message);
        Assert.Null(message.ProcessedAtUtc);
        Assert.Null(message.ErrorMessage);
        Assert.Equal(0, message.RetryCount);
    }

    /// <summary>
    /// Asserts that a ScheduledMessage is due for execution.
    /// </summary>
    public static void ShouldBeDue(this ScheduledMessage message)
    {
        Assert.NotNull(message);
        Assert.True(message.ScheduledAtUtc <= DateTime.UtcNow);
        Assert.Null(message.ProcessedAtUtc);
    }

    /// <summary>
    /// Asserts that a ScheduledMessage is processed.
    /// </summary>
    public static void ShouldBeProcessed(this ScheduledMessage message)
    {
        Assert.NotNull(message);
        Assert.NotNull(message.ProcessedAtUtc);
        Assert.Null(message.ErrorMessage);
    }

    /// <summary>
    /// Asserts that a ScheduledMessage has failed.
    /// </summary>
    public static void ShouldHaveFailed(this ScheduledMessage message, string? expectedError = null)
    {
        Assert.NotNull(message);
        Assert.Null(message.ProcessedAtUtc);
        Assert.NotNull(message.ErrorMessage);
        Assert.True(message.RetryCount > 0);

        if (expectedError is not null)
        {
            Assert.Contains(expectedError, message.ErrorMessage);
        }
    }

    /// <summary>
    /// Asserts that a ScheduledMessage is recurring.
    /// </summary>
    public static void ShouldBeRecurring(this ScheduledMessage message)
    {
        Assert.NotNull(message);
        Assert.True(message.IsRecurring);
        Assert.NotNull(message.CronExpression);
        Assert.NotEmpty(message.CronExpression);
    }
}
