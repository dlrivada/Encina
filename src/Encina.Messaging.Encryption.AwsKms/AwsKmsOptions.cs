using Amazon.KeyManagementService;

namespace Encina.Messaging.Encryption.AwsKms;

/// <summary>
/// Configuration options for the AWS KMS key provider.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="KeyId"/> is required and can be a key ARN, key ID, alias ARN, or alias name.
/// When <see cref="ClientConfig"/> is <c>null</c>, the default AWS SDK configuration is used.
/// </para>
/// </remarks>
public sealed class AwsKmsOptions
{
    /// <summary>
    /// Gets or sets the AWS KMS key identifier (ARN, key ID, alias ARN, or alias name).
    /// </summary>
    public string? KeyId { get; set; }

    /// <summary>
    /// Gets or sets the encryption algorithm. Defaults to <c>"SYMMETRIC_DEFAULT"</c> (AES-256-GCM).
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "SYMMETRIC_DEFAULT";

    /// <summary>
    /// Gets or sets the AWS region. When <c>null</c>, the default SDK region is used.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets custom client configuration for the KMS client.
    /// </summary>
    public AmazonKeyManagementServiceConfig? ClientConfig { get; set; }
}
