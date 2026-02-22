using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Providers;

/// <summary>
/// A secret reader that tries multiple providers in order, returning the first successful result.
/// </summary>
/// <remarks>
/// <para>
/// Implements a chain-of-responsibility failover pattern for secret retrieval.
/// Providers are tried in the order they are registered; the first provider that returns
/// <c>Right</c> wins. If all providers fail, <see cref="SecretsErrors.FailoverExhausted"/>
/// is returned with details of each failure.
/// </para>
/// <para>
/// <b>Thread safety:</b> This class is thread-safe if all underlying providers are thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Build a failover chain
/// var reader = primaryReader.WithFailover(secondaryReader, tertiaryReader);
///
/// // Or construct directly
/// var reader = new FailoverSecretReader(
///     [primaryReader, secondaryReader],
///     logger);
/// </code>
/// </example>
public sealed class FailoverSecretReader : ISecretReader
{
    private readonly List<ISecretReader> _providers;
    private readonly ILogger<FailoverSecretReader> _logger;

    /// <summary>
    /// Gets the number of providers in the failover chain.
    /// </summary>
    public int ProviderCount => _providers.Count;

    /// <summary>
    /// Initializes a new instance of <see cref="FailoverSecretReader"/>.
    /// </summary>
    /// <param name="providers">The ordered list of secret readers to try. Must contain at least one provider.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="providers"/> or <paramref name="logger"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="providers"/> is empty.</exception>
    public FailoverSecretReader(
        IEnumerable<ISecretReader> providers,
        ILogger<FailoverSecretReader> logger)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(logger);

        _providers = providers.ToList();
        if (_providers.Count == 0)
        {
            throw new ArgumentException("At least one provider must be specified.", nameof(providers));
        }

        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var failedCount = 0;

        foreach (var provider in _providers)
        {
            var result = await provider.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);

            if (result.IsRight)
            {
                if (failedCount > 0)
                {
                    Log.FailoverSuccess(_logger, secretName, provider.GetType().Name, failedCount);
                }

                return result;
            }

            failedCount++;
            Log.FailoverTriggered(_logger, provider.GetType().Name, secretName);
        }

        Log.FailoverExhausted(_logger, _providers.Count, secretName);
        return SecretsErrors.FailoverExhausted(secretName, _providers.Count);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var failedCount = 0;

        foreach (var provider in _providers)
        {
            var result = await provider.GetSecretAsync<T>(secretName, cancellationToken).ConfigureAwait(false);

            if (result.IsRight)
            {
                if (failedCount > 0)
                {
                    Log.FailoverSuccess(_logger, secretName, provider.GetType().Name, failedCount);
                }

                return result;
            }

            failedCount++;
            Log.FailoverTriggered(_logger, provider.GetType().Name, secretName);
        }

        Log.FailoverExhausted(_logger, _providers.Count, secretName);
        return SecretsErrors.FailoverExhausted(secretName, _providers.Count);
    }
}

/// <summary>
/// Extension methods for building failover chains.
/// </summary>
public static class FailoverSecretReaderExtensions
{
    /// <summary>
    /// Creates a failover chain with this reader as primary and the specified readers as fallbacks.
    /// </summary>
    /// <param name="primary">The primary secret reader.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fallbacks">Additional readers to try if the primary fails.</param>
    /// <returns>A new <see cref="FailoverSecretReader"/> wrapping all providers.</returns>
    public static FailoverSecretReader WithFailover(
        this ISecretReader primary,
        ILogger<FailoverSecretReader> logger,
        params ISecretReader[] fallbacks)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(fallbacks);

        var providers = new List<ISecretReader>(fallbacks.Length + 1) { primary };
        providers.AddRange(fallbacks);
        return new FailoverSecretReader(providers, logger);
    }
}
