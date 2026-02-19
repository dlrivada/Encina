using Azure;
using Azure.Security.KeyVault.Secrets;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Secrets.AzureKeyVault;

/// <summary>
/// Azure Key Vault implementation of <see cref="ISecretProvider"/>.
/// </summary>
/// <remarks>
/// Maps Azure SDK exceptions to <see cref="EncinaError"/> using <see cref="SecretsErrorCodes"/>:
/// <list type="bullet">
/// <item>HTTP 404 → <see cref="SecretsErrorCodes.NotFoundCode"/></item>
/// <item>HTTP 403 → <see cref="SecretsErrorCodes.AccessDeniedCode"/></item>
/// <item>Other failures → <see cref="SecretsErrorCodes.ProviderUnavailableCode"/></item>
/// </list>
/// </remarks>
public sealed class KeyVaultSecretProvider : ISecretProvider
{
    private const string ProviderName = "AzureKeyVault";

    private readonly SecretClient _client;
    private readonly ILogger<KeyVaultSecretProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultSecretProvider"/> class.
    /// </summary>
    /// <param name="client">The Azure Key Vault secret client.</param>
    /// <param name="logger">The logger instance.</param>
    public KeyVaultSecretProvider(SecretClient client, ILogger<KeyVaultSecretProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            var response = await _client.GetSecretAsync(name, cancellationToken: cancellationToken);
            var kvSecret = response.Value;
            return new Secret(
                kvSecret.Name,
                kvSecret.Value,
                kvSecret.Properties.Version,
                kvSecret.Properties.ExpiresOn?.UtcDateTime);
        }
        catch (RequestFailedException ex)
        {
            return MapAzureException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        try
        {
            var response = await _client.GetSecretAsync(name, version, cancellationToken);
            var kvSecret = response.Value;
            return new Secret(
                kvSecret.Name,
                kvSecret.Value,
                kvSecret.Properties.Version,
                kvSecret.Properties.ExpiresOn?.UtcDateTime);
        }
        catch (RequestFailedException ex)
        {
            return ex.Status == 404
                ? SecretsErrorCodes.VersionNotFound(name, version)
                : MapAzureException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SecretMetadata>> SetSecretAsync(string name, string value, SecretOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var kvSecret = new KeyVaultSecret(name, value);

            if (options?.ExpiresAtUtc is not null)
            {
                kvSecret.Properties.ExpiresOn = options.ExpiresAtUtc;
            }

            if (options?.Tags is not null)
            {
                foreach (var tag in options.Tags)
                {
                    kvSecret.Properties.Tags[tag.Key] = tag.Value;
                }
            }

            var response = await _client.SetSecretAsync(kvSecret, cancellationToken);
            var props = response.Value.Properties;

            return new SecretMetadata(
                name,
                props.Version,
                props.CreatedOn?.UtcDateTime ?? DateTime.UtcNow,
                props.ExpiresOn?.UtcDateTime);
        }
        catch (RequestFailedException ex)
        {
            return MapAzureException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            await _client.StartDeleteSecretAsync(name, cancellationToken);
            return Unit.Default;
        }
        catch (RequestFailedException ex)
        {
            return MapAzureException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var names = new List<string>();
            await foreach (var secretProperties in _client.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                names.Add(secretProperties.Name);
            }

            return Either<EncinaError, IEnumerable<string>>.Right(names);
        }
        catch (RequestFailedException ex)
        {
            return MapAzureException(ex, "list");
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            await _client.GetSecretAsync(name, cancellationToken: cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
        catch (RequestFailedException ex)
        {
            return MapAzureException(ex, name);
        }
    }

    private EncinaError MapAzureException(RequestFailedException ex, string secretName)
    {
        _logger.LogWarning(ex, "Azure Key Vault operation failed for secret '{SecretName}'. Status: {StatusCode}, ErrorCode: {ErrorCode}.",
            secretName, ex.Status, ex.ErrorCode);

        return ex.Status switch
        {
            404 => SecretsErrorCodes.NotFound(secretName),
            403 or 401 => SecretsErrorCodes.AccessDenied(secretName, ex.ErrorCode),
            _ => SecretsErrorCodes.ProviderUnavailable(ProviderName, ex)
        };
    }
}
