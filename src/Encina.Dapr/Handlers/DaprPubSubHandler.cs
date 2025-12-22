using Dapr;
using Dapr.Client;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Dapr;

/// <summary>
/// Generic handler for Dapr Pub/Sub publish requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <remarks>
/// This handler:
/// - Uses DaprClient to publish events to Dapr Pub/Sub components
/// - Returns Unit for successful void operations (Railway Oriented Programming)
/// - Handles DaprException and converts to EncinaError
/// - Supports any Dapr Pub/Sub component (Redis, RabbitMQ, Kafka, Azure Service Bus, etc.)
/// </remarks>
public sealed partial class DaprPubSubHandler<TRequest>
    : IRequestHandler<TRequest, Unit>
    where TRequest : IDaprPubSubRequest
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprPubSubHandler<TRequest>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DaprPubSubHandler{TRequest}"/> class.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public DaprPubSubHandler(
        DaprClient daprClient,
        ILogger<DaprPubSubHandler<TRequest>> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles the Dapr Pub/Sub publish request.
    /// </summary>
    public async Task<Either<EncinaError, Unit>> Handle(
        TRequest request,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;

        try
        {
            LogPublishingEvent(
                requestType,
                request.PubSubName,
                request.TopicName);

            await request.PublishAsync(_daprClient, cancellationToken)
                .ConfigureAwait(false);

            LogEventPublished(
                requestType,
                request.PubSubName,
                request.TopicName);

            return Unit.Default;
        }
        catch (DaprException daprEx)
        {
            // Dapr-specific errors (component not found, configuration errors, etc.)
            LogDaprException(
                requestType,
                request.PubSubName,
                request.TopicName,
                daprEx.Message);

            return EncinaError.New(
                $"Dapr Pub/Sub publish failed for {request.PubSubName}/{request.TopicName}: {daprEx.Message}",
                daprEx);
        }
        catch (TaskCanceledException tcEx) when (tcEx.CancellationToken == cancellationToken)
        {
            // Request was cancelled by caller
            LogRequestCancelled(
                requestType,
                request.PubSubName,
                request.TopicName);

            return EncinaError.New(
                $"Pub/Sub publish to {request.PubSubName}/{request.TopicName} was cancelled",
                tcEx);
        }
        catch (TaskCanceledException tcEx)
        {
            // Timeout
            LogRequestTimedOut(
                requestType,
                request.PubSubName,
                request.TopicName);

            return EncinaError.New(
                $"Pub/Sub publish to {request.PubSubName}/{request.TopicName} timed out",
                tcEx);
        }
        catch (Exception ex)
        {
            LogUnexpectedException(
                requestType,
                request.PubSubName,
                request.TopicName,
                ex.Message);

            return EncinaError.New(ex);
        }
    }

    #region Logging

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Publishing event: {RequestType} -> {PubSubName}/{TopicName}")]
    private partial void LogPublishingEvent(
        string requestType,
        string pubSubName,
        string topicName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Event published successfully: {RequestType} -> {PubSubName}/{TopicName}")]
    private partial void LogEventPublished(
        string requestType,
        string pubSubName,
        string topicName);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Dapr exception publishing {RequestType} -> {PubSubName}/{TopicName}: {Message}")]
    private partial void LogDaprException(
        string requestType,
        string pubSubName,
        string topicName,
        string message);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Event publish cancelled: {RequestType} -> {PubSubName}/{TopicName}")]
    private partial void LogRequestCancelled(
        string requestType,
        string pubSubName,
        string topicName);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Event publish timed out: {RequestType} -> {PubSubName}/{TopicName}")]
    private partial void LogRequestTimedOut(
        string requestType,
        string pubSubName,
        string topicName);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Unexpected exception publishing {RequestType} -> {PubSubName}/{TopicName}: {Message}")]
    private partial void LogUnexpectedException(
        string requestType,
        string pubSubName,
        string topicName,
        string message);

    #endregion
}
