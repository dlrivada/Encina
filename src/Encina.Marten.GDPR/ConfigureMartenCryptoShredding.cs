using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Marten.GDPR;

/// <summary>
/// Configures Marten's <see cref="StoreOptions"/> for crypto-shredding: wraps the System.Text.Json serializer with
/// <see cref="CryptoShredderSerializer"/> and turns off the async daemon's <c>SkipSerializationErrors</c>.
/// </summary>
/// <remarks>
/// <para>
/// It takes only <see cref="IServiceScopeFactory"/>: keys are resolved in a DI scope per serializer call, so the
/// scoped <see cref="PostgreSqlSubjectKeyProvider"/> is never captured by this singleton and its session never
/// needs the document store being built.
/// </para>
/// <para>
/// Marten's continuous projections skip serialization errors by default, which would dead-letter an event whose
/// personal data cannot be decrypted (for example during a key-store outage) and make the projection miss it for
/// good. With <c>SkipSerializationErrors = false</c> the shard pauses and resumes after recovery. The startup
/// validator fails if a later configuration turns it back on.
/// </para>
/// </remarks>
internal sealed class ConfigureMartenCryptoShredding : IConfigureOptions<StoreOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CryptoShreddingOptions> _options;
    private readonly ILogger<CryptoShredderSerializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureMartenCryptoShredding"/> class.
    /// </summary>
    /// <param name="scopeFactory">Creates the per-call DI scopes of the serializer.</param>
    /// <param name="options">The crypto-shredding options.</param>
    /// <param name="logger">Logger for the crypto shredder serializer.</param>
    public ConfigureMartenCryptoShredding(
        IServiceScopeFactory scopeFactory,
        IOptions<CryptoShreddingOptions> options,
        ILogger<CryptoShredderSerializer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Configure(StoreOptions options)
    {
        CryptoShredderSerializerFactory.Apply(options, _scopeFactory, _logger, _options.Value.AnonymizedPlaceholder);
        options.Projections.Errors.SkipSerializationErrors = false;
    }
}
