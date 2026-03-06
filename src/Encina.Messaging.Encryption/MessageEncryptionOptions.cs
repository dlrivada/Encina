namespace Encina.Messaging.Encryption;

/// <summary>
/// Configuration options for message payload encryption in the outbox/inbox pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <c>EncryptingMessageSerializer</c> and related
/// encryption components. Configure via dependency injection:
/// </para>
/// <code>
/// services.AddEncinaMessageEncryption(options =>
/// {
///     options.EncryptAllMessages = true;
///     options.DefaultKeyId = "msg-key-2024";
/// });
/// </code>
/// <para>
/// <strong>Precedence</strong>: The <see cref="Attributes.EncryptedMessageAttribute"/> on individual
/// message types overrides these global settings. For example,
/// <c>[EncryptedMessage(Enabled = false)]</c> disables encryption for that type even when
/// <see cref="EncryptAllMessages"/> is <c>true</c>.
/// </para>
/// </remarks>
public sealed class MessageEncryptionOptions
{
    /// <summary>
    /// Gets or sets whether message encryption is enabled globally.
    /// </summary>
    /// <remarks>
    /// When <c>false</c>, the encryption infrastructure is registered but inactive.
    /// Defaults to <c>true</c>.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether all outbox/inbox messages should be encrypted by default.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, every message is encrypted unless explicitly excluded via
    /// <c>[EncryptedMessage(Enabled = false)]</c>.
    /// When <c>false</c> (default), only messages decorated with <c>[EncryptedMessage]</c>
    /// are encrypted.
    /// </remarks>
    public bool EncryptAllMessages { get; set; }

    /// <summary>
    /// Gets or sets the default encryption key identifier used when no key is specified
    /// per message type or per tenant.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the current active key from
    /// <see cref="Security.Encryption.Abstractions.IKeyProvider.GetCurrentKeyIdAsync"/>
    /// is used.
    /// </remarks>
    public string? DefaultKeyId { get; set; }

    /// <summary>
    /// Gets or sets whether to use tenant-specific encryption keys by default.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, <see cref="Abstractions.ITenantKeyResolver"/> resolves a per-tenant
    /// key ID for each message. Can be overridden per message type via
    /// <c>[EncryptedMessage(UseTenantKey = true/false)]</c>.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool UseTenantKeys { get; set; }

    /// <summary>
    /// Gets or sets the naming pattern for tenant-specific encryption keys.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="string.Format(string, object?)"/> with the tenant ID as <c>{0}</c>.
    /// For example, <c>"tenant-{0}-key"</c> produces <c>"tenant-acme-key"</c> for tenant <c>"acme"</c>.
    /// Defaults to <c>"tenant-{0}-key"</c>.
    /// </remarks>
    public string TenantKeyPattern { get; set; } = "tenant-{0}-key";

    /// <summary>
    /// Gets or sets whether decryption operations should be logged for compliance auditing.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, every decryption emits a structured log entry with the key ID,
    /// message type, and timestamp. Useful for HIPAA and PCI-DSS audit trails.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool AuditDecryption { get; set; }

    /// <summary>
    /// Gets or sets whether to register a health check for the encryption provider.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, registers a health check that verifies key availability
    /// and encryption round-trip functionality.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether OpenTelemetry tracing is enabled for encryption operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, encryption and decryption operations emit spans with
    /// key ID, algorithm, and duration metadata.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets whether OpenTelemetry metrics are enabled for encryption operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, counters and histograms track encryption/decryption volume,
    /// latency, and error rates.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool EnableMetrics { get; set; }
}
