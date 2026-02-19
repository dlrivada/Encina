using Google.Api.Gax;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Grpc.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Secrets.GoogleSecretManager;

/// <summary>
/// Google Cloud Secret Manager implementation of <see cref="ISecretProvider"/>.
/// </summary>
/// <remarks>
/// Maps gRPC exceptions to <see cref="EncinaError"/> using <see cref="SecretsErrorCodes"/>:
/// <list type="bullet">
/// <item><see cref="RpcException"/> with <see cref="StatusCode.NotFound"/> → <see cref="SecretsErrorCodes.NotFoundCode"/></item>
/// <item><see cref="RpcException"/> with <see cref="StatusCode.PermissionDenied"/> → <see cref="SecretsErrorCodes.AccessDeniedCode"/></item>
/// <item>Other <see cref="RpcException"/> → <see cref="SecretsErrorCodes.ProviderUnavailableCode"/></item>
/// </list>
/// </remarks>
public sealed class GoogleSecretManagerProvider : ISecretProvider
{
    private const string ProviderName = "GoogleSecretManager";

    private readonly SecretManagerServiceClient _client;
    private readonly GoogleSecretManagerOptions _options;
    private readonly ILogger<GoogleSecretManagerProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleSecretManagerProvider"/> class.
    /// </summary>
    /// <param name="client">The Google Secret Manager client.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger instance.</param>
    public GoogleSecretManagerProvider(
        SecretManagerServiceClient client,
        IOptions<GoogleSecretManagerOptions> options,
        ILogger<GoogleSecretManagerProvider> logger)
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
            var secretVersionName = new SecretVersionName(_options.ProjectId, name, "latest");

            var response = await _client.AccessSecretVersionAsync(secretVersionName, cancellationToken);

            var value = response.Payload.Data.ToStringUtf8();
            var version = ExtractVersionId(response.Name);

            return new Secret(
                name,
                value,
                version,
                null);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return SecretsErrorCodes.NotFound(name);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            return SecretsErrorCodes.AccessDenied(name, ex.Status.Detail);
        }
        catch (RpcException ex)
        {
            return MapGrpcException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        try
        {
            var secretVersionName = new SecretVersionName(_options.ProjectId, name, version);

            var response = await _client.AccessSecretVersionAsync(secretVersionName, cancellationToken);

            var value = response.Payload.Data.ToStringUtf8();
            var actualVersion = ExtractVersionId(response.Name);

            return new Secret(
                name,
                value,
                actualVersion,
                null);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return SecretsErrorCodes.VersionNotFound(name, version);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            return SecretsErrorCodes.AccessDenied(name, ex.Status.Detail);
        }
        catch (RpcException ex)
        {
            return MapGrpcException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SecretMetadata>> SetSecretAsync(string name, string value, SecretOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            // Ensure the secret exists, create if it doesn't
            var secretName = new SecretName(_options.ProjectId, name);
            try
            {
                await _client.GetSecretAsync(secretName, cancellationToken);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                var createRequest = new CreateSecretRequest
                {
                    ParentAsProjectName = new ProjectName(_options.ProjectId),
                    SecretId = name,
                    Secret = new Google.Cloud.SecretManager.V1.Secret
                    {
                        Replication = new Replication
                        {
                            Automatic = new Replication.Types.Automatic()
                        }
                    }
                };

                if (options?.Tags is not null)
                {
                    foreach (var tag in options.Tags)
                    {
                        createRequest.Secret.Labels[tag.Key] = tag.Value;
                    }
                }

                await _client.CreateSecretAsync(createRequest, cancellationToken);
            }

            // Add a new version with the secret value
            var addRequest = new AddSecretVersionRequest
            {
                ParentAsSecretName = secretName,
                Payload = new SecretPayload
                {
                    Data = ByteString.CopyFromUtf8(value)
                }
            };

            var versionResponse = await _client.AddSecretVersionAsync(addRequest, cancellationToken);
            var version = ExtractVersionId(versionResponse.Name);

            return new SecretMetadata(
                name,
                version,
                DateTime.UtcNow,
                options?.ExpiresAtUtc);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            return SecretsErrorCodes.AccessDenied(name, ex.Status.Detail);
        }
        catch (RpcException ex)
        {
            return MapGrpcException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            var secretName = new SecretName(_options.ProjectId, name);

            await _client.DeleteSecretAsync(secretName, cancellationToken);

            return Unit.Default;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return SecretsErrorCodes.NotFound(name);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            return SecretsErrorCodes.AccessDenied(name, ex.Status.Detail);
        }
        catch (RpcException ex)
        {
            return MapGrpcException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var parentName = new ProjectName(_options.ProjectId);
            var request = new ListSecretsRequest
            {
                ParentAsProjectName = parentName
            };

            var names = new List<string>();
            var pagedResponse = _client.ListSecretsAsync(request);

            await foreach (var secret in pagedResponse.WithCancellation(cancellationToken))
            {
                // Extract the secret name from the full resource name
                // Format: projects/{project}/secrets/{secretName}
                var secretName = SecretName.Parse(secret.SecretName.ToString());
                names.Add(secretName.SecretId);
            }

            return Either<EncinaError, IEnumerable<string>>.Right(names);
        }
        catch (RpcException ex)
        {
            return MapGrpcException(ex, "list");
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            var secretName = new SecretName(_options.ProjectId, name);
            await _client.GetSecretAsync(secretName, cancellationToken);
            return true;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
        catch (RpcException ex)
        {
            return MapGrpcException(ex, name);
        }
    }

    /// <summary>
    /// Extracts the version ID from a full resource name.
    /// </summary>
    /// <param name="resourceName">Full resource name (e.g., <c>projects/my-project/secrets/my-secret/versions/1</c>).</param>
    /// <returns>The version ID string.</returns>
    private static string ExtractVersionId(string resourceName)
    {
        var versionName = SecretVersionName.Parse(resourceName);
        return versionName.SecretVersionId;
    }

    private EncinaError MapGrpcException(RpcException ex, string secretName)
    {
        _logger.LogWarning(ex, "Google Secret Manager operation failed for secret '{SecretName}'. StatusCode: {StatusCode}.",
            secretName, ex.StatusCode);

        return SecretsErrorCodes.ProviderUnavailable(ProviderName, ex);
    }
}
