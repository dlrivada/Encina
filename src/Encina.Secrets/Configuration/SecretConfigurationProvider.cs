using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.Secrets.Configuration;

/// <summary>
/// A <see cref="ConfigurationProvider"/> that loads secrets from an <see cref="ISecretProvider"/>
/// into the .NET configuration system.
/// </summary>
/// <remarks>
/// <para>
/// On <see cref="Load"/>, this provider resolves <see cref="ISecretProvider"/> from the DI container,
/// lists available secrets, and populates the <see cref="ConfigurationProvider.Data"/> dictionary.
/// </para>
/// <para>
/// <b>ROP handling:</b> Uses <c>Match()</c> to branch on <c>Either&lt;EncinaError, T&gt;</c> results.
/// Only successful (<c>Right</c>) results are added to configuration. Errors (<c>Left</c>) are logged
/// but do not prevent the application from starting.
/// </para>
/// <para>
/// <b>Prefix filtering:</b> When <see cref="SecretConfigurationOptions.SecretPrefix"/> is set,
/// only secrets with matching names are loaded. The prefix is optionally stripped from keys.
/// </para>
/// <para>
/// <b>Key mapping:</b> Secret names are converted to configuration keys by replacing
/// the <see cref="SecretConfigurationOptions.KeyDelimiter"/> with the standard
/// configuration section separator (<c>:</c>).
/// </para>
/// <para>
/// <b>Reload support:</b> When <see cref="SecretConfigurationOptions.ReloadInterval"/> is set,
/// a <see cref="Timer"/> periodically calls <see cref="Load"/> to refresh secrets.
/// </para>
/// </remarks>
internal sealed class SecretConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SecretConfigurationOptions _options;
    private Timer? _reloadTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretConfigurationProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve <see cref="ISecretProvider"/>.</param>
    /// <param name="options">The configuration options.</param>
    public SecretConfigurationProvider(IServiceProvider serviceProvider, SecretConfigurationOptions options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    /// <summary>
    /// Loads secrets from the <see cref="ISecretProvider"/> into the configuration data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method uses synchronous blocking (<c>GetAwaiter().GetResult()</c>) because
    /// <see cref="ConfigurationProvider.Load"/> is a synchronous API invoked once during startup.
    /// This is consistent with Azure Key Vault Configuration Provider and other Encina startup patterns.
    /// </para>
    /// <para>
    /// If the <see cref="ISecretProvider"/> is not registered, a warning is logged and no secrets are loaded.
    /// </para>
    /// </remarks>
    public override void Load()
    {
        var secretProvider = _serviceProvider.GetService<ISecretProvider>();
        if (secretProvider is null)
        {
            LogWarning("ISecretProvider is not registered. No secrets will be loaded into configuration.");
            return;
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // List all secrets (synchronous blocking at startup - ConfigurationProvider.Load is synchronous)
        var listResult = secretProvider.ListSecretsAsync().AsTask().GetAwaiter().GetResult();

        listResult.Match(
            Right: secretNames =>
            {
                foreach (var secretName in secretNames)
                {
                    // Apply prefix filter if configured
                    if (_options.SecretPrefix is not null &&
                        !secretName.StartsWith(_options.SecretPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Get the secret value (synchronous blocking at startup)
                    var getResult = secretProvider.GetSecretAsync(secretName).AsTask().GetAwaiter().GetResult();

                    getResult.Match(
                        Right: secret =>
                        {
                            var key = MapSecretNameToConfigurationKey(secretName);
                            data[key] = secret.Value;
                        },
                        Left: error =>
                        {
                            LogWarning($"Failed to load secret '{secretName}': {error.Message}");
                        });
                }
            },
            Left: error =>
            {
                LogWarning($"Failed to list secrets: {error.Message}");
            });

        Data = data;

        // Set up reload timer if configured
        SetupReloadTimer();
    }

    /// <summary>
    /// Releases the reload timer resources.
    /// </summary>
    public void Dispose()
    {
        _reloadTimer?.Dispose();
        _reloadTimer = null;
    }

    /// <summary>
    /// Maps a secret name to a configuration key by applying prefix stripping
    /// and delimiter replacement.
    /// </summary>
    /// <param name="secretName">The original secret name from the provider.</param>
    /// <returns>The configuration key suitable for <see cref="IConfiguration"/> access.</returns>
    private string MapSecretNameToConfigurationKey(string secretName)
    {
        var key = secretName;

        // Strip prefix if configured
        if (_options.SecretPrefix is not null && _options.StripPrefix &&
            key.StartsWith(_options.SecretPrefix, StringComparison.OrdinalIgnoreCase))
        {
            key = key[_options.SecretPrefix.Length..];
        }

        // Replace delimiter with configuration section separator
        if (!string.IsNullOrEmpty(_options.KeyDelimiter))
        {
            key = key.Replace(_options.KeyDelimiter, ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
        }

        return key;
    }

    private void SetupReloadTimer()
    {
        if (_options.ReloadInterval is not null)
        {
            _reloadTimer?.Dispose();
            _reloadTimer = new Timer(
                _ =>
                {
                    Load();
                    OnReload();
                },
                state: null,
                dueTime: _options.ReloadInterval.Value,
                period: _options.ReloadInterval.Value);
        }
    }

    private void LogWarning(string message)
    {
        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<SecretConfigurationProvider>();
        logger?.LogWarning("{Message}", message);
    }
}
