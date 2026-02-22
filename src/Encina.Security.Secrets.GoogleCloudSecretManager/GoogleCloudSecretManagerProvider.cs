using System.Text.Json;
using Encina.Security.Secrets.Abstractions;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Grpc.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.GoogleCloudSecretManager;

/// <summary>
/// Google Cloud Secret Manager implementation of secret reader, writer, and rotator.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses <see cref="SecretManagerServiceClient"/> from the Google Cloud SDK
/// to interact with Google Cloud Secret Manager. It implements all three ISP-compliant
/// interfaces, enabling read, write, and rotation operations.
/// </para>
/// <para>
/// <b>Thread safety:</b> This class is thread-safe. The underlying
/// <see cref="SecretManagerServiceClient"/> is designed for concurrent use across threads.
/// </para>
/// <para>
/// <b>Create-or-update semantics:</b> <see cref="SetSecretAsync"/> uses
/// <c>AddSecretVersion</c> first. If the secret does not exist (<c>StatusCode.NotFound</c>),
/// it falls back to <c>CreateSecret</c> + <c>AddSecretVersion</c>, providing idempotent
/// write behavior.
/// </para>
/// <para>
/// <b>Error handling:</b> gRPC exceptions are mapped to Encina error codes:
/// <list type="bullet">
/// <item><c>StatusCode.NotFound</c> → <see cref="SecretsErrors.NotFoundCode"/></item>
/// <item><c>StatusCode.PermissionDenied</c> → <see cref="SecretsErrors.AccessDeniedCode"/></item>
/// <item>Other gRPC errors → <see cref="SecretsErrors.ProviderUnavailableCode"/></item>
/// <item>Rotation failures → <see cref="SecretsErrors.RotationFailedCode"/></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI (recommended)
/// services.AddGoogleCloudSecretManager(gcp =>
/// {
///     gcp.ProjectId = "my-gcp-project";
/// });
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
public sealed class GoogleCloudSecretManagerProvider : ISecretReader, ISecretWriter, ISecretRotator
{
    private const string ProviderName = "GoogleCloudSecretManager";
    private const string LatestVersion = "latest";

    private readonly SecretManagerServiceClient _client;
    private readonly string _projectId;
    private readonly ILogger<GoogleCloudSecretManagerProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudSecretManagerProvider"/> class.
    /// </summary>
    /// <param name="client">The Google Cloud Secret Manager client.</param>
    /// <param name="options">The Google Cloud Secret Manager options containing the project ID.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="client"/>, <paramref name="options"/>,
    /// or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public GoogleCloudSecretManagerProvider(
        SecretManagerServiceClient client,
        GoogleCloudSecretManagerOptions options,
        ILogger<GoogleCloudSecretManagerProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _projectId = options.ProjectId;
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
            var versionName = new SecretVersionName(_projectId, secretName, LatestVersion);
            var response = await _client
                .AccessSecretVersionAsync(versionName, cancellationToken)
                .ConfigureAwait(false);

            var value = response.Payload.Data.ToStringUtf8();

            Log.SecretRetrieved(_logger, secretName);
            return value;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return LogAndReturnNotFound(secretName);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            return LogAndReturnAccessDenied(secretName, ex);
        }
        catch (RpcException ex)
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
            var versionName = new SecretVersionName(_projectId, secretName, LatestVersion);
            var response = await _client
                .AccessSecretVersionAsync(versionName, cancellationToken)
                .ConfigureAwait(false);

            var raw = response.Payload.Data.ToStringUtf8();

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
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            Log.SecretNotFound(_logger, secretName);
            return SecretsErrors.NotFound(secretName);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            Log.AccessDenied(_logger, secretName, ex.Message, ex);
            return SecretsErrors.AccessDenied(secretName, ex.Message);
        }
        catch (RpcException ex)
        {
            Log.ProviderUnavailable(_logger, ex.Message, ex);
            return SecretsErrors.ProviderUnavailable(ProviderName, ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Implements a create-or-update pattern:
    /// <list type="number">
    /// <item>Attempts <c>AddSecretVersion</c> to update an existing secret.</item>
    /// <item>If the secret does not exist (<c>StatusCode.NotFound</c>),
    /// creates the secret container first, then adds the version.</item>
    /// </list>
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
            await AddOrCreateSecretAsync(secretName, value, cancellationToken)
                .ConfigureAwait(false);

            Log.SecretWritten(_logger, secretName);
            return Unit.Default;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            return LogAndReturnAccessDenied(secretName, ex);
        }
        catch (RpcException ex)
        {
            return LogAndReturnProviderUnavailable(ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Rotates a secret by reading the current value and writing it back as a new version
    /// in Google Cloud Secret Manager. In practice, the
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
            var versionName = new SecretVersionName(_projectId, secretName, LatestVersion);
            var current = await _client
                .AccessSecretVersionAsync(versionName, cancellationToken)
                .ConfigureAwait(false);

            var payload = new SecretPayload { Data = current.Payload.Data };
            var parent = new SecretName(_projectId, secretName);

            await _client
                .AddSecretVersionAsync(parent, payload, cancellationToken)
                .ConfigureAwait(false);

            Log.SecretRotated(_logger, secretName);
            return Unit.Default;
        }
        catch (RpcException ex)
        {
            Log.RotationFailed(_logger, secretName, ex.Message, ex);
            return SecretsErrors.RotationFailed(secretName, ex.Message, ex);
        }
    }

    private async Task AddOrCreateSecretAsync(
        string secretName,
        string value,
        CancellationToken cancellationToken)
    {
        var payload = new SecretPayload
        {
            Data = ByteString.CopyFromUtf8(value)
        };

        try
        {
            var parent = new SecretName(_projectId, secretName);
            await _client
                .AddSecretVersionAsync(parent, payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            // Secret container does not exist yet; create it, then add the version
            var projectName = new ProjectName(_projectId);
            var secret = new Secret
            {
                Replication = new Replication
                {
                    Automatic = new Replication.Types.Automatic()
                }
            };

            await _client
                .CreateSecretAsync(projectName, secretName, secret, cancellationToken)
                .ConfigureAwait(false);

            var parent = new SecretName(_projectId, secretName);
            await _client
                .AddSecretVersionAsync(parent, payload, cancellationToken)
                .ConfigureAwait(false);

            Log.SecretCreated(_logger, secretName);
        }
    }

    private EncinaError LogAndReturnNotFound(string secretName)
    {
        Log.SecretNotFound(_logger, secretName);
        return SecretsErrors.NotFound(secretName);
    }

    private EncinaError LogAndReturnAccessDenied(string secretName, RpcException ex)
    {
        Log.AccessDenied(_logger, secretName, ex.Message, ex);
        return SecretsErrors.AccessDenied(secretName, ex.Message);
    }

    private EncinaError LogAndReturnProviderUnavailable(RpcException ex)
    {
        Log.ProviderUnavailable(_logger, ex.Message, ex);
        return SecretsErrors.ProviderUnavailable(ProviderName, ex);
    }
}
