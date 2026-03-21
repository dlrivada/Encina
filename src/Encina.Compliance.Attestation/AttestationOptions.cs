namespace Encina.Compliance.Attestation;

/// <summary>
/// Top-level configuration options for the attestation compliance pipeline.
/// </summary>
public sealed class AttestationOptions
{
    /// <summary>
    /// Gets or sets whether to register a health check for the attestation provider.
    /// Default is <c>false</c>.
    /// </summary>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets the in-memory provider configuration.
    /// Set via <see cref="AttestationOptionsExtensions.UseInMemory"/>.
    /// </summary>
    internal bool UseInMemoryProvider { get; set; }

    /// <summary>
    /// Gets or sets the hash chain provider configuration.
    /// Set via <see cref="AttestationOptionsExtensions.UseHashChain"/>.
    /// </summary>
    internal HashChainOptions? HashChainOptions { get; set; }

    /// <summary>
    /// Gets or sets the HTTP provider configuration.
    /// Set via <see cref="AttestationOptionsExtensions.UseHttp"/>.
    /// </summary>
    internal HttpAttestationOptions? HttpOptions { get; set; }
}

/// <summary>
/// Extension methods for fluent configuration of attestation providers.
/// </summary>
public static class AttestationOptionsExtensions
{
    /// <summary>
    /// Configures the in-memory attestation provider (for testing and development).
    /// </summary>
    public static AttestationOptions UseInMemory(this AttestationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.UseInMemoryProvider = true;
        return options;
    }

    /// <summary>
    /// Configures the hash chain attestation provider (self-hosted, zero external dependencies).
    /// </summary>
    public static AttestationOptions UseHashChain(
        this AttestationOptions options,
        Action<HashChainOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var hashChainOptions = new HashChainOptions();
        configure?.Invoke(hashChainOptions);
        options.HashChainOptions = hashChainOptions;
        return options;
    }

    /// <summary>
    /// Configures the HTTP attestation provider (external endpoint, e.g., Sigstore/Rekor).
    /// </summary>
    public static AttestationOptions UseHttp(
        this AttestationOptions options,
        Action<HttpAttestationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);
        var httpOptions = new HttpAttestationOptions();
        configure(httpOptions);
        options.HttpOptions = httpOptions;
        return options;
    }
}
