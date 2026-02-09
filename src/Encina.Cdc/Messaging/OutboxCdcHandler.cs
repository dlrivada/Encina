using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Messaging;

/// <summary>
/// Specialized CDC handler that watches the OutboxMessages table and republishes
/// the original notifications via <see cref="IEncina.Publish{TNotification}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This handler provides a CDC-driven alternative to polling-based outbox processing.
/// When enabled via <see cref="CdcConfiguration.UseOutboxCdc"/>, it monitors the
/// outbox table for new inserts and immediately publishes the stored notifications.
/// </para>
/// <para>
/// The handler extracts the <c>NotificationType</c> and <c>Content</c> fields from
/// the outbox row, deserializes the original notification, and publishes it through
/// Encina's standard notification pipeline.
/// </para>
/// <para>
/// Already-processed messages (where <c>ProcessedAtUtc</c> is set) are skipped
/// to avoid duplicate publishing when used alongside a traditional <c>OutboxProcessor</c>.
/// </para>
/// </remarks>
internal sealed class OutboxCdcHandler : IChangeEventHandler<JsonElement>
{
    private readonly IEncina _encina;
    private readonly ILogger<OutboxCdcHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxCdcHandler"/> class.
    /// </summary>
    /// <param name="encina">The Encina coordinator for publishing notifications.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public OutboxCdcHandler(
        IEncina encina,
        ILogger<OutboxCdcHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
        JsonElement entity, ChangeContext context)
    {
        return await ProcessOutboxRowAsync(entity, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(
        JsonElement before, JsonElement after, ChangeContext context)
    {
        // Updates to outbox rows are not processed (e.g., MarkAsProcessed, MarkAsFailed)
        return new(Right(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(
        JsonElement entity, ChangeContext context)
    {
        // Deletions from outbox are not processed
        return new(Right(unit));
    }

    private async ValueTask<Either<EncinaError, Unit>> ProcessOutboxRowAsync(
        JsonElement row, ChangeContext context)
    {
        // Skip if already processed (avoid duplicate publishing)
        if (row.TryGetProperty("processedAtUtc", out var processedAt) &&
            processedAt.ValueKind != JsonValueKind.Null)
        {
            CdcMessagingLog.OutboxCdcSkippedAlreadyProcessed(_logger);
            return Right(unit);
        }

        // Also check PascalCase variant for non-camelCase stores
        if (row.TryGetProperty("ProcessedAtUtc", out var processedAtPascal) &&
            processedAtPascal.ValueKind != JsonValueKind.Null)
        {
            CdcMessagingLog.OutboxCdcSkippedAlreadyProcessed(_logger);
            return Right(unit);
        }

        // Extract notification type and content
        var notificationType = GetStringProperty(row, "notificationType", "NotificationType");
        var content = GetStringProperty(row, "content", "Content");

        if (string.IsNullOrWhiteSpace(notificationType) || string.IsNullOrWhiteSpace(content))
        {
            return Left(CdcErrors.DeserializationFailed(
                context.TableName,
                typeof(JsonElement),
                new InvalidOperationException(
                    "OutboxMessage row is missing required 'NotificationType' or 'Content' fields")));
        }

        CdcMessagingLog.OutboxCdcProcessing(_logger, notificationType);

        // Resolve the notification type
        var type = Type.GetType(notificationType);
        if (type is null)
        {
            CdcMessagingLog.OutboxCdcDeserializationFailed(_logger, notificationType);
            return Left(CdcErrors.DeserializationFailed(
                context.TableName,
                typeof(JsonElement),
                new InvalidOperationException($"Unknown notification type: {notificationType}")));
        }

        // Deserialize the notification
        object? notification;
        try
        {
            notification = JsonSerializer.Deserialize(content, type, JsonOptions);
        }
        catch (JsonException ex)
        {
            CdcMessagingLog.OutboxCdcDeserializationFailed(_logger, notificationType);
            return Left(CdcErrors.DeserializationFailed(context.TableName, type, ex));
        }

        if (notification is not INotification typedNotification)
        {
            CdcMessagingLog.OutboxCdcDeserializationFailed(_logger, notificationType);
            return Left(CdcErrors.DeserializationFailed(
                context.TableName,
                type,
                new InvalidOperationException(
                    $"Deserialized object of type '{notificationType}' does not implement INotification")));
        }

        // Publish the original notification via IEncina
        var result = await PublishNotificationAsync(typedNotification, context.CancellationToken)
            .ConfigureAwait(false);

        if (result.IsRight)
        {
            CdcMessagingLog.OutboxCdcPublished(_logger, notificationType);
        }

        return result;
    }

    private async ValueTask<Either<EncinaError, Unit>> PublishNotificationAsync(
        INotification notification,
        CancellationToken cancellationToken)
    {
        // Use reflection to call the generic Publish<TNotification> method
        // since we only have the runtime type
        var publishMethod = typeof(IEncina)
            .GetMethod(nameof(IEncina.Publish))!
            .MakeGenericMethod(notification.GetType());

        var task = (ValueTask<Either<EncinaError, Unit>>)publishMethod.Invoke(
            _encina, [notification, cancellationToken])!;

        return await task.ConfigureAwait(false);
    }

    private static string? GetStringProperty(JsonElement element, string camelCase, string pascalCase)
    {
        if (element.TryGetProperty(camelCase, out var camelValue) &&
            camelValue.ValueKind == JsonValueKind.String)
        {
            return camelValue.GetString();
        }

        if (element.TryGetProperty(pascalCase, out var pascalValue) &&
            pascalValue.ValueKind == JsonValueKind.String)
        {
            return pascalValue.GetString();
        }

        return null;
    }
}
