namespace Encina.Security.Encryption;

/// <summary>
/// Marks a property for automatic field-level encryption.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a property, the <c>IEncryptionOrchestrator</c> will automatically
/// encrypt the property value before persistence and decrypt it when retrieved.
/// </para>
/// <para>
/// The <see cref="Purpose"/> property enables cryptographic isolation by deriving
/// purpose-specific keys. Properties encrypted with different purposes cannot be
/// cross-decrypted, even with the same master key.
/// </para>
/// <para>
/// The <see cref="KeyId"/> property allows specifying a particular key version.
/// When <c>null</c>, the current active key from <see cref="Abstractions.IKeyProvider"/> is used.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record CreateUserCommand(
///     string Username,
///     [property: Encrypt(Purpose = "User.Email")] string Email,
///     [property: Encrypt(Purpose = "User.SSN")] string SocialSecurityNumber
/// ) : ICommand&lt;UserId&gt;;
///
/// // With explicit key ID for key rotation scenarios
/// public sealed record UpdatePaymentCommand(
///     Guid PaymentId,
///     [property: Encrypt(Purpose = "Payment.CardNumber", KeyId = "pci-key-v2")] string CardNumber
/// ) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EncryptAttribute : EncryptionAttribute
{
    /// <summary>
    /// Gets or sets the purpose string for key derivation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Follows the .NET Data Protection purpose chain convention.
    /// Keys derived for one purpose cannot decrypt data encrypted for a different purpose,
    /// providing cryptographic isolation between different field types.
    /// </para>
    /// <para>
    /// Recommended format: <c>"EntityType.PropertyName"</c>
    /// (e.g., <c>"User.Email"</c>, <c>"Payment.CardNumber"</c>).
    /// </para>
    /// <para>
    /// When <c>null</c>, no purpose-based key derivation is applied.
    /// </para>
    /// </remarks>
    public string? Purpose { get; set; }

    /// <summary>
    /// Gets or sets the specific key identifier to use for encryption.
    /// </summary>
    /// <remarks>
    /// When <c>null</c> (default), the current active key from
    /// <see cref="Abstractions.IKeyProvider"/> is used.
    /// Specify explicitly for key rotation scenarios or compliance requirements
    /// that mandate specific key versions.
    /// </remarks>
    public string? KeyId { get; set; }
}
