using System.Collections.Immutable;

namespace Encina.Security.Encryption;

/// <summary>
/// Immutable context for encryption operations, carrying key selection, purpose, and tenant isolation data.
/// </summary>
/// <remarks>
/// <para>
/// The encryption context determines which key is used, the purpose chain for key derivation,
/// and optional associated data for authenticated encryption (AEAD).
/// </para>
/// <para>
/// In multi-tenant applications, <see cref="TenantId"/> enables per-tenant key isolation,
/// typically derived from <see cref="IRequestContext.TenantId"/>.
/// </para>
/// <para>
/// The <see cref="Purpose"/> property follows the .NET Data Protection purpose chain convention,
/// enabling hierarchical key derivation for different encryption contexts.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new EncryptionContext
/// {
///     KeyId = "master-key-2024",
///     Purpose = "UserProfile.Email",
///     TenantId = requestContext.TenantId
/// };
/// </code>
/// </example>
public sealed record EncryptionContext
{
    /// <summary>
    /// The identifier of the encryption key to use.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the current default key from <see cref="Abstractions.IKeyProvider"/> is used.
    /// Specify explicitly for key rotation scenarios or when decrypting with a specific key version.
    /// </remarks>
    public string? KeyId { get; init; }

    /// <summary>
    /// The purpose string for key derivation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Follows the .NET Data Protection purpose chain convention. Keys derived for one purpose
    /// cannot decrypt data encrypted for a different purpose, providing cryptographic isolation.
    /// </para>
    /// <para>
    /// Example purposes: <c>"UserProfile.Email"</c>, <c>"PaymentInfo.CardNumber"</c>.
    /// </para>
    /// </remarks>
    public string? Purpose { get; init; }

    /// <summary>
    /// Additional authenticated data (AAD) for AEAD algorithms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Associated data is authenticated but not encrypted. It binds the ciphertext to a specific
    /// context (e.g., user ID, record ID) so that ciphertext cannot be moved between records.
    /// </para>
    /// <para>
    /// For AES-256-GCM, this is passed as the <c>associatedData</c> parameter.
    /// </para>
    /// </remarks>
    public ImmutableArray<byte> AssociatedData { get; init; } = [];

    /// <summary>
    /// Optional tenant identifier for multi-tenant key isolation.
    /// </summary>
    /// <remarks>
    /// When set, the key provider uses tenant-specific keys or key derivation,
    /// ensuring cryptographic isolation between tenants.
    /// Typically derived from <see cref="IRequestContext.TenantId"/>.
    /// </remarks>
    public string? TenantId { get; init; }
}
