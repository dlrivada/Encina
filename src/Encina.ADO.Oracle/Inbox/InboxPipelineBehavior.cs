using System.Text.Json;
using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.ADO.Oracle.Inbox;

/// <summary>
/// Pipeline behavior that implements the Inbox Pattern for idempotent message processing using ADO.NET.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class InboxPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IInboxStore _inboxStore;
    private readonly ILogger<InboxPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly InboxOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="inboxStore">The inbox store.</param>
    /// <param name="options">The inbox options.</param>
    /// <param name="logger">The logger.</param>
    public InboxPipelineBehavior(
        IInboxStore inboxStore,
        InboxOptions options,
        ILogger<InboxPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(inboxStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inboxStore = inboxStore;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // Only process if request is idempotent
        if (request is not IIdempotentRequest)
            return await nextStep().ConfigureAwait(false);

        // MessageId is required for idempotent processing
        var messageId = context.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            Log.MissingMessageId(
                _logger,
                typeof(TRequest).Name,
                context.CorrelationId);

            return EncinaErrors.Create(
                "inbox.missing_message_id",
                "Idempotent requests require a MessageId (IdempotencyKey)");
        }

        Log.ProcessingIdempotentRequest(
            _logger,
            typeof(TRequest).Name,
            messageId,
            context.CorrelationId);

        // Check if message already exists in inbox
        var existingMessage = await _inboxStore.GetMessageAsync(messageId, cancellationToken).ConfigureAwait(false);

        if (existingMessage != null)
        {
            // Message already processed - return cached response
            if (existingMessage.IsProcessed && existingMessage.Response != null)
            {
                Log.ReturningCachedResponse(
                    _logger,
                    messageId,
                    context.CorrelationId);

                return DeserializeResponse(existingMessage.Response);
            }

            // Message exists but failed - retry if within limit
            if (existingMessage.RetryCount >= _options.MaxRetries)
            {
                Log.MaxRetriesExceeded(
                    _logger,
                    messageId,
                    _options.MaxRetries,
                    context.CorrelationId);

                return EncinaErrors.Create(
                    "inbox.max_retries_exceeded",
                    $"Message has failed {existingMessage.RetryCount} times and will not be retried");
            }

            // Increment retry count
            await _inboxStore.MarkAsFailedAsync(
                messageId,
                existingMessage.ErrorMessage ?? "Retry",
                null,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Create new inbox entry
            var now = DateTime.UtcNow;
            var newMessage = new InboxMessage
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
            };

            await _inboxStore.AddAsync(newMessage, cancellationToken).ConfigureAwait(false);
            await _inboxStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Process the request
        try
        {
            var result = await nextStep().ConfigureAwait(false);

            // Store response in inbox
            await _inboxStore.MarkAsProcessedAsync(
                messageId,
                SerializeResponse(result),
                cancellationToken).ConfigureAwait(false);

            await _inboxStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            Log.MessageProcessedSuccessfully(
                _logger,
                messageId,
                context.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            Log.ErrorProcessingMessage(
                _logger,
                ex,
                messageId,
                context.CorrelationId);

            await _inboxStore.MarkAsFailedAsync(
                messageId,
                ex.Message,
                null,
                cancellationToken).ConfigureAwait(false);

            await _inboxStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            throw;
        }
    }

    private static string SerializeResponse(Either<EncinaError, TResponse> response)
    {
        var envelope = response.Match(
            Right: value => new ResponseEnvelope { IsSuccess = true, Value = value },
            Left: error => new ResponseEnvelope { IsSuccess = false, Error = error });

        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    private static Either<EncinaError, TResponse> DeserializeResponse(string json)
    {
        var envelope = JsonSerializer.Deserialize<ResponseEnvelope>(json, JsonOptions);
        if (envelope == null)
            return EncinaErrors.Create("inbox.deserialization_failed", "Failed to deserialize cached response");

        if (envelope.IsSuccess && envelope.Value != null)
        {
            var value = JsonSerializer.Deserialize<TResponse>(
                JsonSerializer.Serialize(envelope.Value, JsonOptions),
                JsonOptions);

            return value != null
                ? Right<EncinaError, TResponse>(value)
                : EncinaErrors.Create("inbox.deserialization_failed", "Failed to deserialize response value");
        }

        return envelope.Error ?? EncinaErrors.Create("inbox.unknown_error", "Unknown error in cached response");
    }

    private sealed class ResponseEnvelope
    {
        public bool IsSuccess { get; set; }
        public object? Value { get; set; }
        public EncinaError? Error { get; set; }
    }
}

/// <summary>
/// Marker interface to indicate that a request should be processed idempotently using the Inbox Pattern.
/// </summary>
public interface IIdempotentRequest
{
}
