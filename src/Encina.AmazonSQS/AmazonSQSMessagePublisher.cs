using System.Globalization;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;
using SnsMessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;
using SqsMessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

namespace Encina.AmazonSQS;

/// <summary>
/// Amazon SQS/SNS-based implementation of the message publisher.
/// </summary>
public sealed class AmazonSQSMessagePublisher : IAmazonSQSMessagePublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILogger<AmazonSQSMessagePublisher> _logger;
    private readonly EncinaAmazonSQSOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonSQSMessagePublisher"/> class.
    /// </summary>
    /// <param name="sqsClient">The Amazon SQS client.</param>
    /// <param name="snsClient">The Amazon SNS client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public AmazonSQSMessagePublisher(
        IAmazonSQS sqsClient,
        IAmazonSimpleNotificationService snsClient,
        ILogger<AmazonSQSMessagePublisher> logger,
        IOptions<EncinaAmazonSQSOptions> options)
    {
        ArgumentNullException.ThrowIfNull(sqsClient);
        ArgumentNullException.ThrowIfNull(snsClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _sqsClient = sqsClient;
        _snsClient = snsClient;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> SendToQueueAsync<TMessage>(
        TMessage message,
        string? queueUrl = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveQueueUrl = queueUrl ?? _options.DefaultQueueUrl;

        if (string.IsNullOrEmpty(effectiveQueueUrl))
        {
            return Left<EncinaError, string>(
                EncinaErrors.Create(
                    "SQS_QUEUE_NOT_CONFIGURED",
                    "Queue URL is not configured."));
        }

        try
        {
            Log.SendingToQueue(_logger, typeof(TMessage).Name, effectiveQueueUrl);

            var request = new SendMessageRequest
            {
                QueueUrl = effectiveQueueUrl,
                MessageBody = JsonSerializer.Serialize(message),
                MessageAttributes = new Dictionary<string, SqsMessageAttributeValue>
                {
                    ["MessageType"] = new()
                    {
                        DataType = "String",
                        StringValue = typeof(TMessage).FullName
                    }
                }
            };

            var response = await _sqsClient.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);

            Log.SuccessfullySentMessage(_logger, typeof(TMessage).Name, response.MessageId);

            return Right<EncinaError, string>(response.MessageId);
        }
        catch (Exception ex)
        {
            Log.FailedToSendToQueue(_logger, ex, typeof(TMessage).Name, effectiveQueueUrl);

            return Left<EncinaError, string>(
                EncinaErrors.FromException(
                    "SQS_SEND_FAILED",
                    ex,
                    $"Failed to send message of type {typeof(TMessage).Name} to queue."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> PublishToTopicAsync<TMessage>(
        TMessage message,
        string? topicArn = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveTopicArn = topicArn ?? _options.DefaultTopicArn;

        if (string.IsNullOrEmpty(effectiveTopicArn))
        {
            return Left<EncinaError, string>(
                EncinaErrors.Create(
                    "SNS_TOPIC_NOT_CONFIGURED",
                    "Topic ARN is not configured."));
        }

        try
        {
            Log.PublishingToTopic(_logger, typeof(TMessage).Name, effectiveTopicArn);

            var request = new PublishRequest
            {
                TopicArn = effectiveTopicArn,
                Message = JsonSerializer.Serialize(message),
                MessageAttributes = new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>
                {
                    ["MessageType"] = new()
                    {
                        DataType = "String",
                        StringValue = typeof(TMessage).FullName
                    }
                }
            };

            var response = await _snsClient.PublishAsync(request, cancellationToken).ConfigureAwait(false);

            Log.SuccessfullyPublishedMessage(_logger, typeof(TMessage).Name, response.MessageId);

            return Right<EncinaError, string>(response.MessageId);
        }
        catch (Exception ex)
        {
            Log.FailedToPublishToTopic(_logger, ex, typeof(TMessage).Name, effectiveTopicArn);

            return Left<EncinaError, string>(
                EncinaErrors.FromException(
                    "SNS_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name} to topic."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<string>>> SendBatchAsync<TMessage>(
        IEnumerable<TMessage> messages,
        string? queueUrl = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(messages);

        var effectiveQueueUrl = queueUrl ?? _options.DefaultQueueUrl;

        if (string.IsNullOrEmpty(effectiveQueueUrl))
        {
            return Left<EncinaError, IReadOnlyList<string>>(
                EncinaErrors.Create(
                    "SQS_QUEUE_NOT_CONFIGURED",
                    "Queue URL is not configured."));
        }

        try
        {
            var entries = messages.Select((m, i) => new SendMessageBatchRequestEntry
            {
                Id = i.ToString(CultureInfo.InvariantCulture),
                MessageBody = JsonSerializer.Serialize(m),
                MessageAttributes = new Dictionary<string, SqsMessageAttributeValue>
                {
                    ["MessageType"] = new()
                    {
                        DataType = "String",
                        StringValue = typeof(TMessage).FullName
                    }
                }
            }).ToList();

            Log.SendingBatch(_logger, entries.Count, typeof(TMessage).Name);

            var request = new SendMessageBatchRequest
            {
                QueueUrl = effectiveQueueUrl,
                Entries = entries
            };

            var response = await _sqsClient.SendMessageBatchAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.Failed.Count > 0)
            {
                Log.BatchPartiallyFailed(_logger, response.Failed.Count, entries.Count);
            }

            var messageIds = response.Successful.Select(s => s.MessageId).ToList();

            Log.SuccessfullySentBatch(_logger, messageIds.Count);

            return Right<EncinaError, IReadOnlyList<string>>(messageIds);
        }
        catch (Exception ex)
        {
            Log.FailedToSendBatch(_logger, ex, typeof(TMessage).Name);

            return Left<EncinaError, IReadOnlyList<string>>(
                EncinaErrors.FromException(
                    "SQS_BATCH_SEND_FAILED",
                    ex,
                    $"Failed to send batch of messages of type {typeof(TMessage).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> SendToFifoQueueAsync<TMessage>(
        TMessage message,
        string messageGroupId,
        string? deduplicationId = null,
        string? queueUrl = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageGroupId);

        var effectiveQueueUrl = queueUrl ?? _options.DefaultQueueUrl;

        if (string.IsNullOrEmpty(effectiveQueueUrl))
        {
            return Left<EncinaError, string>(
                EncinaErrors.Create(
                    "SQS_QUEUE_NOT_CONFIGURED",
                    "Queue URL is not configured."));
        }

        try
        {
            Log.SendingFifoMessage(_logger, typeof(TMessage).Name, effectiveQueueUrl, messageGroupId);

            var request = new SendMessageRequest
            {
                QueueUrl = effectiveQueueUrl,
                MessageBody = JsonSerializer.Serialize(message),
                MessageGroupId = messageGroupId,
                MessageDeduplicationId = deduplicationId,
                MessageAttributes = new Dictionary<string, SqsMessageAttributeValue>
                {
                    ["MessageType"] = new()
                    {
                        DataType = "String",
                        StringValue = typeof(TMessage).FullName
                    }
                }
            };

            var response = await _sqsClient.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);

            Log.SuccessfullySentFifoMessage(_logger, response.MessageId);

            return Right<EncinaError, string>(response.MessageId);
        }
        catch (Exception ex)
        {
            Log.FailedToSendFifoMessage(_logger, ex, typeof(TMessage).Name);

            return Left<EncinaError, string>(
                EncinaErrors.FromException(
                    "SQS_FIFO_SEND_FAILED",
                    ex,
                    $"Failed to send FIFO message of type {typeof(TMessage).Name}."));
        }
    }
}
