using Encina.Messaging.Scheduling;

namespace Encina.EntityFrameworkCore.Scheduling;

/// <summary>
/// Factory for creating Entity Framework Core scheduled message instances.
/// </summary>
public sealed class ScheduledMessageFactory : IScheduledMessageFactory
{
    /// <inheritdoc />
    public IScheduledMessage Create(
        Guid id,
        string requestType,
        string content,
        DateTime scheduledAtUtc,
        DateTime createdAtUtc,
        bool isRecurring,
        string? cronExpression)
    {
        return new ScheduledMessage
        {
            Id = id,
            RequestType = requestType,
            Content = content,
            ScheduledAtUtc = scheduledAtUtc,
            CreatedAtUtc = createdAtUtc,
            IsRecurring = isRecurring,
            CronExpression = cronExpression,
            RetryCount = 0
        };
    }
}
