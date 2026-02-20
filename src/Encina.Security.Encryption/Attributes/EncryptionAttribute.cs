namespace Encina.Security.Encryption;

/// <summary>
/// Base class for all Encina field-level encryption attributes.
/// </summary>
/// <remarks>
/// <para>
/// Encryption attributes are discovered by <c>EncryptedPropertyCache</c> at runtime
/// and used by <c>IEncryptionOrchestrator</c> to determine which properties require
/// encryption or decryption during pipeline execution.
/// </para>
/// <para>
/// Subclasses define specific encryption behaviors:
/// <list type="bullet">
/// <item><description><see cref="EncryptAttribute"/> — marks a property for field-level encryption</description></item>
/// <item><description><see cref="EncryptedResponseAttribute"/> — marks a response class for encryption</description></item>
/// <item><description><see cref="DecryptOnReceiveAttribute"/> — marks incoming data for decryption</description></item>
/// </list>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public abstract class EncryptionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the encryption algorithm to use.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="EncryptionAlgorithm.Aes256Gcm"/>.
    /// Override only when a specific algorithm is required by compliance policies.
    /// </remarks>
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.Aes256Gcm;

    /// <summary>
    /// Gets or sets whether encryption failures should cause the pipeline to fail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), encryption or decryption failures propagate as
    /// <see cref="EncinaError"/> through the pipeline, preventing the operation from completing.
    /// </para>
    /// <para>
    /// When <c>false</c>, failures are logged but the property value is left unchanged,
    /// allowing the operation to continue. Use with caution — this may result in plaintext
    /// values being persisted or sensitive data being returned unencrypted.
    /// </para>
    /// </remarks>
    public bool FailOnError { get; set; } = true;
}
