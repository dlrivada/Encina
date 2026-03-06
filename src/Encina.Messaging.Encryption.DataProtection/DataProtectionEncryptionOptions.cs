namespace Encina.Messaging.Encryption.DataProtection;

/// <summary>
/// Configuration options for the ASP.NET Core Data Protection encryption provider.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Purpose"/> string isolates encryption keys for message encryption
/// from other Data Protection usage in the application.
/// </para>
/// </remarks>
public sealed class DataProtectionEncryptionOptions
{
    /// <summary>
    /// Gets or sets the Data Protection purpose string used to create the <c>IDataProtector</c>.
    /// Defaults to <c>"Encina.Messaging.Encryption"</c>.
    /// </summary>
    public string Purpose { get; set; } = "Encina.Messaging.Encryption";
}
