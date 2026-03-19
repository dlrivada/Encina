namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// Represents the lifecycle status of a temporal encryption key.
/// </summary>
/// <remarks>
/// Temporal keys transition through the following states:
/// <list type="number">
/// <item><see cref="Active"/> — The key is currently in use for encrypting new audit entries.</item>
/// <item><see cref="Rotated"/> — The key has been superseded by a newer version but remains
/// available for decrypting events encrypted with this version.</item>
/// <item><see cref="Destroyed"/> — The key material has been permanently deleted (crypto-shredding).
/// Events encrypted with this key can no longer be decrypted.</item>
/// </list>
/// </remarks>
public enum TemporalKeyStatus
{
    /// <summary>
    /// The key is active and used for encrypting new audit entries in its time period.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The key has been rotated to a newer version. It remains available for decrypting
    /// existing events but is no longer used for new encryptions.
    /// </summary>
    Rotated = 1,

    /// <summary>
    /// The key material has been permanently destroyed (crypto-shredding).
    /// Events encrypted with this key will show placeholder values in query results.
    /// </summary>
    Destroyed = 2
}
