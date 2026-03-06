using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using Marten;

using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR;

/// <summary>
/// Factory helper that configures Marten's <see cref="StoreOptions"/> to use the
/// <see cref="CryptoShredderSerializer"/> as a decorator around the existing serializer.
/// </summary>
/// <remarks>
/// <para>
/// This factory preserves all existing serializer settings (enum storage, casing, value casting)
/// by wrapping the current <see cref="ISerializer"/> rather than replacing it. Call
/// <see cref="Apply"/> during Marten configuration to enable transparent crypto-shredding.
/// </para>
/// <para>
/// Typically invoked from <c>ServiceCollectionExtensions</c> via <c>ConfigureMarten</c>
/// callback or from an <see cref="Microsoft.Extensions.Options.IConfigureOptions{StoreOptions}"/>
/// implementation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMarten(opts =>
/// {
///     opts.Connection(connectionString);
///     CryptoShredderSerializerFactory.Apply(
///         opts, subjectKeyProvider, forgottenSubjectHandler, logger);
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
    /// Wraps the current serializer in Marten's <see cref="StoreOptions"/> with a
    /// <see cref="CryptoShredderSerializer"/> that provides transparent PII encryption.
    /// </summary>
    /// <param name="options">The Marten store options to configure.</param>
    /// <param name="subjectKeyProvider">The provider for per-subject encryption keys.</param>
    /// <param name="forgottenSubjectHandler">
    /// The handler invoked when a forgotten subject's data is encountered during deserialization.
    /// </param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    /// <param name="anonymizedPlaceholder">
    /// The placeholder value for PII of forgotten subjects. Defaults to <c>"[REDACTED]"</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method retrieves the current serializer from <paramref name="options"/> via
    /// <c>options.Serializer()</c>, wraps it with <see cref="CryptoShredderSerializer"/>,
    /// and sets the wrapped instance back via <c>options.Serializer(wrapper)</c>.
    /// </para>
    /// <para>
    /// Must be called <b>after</b> any other serializer configuration (e.g.,
    /// <c>UseSystemTextJsonForSerialization</c> or <c>UseNewtonsoftForSerialization</c>)
    /// to ensure the crypto-shredding wrapper decorates the final serializer.
    /// </para>
    /// </remarks>
    public static void Apply(
        StoreOptions options,
        ISubjectKeyProvider subjectKeyProvider,
        IForgottenSubjectHandler forgottenSubjectHandler,
        ILogger<CryptoShredderSerializer> logger,
        string anonymizedPlaceholder = DefaultAnonymizedPlaceholder)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subjectKeyProvider);
        ArgumentNullException.ThrowIfNull(forgottenSubjectHandler);
        ArgumentNullException.ThrowIfNull(logger);

        var innerSerializer = options.Serializer();

        var wrapper = new CryptoShredderSerializer(
            innerSerializer,
            subjectKeyProvider,
            forgottenSubjectHandler,
            logger,
            anonymizedPlaceholder);

        options.Serializer(wrapper);

        logger.SerializerWrapped(innerSerializer.GetType().Name);
    }
}
