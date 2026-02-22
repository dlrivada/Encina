using System.Text.Json;
using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Providers;

/// <summary>
/// A secret reader that retrieves secrets from environment variables.
/// </summary>
/// <remarks>
/// <para>
/// This provider is ideal for development, CI/CD, and containerized environments where
/// secrets are injected as environment variables. It implements only <see cref="ISecretReader"/>
/// following the Interface Segregation Principle.
/// </para>
/// <para>
/// Secret names are used directly as environment variable names. When
/// <see cref="SecretsOptions.KeyPrefix"/> is set, it is prepended to the secret name.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI
/// services.AddEncinaSecrets();
///
/// // Inject and use
/// var result = await secretReader.GetSecretAsync("DATABASE_URL");
/// </code>
/// </example>
public sealed class EnvironmentSecretProvider : ISecretReader
{
    private readonly ILogger<EnvironmentSecretProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentSecretProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public EnvironmentSecretProvider(ILogger<EnvironmentSecretProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var value = Environment.GetEnvironmentVariable(secretName);

        if (value is null)
        {
            Log.SecretNotFound(_logger, secretName, "environment");
            return ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound(secretName));
        }

        Log.SecretRetrieved(_logger, secretName, "environment");
        return ValueTask.FromResult<Either<EncinaError, string>>(value);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var value = Environment.GetEnvironmentVariable(secretName);

        if (value is null)
        {
            Log.SecretNotFound(_logger, secretName, "environment");
            return ValueTask.FromResult<Either<EncinaError, T>>(
                SecretsErrors.NotFound(secretName));
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<T>(value);

            if (deserialized is null)
            {
                return ValueTask.FromResult<Either<EncinaError, T>>(
                    SecretsErrors.DeserializationFailed(secretName, typeof(T)));
            }

            Log.SecretRetrieved(_logger, secretName, "environment");
            return ValueTask.FromResult<Either<EncinaError, T>>(deserialized);
        }
        catch (JsonException ex)
        {
            Log.SecretDeserializationFailed(_logger, secretName, typeof(T).Name, ex);
            return ValueTask.FromResult<Either<EncinaError, T>>(
                SecretsErrors.DeserializationFailed(secretName, typeof(T), ex));
        }
    }
}
