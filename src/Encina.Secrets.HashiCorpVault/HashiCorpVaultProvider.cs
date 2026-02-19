using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace Encina.Secrets.HashiCorpVault;

/// <summary>
/// HashiCorp Vault implementation of <see cref="ISecretProvider"/> using the KV v2 secrets engine.
/// </summary>
/// <remarks>
/// Maps VaultSharp exceptions to <see cref="EncinaError"/> using <see cref="SecretsErrorCodes"/>:
/// <list type="bullet">
/// <item><see cref="VaultApiException"/> with <c>HttpStatusCode</c> 404 → <see cref="SecretsErrorCodes.NotFoundCode"/></item>
/// <item><see cref="VaultApiException"/> with <c>HttpStatusCode</c> 403 → <see cref="SecretsErrorCodes.AccessDeniedCode"/></item>
/// <item>Other <see cref="VaultApiException"/> → <see cref="SecretsErrorCodes.ProviderUnavailableCode"/></item>
/// </list>
/// </remarks>
public sealed class HashiCorpVaultProvider : ISecretProvider
{
    private const string ProviderName = "HashiCorpVault";
    private const string DataKey = "data";

    private readonly IVaultClient _client;
    private readonly HashiCorpVaultOptions _options;
    private readonly ILogger<HashiCorpVaultProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashiCorpVaultProvider"/> class.
    /// </summary>
    /// <param name="client">The VaultSharp client.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger instance.</param>
    public HashiCorpVaultProvider(IVaultClient client, IOptions<HashiCorpVaultOptions> options, ILogger<HashiCorpVaultProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            var secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: name,
                mountPoint: _options.MountPoint);

            var value = ExtractSecretValue(secret.Data.Data);
            var version = secret.Data.Metadata.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return new Secret(
                name,
                value,
                version,
                null);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return SecretsErrorCodes.NotFound(name);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return SecretsErrorCodes.AccessDenied(name, ex.Message);
        }
        catch (VaultApiException ex)
        {
            return MapVaultException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        try
        {
            if (!int.TryParse(version, out var versionNumber))
            {
                return SecretsErrorCodes.VersionNotFound(name, version);
            }

            var secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: name,
                version: versionNumber,
                mountPoint: _options.MountPoint);

            var value = ExtractSecretValue(secret.Data.Data);
            var actualVersion = secret.Data.Metadata.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return new Secret(
                name,
                value,
                actualVersion,
                null);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return SecretsErrorCodes.VersionNotFound(name, version);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return SecretsErrorCodes.AccessDenied(name, ex.Message);
        }
        catch (VaultApiException ex)
        {
            return MapVaultException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SecretMetadata>> SetSecretAsync(string name, string value, SecretOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var data = new Dictionary<string, object> { [DataKey] = value };

            if (options?.Tags is not null)
            {
                foreach (var tag in options.Tags)
                {
                    data[tag.Key] = tag.Value;
                }
            }

            var result = await _client.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                path: name,
                data: data,
                mountPoint: _options.MountPoint);

            var createdAtUtc = DateTimeOffset.TryParse(result.Data.CreatedTime, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedTime)
                ? parsedTime.UtcDateTime
                : DateTime.UtcNow;

            return new SecretMetadata(
                name,
                result.Data.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                createdAtUtc,
                options?.ExpiresAtUtc);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return SecretsErrorCodes.AccessDenied(name, ex.Message);
        }
        catch (VaultApiException ex)
        {
            return MapVaultException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            // Delete all versions of the secret metadata (permanent delete)
            await _client.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(
                path: name,
                mountPoint: _options.MountPoint);

            return Unit.Default;
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return SecretsErrorCodes.NotFound(name);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return SecretsErrorCodes.AccessDenied(name, ex.Message);
        }
        catch (VaultApiException ex)
        {
            return MapVaultException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _client.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(
                path: string.Empty,
                mountPoint: _options.MountPoint);

            return Either<EncinaError, IEnumerable<string>>.Right(result.Data.Keys.AsEnumerable());
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // No secrets found — return empty list
            return Either<EncinaError, IEnumerable<string>>.Right(Enumerable.Empty<string>());
        }
        catch (VaultApiException ex)
        {
            return MapVaultException(ex, "list");
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            await _client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(
                path: name,
                mountPoint: _options.MountPoint);

            return true;
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (VaultApiException ex)
        {
            return MapVaultException(ex, name);
        }
    }

    private static string ExtractSecretValue(IDictionary<string, object> data)
    {
        if (data.TryGetValue(DataKey, out var value) && value is string stringValue)
        {
            return stringValue;
        }

        // If no "data" key, serialize the entire dictionary as JSON
        return System.Text.Json.JsonSerializer.Serialize(data);
    }

    private EncinaError MapVaultException(VaultApiException ex, string secretName)
    {
        _logger.LogWarning(ex, "HashiCorp Vault operation failed for secret '{SecretName}'. StatusCode: {StatusCode}.",
            secretName, ex.HttpStatusCode);

        return SecretsErrorCodes.ProviderUnavailable(ProviderName, ex);
    }
}
