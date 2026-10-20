using Encina.Marten.GDPR.Diagnostics;

using Marten;
using Marten.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR;

/// <summary>
/// Installs crypto-shredding on Marten's <see cref="StoreOptions"/>: wraps Marten's System.Text.Json serializer
/// with a <see cref="CryptoShredderSerializer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Only Marten's <see cref="SystemTextJsonSerializer"/> is supported. Any other serializer (for example
/// <c>Marten.Newtonsoft</c>) fails closed with <see cref="CryptoShreddingConfigurationException"/>; it is never
/// passed through unencrypted.
/// </para>
/// <para>
/// Call it after the serializer configuration (<c>UseSystemTextJsonForSerialization</c>) and after any
/// <c>UseTypeInfoResolver(context)</c>: Marten puts a resolver installed later ahead of the modifier, which the
/// installation check then reports. <c>AddEncinaMartenGdpr</c> calls it through an <c>IConfigureOptions</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMarten(sp =>
/// {
///     var opts = new StoreOptions();
///     opts.Connection(connectionString);
///     CryptoShredderSerializerFactory.Apply(
///         opts,
///         sp.GetRequiredService&lt;IServiceScopeFactory&gt;(),
///         sp.GetRequiredService&lt;ILogger&lt;CryptoShredderSerializer&gt;&gt;());
///     return opts;
/// });
/// </code>
/// </example>
public static class CryptoShredderSerializerFactory
{
    /// <summary>
    /// The default placeholder value substituted for PII of forgotten subjects.
    /// </summary>
    public const string DefaultAnonymizedPlaceholder = "[REDACTED]";

    /// <summary>
    /// Wraps the store serializer with a <see cref="CryptoShredderSerializer"/>. Calling it again on options that
    /// are already wrapped does nothing.
    /// </summary>
    /// <param name="options">The Marten store options to configure.</param>
    /// <param name="scopeFactory">Creates the per-call DI scope that resolves the key provider and the forgotten-subject handler.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    /// <param name="anonymizedPlaceholder">The placeholder for PII of forgotten subjects. Defaults to <c>"[REDACTED]"</c>.</param>
    /// <exception cref="CryptoShreddingConfigurationException">
    /// The serializer is not Marten's System.Text.Json serializer, or its options are already read-only.
    /// </exception>
    public static void Apply(
        StoreOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<CryptoShredderSerializer> logger,
        string anonymizedPlaceholder = DefaultAnonymizedPlaceholder)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(anonymizedPlaceholder);

        var current = options.Serializer();
        if (current is CryptoShredderSerializer)
        {
            return;
        }

        if (current is not SystemTextJsonSerializer systemTextJson)
        {
            var componentType = current.GetType().FullName ?? current.GetType().Name;
            logger.CryptoShreddingInfrastructureInvalid(nameof(CryptoShreddingConfigurationProblem.SerializerNotSupported), componentType);
            throw new CryptoShreddingConfigurationException(CryptoShreddingConfigurationProblem.SerializerNotSupported, [], componentType);
        }

        var wrapper = new CryptoShredderSerializer(systemTextJson, scopeFactory, logger, anonymizedPlaceholder);
        options.Serializer(wrapper);
        logger.SerializerWrapped(wrapper.InstalledOptionsCount);
    }
}
