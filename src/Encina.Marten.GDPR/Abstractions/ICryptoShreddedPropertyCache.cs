namespace Encina.Marten.GDPR.Abstractions;

/// <summary>
/// Thread-safe cache for discovering and retrieving properties decorated with
/// <see cref="CryptoShreddedAttribute"/> on event types.
/// </summary>
/// <remarks>
/// <para>
/// Follows the same pattern as <c>EncryptedPropertyCache</c> from
/// <c>Encina.Security.Encryption</c>: uses <c>ConcurrentDictionary</c> with
/// compiled expression tree setters for high-performance property access without
/// per-operation reflection overhead.
/// </para>
/// <para>
/// Properties are discovered lazily on first access and cached for the lifetime of the
/// application. Discovery validates that:
/// </para>
/// <list type="bullet">
/// <item><description>The <c>[CryptoShredded]</c> attribute co-exists with
/// <c>[PersonalData]</c> from <c>Encina.Compliance.DataSubjectRights</c></description></item>
/// <item><description>The <see cref="CryptoShreddedAttribute.SubjectIdProperty"/> refers
/// to a valid, readable property on the declaring type</description></item>
/// <item><description>The target property is a <c>string</c> type (only strings can be
/// encrypted for crypto-shredding)</description></item>
/// </list>
/// <para>
/// Misconfigured properties generate warnings via
/// <see cref="CryptoShreddingErrors.AttributeMisconfigured"/> and are excluded from the cache.
/// </para>
/// </remarks>
internal interface ICryptoShreddedPropertyCache
{
    /// <summary>
    /// Gets the crypto-shredded field descriptors for the specified event type.
    /// </summary>
    /// <param name="eventType">The event type to discover crypto-shredded properties on.</param>
    /// <returns>
    /// An array of <see cref="CryptoShreddedFieldInfo"/> for properties decorated with
    /// <see cref="CryptoShreddedAttribute"/>. Returns an empty array if the type has no
    /// crypto-shredded properties.
    /// </returns>
    CryptoShreddedFieldInfo[] GetFields(Type eventType);

    /// <summary>
    /// Checks whether the specified event type has any properties decorated with
    /// <see cref="CryptoShreddedAttribute"/>.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns>
    /// <c>true</c> if the type has at least one crypto-shredded property; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This is a fast-path check used by the <c>CryptoShredderSerializer</c> to skip
    /// encryption/decryption processing for events that have no PII fields.
    /// </remarks>
    bool HasCryptoShreddedFields(Type eventType);
}
