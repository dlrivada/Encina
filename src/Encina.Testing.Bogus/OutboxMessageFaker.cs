using Bogus;
using Encina.Messaging.Outbox;

namespace Encina.Testing.Bogus;

/// <summary>
/// Faker for generating realistic <see cref="IOutboxMessage"/> test data.
/// </summary>
/// <remarks>
/// <para>
/// Generates outbox messages with realistic notification types, content, and timestamps.
/// The generated messages can be configured to represent different states:
/// pending, processed, or failed.
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// var faker = new OutboxMessageFaker();
/// var pendingMessage = faker.Generate();
///
/// var processedMessage = new OutboxMessageFaker()
///     .AsProcessed()
///     .Generate();
///
/// var failedMessage = new OutboxMessageFaker()
///     .AsFailed(retryCount: 3)
///     .Generate();
/// </code>
/// </para>
/// </remarks>
public sealed class OutboxMessageFaker : Faker<FakeOutboxMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMessageFaker"/> class.
    /// </summary>
    /// <param name="locale">The locale for generating localized data (default: "en").</param>
    public OutboxMessageFaker(string locale = "en")
        : base(locale)
    {
        UseSeed(EncinaFaker<object>.DefaultSeed);
        CustomInstantiator(_ => new FakeOutboxMessage());

        RuleFor(m => m.Id, f => f.Random.Guid());
        RuleFor(m => m.NotificationType, f => f.NotificationType());
        RuleFor(m => m.Content, f => f.JsonContent());
        RuleFor(m => m.CreatedAtUtc, f => f.Date.RecentUtc(7));
        RuleFor(m => m.ProcessedAtUtc, _ => null);
        RuleFor(m => m.ErrorMessage, _ => null);
        RuleFor(m => m.RetryCount, _ => 0);
        RuleFor(m => m.NextRetryAtUtc, _ => null);
    }

    /// <summary>
    /// Configures the faker to generate processed messages.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public OutboxMessageFaker AsProcessed()
    {
        RuleFor(m => m.ProcessedAtUtc, f => f.Date.RecentUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate failed messages with retry information.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts (default: 3).</param>
    /// <returns>This faker instance for method chaining.</returns>
    public OutboxMessageFaker AsFailed(int retryCount = 3)
    {
        RuleFor(m => m.ErrorMessage, f => f.Lorem.Sentence());
        RuleFor(m => m.RetryCount, _ => retryCount);
        RuleFor(m => m.NextRetryAtUtc, f => f.Date.SoonUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to use a specific notification type.
    /// </summary>
    /// <param name="notificationType">The notification type name.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="notificationType"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="notificationType"/> is empty or whitespace.</exception>
    public OutboxMessageFaker WithNotificationType(string notificationType)
    {
        ArgumentNullException.ThrowIfNull(notificationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationType);

        RuleFor(m => m.NotificationType, _ => notificationType);
        return this;
    }

    /// <summary>
    /// Configures the faker to use specific JSON content.
    /// </summary>
    /// <param name="content">The JSON content.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    public OutboxMessageFaker WithContent(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        RuleFor(m => m.Content, _ => content);
        return this;
    }

    /// <summary>
    /// Generates a message as <see cref="IOutboxMessage"/>.
    /// </summary>
    /// <returns>A generated outbox message.</returns>
    public IOutboxMessage GenerateMessage() => Generate();
}

/// <summary>
/// Concrete implementation of <see cref="IOutboxMessage"/> for testing.
/// </summary>
public sealed class FakeOutboxMessage : IOutboxMessage
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string NotificationType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Content { get; set; } = string.Empty;

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
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    /// <inheritdoc/>
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries;
}
