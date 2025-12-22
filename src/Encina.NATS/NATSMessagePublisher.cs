using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using static LanguageExt.Prelude;

namespace Encina.NATS;

/// <summary>
/// NATS-based implementation of the message publisher.
/// </summary>
public sealed class NATSMessagePublisher : INATSMessagePublisher, IAsyncDisposable
{
    private readonly INatsConnection _connection;
    private readonly INatsJSContext? _jetStream;
    private readonly ILogger<NATSMessagePublisher> _logger;
    private readonly EncinaNATSOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NATSMessagePublisher"/> class.
    /// </summary>
    /// <param name="connection">The NATS connection.</param>
    /// <param name="jetStream">The JetStream context (optional).</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public NATSMessagePublisher(
        INatsConnection connection,
        INatsJSContext? jetStream,
        ILogger<NATSMessagePublisher> logger,
        IOptions<EncinaNATSOptions> options)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _connection = connection;
        _jetStream = jetStream;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        string? subject = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveSubject = subject ?? $"{_options.SubjectPrefix}.{typeof(TMessage).Name}";

        try
        {
            Log.PublishingMessage(_logger, typeof(TMessage).Name, effectiveSubject);

            var data = JsonSerializer.SerializeToUtf8Bytes(message);

            await _connection.PublishAsync(
                effectiveSubject,
                data,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            Log.SuccessfullyPublishedMessage(_logger, effectiveSubject);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToPublishMessage(_logger, ex, typeof(TMessage).Name, effectiveSubject);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    "NATS_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name} to subject {effectiveSubject}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> RequestAsync<TRequest, TResponse>(
        TRequest request,
        string? subject = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveSubject = subject ?? $"{_options.SubjectPrefix}.{typeof(TRequest).Name}";
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);

        try
        {
            Log.SendingRequest(_logger, typeof(TRequest).Name, effectiveSubject);

            var data = JsonSerializer.SerializeToUtf8Bytes(request);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(effectiveTimeout);

            var reply = await _connection.RequestAsync<byte[], byte[]>(
                effectiveSubject,
                data,
                cancellationToken: cts.Token).ConfigureAwait(false);

            var response = JsonSerializer.Deserialize<TResponse>(reply.Data);

            if (response is null)
            {
                return Left<EncinaError, TResponse>(
                    EncinaErrors.Create(
                        "NATS_DESERIALIZE_FAILED",
                        "Failed to deserialize response."));
            }

            Log.SuccessfullyReceivedResponse(_logger, typeof(TRequest).Name);

            return Right<EncinaError, TResponse>(response);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, TResponse>(
                EncinaErrors.Create(
                    "NATS_REQUEST_TIMEOUT",
                    $"Request timed out after {effectiveTimeout.TotalSeconds} seconds."));
        }
        catch (Exception ex)
        {
            Log.FailedToSendRequest(_logger, ex, typeof(TRequest).Name);

            return Left<EncinaError, TResponse>(
                EncinaErrors.FromException(
                    "NATS_REQUEST_FAILED",
                    ex,
                    $"Failed to send request of type {typeof(TRequest).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, NATSPublishAck>> JetStreamPublishAsync<TMessage>(
        TMessage message,
        string? subject = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_jetStream is null)
        {
            return Left<EncinaError, NATSPublishAck>(
                EncinaErrors.Create(
                    "NATS_JETSTREAM_NOT_ENABLED",
                    "JetStream is not enabled. Set UseJetStream = true in options."));
        }

        var effectiveSubject = subject ?? $"{_options.SubjectPrefix}.{typeof(TMessage).Name}";

        try
        {
            Log.PublishingToJetStream(_logger, typeof(TMessage).Name, effectiveSubject);

            var data = JsonSerializer.SerializeToUtf8Bytes(message);

            var ack = await _jetStream.PublishAsync(
                effectiveSubject,
                data,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            Log.SuccessfullyPublishedToJetStream(_logger, ack.Stream ?? string.Empty, ack.Seq);

            return Right<EncinaError, NATSPublishAck>(
                new NATSPublishAck(ack.Stream ?? string.Empty, ack.Seq, ack.Duplicate));
        }
        catch (Exception ex)
        {
            Log.FailedToPublishToJetStream(_logger, ex, typeof(TMessage).Name);

            return Left<EncinaError, NATSPublishAck>(
                EncinaErrors.FromException(
                    "NATS_JETSTREAM_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name} to JetStream."));
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
