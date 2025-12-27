using System.Text.Json;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.AwsLambda;

/// <summary>
/// Helper class for processing EventBridge (CloudWatch Events) with Encina.
/// </summary>
/// <remarks>
/// <para>
/// This class provides utilities for processing EventBridge events in AWS Lambda functions,
/// including automatic deserialization and error handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderEventHandler
/// {
///     private readonly IEncina _encina;
///
///     public async Task HandleOrderEvent(
///         CloudWatchEvent&lt;OrderCreatedEvent&gt; eventBridgeEvent,
///         ILambdaContext context)
///     {
///         var result = await EventBridgeHandler.ProcessAsync(
///             eventBridgeEvent,
///             async detail =&gt; await _encina.Publish(new OrderCreatedNotification(detail)));
///
///         result.IfLeft(error =&gt; context.Logger.LogError($"Failed: {error.Message}"));
///     }
/// }
/// </code>
/// </example>
public static class EventBridgeHandler
{
    /// <summary>
    /// Processes an EventBridge event with the specified handler.
    /// </summary>
    /// <typeparam name="TDetail">The event detail type.</typeparam>
    /// <typeparam name="TResult">The result type from processing.</typeparam>
    /// <param name="eventBridgeEvent">The EventBridge event.</param>
    /// <param name="processEvent">Function to process the event detail.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <returns>The result of processing or an error.</returns>
    public static async Task<Either<EncinaError, TResult>> ProcessAsync<TDetail, TResult>(
        CloudWatchEvent<TDetail> eventBridgeEvent,
        Func<TDetail, Task<Either<EncinaError, TResult>>> processEvent,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(eventBridgeEvent);
        ArgumentNullException.ThrowIfNull(processEvent);

        try
        {
            if (eventBridgeEvent.Detail is null)
            {
                var error = EncinaErrors.Create("eventbridge.detail_null", "EventBridge event detail is null");
                logger?.LogError("EventBridge event {Id} has null detail", eventBridgeEvent.Id);
                return error;
            }

            var result = await processEvent(eventBridgeEvent.Detail);

            result.IfLeft(error =>
            {
                logger?.LogError(
                    "Failed to process EventBridge event {Id} from source {Source}: {ErrorMessage}",
                    eventBridgeEvent.Id,
                    eventBridgeEvent.Source,
                    error.Message);
            });

            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Exception while processing EventBridge event {Id} from source {Source}",
                eventBridgeEvent.Id,
                eventBridgeEvent.Source);

            return EncinaErrors.Create(
                "eventbridge.processing_failed",
                $"Failed to process EventBridge event: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Processes an EventBridge event and returns Unit on success.
    /// </summary>
    /// <typeparam name="TDetail">The event detail type.</typeparam>
    /// <param name="eventBridgeEvent">The EventBridge event.</param>
    /// <param name="processEvent">Function to process the event detail.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <returns>Unit on success or an error.</returns>
    public static async Task<Either<EncinaError, Unit>> ProcessAsync<TDetail>(
        CloudWatchEvent<TDetail> eventBridgeEvent,
        Func<TDetail, Task<Either<EncinaError, Unit>>> processEvent,
        ILogger? logger = null)
    {
        return await ProcessAsync<TDetail, Unit>(eventBridgeEvent, processEvent, logger);
    }

    /// <summary>
    /// Processes a raw EventBridge event (JSON string) with automatic deserialization.
    /// </summary>
    /// <typeparam name="TDetail">The event detail type.</typeparam>
    /// <typeparam name="TResult">The result type from processing.</typeparam>
    /// <param name="eventJson">The raw JSON event string.</param>
    /// <param name="processEvent">Function to process the event detail.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    /// <returns>The result of processing or an error.</returns>
    public static async Task<Either<EncinaError, TResult>> ProcessRawAsync<TDetail, TResult>(
        string eventJson,
        Func<TDetail, Task<Either<EncinaError, TResult>>> processEvent,
        ILogger? logger = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        ArgumentNullException.ThrowIfNull(eventJson);
        ArgumentNullException.ThrowIfNull(processEvent);

        try
        {
            var eventBridgeEvent = JsonSerializer.Deserialize<CloudWatchEvent<TDetail>>(eventJson, jsonOptions);
            if (eventBridgeEvent is null)
            {
                return EncinaErrors.Create("eventbridge.deserialization_failed", "Failed to deserialize EventBridge event");
            }

            return await ProcessAsync(eventBridgeEvent, processEvent, logger);
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex, "Failed to deserialize EventBridge event JSON");
            return EncinaErrors.Create("eventbridge.deserialization_failed", $"JSON deserialization failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extracts metadata from an EventBridge event.
    /// </summary>
    /// <typeparam name="TDetail">The event detail type.</typeparam>
    /// <param name="eventBridgeEvent">The EventBridge event.</param>
    /// <returns>The event metadata.</returns>
    public static EventBridgeMetadata GetMetadata<TDetail>(CloudWatchEvent<TDetail> eventBridgeEvent)
    {
        ArgumentNullException.ThrowIfNull(eventBridgeEvent);

        return new EventBridgeMetadata
        {
            Id = eventBridgeEvent.Id,
            Source = eventBridgeEvent.Source,
            DetailType = eventBridgeEvent.DetailType,
            Account = eventBridgeEvent.Account,
            Region = eventBridgeEvent.Region,
            Time = eventBridgeEvent.Time
        };
    }
}

/// <summary>
/// Metadata extracted from an EventBridge event.
/// </summary>
public sealed class EventBridgeMetadata
{
    /// <summary>
    /// Gets or sets the unique event ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source (e.g., "aws.ec2", "custom.myapp").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event detail type.
    /// </summary>
    public string DetailType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS account ID.
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTime Time { get; set; }
}
