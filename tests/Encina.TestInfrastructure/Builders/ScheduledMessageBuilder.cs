using Encina.Dapper.Sqlite.Scheduling;

namespace Encina.TestInfrastructure.Builders;

/// <summary>
/// Builder for creating ScheduledMessage test data.
/// Provides fluent API for constructing test scheduled messages with sensible defaults.
/// </summary>
public sealed class ScheduledMessageBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _requestType = "TestCommand";
    private string _content = "{}";
    private DateTime _scheduledAtUtc = DateTime.UtcNow.AddHours(1);
    private readonly DateTime _createdAtUtc = DateTime.UtcNow;
    private DateTime? _processedAtUtc;
    private string? _errorMessage;
    private int _retryCount;
    private DateTime? _nextRetryAtUtc;
    private bool _isRecurring;
    private string? _cronExpression;

    /// <summary>
    /// Sets the message ID.
    /// </summary>
    public ScheduledMessageBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the request type.
    /// </summary>
    public ScheduledMessageBuilder WithRequestType(string requestType)
    {
        _requestType = requestType;
        return this;
    }

    /// <summary>
    /// Sets the message content (JSON).
    /// </summary>
    public ScheduledMessageBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    /// <summary>
    /// Sets the scheduled timestamp.
    /// </summary>
    public ScheduledMessageBuilder WithScheduledAtUtc(DateTime scheduledAtUtc)
    {
        _scheduledAtUtc = scheduledAtUtc;
        return this;
    }

    /// <summary>
    /// Schedules the message for immediate execution.
    /// </summary>
    public ScheduledMessageBuilder AsImmediate()
    {
        _scheduledAtUtc = DateTime.UtcNow.AddSeconds(-1);
        return this;
    }

    /// <summary>
    /// Schedules the message for future execution.
    /// </summary>
    public ScheduledMessageBuilder AsFuture(TimeSpan delay)
    {
        _scheduledAtUtc = DateTime.UtcNow.Add(delay);
        return this;
    }

    /// <summary>
    /// Marks the message as processed.
    /// </summary>
    public ScheduledMessageBuilder AsProcessed(DateTime? processedAtUtc = null)
    {
        _processedAtUtc = processedAtUtc ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks the message as failed with an error.
    /// </summary>
    public ScheduledMessageBuilder WithError(string errorMessage, int retryCount = 1, DateTime? nextRetryAtUtc = null)
    {
        _errorMessage = errorMessage;
        _retryCount = retryCount;
        _nextRetryAtUtc = nextRetryAtUtc;
        return this;
    }

    /// <summary>
    /// Sets the retry count.
    /// </summary>
    public ScheduledMessageBuilder WithRetryCount(int retryCount)
    {
        _retryCount = retryCount;
        return this;
    }

    /// <summary>
    /// Sets the next retry timestamp.
    /// </summary>
    public ScheduledMessageBuilder WithNextRetryAtUtc(DateTime? nextRetryAtUtc)
    {
        _nextRetryAtUtc = nextRetryAtUtc;
        return this;
    }

    /// <summary>
    /// Sets the cron expression for recurring messages.
    /// </summary>
    public ScheduledMessageBuilder WithCronExpression(string cronExpression)
    {
        _cronExpression = cronExpression;
        _isRecurring = true;
        return this;
    }

    /// <summary>
    /// Sets daily recurrence pattern.
    /// </summary>
    public ScheduledMessageBuilder AsDaily(int hour = 9, int minute = 0)
    {
        _cronExpression = $"0 {minute} {hour} * * *";
        _isRecurring = true;
        return this;
    }

    /// <summary>
    /// Builds the ScheduledMessage instance.
    /// </summary>
    public ScheduledMessage Build() => new()
    {
        Id = _id,
        RequestType = _requestType,
        Content = _content,
        ScheduledAtUtc = _scheduledAtUtc,
        CreatedAtUtc = _createdAtUtc,
        ProcessedAtUtc = _processedAtUtc,
        ErrorMessage = _errorMessage,
        RetryCount = _retryCount,
        NextRetryAtUtc = _nextRetryAtUtc,
        IsRecurring = _isRecurring,
        CronExpression = _cronExpression
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static ScheduledMessageBuilder Create() => new();
}
