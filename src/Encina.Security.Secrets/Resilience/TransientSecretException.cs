namespace Encina.Security.Secrets.Resilience;

/// <summary>
/// Internal exception used as a bridge between Railway Oriented Programming (ROP)
/// and Polly's exception-based retry mechanism.
/// </summary>
/// <remarks>
/// <para>
/// Polly's <c>ResiliencePipeline</c> retries based on exceptions, but Encina providers
/// return <c>Either&lt;EncinaError, T&gt;</c>. This exception wraps a transient
/// <see cref="EncinaError"/> so that Polly can detect and retry it.
/// </para>
/// <para>
/// Only thrown for transient errors (e.g., <c>secrets.provider_unavailable</c>).
/// Non-transient errors (e.g., <c>secrets.not_found</c>) are returned directly
/// without triggering Polly.
/// </para>
/// </remarks>
internal sealed class TransientSecretException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransientSecretException"/> class.
    /// </summary>
    /// <param name="error">The transient error from the secret provider.</param>
    public TransientSecretException(EncinaError error)
        : base(error.Message)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the original <see cref="EncinaError"/> that triggered the retry.
    /// </summary>
    public EncinaError Error { get; }
}
