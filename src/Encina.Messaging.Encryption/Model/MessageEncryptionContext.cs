using System.Collections.Immutable;

namespace Encina.Messaging.Encryption.Model;

/// <summary>
/// Immutable context for message encryption operations, carrying key selection, tenant isolation,
/// and message identification data.
/// </summary>
/// <remarks>
/// <para>
/// The encryption context determines which key is used for encryption, provides tenant isolation
/// for multi-tenant applications, and carries message metadata for audit and debugging.
/// </para>
/// <para>
/// Unlike <see cref="Security.Encryption.EncryptionContext"/> (which carries field-level encryption
/// context with <c>Purpose</c> for key derivation), this context focuses on message-level concerns:
/// tenant key isolation, message type identification, and message correlation.
/// </para>
/// <para>
/// The <see cref="AssociatedData"/> property enables authenticated encryption with associated data (AEAD),
/// binding the ciphertext to message metadata so it cannot be swapped between messages.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new MessageEncryptionContext
/// {
///     KeyId = "msg-key-2024",
///     TenantId = "acme-corp",
///     MessageType = "OrderPlacedNotification",
///     MessageId = Guid.NewGuid()
/// };
/// </code>
/// </example>
public sealed record MessageEncryptionContext
{
    /// <summary>
    /// The identifier of the encryption key to use.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the current default key from
    /// <see cref="Security.Encryption.Abstractions.IKeyProvider"/> is used.
    /// Specify explicitly for key rotation scenarios or when decrypting with
    /// a specific key version extracted from the encrypted payload header.
    /// </remarks>
    public string? KeyId { get; init; }

    /// <summary>
    /// Optional tenant identifier for multi-tenant key isolation.
    /// </summary>
    /// <remarks>
    /// When set, the <see cref="Abstractions.ITenantKeyResolver"/> maps this tenant ID
    /// to a tenant-specific key identifier, ensuring cryptographic isolation between tenants.
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// The fully qualified type name of the message being encrypted.
    /// </summary>
    /// <remarks>
    /// Used for audit logging and diagnostics. Does not affect encryption behavior.
    /// </remarks>
    public string? MessageType { get; init; }

    /// <summary>
    /// The unique identifier of the message being encrypted.
    /// </summary>
    /// <remarks>
    /// Used for audit logging, diagnostics, and optionally as part of
    /// <see cref="AssociatedData"/> to bind ciphertext to a specific message instance.
    /// </remarks>
    public Guid? MessageId { get; init; }

    /// <summary>
    /// Additional authenticated data (AAD) for AEAD algorithms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Associated data is authenticated but not encrypted. It binds the ciphertext
    /// to a specific context (e.g., message ID, tenant ID) so that ciphertext cannot
    /// be moved between messages.
    /// </para>
    /// <para>
    /// For AES-256-GCM, this is passed as the <c>associatedData</c> parameter.
    /// </para>
    /// </remarks>
    public ImmutableArray<byte> AssociatedData { get; init; } = [];
}
