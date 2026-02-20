namespace Encina.Security.Encryption;

/// <summary>
/// Indicates that the target contains encrypted data that should be decrypted
/// when received by the pipeline.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request class, the encryption pipeline behavior will decrypt
/// all properties marked with <see cref="EncryptAttribute"/> before the handler executes.
/// </para>
/// <para>
/// When applied to a property, only that specific property is decrypted on receive.
/// </para>
/// <para>
/// This attribute is used in scenarios where data arrives pre-encrypted from external
/// sources (e.g., client-side encryption, encrypted API payloads, or data retrieved
/// from encrypted storage).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Class-level: decrypt all marked properties before handler execution
/// [DecryptOnReceive]
/// public sealed record ProcessPaymentCommand(
///     Guid OrderId,
///     [property: Encrypt(Purpose = "Payment.CardNumber")] string EncryptedCardNumber,
///     [property: Encrypt(Purpose = "Payment.CVV")] string EncryptedCvv
/// ) : ICommand&lt;PaymentResult&gt;;
///
/// // Property-level: decrypt only this specific property
/// public sealed record UpdateProfileCommand(
///     Guid UserId,
///     string DisplayName,
///     [property: DecryptOnReceive] string EncryptedEmail
/// ) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class DecryptOnReceiveAttribute : EncryptionAttribute
{
}
