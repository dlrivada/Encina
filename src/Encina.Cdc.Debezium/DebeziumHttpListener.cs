using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc.Debezium;

/// <summary>
/// Background service that listens for HTTP POST requests from Debezium Server
/// and writes the received events to a <see cref="Channel{T}"/> for consumption
/// by <see cref="DebeziumCdcConnector"/>.
/// </summary>
/// <remarks>
/// <para>
/// This listener runs an <see cref="HttpListener"/> on the configured URL/port/path
/// and accepts incoming Debezium events. It validates the bearer token if configured
/// and writes valid events to the internal channel.
/// </para>
/// <para>
/// Resilience features:
/// <list type="bullet">
///   <item><description>Retry with exponential backoff on listener start failure</description></item>
///   <item><description>Backpressure via HTTP 503 when the bounded channel is full</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DebeziumHttpListener : BackgroundService
{
    private readonly DebeziumCdcOptions _options;
    private readonly ChannelWriter<JsonElement> _channelWriter;
    private readonly ILogger<DebeziumHttpListener> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebeziumHttpListener"/> class.
    /// </summary>
    /// <param name="options">Debezium CDC options.</param>
    /// <param name="channel">The channel to write received events to.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public DebeziumHttpListener(
        DebeziumCdcOptions options,
        Channel<JsonElement> channel,
        ILogger<DebeziumHttpListener> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _channelWriter = channel.Writer;
        _logger = logger;
    }

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "BackgroundService loop must catch all exceptions to continue processing")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new HttpListener();
        var prefix = $"{_options.ListenUrl}:{_options.ListenPort}{_options.ListenPath}/";
        listener.Prefixes.Add(prefix);

        try
        {
            await StartListenerWithRetryAsync(listener, prefix, stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().WaitAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await HandleRequestAsync(context, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    DebeziumCdcLog.RequestFailed(_logger, ex);
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
            }
        }
        finally
        {
            listener.Stop();
            listener.Close();
            DebeziumCdcLog.ListenerStopped(_logger);
        }
    }

    /// <summary>
    /// Starts the HTTP listener with retry and exponential backoff.
    /// Throws after <see cref="DebeziumCdcOptions.MaxListenerRetries"/> failed attempts.
    /// </summary>
    private async Task StartListenerWithRetryAsync(
        HttpListener listener,
        string prefix,
        CancellationToken stoppingToken)
    {
        var retryCount = 0;

        while (true)
        {
            try
            {
                listener.Start();
                DebeziumCdcLog.ListenerStarted(_logger, prefix);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                retryCount++;

                if (retryCount > _options.MaxListenerRetries)
                {
                    DebeziumCdcLog.ListenerStartFailed(_logger, ex, retryCount);
                    throw;
                }

                var delay = _options.ListenerRetryDelay * Math.Pow(2, retryCount - 1);
                DebeziumCdcLog.ListenerRetrying(_logger, retryCount, _options.MaxListenerRetries, delay);
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        // Only accept POST requests
        if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = 405;
            response.Close();
            return;
        }

        // Validate bearer token if configured
        if (!string.IsNullOrEmpty(_options.BearerToken))
        {
            var authHeader = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) ||
                !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                authHeader["Bearer ".Length..] != _options.BearerToken)
            {
                response.StatusCode = 401;
                response.Close();
                return;
            }
        }

        // Read and parse the event
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(body))
        {
            response.StatusCode = 400;
            response.Close();
            return;
        }

        try
        {
            var eventJson = JsonDocument.Parse(body).RootElement.Clone();

            // Apply backpressure: return 503 when the bounded channel is full
            if (!_channelWriter.TryWrite(eventJson))
            {
                DebeziumCdcLog.ChannelFull(_logger, _options.ChannelCapacity);
                response.StatusCode = 503;
                response.Close();
                return;
            }

            DebeziumCdcLog.EventReceived(_logger);

            response.StatusCode = 200;
        }
        catch (JsonException)
        {
            response.StatusCode = 400;
        }

        response.Close();
    }
}
