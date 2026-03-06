using Encina.Marten.GDPR.Abstractions;

using Marten;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Marten.GDPR;

/// <summary>
/// Configures Marten's <see cref="StoreOptions"/> to wrap the active serializer with
/// <see cref="CryptoShredderSerializer"/> for transparent PII encryption.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IConfigureOptions{StoreOptions}"/> so that the serializer wrapping
/// occurs during DI container build, after all other Marten serializer configuration
/// (e.g., <c>UseSystemTextJsonForSerialization</c>) has been applied.
/// </para>
/// <para>
/// This ensures the crypto-shredding wrapper decorates the final serializer, preserving
/// all existing serializer settings (enum storage, casing, value casting).
/// </para>
/// </remarks>
internal sealed class ConfigureMartenCryptoShredding : IConfigureOptions<StoreOptions>
{
    private readonly ISubjectKeyProvider _subjectKeyProvider;
    private readonly IForgottenSubjectHandler _forgottenSubjectHandler;
    private readonly ILogger<CryptoShredderSerializer> _logger;
    private readonly IOptions<CryptoShreddingOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureMartenCryptoShredding"/> class.
    /// </summary>
    /// <param name="subjectKeyProvider">The provider for per-subject encryption keys.</param>
    /// <param name="forgottenSubjectHandler">The handler for forgotten subject encounters.</param>
    /// <param name="logger">Logger for the crypto shredder serializer.</param>
    /// <param name="options">The crypto-shredding options.</param>
    public ConfigureMartenCryptoShredding(
        ISubjectKeyProvider subjectKeyProvider,
        IForgottenSubjectHandler forgottenSubjectHandler,
        ILogger<CryptoShredderSerializer> logger,
        IOptions<CryptoShreddingOptions> options)
    {
        _subjectKeyProvider = subjectKeyProvider;
        _forgottenSubjectHandler = forgottenSubjectHandler;
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc />
    public void Configure(StoreOptions options)
    {
        CryptoShredderSerializerFactory.Apply(
            options,
            _subjectKeyProvider,
            _forgottenSubjectHandler,
            _logger,
            _options.Value.AnonymizedPlaceholder);
    }
}
