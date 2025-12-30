using Bogus;
using Encina.Messaging.Inbox;

namespace Encina.Testing.Bogus;

/// <summary>
/// Faker for generating realistic <see cref="IInboxMessage"/> test data.
/// </summary>
/// <remarks>
/// <para>
/// Generates inbox messages with realistic request types, responses, and timestamps.
/// The generated messages can be configured to represent different states:
/// pending, processed, or failed.
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// var faker = new InboxMessageFaker();
/// var pendingMessage = faker.Generate();
///
/// var processedMessage = new InboxMessageFaker()
///     .AsProcessed()
///     .Generate();
///
/// var failedMessage = new InboxMessageFaker()
///     .AsFailed(retryCount: 2)
///     .Generate();
/// </code>
/// </para>
/// </remarks>
public sealed class InboxMessageFaker : Faker<FakeInboxMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InboxMessageFaker"/> class.
    /// </summary>
    /// <param name="locale">The locale for generating localized data (default: "en").</param>
    public InboxMessageFaker(string locale = "en")
        : base(locale)
    {
        UseSeed(EncinaFaker<object>.DefaultSeed);
        CustomInstantiator(_ => new FakeInboxMessage());

        RuleFor(m => m.MessageId, f => f.Random.IdempotencyKey());
        RuleFor(m => m.RequestType, f => f.RequestType());
        RuleFor(m => m.Response, _ => null);
        RuleFor(m => m.ErrorMessage, _ => null);
        RuleFor(m => m.ReceivedAtUtc, f => f.Date.RecentUtc(7));
        RuleFor(m => m.ProcessedAtUtc, _ => null);
        RuleFor(m => m.ExpiresAtUtc, f => f.Date.SoonUtc(30));
        RuleFor(m => m.RetryCount, _ => 0);
        RuleFor(m => m.NextRetryAtUtc, _ => null);
    }

    /// <summary>
    /// Configures the faker to generate processed messages with a response.
    /// </summary>
    /// <param name="response">Optional response content.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public InboxMessageFaker AsProcessed(string? response = null)
    {
        RuleFor(m => m.ProcessedAtUtc, f => f.Date.RecentUtc(1));
        RuleFor(m => m.Response, f => response ?? f.JsonContent());
        return this;
    }

    /// <summary>
    /// Configures the faker to generate failed messages with retry information.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts (default: 2).</param>
    /// <returns>This faker instance for method chaining.</returns>
    public InboxMessageFaker AsFailed(int retryCount = 2)
    {
        RuleFor(m => m.ErrorMessage, f => f.Lorem.Sentence());
        RuleFor(m => m.RetryCount, _ => retryCount);
        RuleFor(m => m.NextRetryAtUtc, f => f.Date.SoonUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate expired messages.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public InboxMessageFaker AsExpired()
    {
        RuleFor(m => m.ExpiresAtUtc, f => f.Date.RecentUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to use a specific message ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messageId"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> is empty or whitespace.</exception>
    public InboxMessageFaker WithMessageId(string messageId)
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        RuleFor(m => m.MessageId, _ => messageId);
        return this;
    }

    /// <summary>
    /// Configures the faker to use a specific request type.
    /// </summary>
    /// <param name="requestType">The request type name.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestType"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="requestType"/> is empty or whitespace.</exception>
    public InboxMessageFaker WithRequestType(string requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestType);

        RuleFor(m => m.RequestType, _ => requestType);
        return this;
    }

    /// <summary>
    /// Generates a message as <see cref="IInboxMessage"/>.
    /// </summary>
    /// <returns>A generated inbox message.</returns>
    public IInboxMessage GenerateMessage() => Generate();
}

/// <summary>
/// Concrete implementation of <see cref="IInboxMessage"/> for testing.
/// </summary>
public sealed class FakeInboxMessage : IInboxMessage
{
    /// <inheritdoc/>
    public string MessageId { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Response { get; set; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public DateTime ReceivedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime ExpiresAtUtc { get; set; }

    /// <inheritdoc/>
    public int RetryCount { get; set; }

    /// <inheritdoc/>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc/>
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    /// <inheritdoc/>
    public bool IsExpired() => DateTime.UtcNow > ExpiresAtUtc;
}
