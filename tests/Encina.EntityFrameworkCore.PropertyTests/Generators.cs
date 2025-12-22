using FsCheck;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;

namespace Encina.EntityFrameworkCore.PropertyTests;

/// <summary>
/// Custom FsCheck generators for messaging pattern entities.
/// These generators create valid random instances for property-based testing.
/// </summary>
public static class Generators
{
    /// <summary>
    /// Generates valid OutboxMessage instances with random data.
    /// </summary>
    public static Arbitrary<OutboxMessage> OutboxMessageArbitrary() =>
        Arb.From(
            from id in Arb.Generate<Guid>()
            from notificationType in NonEmptyStringGen()
            from content in JsonContentGen()
            from createdOffset in Gen.Choose(-1440, 0) // Last 24 hours
            from retryCount in Gen.Choose(0, 10)
            from hasError in Arb.Generate<bool>()
            from hasNextRetry in Arb.Generate<bool>()
            from errorMsg in hasError ? NonEmptyStringGen() : Gen.Constant<string?>(null)
            from nextRetryOffset in hasNextRetry ? Gen.Choose(1, 120) : Gen.Constant<int?>(null)
            from hasProcessed in Arb.Generate<bool>()
            select new OutboxMessage
            {
                Id = id,
                NotificationType = notificationType,
                Content = content,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(createdOffset),
                RetryCount = retryCount,
                ErrorMessage = errorMsg,
                NextRetryAtUtc = nextRetryOffset.HasValue
                    ? DateTime.UtcNow.AddMinutes(nextRetryOffset.Value)
                    : null,
                ProcessedAtUtc = hasProcessed ? DateTime.UtcNow : null
            });

    /// <summary>
    /// Generates valid InboxMessage instances with random data.
    /// </summary>
    public static Arbitrary<InboxMessage> InboxMessageArbitrary() =>
        Arb.From(
            from messageId in NonEmptyStringGen()
            from requestType in NonEmptyStringGen()
            from content in JsonContentGen()
            from receivedOffset in Gen.Choose(-1440, 0) // Last 24 hours
            from expiresOffset in Gen.Choose(-720, 720) // +/- 12 hours
            from retryCount in Gen.Choose(0, 10)
            from hasError in Arb.Generate<bool>()
            from hasNextRetry in Arb.Generate<bool>()
            from hasProcessed in Arb.Generate<bool>()
            from errorMsg in hasError ? NonEmptyStringGen() : Gen.Constant<string?>(null)
            from response in hasProcessed ? JsonContentGen() : Gen.Constant<string?>(null)
            from nextRetryOffset in hasNextRetry ? Gen.Choose(1, 120) : Gen.Constant<int?>(null)
            select new InboxMessage
            {
                MessageId = $"{messageId}-{Guid.NewGuid()}",
                RequestType = requestType,
                Content = content,
                ReceivedAtUtc = DateTime.UtcNow.AddMinutes(receivedOffset),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(expiresOffset),
                RetryCount = retryCount,
                ErrorMessage = errorMsg,
                Response = response,
                ProcessedAtUtc = hasProcessed ? DateTime.UtcNow : null,
                NextRetryAtUtc = nextRetryOffset.HasValue
                    ? DateTime.UtcNow.AddMinutes(nextRetryOffset.Value)
                    : null
            });

    /// <summary>
    /// Generates valid SagaState instances with random data.
    /// </summary>
    public static Arbitrary<SagaState> SagaStateArbitrary() =>
        Arb.From(
            from sagaId in Arb.Generate<Guid>()
            from sagaType in NonEmptyStringGen()
            from data in JsonContentGen()
            from step in Gen.Choose(0, 20)
            from status in SagaStatusGen()
            from startedOffset in Gen.Choose(-1440, 0)
            from lastUpdatedOffset in Gen.Choose(-720, 0)
            from hasCorrelation in Arb.Generate<bool>()
            from hasTimeout in Arb.Generate<bool>()
            from hasMetadata in Arb.Generate<bool>()
            from correlationId in hasCorrelation ? Arb.Generate<Guid>().Select(g => g.ToString()) : Gen.Constant<string?>(null)
            from timeoutOffset in hasTimeout ? Gen.Choose(60, 1440) : Gen.Constant<int?>(null)
            from metadata in hasMetadata ? JsonContentGen() : Gen.Constant<string?>(null)
            select new SagaState
            {
                SagaId = sagaId,
                SagaType = sagaType,
                Data = data,
                CurrentStep = step,
                Status = status,
                StartedAtUtc = DateTime.UtcNow.AddMinutes(startedOffset),
                LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(lastUpdatedOffset),
                CorrelationId = correlationId,
                TimeoutAtUtc = timeoutOffset.HasValue
                    ? DateTime.UtcNow.AddMinutes(timeoutOffset.Value)
                    : null,
                Metadata = metadata,
                CompletedAtUtc = IsTerminalStatus(status) ? DateTime.UtcNow : null
            });

    /// <summary>
    /// Generates valid ScheduledMessage instances with random data.
    /// </summary>
    public static Arbitrary<ScheduledMessage> ScheduledMessageArbitrary() =>
        Arb.From(
            from id in Arb.Generate<Guid>()
            from requestType in NonEmptyStringGen()
            from content in JsonContentGen()
            from scheduledOffset in Gen.Choose(-1440, 1440) // +/- 24 hours
            from createdOffset in Gen.Choose(-1440, 0)
            from retryCount in Gen.Choose(0, 10)
            from isRecurring in Arb.Generate<bool>()
            from hasCron in isRecurring ? Gen.Constant(true) : Arb.Generate<bool>()
            from hasError in Arb.Generate<bool>()
            from hasNextRetry in Arb.Generate<bool>()
            from hasProcessed in Arb.Generate<bool>()
            from hasCorrelation in Arb.Generate<bool>()
            from cronExpr in hasCron ? CronExpressionGen() : Gen.Constant<string?>(null)
            from errorMsg in hasError ? NonEmptyStringGen() : Gen.Constant<string?>(null)
            from nextRetryOffset in hasNextRetry ? Gen.Choose(1, 120) : Gen.Constant<int?>(null)
            from correlationId in hasCorrelation ? Arb.Generate<Guid>().Select(g => g.ToString()) : Gen.Constant<string?>(null)
            select new ScheduledMessage
            {
                Id = id,
                RequestType = requestType,
                Content = content,
                ScheduledAtUtc = DateTime.UtcNow.AddMinutes(scheduledOffset),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(createdOffset),
                RetryCount = retryCount,
                IsRecurring = isRecurring,
                CronExpression = cronExpr,
                ErrorMessage = errorMsg,
                NextRetryAtUtc = nextRetryOffset.HasValue
                    ? DateTime.UtcNow.AddMinutes(nextRetryOffset.Value)
                    : null,
                ProcessedAtUtc = hasProcessed ? DateTime.UtcNow : null,
                LastExecutedAtUtc = hasProcessed ? DateTime.UtcNow.AddMinutes(-5) : null,
                CorrelationId = correlationId
            });

    /// <summary>
    /// Generates pending (unprocessed, not exhausted retries) OutboxMessage instances.
    /// </summary>
    public static Arbitrary<OutboxMessage> PendingOutboxMessageArbitrary() =>
        Arb.From(
            from id in Arb.Generate<Guid>()
            from notificationType in NonEmptyStringGen()
            from content in JsonContentGen()
            from createdOffset in Gen.Choose(-1440, 0)
            from retryCount in Gen.Choose(0, 2) // Below typical maxRetries
            select new OutboxMessage
            {
                Id = id,
                NotificationType = notificationType,
                Content = content,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(createdOffset),
                RetryCount = retryCount,
                ProcessedAtUtc = null, // Not processed
                ErrorMessage = null,
                NextRetryAtUtc = null
            });

    /// <summary>
    /// Generates due (scheduled in the past, not processed) ScheduledMessage instances.
    /// </summary>
    public static Arbitrary<ScheduledMessage> DueScheduledMessageArbitrary() =>
        Arb.From(
            from id in Arb.Generate<Guid>()
            from requestType in NonEmptyStringGen()
            from content in JsonContentGen()
            from scheduledOffset in Gen.Choose(-1440, -1) // In the past
            from retryCount in Gen.Choose(0, 2)
            select new ScheduledMessage
            {
                Id = id,
                RequestType = requestType,
                Content = content,
                ScheduledAtUtc = DateTime.UtcNow.AddMinutes(scheduledOffset),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(scheduledOffset - 10),
                RetryCount = retryCount,
                ProcessedAtUtc = null, // Not processed
                NextRetryAtUtc = null
            });

    /// <summary>
    /// Generates stuck (long-running, not completed) SagaState instances.
    /// </summary>
    public static Arbitrary<SagaState> StuckSagaStateArbitrary() =>
        Arb.From(
            from sagaId in Arb.Generate<Guid>()
            from sagaType in NonEmptyStringGen()
            from data in JsonContentGen()
            from step in Gen.Choose(0, 10)
            from lastUpdatedOffset in Gen.Choose(-1440, -60) // At least 1h old
            from status in Gen.Elements(SagaStatus.Running, SagaStatus.Compensating)
            select new SagaState
            {
                SagaId = sagaId,
                SagaType = sagaType,
                Data = data,
                CurrentStep = step,
                Status = status,
                StartedAtUtc = DateTime.UtcNow.AddMinutes(lastUpdatedOffset),
                LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(lastUpdatedOffset)
            });

    // ====== Private Helper Generators ======

    private static Gen<string> NonEmptyStringGen() =>
        from prefix in Gen.Elements("Test", "Message", "Saga", "Request", "Notification")
        from suffix in Arb.Generate<Guid>()
        select $"{prefix}_{suffix}";

    private static Gen<string> JsonContentGen() =>
        from key in Gen.Elements("data", "payload", "value", "content", "step")
        from value in Arb.Generate<Guid>()
        from includeInt in Arb.Generate<bool>()
        from intValue in Gen.Choose(1, 1000)
        select includeInt
            ? $"{{\"{key}\":\"{value}\",\"index\":{intValue}}}"
            : $"{{\"{key}\":\"{value}\"}}";

    private static Gen<SagaStatus> SagaStatusGen() =>
        Gen.Elements(
            SagaStatus.Running,
            SagaStatus.Compensating,
            SagaStatus.Compensated,
            SagaStatus.Completed,
            SagaStatus.Failed,
            SagaStatus.TimedOut);

    private static Gen<string> CronExpressionGen() =>
        Gen.Elements(
            "0 0 * * *",       // Daily at midnight
            "0 */6 * * *",     // Every 6 hours
            "*/15 * * * *",    // Every 15 minutes
            "0 0 * * 0",       // Weekly on Sunday
            "0 9 * * 1-5");    // Weekdays at 9am

    private static bool IsTerminalStatus(SagaStatus status) =>
        status is SagaStatus.Completed or SagaStatus.Compensated or SagaStatus.Failed;
}

/// <summary>
/// Assembly-level attribute to register custom generators with FsCheck.
/// </summary>
[assembly: Properties(Arbitrary = new[] { typeof(Generators) })]
