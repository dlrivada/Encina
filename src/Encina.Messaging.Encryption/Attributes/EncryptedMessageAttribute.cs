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
/// <strong>No opt-out</strong>: a type marked with this attribute, or a type derived from a
/// marked type (the attribute is inherited), is always encrypted, whatever the value of
/// <c>MessageEncryptionOptions.EncryptAllMessages</c>. The attribute has no member that
/// disables encryption: once a type is marked, it cannot be left unencrypted.
/// </para>
/// <para>
/// <strong>Key selection</strong>: Use <see cref="KeyId"/> to specify a per-type encryption key.
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
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class EncryptedMessageAttribute : Attribute
{
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
    /// Gets or sets whether tenant-specific encryption keys are requested for this message type.
    /// </summary>
    /// <remarks>
    /// Currently this property has no effect: no key selection depends on it and the pipeline
    /// does not call <see cref="Abstractions.ITenantKeyResolver"/>.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool UseTenantKey { get; set; }
}
