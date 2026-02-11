using Bogus;
using Encina.Messaging.Scheduling;

namespace Encina.Testing.Bogus;

/// <summary>
/// Faker for generating realistic <see cref="IScheduledMessage"/> test data.
/// </summary>
/// <remarks>
/// <para>
/// Generates scheduled messages with realistic request types, content, and scheduling information.
/// The generated messages can be configured for one-time or recurring execution.
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// var faker = new ScheduledMessageFaker();
/// var pendingMessage = faker.Generate();
///
/// var processedMessage = new ScheduledMessageFaker()
///     .AsProcessed()
///     .Generate();
///
/// var recurringMessage = new ScheduledMessageFaker()
///     .AsRecurring("0 0 * * *") // Daily at midnight
///     .Generate();
/// </code>
/// </para>
/// </remarks>
public sealed class ScheduledMessageFaker : Faker<FakeScheduledMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageFaker"/> class.
    /// </summary>
    /// <param name="locale">The locale for generating localized data (default: "en").</param>
    public ScheduledMessageFaker(string locale = "en")
        : base(locale)
    {
        UseSeed(EncinaFaker<object>.DefaultSeed);
        CustomInstantiator(_ => new FakeScheduledMessage());

        RuleFor(m => m.Id, f => f.Random.Guid());
        RuleFor(m => m.RequestType, f => f.RequestType());
        RuleFor(m => m.Content, f => f.JsonContent());
        RuleFor(m => m.ScheduledAtUtc, f => f.Date.SoonUtc(7));
        RuleFor(m => m.CreatedAtUtc, f => f.Date.RecentUtc(1));
        RuleFor(m => m.ProcessedAtUtc, _ => null);
        RuleFor(m => m.ErrorMessage, _ => null);
        RuleFor(m => m.RetryCount, _ => 0);
        RuleFor(m => m.NextRetryAtUtc, _ => null);
        RuleFor(m => m.IsRecurring, _ => false);
        RuleFor(m => m.CronExpression, _ => null);
        RuleFor(m => m.LastExecutedAtUtc, _ => null);
    }

    /// <summary>
    /// Configures the faker to generate processed messages.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public ScheduledMessageFaker AsProcessed()
    {
        RuleFor(m => m.ProcessedAtUtc, f => f.Date.RecentUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate failed messages with retry information.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts (default: 2).</param>
    /// <returns>This faker instance for method chaining.</returns>
    public ScheduledMessageFaker AsFailed(int retryCount = 2)
    {
        RuleFor(m => m.ErrorMessage, f => f.Lorem.Sentence());
        RuleFor(m => m.RetryCount, _ => retryCount);
        RuleFor(m => m.NextRetryAtUtc, f => f.Date.SoonUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate messages that are due for execution.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public ScheduledMessageFaker AsDue()
    {
        RuleFor(m => m.ScheduledAtUtc, f => f.Date.RecentUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate recurring messages.
    /// </summary>
    /// <param name="cronExpression">The cron expression for the schedule.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public ScheduledMessageFaker AsRecurring(string? cronExpression = null)
    {
        RuleFor(m => m.IsRecurring, _ => true);
        RuleFor(m => m.CronExpression, f => cronExpression ?? f.PickRandom(
            "0 0 * * *",     // Daily at midnight
            "0 */6 * * *",   // Every 6 hours
            "0 0 * * 1",     // Weekly on Monday
            "0 0 1 * *",     // Monthly on the 1st
            "*/15 * * * *"   // Every 15 minutes
        ));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate recurring messages that have been executed.
    /// </summary>
    /// <param name="cronExpression">The cron expression for the schedule.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public ScheduledMessageFaker AsRecurringExecuted(string? cronExpression = null)
    {
        AsRecurring(cronExpression);
        RuleFor(m => m.LastExecutedAtUtc, f => f.Date.RecentUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to use a specific request type.
    /// </summary>
    /// <param name="requestType">The request type name.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestType"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="requestType"/> is empty or whitespace.</exception>
    public ScheduledMessageFaker WithRequestType(string requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestType);

        RuleFor(m => m.RequestType, _ => requestType);
        return this;
    }

    /// <summary>
    /// Configures the faker to use specific JSON content.
    /// </summary>
    /// <param name="content">The JSON content.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    public ScheduledMessageFaker WithContent(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        RuleFor(m => m.Content, _ => content);
        return this;
    }

    /// <summary>
    /// Configures the faker to schedule at a specific time.
    /// </summary>
    /// <param name="scheduledAt">The scheduled execution time. Will be converted to UTC if not already.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <remarks>
    /// If <paramref name="scheduledAt"/> has <see cref="DateTimeKind.Utc"/>, it is used as-is.
    /// If it has <see cref="DateTimeKind.Local"/> or <see cref="DateTimeKind.Unspecified"/>,
    /// it is converted to UTC using <see cref="DateTime.ToUniversalTime"/>.
    /// </remarks>
    public ScheduledMessageFaker ScheduledAt(DateTime scheduledAt)
    {
        var utcTime = scheduledAt.Kind == DateTimeKind.Utc
            ? scheduledAt
            : scheduledAt.ToUniversalTime();

        RuleFor(m => m.ScheduledAtUtc, _ => utcTime);
        return this;
    }

    /// <summary>
    /// Generates a message as <see cref="IScheduledMessage"/>.
    /// </summary>
    /// <returns>A generated scheduled message.</returns>
    public IScheduledMessage GenerateMessage() => Generate();
}

/// <summary>
/// Concrete implementation of <see cref="IScheduledMessage"/> for testing.
/// </summary>
public sealed class FakeScheduledMessage : IScheduledMessage
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc/>
    public DateTime ScheduledAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public int RetryCount { get; set; }

    /// <inheritdoc/>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc/>
    public bool IsRecurring { get; set; }

    /// <inheritdoc/>
    public string? CronExpression { get; set; }

    /// <inheritdoc/>
    public DateTime? LastExecutedAtUtc { get; set; }

    /// <inheritdoc/>
    public bool IsProcessed => ProcessedAtUtc.HasValue && !IsRecurring;

    /// <inheritdoc/>
    /// <remarks>
    /// This implementation uses <see cref="TimeProvider.System"/> which is non-deterministic.
    /// For deterministic testing, assert against <see cref="ScheduledAtUtc"/> directly
    /// using a captured timestamp instead of calling this method.
    /// </remarks>
    public bool IsDue() => TimeProvider.System.GetUtcNow().UtcDateTime >= ScheduledAtUtc && !IsProcessed;

    /// <summary>
    /// Determines whether the message is due for execution at a specific point in time.
    /// </summary>
    /// <param name="asOf">The reference time to check against.</param>
    /// <returns>True if the scheduled time has been reached and the message is not processed.</returns>
    /// <remarks>
    /// Use this overload for deterministic testing by passing a captured timestamp.
    /// </remarks>
    public bool IsDue(DateTime asOf) => asOf >= ScheduledAtUtc && !IsProcessed;

    /// <inheritdoc/>
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries;
}
