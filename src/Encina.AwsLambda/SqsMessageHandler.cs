using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.AwsLambda;

/// <summary>
/// Helper class for processing SQS messages with Encina.
/// </summary>
/// <remarks>
/// <para>
/// This class provides utilities for processing SQS events in AWS Lambda functions,
/// including batch processing with partial failure support and automatic deserialization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderProcessor
/// {
///     private readonly IEncina _encina;
///     private readonly ILogger&lt;OrderProcessor&gt; _logger;
///
///     public async Task&lt;SQSBatchResponse&gt; ProcessOrders(SQSEvent sqsEvent, ILambdaContext context)
///     {
///         return await SqsMessageHandler.ProcessBatchAsync(
///             sqsEvent,
///             async record =&gt;
///             {
///                 var command = JsonSerializer.Deserialize&lt;ProcessOrder&gt;(record.Body);
///                 return await _encina.Send(command!);
///             },
///             _logger);
///     }
/// }
/// </code>
/// </example>
public static class SqsMessageHandler
{
    private const string SqsDeserializationFailedCode = "sqs.deserialization_failed";

    /// <summary>
    /// Processes SQS messages in a batch, returning partial failure responses.
    /// </summary>
    /// <typeparam name="T">The result type from processing each message.</typeparam>
    /// <param name="sqsEvent">The SQS event containing messages to process.</param>
    /// <param name="processMessage">Function to process each message record.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch response with any failed message identifiers.</returns>
    public static async Task<SQSBatchResponse> ProcessBatchAsync<T>(
        SQSEvent sqsEvent,
        Func<SQSEvent.SQSMessage, Task<Either<EncinaError, T>>> processMessage,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sqsEvent);
        ArgumentNullException.ThrowIfNull(processMessage);

        var batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in sqsEvent.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await processMessage(record);

                result.IfLeft(error =>
                {
                    logger?.LogError(
                        "Failed to process SQS message {MessageId}: {ErrorMessage}",
                        record.MessageId,
                        error.Message);

                    batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure
                    {
                        ItemIdentifier = record.MessageId
                    });
                });
            }
            catch (Exception ex)
            {
                logger?.LogError(
                    ex,
                    "Exception while processing SQS message {MessageId}",
                    record.MessageId);

                batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = record.MessageId
                });
            }
        }

        return new SQSBatchResponse
        {
            BatchItemFailures = batchItemFailures
        };
    }

    /// <summary>
    /// Processes SQS messages in a batch with automatic deserialization.
    /// </summary>
    /// <typeparam name="TMessage">The message type to deserialize.</typeparam>
    /// <typeparam name="TResult">The result type from processing.</typeparam>
    /// <param name="sqsEvent">The SQS event containing messages to process.</param>
    /// <param name="processMessage">Function to process each deserialized message.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch response with any failed message identifiers.</returns>
    public static async Task<SQSBatchResponse> ProcessBatchAsync<TMessage, TResult>(
        SQSEvent sqsEvent,
        Func<TMessage, Task<Either<EncinaError, TResult>>> processMessage,
        ILogger? logger = null,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sqsEvent);
        ArgumentNullException.ThrowIfNull(processMessage);

        return await ProcessBatchAsync(
            sqsEvent,
            async record =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<TMessage>(record.Body, jsonOptions);
                    if (message is null)
                    {
                        return EncinaErrors.Create(SqsDeserializationFailedCode, "Failed to deserialize SQS message body");
                    }

                    return await processMessage(message);
                }
                catch (JsonException ex)
                {
                    logger?.LogError(ex, "Failed to deserialize SQS message {MessageId}", record.MessageId);
                    return EncinaErrors.Create(SqsDeserializationFailedCode, $"JSON deserialization failed: {ex.Message}", ex);
                }
            },
            logger,
            cancellationToken);
    }

    /// <summary>
    /// Processes SQS messages sequentially, stopping on first error.
    /// </summary>
    /// <typeparam name="T">The result type from processing each message.</typeparam>
    /// <param name="sqsEvent">The SQS event containing messages to process.</param>
    /// <param name="processMessage">Function to process each message record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success, or the first error encountered.</returns>
    public static async Task<Either<EncinaError, Unit>> ProcessAllAsync<T>(
        SQSEvent sqsEvent,
        Func<SQSEvent.SQSMessage, Task<Either<EncinaError, T>>> processMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sqsEvent);
        ArgumentNullException.ThrowIfNull(processMessage);

        foreach (var record in sqsEvent.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await processMessage(record);
            if (result.IsLeft)
            {
                return result.Match<Either<EncinaError, Unit>>(
                    Right: _ => Unit.Default,
                    Left: error => error);
            }
        }

        return Unit.Default;
    }

    /// <summary>
    /// Deserializes an SQS message body to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="record">The SQS message record.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    /// <returns>The deserialized message or an error.</returns>
    public static Either<EncinaError, T> DeserializeMessage<T>(
        SQSEvent.SQSMessage record,
        JsonSerializerOptions? jsonOptions = null)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            var message = JsonSerializer.Deserialize<T>(record.Body, jsonOptions);
            if (message is null)
            {
                return EncinaErrors.Create(SqsDeserializationFailedCode, "Deserialized message was null");
            }

            return message;
        }
        catch (JsonException ex)
        {
            return EncinaErrors.Create(SqsDeserializationFailedCode, $"JSON deserialization failed: {ex.Message}", ex);
        }
    }
}
