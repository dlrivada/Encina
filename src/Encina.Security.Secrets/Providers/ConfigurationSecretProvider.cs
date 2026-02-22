using System.Text.Json;
using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Providers;

/// <summary>
/// A secret reader that retrieves secrets from <see cref="IConfiguration"/>.
/// </summary>
/// <remarks>
/// <para>
/// This provider reads secrets from the .NET configuration system (appsettings.json,
/// user secrets, environment variables, etc.). It is ideal for development and
/// local environments where secrets are stored in configuration files.
/// </para>
/// <para>
/// By default, secrets are read from the <c>Secrets</c> configuration section.
/// Use a custom section path by setting the <c>sectionPath</c> constructor parameter.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // appsettings.json
/// {
///   "Secrets": {
///     "database-connection-string": "Server=localhost;...",
///     "api-key": "my-api-key"
///   }
/// }
///
/// // Register via DI
/// services.AddSingleton&lt;ISecretReader&gt;(sp =>
///     new ConfigurationSecretProvider(
///         sp.GetRequiredService&lt;IConfiguration&gt;(),
///         sp.GetRequiredService&lt;ILogger&lt;ConfigurationSecretProvider&gt;&gt;()));
/// </code>
/// </example>
public sealed class ConfigurationSecretProvider : ISecretReader
{
    /// <summary>
    /// The default configuration section path where secrets are read from.
    /// </summary>
    public const string DefaultSectionPath = "Secrets";

    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationSecretProvider> _logger;
    private readonly string _sectionPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSecretProvider"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sectionPath">The configuration section path to read secrets from. Defaults to <c>"Secrets"</c>.</param>
    public ConfigurationSecretProvider(
        IConfiguration configuration,
        ILogger<ConfigurationSecretProvider> logger,
        string sectionPath = DefaultSectionPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionPath);

        _configuration = configuration;
        _logger = logger;
        _sectionPath = sectionPath;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var key = $"{_sectionPath}:{secretName}";
        var value = _configuration[key];

        if (value is null)
        {
            Log.SecretNotFound(_logger, secretName, "configuration");
            return ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound(secretName));
        }

        Log.SecretRetrieved(_logger, secretName, "configuration");
        return ValueTask.FromResult<Either<EncinaError, string>>(value);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var key = $"{_sectionPath}:{secretName}";
        var section = _configuration.GetSection(key);

        if (!section.Exists())
        {
            Log.SecretNotFound(_logger, secretName, "configuration");
            return ValueTask.FromResult<Either<EncinaError, T>>(
                SecretsErrors.NotFound(secretName));
        }

        try
        {
            // Try binding to a complex object first
            var bound = section.Get<T>();

            if (bound is not null)
            {
                Log.SecretRetrieved(_logger, secretName, "configuration");
                return ValueTask.FromResult<Either<EncinaError, T>>(bound);
            }

            // Fall back to JSON deserialization from the raw value
            var rawValue = section.Value;

            if (rawValue is null)
            {
                return ValueTask.FromResult<Either<EncinaError, T>>(
                    SecretsErrors.DeserializationFailed(secretName, typeof(T)));
            }

            var deserialized = JsonSerializer.Deserialize<T>(rawValue);

            if (deserialized is null)
            {
                return ValueTask.FromResult<Either<EncinaError, T>>(
                    SecretsErrors.DeserializationFailed(secretName, typeof(T)));
            }

            Log.SecretRetrieved(_logger, secretName, "configuration");
            return ValueTask.FromResult<Either<EncinaError, T>>(deserialized);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            Log.SecretDeserializationFailed(_logger, secretName, typeof(T).Name, ex);
            return ValueTask.FromResult<Either<EncinaError, T>>(
                SecretsErrors.DeserializationFailed(secretName, typeof(T), ex));
        }
    }
}
