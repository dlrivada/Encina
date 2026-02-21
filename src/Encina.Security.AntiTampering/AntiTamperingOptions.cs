using System.Text;

namespace Encina.Security.AntiTampering;

/// <summary>
/// Configuration options for the Encina HMAC-based anti-tampering pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <c>HMACValidationPipelineBehavior</c>
/// and the request signing infrastructure.
/// Register via <c>AddEncinaAntiTampering(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAntiTampering(options =>
/// {
///     options.Algorithm = HMACAlgorithm.SHA256;
///     options.TimestampToleranceMinutes = 5;
///     options.RequireNonce = true;
///     options.AddHealthCheck = true;
///     options.AddKey("test-key", "my-secret-value");
/// });
/// </code>
/// </example>
public sealed class AntiTamperingOptions
{
    private readonly Dictionary<string, byte[]> _testKeys = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the HMAC algorithm to use for signature computation.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="HMACAlgorithm.SHA256"/>. Override for compliance requirements
    /// that mandate SHA-384 or SHA-512.
    /// </remarks>
    public HMACAlgorithm Algorithm { get; set; } = HMACAlgorithm.SHA256;

    /// <summary>
    /// Gets or sets the maximum allowed age of a request timestamp in minutes.
    /// </summary>
    /// <remarks>
    /// Requests with timestamps older than this value are rejected as expired.
    /// Default is 5 minutes. Increase for high-latency networks.
    /// </remarks>
    public int TimestampToleranceMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether nonce validation is required for replay protection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), every signed request must include a unique nonce
    /// that is validated against the <see cref="Abstractions.INonceStore"/>.
    /// </para>
    /// <para>
    /// Set to <c>false</c> to disable nonce validation globally. Individual requests
    /// can also skip nonce validation via <see cref="RequireSignatureAttribute.SkipReplayProtection"/>.
    /// </para>
    /// </remarks>
    public bool RequireNonce { get; set; } = true;

    /// <summary>
    /// Gets or sets the duration in minutes before nonce entries expire.
    /// </summary>
    /// <remarks>
    /// Should be at least equal to <see cref="TimestampToleranceMinutes"/> to ensure
    /// nonces are tracked for the full tolerance window. Default is 10 minutes.
    /// </remarks>
    public int NonceExpiryMinutes { get; set; } = 10;

    /// <summary>
    /// Gets or sets the HTTP header name for the HMAC signature.
    /// </summary>
    /// <remarks>
    /// Default is <c>"X-Signature"</c>. Customize to match your API contract.
    /// </remarks>
    public string SignatureHeader { get; set; } = "X-Signature";

    /// <summary>
    /// Gets or sets the HTTP header name for the request timestamp.
    /// </summary>
    /// <remarks>
    /// Default is <c>"X-Timestamp"</c>. The timestamp should be in ISO 8601 format.
    /// </remarks>
    public string TimestampHeader { get; set; } = "X-Timestamp";

    /// <summary>
    /// Gets or sets the HTTP header name for the nonce.
    /// </summary>
    /// <remarks>
    /// Default is <c>"X-Nonce"</c>. Each request must include a unique nonce value.
    /// </remarks>
    public string NonceHeader { get; set; } = "X-Nonce";

    /// <summary>
    /// Gets or sets the HTTP header name for the key identifier.
    /// </summary>
    /// <remarks>
    /// Default is <c>"X-Key-Id"</c>. Identifies which HMAC key was used for signing.
    /// </remarks>
    public string KeyIdHeader { get; set; } = "X-Key-Id";

    /// <summary>
    /// Gets or sets whether to register the anti-tampering health check.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the key provider
    /// is resolvable and can return at least one key.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether to enable OpenTelemetry tracing for signing and verification operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, signing and verification operations emit OpenTelemetry activities
    /// via the <c>Encina.Security.AntiTampering</c> ActivitySource.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets whether to enable OpenTelemetry metrics for signing and verification operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, signing and verification operations emit counters and histograms
    /// via the <c>Encina.Security.AntiTampering</c> Meter.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableMetrics { get; set; }

    /// <summary>
    /// Gets the test keys registered via <see cref="AddKey"/>.
    /// </summary>
    /// <remarks>
    /// For use by the built-in <c>InMemoryKeyProvider</c> during testing and development.
    /// In production, use a cloud-based <see cref="Abstractions.IKeyProvider"/> implementation.
    /// </remarks>
    internal IReadOnlyDictionary<string, byte[]> TestKeys => _testKeys;

    /// <summary>
    /// Registers a test HMAC key for development and testing scenarios.
    /// </summary>
    /// <param name="keyId">The unique identifier for this key.</param>
    /// <param name="secretValue">The secret value to use as key material (will be UTF-8 encoded).</param>
    /// <returns>This <see cref="AntiTamperingOptions"/> instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Keys added via this method are used by the built-in <c>InMemoryKeyProvider</c>.
    /// This is intended for testing and development only.
    /// </para>
    /// <para>
    /// In production, register a custom <see cref="Abstractions.IKeyProvider"/> implementation
    /// backed by a cloud secret manager (Azure Key Vault, AWS Secrets Manager, etc.).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="keyId"/> or <paramref name="secretValue"/> is null or whitespace.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddEncinaAntiTampering(options =>
    /// {
    ///     options.AddKey("test-key-v1", "super-secret-test-value");
    ///     options.AddKey("partner-key", "partner-shared-secret");
    /// });
    /// </code>
    /// </example>
    public AntiTamperingOptions AddKey(string keyId, string secretValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretValue);

        _testKeys[keyId] = Encoding.UTF8.GetBytes(secretValue);
        return this;
    }
}
