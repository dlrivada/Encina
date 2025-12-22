using System.Data;
using System.Text.Json;
using Dapper;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Encina.Messaging.Inbox;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Oracle.Inbox;

/// <summary>
/// Pipeline behavior that implements the Inbox Pattern for idempotent message processing.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior ensures that requests marked with <see cref="IIdempotentRequest"/> are
/// processed exactly once, even if received multiple times. It uses the MessageId from
/// the request context to track processed messages.
/// </para>
/// <para>
/// <b>Processing Flow</b>:
/// <list type="number">
/// <item><description>Check if MessageId exists in context (required for idempotent requests)</description></item>
/// <item><description>Look up message in inbox by MessageId</description></item>
/// <item><description>If found and processed, return cached response</description></item>
/// <item><description>If not found, create inbox entry</description></item>
/// <item><description>Process request</description></item>
/// <item><description>Store response in inbox</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class InboxPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDbConnection _connection;
    private readonly ILogger<InboxPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly InboxOptions _options;
    private readonly string _tableName;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="options">The inbox options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="tableName">The inbox table name (default: InboxMessages).</param>
    public InboxPipelineBehavior(
        IDbConnection connection,
        InboxOptions options,
        ILogger<InboxPipelineBehavior<TRequest, TResponse>> logger,
        string tableName = "InboxMessages")
    {
        _connection = connection;
        _options = options;
        _logger = logger;
        _tableName = tableName;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Only process if request is idempotent
        if (request is not IIdempotentRequest)
            return await nextStep().ConfigureAwait(false);

        // MessageId is required for idempotent processing
        var messageId = context.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            Log.MissingIdempotencyKey(_logger, typeof(TRequest).Name, context.CorrelationId);

            return MediatorErrors.Create(
                "inbox.missing_message_id",
                "Idempotent requests require a MessageId (IdempotencyKey)");
        }

        Log.ProcessingIdempotentRequest(_logger, typeof(TRequest).Name, messageId, context.CorrelationId);

        // Check if message already exists in inbox
        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE MessageId = :MessageId";

        var existingMessage = await _connection.QuerySingleOrDefaultAsync<InboxMessage>(
            sql,
            new { MessageId = messageId }).ConfigureAwait(false);

        if (existingMessage != null)
        {
            // Message already processed - return cached response
            if (existingMessage.IsProcessed && existingMessage.Response != null)
            {
                Log.ReturningCachedResponse(_logger, messageId, context.CorrelationId);

                return DeserializeResponse(existingMessage.Response);
            }

            // Message exists but failed - retry if within limit
            if (existingMessage.RetryCount >= _options.MaxRetries)
            {
                Log.MaxRetriesExceeded(_logger, messageId, _options.MaxRetries, context.CorrelationId);

                return MediatorErrors.Create(
                    "inbox.max_retries_exceeded",
                    $"Message has failed {existingMessage.RetryCount} times and will not be retried");
            }

            // Update retry count
            var updateRetrySql = $@"
                UPDATE {_tableName}
                SET RetryCount = RetryCount + 1
                WHERE MessageId = :MessageId";

            await _connection.ExecuteAsync(updateRetrySql, new { MessageId = messageId }).ConfigureAwait(false);
        }
        else
        {
            // Create new inbox entry
            var now = DateTime.UtcNow;
            var insertSql = $@"
                INSERT INTO {_tableName}
                (MessageId, RequestType, ReceivedAtUtc, RetryCount, ExpiresAtUtc, Metadata)
                VALUES
                (:MessageId, :RequestType, :ReceivedAtUtc, :RetryCount, :ExpiresAtUtc, :Metadata)";

            await _connection.ExecuteAsync(
                insertSql,
                new
                {
                    MessageId = messageId,
                    RequestType = typeof(TRequest).AssemblyQualifiedName
                        ?? typeof(TRequest).FullName
                        ?? typeof(TRequest).Name,
                    ReceivedAtUtc = now,
                    RetryCount = 0,
                    ExpiresAtUtc = now.Add(_options.MessageRetentionPeriod),
                    Metadata = JsonSerializer.Serialize(new
                    {
                        context.CorrelationId,
                        context.UserId,
                        context.TenantId,
                        Timestamp = context.Timestamp
                    }, JsonOptions)
                }).ConfigureAwait(false);
        }

        // Process the request
        try
        {
            var result = await nextStep().ConfigureAwait(false);

            // Store response in inbox
            var updateSql = $@"
                UPDATE {_tableName}
                SET Response = :Response,
                    ProcessedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                    ErrorMessage = NULL
                WHERE MessageId = :MessageId";

            await _connection.ExecuteAsync(
                updateSql,
                new
                {
                    MessageId = messageId,
                    Response = SerializeResponse(result)
                }).ConfigureAwait(false);

            Log.ProcessedAndCachedMessage(_logger, messageId, context.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            Log.ErrorProcessingMessage(_logger, ex, messageId, context.CorrelationId);

            var updateErrorSql = $@"
                UPDATE {_tableName}
                SET ErrorMessage = :ErrorMessage
                WHERE MessageId = :MessageId";

            await _connection.ExecuteAsync(
                updateErrorSql,
                new
                {
                    MessageId = messageId,
                    ErrorMessage = ex.Message
                }).ConfigureAwait(false);

            throw;
        }
    }

    private static string SerializeResponse(Either<MediatorError, TResponse> response)
    {
        var envelope = response.Match(
            Right: value => new ResponseEnvelope { IsSuccess = true, Value = value },
            Left: error => new ResponseEnvelope { IsSuccess = false, Error = error });

        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    private static Either<MediatorError, TResponse> DeserializeResponse(string json)
    {
        var envelope = JsonSerializer.Deserialize<ResponseEnvelope>(json, JsonOptions);
        if (envelope == null)
            return MediatorErrors.Create("inbox.deserialization_failed", "Failed to deserialize cached response");

        if (envelope.IsSuccess && envelope.Value != null)
        {
            var value = JsonSerializer.Deserialize<TResponse>(
                JsonSerializer.Serialize(envelope.Value, JsonOptions),
                JsonOptions);

            return value != null
                ? Right<MediatorError, TResponse>(value)
                : MediatorErrors.Create("inbox.deserialization_failed", "Failed to deserialize response value");
        }

        return envelope.Error ?? MediatorErrors.Create("inbox.unknown_error", "Unknown error in cached response");
    }

    private sealed class ResponseEnvelope
    {
        public bool IsSuccess { get; set; }
        public object? Value { get; set; }
        public MediatorError? Error { get; set; }
    }
}

/// <summary>
/// Marker interface to indicate that a request should be processed idempotently using the Inbox Pattern.
/// </summary>
/// <remarks>
/// <para>
/// Requests implementing this interface will be tracked in the inbox to ensure exactly-once processing.
/// The MessageId (from <c>IRequestContext.IdempotencyKey</c>) is used as the deduplication key.
/// </para>
/// </remarks>
public interface IIdempotentRequest
{
}
