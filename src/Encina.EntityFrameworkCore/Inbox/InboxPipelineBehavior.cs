using System.Text.Json;
using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Inbox;

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
/// <para>
/// <b>Message ID Sources</b>:
/// <list type="bullet">
/// <item><description>Queue message ID (RabbitMQ, Azure Service Bus, etc.)</description></item>
/// <item><description>Webhook signature/ID</description></item>
/// <item><description>External system correlation ID</description></item>
/// <item><description>IdempotencyKey header from API requests</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mark command as idempotent
/// public record ProcessPaymentCommand(decimal Amount, string PaymentId)
///     : ICommand&lt;Receipt&gt;, IIdempotentRequest;
///
/// // In ASP.NET Core controller
/// [HttpPost("payments")]
/// public async Task&lt;IActionResult&gt; ProcessPayment(
///     [FromBody] ProcessPaymentCommand command,
///     [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
/// {
///     // MessageId from IdempotencyKey header will be used automatically
///     var result = await _Encina.Send(command);
///
///     return result.Match(
///         Right: receipt => Ok(receipt),
///         Left: error => error.ToActionResult(HttpContext)
///     );
/// }
///
/// // If same idempotency key is sent again, cached receipt is returned
/// </code>
/// </example>
public sealed class InboxPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly DbContext _dbContext;
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
    /// <param name="dbContext">The database context.</param>
    /// <param name="options">The inbox options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/>, <paramref name="options"/>, or <paramref name="logger"/> is null.</exception>
    public InboxPipelineBehavior(
        DbContext dbContext,
        InboxOptions options,
        ILogger<InboxPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
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
            return await nextStep();

        // MessageId is required for idempotent processing
        var messageId = context.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            Log.MissingIdempotencyKey(_logger, typeof(TRequest).Name, context.CorrelationId);

            return EncinaErrors.Create(
                "inbox.missing_message_id",
                "Idempotent requests require a MessageId (IdempotencyKey)");
        }

        Log.ProcessingIdempotentRequest(_logger, typeof(TRequest).Name, messageId, context.CorrelationId);

        // Check if message already exists in inbox
        var existingMessage = await _dbContext.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

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

                return EncinaErrors.Create(
                    "inbox.max_retries_exceeded",
                    $"Message has failed {existingMessage.RetryCount} times and will not be retried");
            }

            // Update retry count
            existingMessage.RetryCount++;
        }
        else
        {
            // Create new inbox entry
            var now = DateTime.UtcNow;
            existingMessage = new InboxMessage
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

            _dbContext.Set<InboxMessage>().Add(existingMessage);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Process the request
        try
        {
            var result = await nextStep();

            // Store response in inbox
            existingMessage.Response = SerializeResponse(result);
            existingMessage.ProcessedAtUtc = DateTime.UtcNow;
            existingMessage.ErrorMessage = null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.ProcessedAndCachedMessage(_logger, messageId, context.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            Log.ErrorProcessingMessage(_logger, ex, messageId, context.CorrelationId);

            existingMessage.ErrorMessage = ex.Message;
            await _dbContext.SaveChangesAsync(cancellationToken);

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
/// <remarks>
/// <para>
/// Requests implementing this interface will be tracked in the inbox to ensure exactly-once processing.
/// The MessageId (from <c>IRequestContext.IdempotencyKey</c>) is used as the deduplication key.
/// </para>
/// <para>
/// <b>Use Cases</b>:
/// <list type="bullet">
/// <item><description>Processing messages from queues (prevent duplicate processing)</description></item>
/// <item><description>Handling webhooks (prevent replay attacks)</description></item>
/// <item><description>API idempotency (support retry-safe operations)</description></item>
/// <item><description>Event sourcing (prevent duplicate event application)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Payment processing must be idempotent
/// public record ProcessPaymentCommand(decimal Amount, string PaymentReference)
///     : ICommand&lt;PaymentResult&gt;, IIdempotentRequest;
///
/// // Webhook processing must be idempotent
/// public record HandleWebhookCommand(string WebhookId, string Payload)
///     : ICommand, IIdempotentRequest;
/// </code>
/// </example>
public interface IIdempotentRequest
{
}
