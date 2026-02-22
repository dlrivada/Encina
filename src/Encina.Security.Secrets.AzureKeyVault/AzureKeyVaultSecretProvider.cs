using System.Text.Json;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.AzureKeyVault;

/// <summary>
/// Azure Key Vault implementation of secret reader, writer, and rotator.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses the <see cref="SecretClient"/> from the Azure SDK to interact with
/// Azure Key Vault. It implements all three ISP-compliant interfaces, enabling read, write,
/// and rotation operations.
/// </para>
/// <para>
/// <b>Thread safety:</b> This class is thread-safe. The underlying <see cref="SecretClient"/>
/// is designed for concurrent use across threads.
/// </para>
/// <para>
/// <b>Error handling:</b> Azure SDK exceptions are mapped to Encina error codes:
/// <list type="bullet">
/// <item>HTTP 404 → <see cref="SecretsErrors.NotFoundCode"/></item>
/// <item>HTTP 401/403 → <see cref="SecretsErrors.AccessDeniedCode"/></item>
/// <item>Other HTTP errors → <see cref="SecretsErrors.ProviderUnavailableCode"/></item>
/// <item>Rotation failures → <see cref="SecretsErrors.RotationFailedCode"/></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI (recommended)
/// services.AddAzureKeyVaultSecrets(new Uri("https://my-vault.vault.azure.net/"));
///
/// // Inject and use
/// public class MyService(ISecretReader secretReader)
/// {
///     public async Task&lt;string&gt; GetApiKeyAsync(CancellationToken ct)
///     {
///         var result = await secretReader.GetSecretAsync("api-key", ct);
///         return result.Match(
///             Right: value =&gt; value,
///             Left: error =&gt; throw new InvalidOperationException(error.Message));
///     }
/// }
/// </code>
/// </example>
public sealed class AzureKeyVaultSecretProvider : ISecretReader, ISecretWriter, ISecretRotator
{
    private const string ProviderName = "AzureKeyVault";

    private readonly SecretClient _client;
    private readonly ILogger<AzureKeyVaultSecretProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultSecretProvider"/> class.
    /// </summary>
    /// <param name="client">The Azure Key Vault secret client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="client"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public AzureKeyVaultSecretProvider(
        SecretClient client,
        ILogger<AzureKeyVaultSecretProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        try
        {
            Response<KeyVaultSecret> response = await _client
                .GetSecretAsync(secretName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            Log.SecretRetrieved(_logger, secretName);
            return response.Value.Value;
        }
        catch (RequestFailedException ex)
        {
            return MapException(secretName, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        try
        {
            Response<KeyVaultSecret> response = await _client
                .GetSecretAsync(secretName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var raw = response.Value.Value;

            try
            {
                var deserialized = JsonSerializer.Deserialize<T>(raw);

                if (deserialized is null)
                {
                    return SecretsErrors.DeserializationFailed(secretName, typeof(T));
                }

                Log.SecretRetrieved(_logger, secretName);
                return deserialized;
            }
            catch (JsonException ex)
            {
                Log.DeserializationFailed(_logger, secretName, typeof(T).Name, ex);
                return SecretsErrors.DeserializationFailed(secretName, typeof(T), ex);
            }
        }
        catch (RequestFailedException ex)
        {
            return MapExceptionTyped<T>(secretName, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> SetSecretAsync(
        string secretName,
        string value,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            await _client
                .SetSecretAsync(secretName, value, cancellationToken)
                .ConfigureAwait(false);

            Log.SecretWritten(_logger, secretName);
            return Unit.Default;
        }
        catch (RequestFailedException ex)
        {
            return MapExceptionUnit(secretName, ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Azure Key Vault automatically versions secrets on each write.
    /// This method reads the current secret value and writes it back,
    /// creating a new version in the vault. In practice, the
    /// <c>SecretRotationCoordinator</c>
    /// generates the new value via <see cref="ISecretRotationHandler"/> and writes
    /// it through <see cref="ISecretWriter"/>.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> RotateSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        try
        {
            Response<KeyVaultSecret> current = await _client
                .GetSecretAsync(secretName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await _client
                .SetSecretAsync(secretName, current.Value.Value, cancellationToken)
                .ConfigureAwait(false);

            Log.SecretRotated(_logger, secretName);
            return Unit.Default;
        }
        catch (RequestFailedException ex)
        {
            Log.RotationFailed(_logger, secretName, ex.Message, ex);
            return SecretsErrors.RotationFailed(secretName, ex.Message, ex);
        }
    }

    private Either<EncinaError, string> MapException(string secretName, RequestFailedException ex) =>
        ex.Status switch
        {
            404 => LogAndReturnNotFound(secretName),
            401 or 403 => LogAndReturnAccessDenied(secretName, ex),
            _ => LogAndReturnProviderUnavailable(ex)
        };

    private Either<EncinaError, T> MapExceptionTyped<T>(string secretName, RequestFailedException ex) where T : class =>
        ex.Status switch
        {
            404 => LogAndReturnNotFound(secretName),
            401 or 403 => LogAndReturnAccessDenied(secretName, ex),
            _ => LogAndReturnProviderUnavailable(ex)
        };

    private Either<EncinaError, Unit> MapExceptionUnit(string secretName, RequestFailedException ex) =>
        ex.Status switch
        {
            404 => LogAndReturnNotFound(secretName),
            401 or 403 => LogAndReturnAccessDenied(secretName, ex),
            _ => LogAndReturnProviderUnavailable(ex)
        };

    private EncinaError LogAndReturnNotFound(string secretName)
    {
        Log.SecretNotFound(_logger, secretName);
        return SecretsErrors.NotFound(secretName);
    }

    private EncinaError LogAndReturnAccessDenied(string secretName, RequestFailedException ex)
    {
        Log.AccessDenied(_logger, secretName, ex.Message, ex);
        return SecretsErrors.AccessDenied(secretName, ex.Message);
    }

    private EncinaError LogAndReturnProviderUnavailable(RequestFailedException ex)
    {
        Log.ProviderUnavailable(_logger, ex.Message, ex);
        return SecretsErrors.ProviderUnavailable(ProviderName, ex);
    }
}
