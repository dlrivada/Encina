namespace Encina.Messaging.Encryption.Attributes;

/// <summary>
/// Marks a notification or command type for automatic payload-level encryption when stored
/// in the outbox or inbox.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a message type (notification, command, or event), the
/// <c>EncryptingMessageSerializer</c> will automatically encrypt the serialized payload
/// before persistence and decrypt it upon retrieval.
/// </para>
/// <para>
/// This attribute operates at the message level — encrypting the entire serialized
/// <c>OutboxMessage.Content</c> string. For field-level encryption of individual properties,
/// use <see cref="Security.Encryption.EncryptAttribute"/> instead.
/// </para>
/// <para>
/// <strong>Precedence</strong>: This attribute overrides global configuration.
/// <c>[EncryptedMessage(Enabled = false)]</c> disables encryption for a specific type
/// even when <c>MessageEncryptionOptions.EncryptAllMessages</c> is <c>true</c>.
/// </para>
/// <para>
/// <strong>Key selection</strong>: Use <see cref="KeyId"/> to specify a per-type encryption key,
/// or <see cref="UseTenantKey"/> to enable multi-tenant key isolation via
/// <see cref="Abstractions.ITenantKeyResolver"/>.
/// </para>
/// <para>
/// <strong>Compliance</strong>: Supports GDPR (Article 32), HIPAA (§164.312(a)(2)(iv)),
/// and PCI-DSS (Requirement 3) encryption-at-rest requirements.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic encryption with default key
/// [EncryptedMessage]
/// public sealed record OrderPlacedNotification(Guid OrderId, decimal Total) : INotification;
///
/// // Per-type key for PCI-DSS compliance
/// [EncryptedMessage(KeyId = "payment-key")]
/// public sealed record PaymentProcessedNotification(Guid PaymentId) : INotification;
///
/// // Multi-tenant key isolation
/// [EncryptedMessage(UseTenantKey = true)]
/// public sealed record TenantDataChangedNotification(string TenantId) : INotification;
///
/// // Explicitly disable encryption for a type (overrides global config)
/// [EncryptedMessage(Enabled = false)]
/// public sealed record PublicAnnouncementNotification(string Message) : INotification;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class EncryptedMessageAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether encryption is enabled for this message type.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Set to <c>false</c> to explicitly disable encryption
    /// for this type, even when global encryption is enabled via
    /// <c>MessageEncryptionOptions.EncryptAllMessages</c>.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the specific encryption key identifier for this message type.
    /// </summary>
    /// <remarks>
    /// When <c>null</c> (default), the current active key from
    /// <see cref="Security.Encryption.Abstractions.IKeyProvider"/> is used.
    /// Specify explicitly to use a dedicated key for specific message types
    /// (e.g., a PCI-DSS compliant key for payment-related messages).
    /// </remarks>
    public string? KeyId { get; set; }

    /// <summary>
    /// Gets or sets whether to use tenant-specific encryption keys for this message type.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the <see cref="Abstractions.ITenantKeyResolver"/> is used to
    /// resolve a tenant-specific key ID based on the current tenant context.
    /// This ensures cryptographic isolation between tenants.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool UseTenantKey { get; set; }
}
