using System.Diagnostics;
using System.Text;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Diagnostics;
using Encina.Messaging.Encryption.Model;
using Encina.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Messaging.Encryption.Serialization;

/// <summary>
/// Decorator over <see cref="IMessageSerializer"/> that adds transparent payload-level
/// encryption and decryption of serialized message content.
/// </summary>
/// <remarks>
/// <para>
/// Implements the decorator pattern: wraps an inner <see cref="IMessageSerializer"/> (typically
/// <c>JsonMessageSerializer</c>) and intercepts serialization/deserialization to add
/// encryption when required.
/// </para>
/// <para>
/// <strong>Serialize flow</strong>:
/// <list type="number">
///   <item>Delegate to inner serializer to produce JSON string</item>
///   <item>Check if encryption is required (global config or <c>[EncryptedMessage]</c> attribute)</item>
///   <item>If required: encrypt JSON bytes → format as <c>ENC:v1:{keyId}:{algorithm}:...</c></item>
///   <item>Return the encrypted string (or plain JSON if encryption is not required)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Deserialize flow</strong>:
/// <list type="number">
///   <item>Check if content starts with <c>ENC:</c> prefix</item>
///   <item>If encrypted: parse header → decrypt → get JSON string</item>
///   <item>Delegate to inner serializer to deserialize JSON → typed object</item>
/// </list>
/// </para>
/// <para>
/// <strong>Attribute precedence</strong>: <c>[EncryptedMessage(Enabled = false)]</c> overrides
/// <c>MessageEncryptionOptions.EncryptAllMessages = true</c>.
/// </para>
/// <para>
/// This class is thread-safe when the inner serializer and encryption provider are thread-safe.
/// </para>
/// </remarks>
public sealed class EncryptingMessageSerializer : IMessageSerializer
{
    private readonly IMessageSerializer _inner;
    private readonly IMessageEncryptionProvider _provider;
    private readonly IOptions<MessageEncryptionOptions> _options;
    private readonly ILogger<EncryptingMessageSerializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptingMessageSerializer"/> class.
    /// </summary>
    /// <param name="inner">The inner serializer to delegate to for JSON serialization/deserialization.</param>
    /// <param name="provider">The encryption provider for payload encryption/decryption.</param>
    /// <param name="options">The message encryption options.</param>
    /// <param name="logger">The logger for encryption operations.</param>
    public EncryptingMessageSerializer(
        IMessageSerializer inner,
        IMessageEncryptionProvider provider,
        IOptions<MessageEncryptionOptions> options,
        ILogger<EncryptingMessageSerializer> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _provider = provider;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Serializes the message to JSON via the inner serializer, then encrypts the payload
    /// if encryption is required for the message type (via attribute or global configuration).
    /// </remarks>
    public string Serialize<T>(T message)
    {
        var json = _inner.Serialize(message);
        var opts = _options.Value;
        var messageTypeName = typeof(T).Name;

        if (!opts.Enabled)
        {
            _logger.EncryptionSkippedDisabled(messageTypeName);
            return json;
        }

        var messageType = typeof(T);
        if (!ShouldEncrypt(messageType, opts))
        {
            _logger.EncryptionSkippedNotRequired(messageTypeName);
            return json;
        }

        var context = BuildEncryptionContext(messageType, opts);
        var plaintext = Encoding.UTF8.GetBytes(json);

        _logger.EncryptionStarted(messageTypeName, plaintext.Length);

        using var activity = opts.EnableTracing
            ? MessageEncryptionDiagnostics.StartEncrypt(messageTypeName)
            : null;

        var stopwatch = Stopwatch.StartNew();

        // EncryptAsync is async but Serialize is sync — use blocking call.
        // This is acceptable because encryption is a CPU-bound operation (AES-GCM).
        var result = _provider.EncryptAsync(plaintext, context).AsTask().GetAwaiter().GetResult();

        stopwatch.Stop();
        var durationMs = stopwatch.Elapsed.TotalMilliseconds;

        return result.Match(
            Right: payload =>
            {
                var formatted = EncryptedPayloadFormatter.Format(payload);

                _logger.EncryptionCompleted(messageTypeName, payload.KeyId, durationMs);
                MessageEncryptionDiagnostics.RecordSuccess(activity, payload.KeyId, payload.Algorithm);

                if (opts.EnableMetrics)
                {
                    MessageEncryptionDiagnostics.RecordOperationMetrics(
                        "encrypt", messageTypeName, payload.KeyId, durationMs, payload.Ciphertext.Length);
                }

                return formatted;
            },
            Left: error =>
            {
                _logger.EncryptionFailed(messageTypeName, error.Message);
                MessageEncryptionDiagnostics.RecordFailure(activity, error.Message);

                if (opts.EnableMetrics)
                {
                    MessageEncryptionDiagnostics.RecordFailureMetrics("encrypt", messageTypeName);
                }

                throw new InvalidOperationException(
                    $"Message encryption failed for type '{messageTypeName}': {error.Message}");
            });
    }

    /// <inheritdoc />
    /// <remarks>
    /// If the content is an encrypted payload (starts with <c>ENC:</c>), decrypts it first
    /// and then deserializes the resulting JSON via the inner serializer.
    /// </remarks>
    public T? Deserialize<T>(string content)
    {
        var json = DecryptIfNeeded(content);
        return _inner.Deserialize<T>(json);
    }

    /// <inheritdoc />
    /// <remarks>
    /// If the content is an encrypted payload (starts with <c>ENC:</c>), decrypts it first
    /// and then deserializes the resulting JSON via the inner serializer.
    /// </remarks>
    public object? Deserialize(string content, Type type)
    {
        var json = DecryptIfNeeded(content);
        return _inner.Deserialize(json, type);
    }

    private string DecryptIfNeeded(string content)
    {
        if (!EncryptedPayloadFormatter.TryParse(content, out var payload) || payload is null)
        {
            _logger.ContentPassthrough();
            return content;
        }

        var opts = _options.Value;

        _logger.DecryptionStarted(payload.KeyId, payload.Algorithm);

        using var activity = opts.EnableTracing
            ? MessageEncryptionDiagnostics.StartDecrypt(payload.KeyId)
            : null;

        var stopwatch = Stopwatch.StartNew();

        var context = new MessageEncryptionContext
        {
            KeyId = payload.KeyId
        };

        var result = _provider.DecryptAsync(payload, context).AsTask().GetAwaiter().GetResult();

        stopwatch.Stop();
        var durationMs = stopwatch.Elapsed.TotalMilliseconds;

        return result.Match(
            Right: decryptedBytes =>
            {
                var json = Encoding.UTF8.GetString(decryptedBytes.AsSpan());

                _logger.DecryptionCompleted(payload.KeyId, durationMs);
                MessageEncryptionDiagnostics.RecordSuccess(activity, payload.KeyId, payload.Algorithm);

                if (opts.EnableMetrics)
                {
                    MessageEncryptionDiagnostics.RecordOperationMetrics(
                        "decrypt", "unknown", payload.KeyId, durationMs, payload.Ciphertext.Length);
                }

                if (opts.AuditDecryption)
                {
                    _logger.DecryptionAudit(payload.KeyId, "unknown");
                }

                return json;
            },
            Left: error =>
            {
                _logger.DecryptionFailed(payload.KeyId, error.Message);
                MessageEncryptionDiagnostics.RecordFailure(activity, error.Message);

                if (opts.EnableMetrics)
                {
                    MessageEncryptionDiagnostics.RecordFailureMetrics("decrypt", "unknown");
                }

                throw new InvalidOperationException(
                    $"Message decryption failed with key '{payload.KeyId}': {error.Message}");
            });
    }

    private static bool ShouldEncrypt(Type messageType, MessageEncryptionOptions options)
    {
        var info = EncryptedMessageAttributeCache.GetEncryptionInfo(messageType);

        if (info is not null)
        {
            // Attribute overrides global config
            return info.Enabled;
        }

        // No attribute — fall back to global config
        return options.EncryptAllMessages;
    }

    private static MessageEncryptionContext BuildEncryptionContext(
        Type messageType,
        MessageEncryptionOptions options)
    {
        var info = EncryptedMessageAttributeCache.GetEncryptionInfo(messageType);

        var keyId = info?.KeyId ?? options.DefaultKeyId;

        return new MessageEncryptionContext
        {
            KeyId = keyId,
            MessageType = messageType.AssemblyQualifiedName ?? messageType.FullName ?? messageType.Name
        };
    }

}
