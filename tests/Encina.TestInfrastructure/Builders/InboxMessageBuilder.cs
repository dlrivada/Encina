using Encina.Dapper.Sqlite.Inbox;

namespace Encina.TestInfrastructure.Builders;

/// <summary>
/// Builder for creating InboxMessage test data.
/// Provides fluent API for constructing test messages with sensible defaults.
/// </summary>
public sealed class InboxMessageBuilder
{
    private string _messageId = Guid.NewGuid().ToString();
    private string _requestType = "TestRequest";
    private DateTime _receivedAtUtc = DateTime.UtcNow;
    private DateTime? _processedAtUtc;
    private string? _response;
    private string? _errorMessage;
    private int _retryCount;
    private DateTime? _nextRetryAtUtc;
    private DateTime _expiresAtUtc = DateTime.UtcNow.AddDays(30);

    /// <summary>
    /// Sets the message ID.
    /// </summary>
    public InboxMessageBuilder WithMessageId(string messageId)
    {
        _messageId = messageId;
        return this;
    }

    /// <summary>
    /// Sets the request type.
    /// </summary>
    public InboxMessageBuilder WithRequestType(string requestType)
    {
        _requestType = requestType;
        return this;
    }

    /// <summary>
    /// Sets the received timestamp.
    /// </summary>
    public InboxMessageBuilder WithReceivedAtUtc(DateTime receivedAtUtc)
    {
        _receivedAtUtc = receivedAtUtc;
        return this;
    }

    /// <summary>
    /// Marks the message as processed with a response.
    /// </summary>
    public InboxMessageBuilder AsProcessed(string? response = null, DateTime? processedAtUtc = null)
    {
        _response = response ?? "{\"status\":\"success\"}";
        _processedAtUtc = processedAtUtc ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks the message as failed with an error.
    /// </summary>
    public InboxMessageBuilder WithError(string errorMessage, int retryCount = 1, DateTime? nextRetryAtUtc = null)
    {
        _errorMessage = errorMessage;
        _retryCount = retryCount;
        _nextRetryAtUtc = nextRetryAtUtc;
        return this;
    }

    /// <summary>
    /// Sets the retry count.
    /// </summary>
    public InboxMessageBuilder WithRetryCount(int retryCount)
    {
        _retryCount = retryCount;
        return this;
    }

    /// <summary>
    /// Sets the next retry timestamp.
    /// </summary>
    public InboxMessageBuilder WithNextRetryAtUtc(DateTime? nextRetryAtUtc)
    {
        _nextRetryAtUtc = nextRetryAtUtc;
        return this;
    }

    /// <summary>
    /// Sets the expiration timestamp.
    /// </summary>
    public InboxMessageBuilder WithExpiresAtUtc(DateTime expiresAtUtc)
    {
        _expiresAtUtc = expiresAtUtc;
        return this;
    }

    /// <summary>
    /// Marks the message as expired.
    /// </summary>
    public InboxMessageBuilder AsExpired()
    {
        _expiresAtUtc = DateTime.UtcNow.AddDays(-1);
        _processedAtUtc = DateTime.UtcNow.AddDays(-5);
        return this;
    }

    /// <summary>
    /// Builds the InboxMessage instance.
    /// </summary>
    public InboxMessage Build() => new()
    {
        MessageId = _messageId,
        RequestType = _requestType,
        ReceivedAtUtc = _receivedAtUtc,
        ProcessedAtUtc = _processedAtUtc,
        Response = _response,
        ErrorMessage = _errorMessage,
        RetryCount = _retryCount,
        NextRetryAtUtc = _nextRetryAtUtc,
        ExpiresAtUtc = _expiresAtUtc
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static InboxMessageBuilder Create() => new();
}
