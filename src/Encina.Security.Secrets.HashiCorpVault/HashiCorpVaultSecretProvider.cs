using System.Net;
using System.Text.Json;
using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.Core;

namespace Encina.Security.Secrets.HashiCorpVault;

/// <summary>
/// HashiCorp Vault implementation of secret reader, writer, and rotator
/// using the KV v2 secrets engine.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses <see cref="IVaultClient"/> from VaultSharp to interact with
/// HashiCorp Vault's KV v2 secrets engine. It implements all three ISP-compliant interfaces,
/// enabling read, write, and rotation operations.
/// </para>
/// <para>
/// <b>Thread safety:</b> This class is thread-safe. The underlying <see cref="IVaultClient"/>
/// is designed for concurrent use across threads.
/// </para>
/// <para>
/// <b>KV v2 value extraction:</b> KV v2 stores secrets as <c>Dictionary&lt;string, object&gt;</c>.
/// <see cref="GetSecretAsync(string, CancellationToken)"/> looks for a key named <c>"data"</c>
/// with a string value; if not found, the entire dictionary is serialized to JSON.
/// </para>
/// <para>
/// <b>Error handling:</b> VaultSharp exceptions are mapped to Encina error codes:
/// <list type="bullet">
/// <item>HTTP 404 → <see cref="SecretsErrors.NotFoundCode"/></item>
/// <item>HTTP 403 → <see cref="SecretsErrors.AccessDeniedCode"/></item>
/// <item>Other HTTP errors → <see cref="SecretsErrors.ProviderUnavailableCode"/></item>
/// <item>Rotation failures → <see cref="SecretsErrors.RotationFailedCode"/></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI (recommended)
/// services.AddHashiCorpVaultSecrets(vault =>
/// {
///     vault.VaultAddress = "https://vault.example.com:8200";
///     vault.AuthMethod = new TokenAuthMethodInfo("hvs.my-token");
/// });
///
/// // Inject and use
/// public class MyService(ISecretReader secretReader)
/// {
///     public async Task&lt;string&gt; GetApiKeyAsync(CancellationToken ct)
///     {
///         var result = await secretReader.GetSecretAsync("app/api-key", ct);
///         return result.Match(
///             Right: value =&gt; value,
///             Left: error =&gt; throw new InvalidOperationException(error.Message));
///     }
/// }
/// </code>
/// </example>
public sealed class HashiCorpVaultSecretProvider : ISecretReader, ISecretWriter, ISecretRotator
{
    private const string ProviderName = "HashiCorpVault";
    private const string DataKey = "data";

    private readonly IVaultClient _client;
    private readonly string _mountPoint;
    private readonly ILogger<HashiCorpVaultSecretProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashiCorpVaultSecretProvider"/> class.
    /// </summary>
    /// <param name="client">The VaultSharp client.</param>
    /// <param name="options">The HashiCorp Vault options containing the mount point.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="client"/>, <paramref name="options"/>,
    /// or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public HashiCorpVaultSecretProvider(
        IVaultClient client,
        HashiCorpVaultOptions options,
        ILogger<HashiCorpVaultSecretProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _mountPoint = options.MountPoint;
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
            var secret = await _client.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(secretName, mountPoint: _mountPoint)
                .ConfigureAwait(false);

            var value = ExtractStringValue(secret.Data.Data);

            Log.SecretRetrieved(_logger, secretName);
            return value;
        }
        catch (VaultApiException ex) when (IsNotFound(ex))
        {
            return LogAndReturnNotFound(secretName);
        }
        catch (VaultApiException ex) when (IsForbidden(ex))
        {
            return LogAndReturnAccessDenied(secretName, ex);
        }
        catch (VaultApiException ex)
        {
            return LogAndReturnProviderUnavailable(ex);
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
            var secret = await _client.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(secretName, mountPoint: _mountPoint)
                .ConfigureAwait(false);

            var raw = ExtractStringValue(secret.Data.Data);

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
        catch (VaultApiException ex) when (IsNotFound(ex))
        {
            Log.SecretNotFound(_logger, secretName);
            return SecretsErrors.NotFound(secretName);
        }
        catch (VaultApiException ex) when (IsForbidden(ex))
        {
            Log.AccessDenied(_logger, secretName, ex.Message, ex);
            return SecretsErrors.AccessDenied(secretName, ex.Message);
        }
        catch (VaultApiException ex)
        {
            Log.ProviderUnavailable(_logger, ex.Message, ex);
            return SecretsErrors.ProviderUnavailable(ProviderName, ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Writes the secret value to the KV v2 engine. The value is stored as a dictionary
    /// with a single key <c>"data"</c> containing the string value. KV v2 automatically
    /// creates a new version on every write.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> SetSecretAsync(
        string secretName,
        string value,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var data = new Dictionary<string, object> { [DataKey] = value };

            await _client.V1.Secrets.KeyValue.V2
                .WriteSecretAsync(secretName, data, mountPoint: _mountPoint)
                .ConfigureAwait(false);

            Log.SecretWritten(_logger, secretName);
            return Unit.Default;
        }
        catch (VaultApiException ex) when (IsForbidden(ex))
        {
            return LogAndReturnAccessDenied(secretName, ex);
        }
        catch (VaultApiException ex)
        {
            return LogAndReturnProviderUnavailable(ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Rotates a secret by reading the current value and writing it back, creating a
    /// new version in the KV v2 engine. In practice, the
    /// <c>SecretRotationCoordinator</c> generates the new value via
    /// <see cref="ISecretRotationHandler"/> and writes it through <see cref="ISecretWriter"/>.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> RotateSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        try
        {
            var current = await _client.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(secretName, mountPoint: _mountPoint)
                .ConfigureAwait(false);

            await _client.V1.Secrets.KeyValue.V2
                .WriteSecretAsync(secretName, current.Data.Data, mountPoint: _mountPoint)
                .ConfigureAwait(false);

            Log.SecretRotated(_logger, secretName);
            return Unit.Default;
        }
        catch (VaultApiException ex)
        {
            Log.RotationFailed(_logger, secretName, ex.Message, ex);
            return SecretsErrors.RotationFailed(secretName, ex.Message, ex);
        }
    }

    /// <summary>
    /// Extracts a single string value from a KV v2 data dictionary.
    /// </summary>
    /// <remarks>
    /// Looks for a key named <c>"data"</c> with a string value. If not found,
    /// serializes the entire dictionary to JSON.
    /// </remarks>
    private static string ExtractStringValue(IDictionary<string, object> data)
    {
        if (data.TryGetValue(DataKey, out var dataValue) && dataValue is string stringValue)
        {
            return stringValue;
        }

        return JsonSerializer.Serialize(data);
    }

    private static bool IsNotFound(VaultApiException ex) =>
        ex.HttpStatusCode == HttpStatusCode.NotFound;

    private static bool IsForbidden(VaultApiException ex) =>
        ex.HttpStatusCode == HttpStatusCode.Forbidden;

    private EncinaError LogAndReturnNotFound(string secretName)
    {
        Log.SecretNotFound(_logger, secretName);
        return SecretsErrors.NotFound(secretName);
    }

    private EncinaError LogAndReturnAccessDenied(string secretName, VaultApiException ex)
    {
        Log.AccessDenied(_logger, secretName, ex.Message, ex);
        return SecretsErrors.AccessDenied(secretName, ex.Message);
    }

    private EncinaError LogAndReturnProviderUnavailable(VaultApiException ex)
    {
        Log.ProviderUnavailable(_logger, ex.Message, ex);
        return SecretsErrors.ProviderUnavailable(ProviderName, ex);
    }
}
