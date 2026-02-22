using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.AwsSecretsManager;

/// <summary>
/// AWS Secrets Manager implementation of secret reader, writer, and rotator.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses <see cref="IAmazonSecretsManager"/> from the AWS SDK to interact with
/// AWS Secrets Manager. It implements all three ISP-compliant interfaces, enabling read, write,
/// and rotation operations.
/// </para>
/// <para>
/// <b>Thread safety:</b> This class is thread-safe. The underlying <see cref="IAmazonSecretsManager"/>
/// client is designed for concurrent use across threads.
/// </para>
/// <para>
/// <b>Create-or-update semantics:</b> <see cref="SetSecretAsync"/> uses <c>PutSecretValue</c> first.
/// If the secret does not exist (<see cref="ResourceNotFoundException"/>), it falls back to
/// <c>CreateSecret</c>, providing idempotent write behavior.
/// </para>
/// <para>
/// <b>Error handling:</b> AWS SDK exceptions are mapped to Encina error codes:
/// <list type="bullet">
/// <item><see cref="ResourceNotFoundException"/> → <see cref="SecretsErrors.NotFoundCode"/></item>
/// <item><c>AccessDeniedException</c> error code → <see cref="SecretsErrors.AccessDeniedCode"/></item>
/// <item>Other AWS errors → <see cref="SecretsErrors.ProviderUnavailableCode"/></item>
/// <item>Rotation failures → <see cref="SecretsErrors.RotationFailedCode"/></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI (recommended)
/// services.AddAwsSecretsManager(aws => aws.Region = RegionEndpoint.USEast1);
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
public sealed class AwsSecretsManagerProvider : ISecretReader, ISecretWriter, ISecretRotator
{
    private const string ProviderName = "AwsSecretsManager";

    private readonly IAmazonSecretsManager _client;
    private readonly ILogger<AwsSecretsManagerProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsSecretsManagerProvider"/> class.
    /// </summary>
    /// <param name="client">The AWS Secrets Manager client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="client"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public AwsSecretsManagerProvider(
        IAmazonSecretsManager client,
        ILogger<AwsSecretsManagerProvider> logger)
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
            var response = await _client
                .GetSecretValueAsync(
                    new GetSecretValueRequest { SecretId = secretName },
                    cancellationToken)
                .ConfigureAwait(false);

            Log.SecretRetrieved(_logger, secretName);
            return response.SecretString;
        }
        catch (ResourceNotFoundException)
        {
            return LogAndReturnNotFound(secretName);
        }
        catch (AmazonSecretsManagerException ex) when (IsAccessDenied(ex))
        {
            return LogAndReturnAccessDenied(secretName, ex);
        }
        catch (AmazonSecretsManagerException ex)
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
            var response = await _client
                .GetSecretValueAsync(
                    new GetSecretValueRequest { SecretId = secretName },
                    cancellationToken)
                .ConfigureAwait(false);

            var raw = response.SecretString;

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
        catch (ResourceNotFoundException)
        {
            Log.SecretNotFound(_logger, secretName);
            return SecretsErrors.NotFound(secretName);
        }
        catch (AmazonSecretsManagerException ex) when (IsAccessDenied(ex))
        {
            Log.AccessDenied(_logger, secretName, ex.Message, ex);
            return SecretsErrors.AccessDenied(secretName, ex.Message);
        }
        catch (AmazonSecretsManagerException ex)
        {
            Log.ProviderUnavailable(_logger, ex.Message, ex);
            return SecretsErrors.ProviderUnavailable(ProviderName, ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Implements a create-or-update pattern:
    /// <list type="number">
    /// <item>Attempts <c>PutSecretValue</c> to update an existing secret.</item>
    /// <item>If the secret does not exist (<see cref="ResourceNotFoundException"/>),
    /// falls back to <c>CreateSecret</c>.</item>
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
            await PutOrCreateSecretAsync(secretName, value, cancellationToken)
                .ConfigureAwait(false);

            Log.SecretWritten(_logger, secretName);
            return Unit.Default;
        }
        catch (AmazonSecretsManagerException ex) when (IsAccessDenied(ex))
        {
            return LogAndReturnAccessDenied(secretName, ex);
        }
        catch (AmazonSecretsManagerException ex)
        {
            return LogAndReturnProviderUnavailable(ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Rotates a secret by reading the current value and writing it back, creating a
    /// new version in AWS Secrets Manager. In practice, the
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
            var current = await _client
                .GetSecretValueAsync(
                    new GetSecretValueRequest { SecretId = secretName },
                    cancellationToken)
                .ConfigureAwait(false);

            await _client
                .PutSecretValueAsync(
                    new PutSecretValueRequest
                    {
                        SecretId = secretName,
                        SecretString = current.SecretString
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            Log.SecretRotated(_logger, secretName);
            return Unit.Default;
        }
        catch (AmazonSecretsManagerException ex)
        {
            Log.RotationFailed(_logger, secretName, ex.Message, ex);
            return SecretsErrors.RotationFailed(secretName, ex.Message, ex);
        }
    }

    private async Task PutOrCreateSecretAsync(
        string secretName,
        string value,
        CancellationToken cancellationToken)
    {
        try
        {
            await _client
                .PutSecretValueAsync(
                    new PutSecretValueRequest
                    {
                        SecretId = secretName,
                        SecretString = value
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ResourceNotFoundException)
        {
            await _client
                .CreateSecretAsync(
                    new CreateSecretRequest
                    {
                        Name = secretName,
                        SecretString = value
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            Log.SecretCreated(_logger, secretName);
        }
    }

    private static bool IsAccessDenied(AmazonSecretsManagerException ex) =>
        string.Equals(ex.ErrorCode, "AccessDeniedException", StringComparison.Ordinal);

    private EncinaError LogAndReturnNotFound(string secretName)
    {
        Log.SecretNotFound(_logger, secretName);
        return SecretsErrors.NotFound(secretName);
    }

    private EncinaError LogAndReturnAccessDenied(string secretName, AmazonSecretsManagerException ex)
    {
        Log.AccessDenied(_logger, secretName, ex.Message, ex);
        return SecretsErrors.AccessDenied(secretName, ex.Message);
    }

    private EncinaError LogAndReturnProviderUnavailable(AmazonSecretsManagerException ex)
    {
        Log.ProviderUnavailable(_logger, ex.Message, ex);
        return SecretsErrors.ProviderUnavailable(ProviderName, ex);
    }
}
