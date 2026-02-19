using Microsoft.Extensions.Configuration;

namespace Encina.Secrets.Configuration;

/// <summary>
/// Extension methods for adding Encina secrets to <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds secrets from an <see cref="ISecretProvider"/> as a configuration source.
    /// </summary>
    /// <param name="builder">The configuration builder to extend.</param>
    /// <param name="serviceProvider">
    /// The service provider used to resolve <see cref="ISecretProvider"/>.
    /// This is typically obtained from <c>builder.Services.BuildServiceProvider()</c>
    /// or from a host builder context.
    /// </param>
    /// <returns>The configuration builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Secrets are loaded synchronously during application startup and made available
    /// through <see cref="IConfiguration"/>. For example, a secret named <c>"DatabasePassword"</c>
    /// becomes accessible as <c>Configuration["DatabasePassword"]</c>.
    /// </para>
    /// <para>
    /// An <see cref="ISecretProvider"/> must be registered in the service provider
    /// before this configuration source is built. If no provider is found, a warning
    /// is logged and no secrets are loaded.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs
    /// builder.Services.AddEncinaKeyVaultSecrets(options => { ... });
    /// var sp = builder.Services.BuildServiceProvider();
    /// builder.Configuration.AddEncinaSecrets(sp);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="serviceProvider"/> is null.</exception>
    public static IConfigurationBuilder AddEncinaSecrets(
        this IConfigurationBuilder builder,
        IServiceProvider serviceProvider)
    {
        return builder.AddEncinaSecrets(serviceProvider, _ => { });
    }

    /// <summary>
    /// Adds secrets from an <see cref="ISecretProvider"/> as a configuration source
    /// with custom options.
    /// </summary>
    /// <param name="builder">The configuration builder to extend.</param>
    /// <param name="serviceProvider">
    /// The service provider used to resolve <see cref="ISecretProvider"/>.
    /// </param>
    /// <param name="configure">An action to configure <see cref="SecretConfigurationOptions"/>.</param>
    /// <returns>The configuration builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload to configure prefix filtering, key delimiter mapping,
    /// and periodic reload behavior.
    /// </para>
    /// <para>
    /// <b>Prefix filtering:</b> Set <see cref="SecretConfigurationOptions.SecretPrefix"/>
    /// to load only secrets whose names start with the specified prefix.
    /// </para>
    /// <para>
    /// <b>Key mapping:</b> Set <see cref="SecretConfigurationOptions.KeyDelimiter"/>
    /// to map secret names to hierarchical configuration sections. For example, with
    /// delimiter <c>"--"</c>, a secret named <c>"Database--Password"</c> maps to
    /// <c>Configuration["Database:Password"]</c>.
    /// </para>
    /// <para>
    /// <b>Periodic reload:</b> Set <see cref="SecretConfigurationOptions.ReloadInterval"/>
    /// to periodically refresh secrets from the provider.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Configuration.AddEncinaSecrets(sp, options =>
    /// {
    ///     options.SecretPrefix = "myapp/";
    ///     options.KeyDelimiter = "/";
    ///     options.ReloadInterval = TimeSpan.FromMinutes(5);
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="serviceProvider"/>, or <paramref name="configure"/> is null.</exception>
    public static IConfigurationBuilder AddEncinaSecrets(
        this IConfigurationBuilder builder,
        IServiceProvider serviceProvider,
        Action<SecretConfigurationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SecretConfigurationOptions();
        configure(options);

        builder.Add(new SecretConfigurationSource(serviceProvider, options));

        return builder;
    }
}
