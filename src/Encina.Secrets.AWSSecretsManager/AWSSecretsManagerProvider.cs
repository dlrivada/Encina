using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Secrets.AWSSecretsManager;

/// <summary>
/// AWS Secrets Manager implementation of <see cref="ISecretProvider"/>.
/// </summary>
/// <remarks>
/// Maps AWS SDK exceptions to <see cref="EncinaError"/> using <see cref="SecretsErrorCodes"/>:
/// <list type="bullet">
/// <item><see cref="ResourceNotFoundException"/> → <see cref="SecretsErrorCodes.NotFoundCode"/></item>
/// <item><c>AccessDeniedException</c> → <see cref="SecretsErrorCodes.AccessDeniedCode"/></item>
/// <item>Other failures → <see cref="SecretsErrorCodes.ProviderUnavailableCode"/></item>
/// </list>
/// </remarks>
public sealed class AWSSecretsManagerProvider : ISecretProvider
{
    private const string ProviderName = "AWSSecretsManager";

    private readonly IAmazonSecretsManager _client;
    private readonly ILogger<AWSSecretsManagerProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AWSSecretsManagerProvider"/> class.
    /// </summary>
    /// <param name="client">The AWS Secrets Manager client.</param>
    /// <param name="logger">The logger instance.</param>
    public AWSSecretsManagerProvider(IAmazonSecretsManager client, ILogger<AWSSecretsManagerProvider> logger)
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
            var response = await _client.GetSecretValueAsync(
                new GetSecretValueRequest { SecretId = name },
                cancellationToken);

            return new Secret(
                response.Name,
                response.SecretString,
                response.VersionId,
                null);
        }
        catch (ResourceNotFoundException)
        {
            return SecretsErrorCodes.NotFound(name);
        }
        catch (AmazonSecretsManagerException ex) when (ex.ErrorCode == "AccessDeniedException")
        {
            return SecretsErrorCodes.AccessDenied(name, ex.ErrorCode);
        }
        catch (AmazonSecretsManagerException ex)
        {
            return MapAwsException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        try
        {
            var response = await _client.GetSecretValueAsync(
                new GetSecretValueRequest
                {
                    SecretId = name,
                    VersionId = version
                },
                cancellationToken);

            return new Secret(
                response.Name,
                response.SecretString,
                response.VersionId,
                null);
        }
        catch (ResourceNotFoundException)
        {
            return SecretsErrorCodes.VersionNotFound(name, version);
        }
        catch (AmazonSecretsManagerException ex) when (ex.ErrorCode == "AccessDeniedException")
        {
            return SecretsErrorCodes.AccessDenied(name, ex.ErrorCode);
        }
        catch (AmazonSecretsManagerException ex)
        {
            return MapAwsException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SecretMetadata>> SetSecretAsync(string name, string value, SecretOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            // Try to update existing secret, create if not found
            try
            {
                var putResponse = await _client.PutSecretValueAsync(
                    new PutSecretValueRequest
                    {
                        SecretId = name,
                        SecretString = value
                    },
                    cancellationToken);

                return new SecretMetadata(
                    putResponse.Name,
                    putResponse.VersionId,
                    DateTime.UtcNow,
                    options?.ExpiresAtUtc);
            }
            catch (ResourceNotFoundException)
            {
                var createRequest = new CreateSecretRequest
                {
                    Name = name,
                    SecretString = value
                };

                if (options?.Tags is not null)
                {
                    createRequest.Tags = options.Tags
                        .Select(t => new Tag { Key = t.Key, Value = t.Value })
                        .ToList();
                }

                var createResponse = await _client.CreateSecretAsync(createRequest, cancellationToken);

                return new SecretMetadata(
                    createResponse.Name,
                    createResponse.VersionId,
                    DateTime.UtcNow,
                    options?.ExpiresAtUtc);
            }
        }
        catch (AmazonSecretsManagerException ex) when (ex.ErrorCode == "AccessDeniedException")
        {
            return SecretsErrorCodes.AccessDenied(name, ex.ErrorCode);
        }
        catch (AmazonSecretsManagerException ex)
        {
            return MapAwsException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            await _client.DeleteSecretAsync(
                new DeleteSecretRequest
                {
                    SecretId = name,
                    ForceDeleteWithoutRecovery = false
                },
                cancellationToken);

            return Unit.Default;
        }
        catch (ResourceNotFoundException)
        {
            return SecretsErrorCodes.NotFound(name);
        }
        catch (AmazonSecretsManagerException ex) when (ex.ErrorCode == "AccessDeniedException")
        {
            return SecretsErrorCodes.AccessDenied(name, ex.ErrorCode);
        }
        catch (AmazonSecretsManagerException ex)
        {
            return MapAwsException(ex, name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var names = new List<string>();
            string? nextToken = null;

            do
            {
                var response = await _client.ListSecretsAsync(
                    new ListSecretsRequest { NextToken = nextToken },
                    cancellationToken);

                names.AddRange(response.SecretList.Select(s => s.Name));
                nextToken = response.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));

            return Either<EncinaError, IEnumerable<string>>.Right(names);
        }
        catch (AmazonSecretsManagerException ex)
        {
            return MapAwsException(ex, "list");
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            await _client.DescribeSecretAsync(
                new DescribeSecretRequest { SecretId = name },
                cancellationToken);
            return true;
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
        catch (AmazonSecretsManagerException ex)
        {
            return MapAwsException(ex, name);
        }
    }

    private EncinaError MapAwsException(AmazonSecretsManagerException ex, string secretName)
    {
        _logger.LogWarning(ex, "AWS Secrets Manager operation failed for secret '{SecretName}'. ErrorCode: {ErrorCode}.",
            secretName, ex.ErrorCode);

        return SecretsErrorCodes.ProviderUnavailable(ProviderName, ex);
    }
}
