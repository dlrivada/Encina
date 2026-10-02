using System.Text.Json.Serialization;

namespace Encina.Cdc.Debezium;

/// <summary>
/// Configuration options for the Debezium HTTP Consumer CDC connector.
/// </summary>
/// <remarks>
/// <para>
/// This connector receives change events from Debezium Server's HTTP Client sink.
/// Debezium Server runs as a separate process (Java) and pushes events via HTTP POST.
/// No Java dependency is required in the .NET application.
/// </para>
/// <para>
/// Configure Debezium Server's HTTP sink to point to this connector's listener:
/// <code>
/// debezium.sink.type=http
/// debezium.sink.http.url=http://your-app:8080/debezium
/// </code>
/// </para>
/// </remarks>
public sealed class DebeziumCdcOptions
{
    /// <summary>
    /// Gets or sets the scheme and host the HTTP listener binds to for HTTP POST events from Debezium Server.
    /// </summary>
    /// <remarks>
    /// This is an inbound <see cref="System.Net.HttpListener"/> prefix without port or path, such as
    /// <c>http://+</c> (all interfaces, the default), <c>http://*</c>, <c>http://localhost</c> or
    /// <c>https://cdc.example.com</c>. It is validated for format and an <c>http</c>/<c>https</c>
    /// scheme only: it is not an outbound target, so SSRF rules do not apply.
    /// </remarks>
    public string ListenUrl { get; set; } = "http://+";

    /// <summary>
    /// Gets or sets the port to listen on for HTTP POST events (1-65535).
    /// </summary>
    public int ListenPort { get; set; } = 8080;

    /// <summary>
    /// Gets or sets the HTTP path to listen on for events. Must start with <c>/</c>.
    /// </summary>
    public string ListenPath { get; set; } = "/debezium";

    /// <summary>
    /// Gets or sets the optional bearer token for authenticating incoming requests.
    /// When set, all incoming HTTP POST requests must include this token.
    /// </summary>
    /// <remarks>
    /// WARNING: Contains sensitive credential data. Never log or serialize.
    /// </remarks>
    [JsonIgnore]
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the expected event format from Debezium Server.
    /// </summary>
    public DebeziumEventFormat EventFormat { get; set; } = DebeziumEventFormat.CloudEvents;

    /// <summary>
    /// Gets or sets the maximum number of events to buffer in the internal channel.
    /// When the channel is full, the HTTP listener returns 503 (Service Unavailable)
    /// to apply backpressure to Debezium Server.
    /// </summary>
    public int ChannelCapacity { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of retries when the HTTP listener fails to start.
    /// Uses exponential backoff between retries.
    /// </summary>
    public int MaxListenerRetries { get; set; } = 5;

    /// <summary>
    /// Gets or sets the base delay between listener start retries.
    /// Exponential backoff is applied: delay * 2^(attempt-1).
    /// </summary>
    public TimeSpan ListenerRetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <inheritdoc/>
    public override string ToString() =>
        $"DebeziumCdcOptions {{ Url={ListenUrl}:{ListenPort}{ListenPath}, Format={EventFormat} }}";
}
