namespace Encina.Security.Encryption;

/// <summary>
/// Configuration options for the Encina field-level encryption pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <see cref="EncryptionPipelineBehavior{TRequest, TResponse}"/>
/// and the <c>EncryptionOrchestrator</c>.
/// Register via <c>AddEncinaEncryption(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEncryption(options =>
/// {
///     options.DefaultAlgorithm = EncryptionAlgorithm.Aes256Gcm;
///     options.FailOnDecryptionError = true;
///     options.AddHealthCheck = true;
/// });
/// </code>
/// </example>
public sealed class EncryptionOptions
{
    /// <summary>
    /// Gets or sets the default encryption algorithm to use.
    /// </summary>
    /// <remarks>
    /// Override only when a specific algorithm is required by compliance policies.
    /// Default is <see cref="EncryptionAlgorithm.Aes256Gcm"/>.
    /// </remarks>
    public EncryptionAlgorithm DefaultAlgorithm { get; set; } = EncryptionAlgorithm.Aes256Gcm;

    /// <summary>
    /// Gets or sets whether decryption failures should cause the pipeline to fail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), decryption failures propagate as <see cref="EncinaError"/>
    /// through the pipeline, preventing the operation from completing.
    /// </para>
    /// <para>
    /// When <c>false</c>, failures are logged but the property value is left unchanged,
    /// allowing the operation to continue. Use with caution — this may result in encrypted
    /// values being passed to handlers that expect plaintext.
    /// </para>
    /// </remarks>
    public bool FailOnDecryptionError { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register the encryption health check.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies encryption services
    /// are resolvable and operational (e.g., key provider returns a valid current key).
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether to enable OpenTelemetry tracing for encryption operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, encryption and decryption operations emit OpenTelemetry activities
    /// via the <c>Encina.Security.Encryption</c> ActivitySource.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets whether to enable OpenTelemetry metrics for encryption operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, encryption and decryption operations emit counters and histograms
    /// via the <c>Encina.Security.Encryption</c> Meter.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableMetrics { get; set; }
}
