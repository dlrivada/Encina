namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Metadata about a cryptographic key used for pseudonymization or tokenization.
/// </summary>
/// <remarks>
/// <para>
/// Key metadata is returned by the <c>IKeyProvider</c> to describe available keys
/// without exposing the key material itself. This enables key inventory management,
/// rotation tracking, and expiration monitoring.
/// </para>
/// <para>
/// Per EDPB Guidelines 01/2025 Section 4.3, cryptographic keys must be managed
/// separately from pseudonymized data and rotated periodically. The <see cref="IsActive"/>
/// flag indicates which key should be used for new pseudonymization operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var keys = await keyProvider.ListKeysAsync(ct);
/// foreach (var key in keys)
/// {
///     Console.WriteLine($"Key: {key.KeyId}, Active: {key.IsActive}, Expires: {key.ExpiresAtUtc}");
/// }
/// </code>
/// </example>
public sealed record KeyInfo
{
    /// <summary>
    /// Unique identifier for this key.
    /// </summary>
    /// <remarks>
    /// Key identifiers are stable across rotations. After rotation, the old key
    /// retains its identifier but is marked as inactive (<see cref="IsActive"/> = <c>false</c>).
    /// The new key receives a fresh identifier.
    /// </remarks>
    public required string KeyId { get; init; }

    /// <summary>
    /// The cryptographic algorithm this key is intended for.
    /// </summary>
    public required PseudonymizationAlgorithm Algorithm { get; init; }

    /// <summary>
    /// Timestamp when this key was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Optional expiration timestamp for this key (UTC).
    /// </summary>
    /// <remarks>
    /// When set, the key should not be used for new pseudonymization operations after this
    /// time. Existing data encrypted with an expired key can still be depseudonymized.
    /// <c>null</c> indicates the key does not expire automatically.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// Whether this key is currently active for new pseudonymization operations.
    /// </summary>
    /// <remarks>
    /// Only one key should be active at a time per algorithm. After key rotation,
    /// the old key becomes inactive but remains available for depseudonymizing
    /// data that was encrypted with it.
    /// </remarks>
    public required bool IsActive { get; init; }
}
