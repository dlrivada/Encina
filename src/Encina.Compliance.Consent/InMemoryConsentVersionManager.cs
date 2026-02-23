using System.Collections.Concurrent;

using Encina.Compliance.Consent.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Consent;

/// <summary>
/// In-memory implementation of <see cref="IConsentVersionManager"/> for development, testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// Consent versions are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// purpose string, ensuring thread-safe concurrent access. Each purpose maps to its current
/// (latest) consent version.
/// </para>
/// <para>
/// Reconsent detection works by comparing the <see cref="ConsentRecord.ConsentVersionId"/> of an
/// existing consent with the current version's <see cref="ConsentVersion.VersionId"/>.
/// </para>
/// <para>
/// When an <see cref="IEncina"/> instance is provided, the version manager publishes a
/// <see cref="ConsentVersionChangedEvent"/> after a new version is successfully stored.
/// Event publishing is fire-and-forget — failures do not affect the version management operation.
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Versions are lost when the process restarts.
/// For production use, consider database-backed implementations.
/// </para>
/// </remarks>
public sealed class InMemoryConsentVersionManager : IConsentVersionManager
{
    private readonly ConcurrentDictionary<string, ConsentVersion> _versions = new(StringComparer.OrdinalIgnoreCase);
    private readonly IConsentStore _consentStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryConsentVersionManager> _logger;
    private readonly IEncina? _encina;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConsentVersionManager"/> class.
    /// </summary>
    /// <param name="consentStore">The consent store for looking up existing consent records during reconsent checks.</param>
    /// <param name="timeProvider">Time provider for event timestamps.</param>
    /// <param name="logger">Logger for structured version manager logging.</param>
    /// <param name="encina">
    /// Optional Encina mediator for publishing domain events.
    /// When <c>null</c>, no events are published (suitable for testing or simple deployments).
    /// </param>
    public InMemoryConsentVersionManager(
        IConsentStore consentStore,
        TimeProvider timeProvider,
        ILogger<InMemoryConsentVersionManager> logger,
        IEncina? encina = null)
    {
        ArgumentNullException.ThrowIfNull(consentStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _consentStore = consentStore;
        _timeProvider = timeProvider;
        _logger = logger;
        _encina = encina;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, ConsentVersion>> GetCurrentVersionAsync(
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        if (_versions.TryGetValue(purpose, out var version))
        {
            return ValueTask.FromResult<Either<EncinaError, ConsentVersion>>(Right(version));
        }

        return ValueTask.FromResult<Either<EncinaError, ConsentVersion>>(
            EncinaError.New($"No consent version found for purpose '{purpose}'."));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> PublishNewVersionAsync(
        ConsentVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        _versions[version.Purpose] = version;

        // Publish domain event (fire-and-forget)
        if (_encina is not null)
        {
            var @event = new ConsentVersionChangedEvent(
                version.Purpose,
                _timeProvider.GetUtcNow(),
                version.VersionId,
                version.RequiresExplicitReconsent);

            var result = await _encina.Publish(@event, cancellationToken).ConfigureAwait(false);

            result.Match(
                Right: _ => _logger.ConsentVersionEventPublished(nameof(ConsentVersionChangedEvent), version.Purpose),
                Left: error => _logger.ConsentEventPublishFailed(nameof(ConsentVersionChangedEvent), error.Message));
        }

        return unit;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> RequiresReconsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        // If no version is registered for this purpose, reconsent is not required
        if (!_versions.TryGetValue(purpose, out var currentVersion))
        {
            return Right<EncinaError, bool>(false);
        }

        // If the current version doesn't require explicit reconsent, skip
        if (!currentVersion.RequiresExplicitReconsent)
        {
            return Right<EncinaError, bool>(false);
        }

        // Get existing consent record
        var consentResult = await _consentStore
            .GetConsentAsync(subjectId, purpose, cancellationToken)
            .ConfigureAwait(false);

        if (consentResult.IsLeft)
        {
            return (EncinaError)consentResult;
        }

        var consentOpt = (Option<ConsentRecord>)consentResult;

        // No consent exists — no reconsent required (it's a first-time consent scenario)
        if (consentOpt.IsNone)
        {
            return Right<EncinaError, bool>(false);
        }

        var consent = (ConsentRecord)consentOpt;

        // Compare consent version with current version
        var versionMismatch = !string.Equals(consent.ConsentVersionId, currentVersion.VersionId, StringComparison.Ordinal);

        return Right<EncinaError, bool>(versionMismatch);
    }

    /// <summary>
    /// Gets all registered consent versions.
    /// </summary>
    /// <returns>All stored consent versions.</returns>
    /// <remarks>Intended for testing and diagnostics only.</remarks>
    public IReadOnlyList<ConsentVersion> GetAllVersions()
    {
        return _versions.Values.ToList();
    }

    /// <summary>
    /// Clears all consent versions from the store.
    /// </summary>
    /// <remarks>Intended for testing only to reset state between tests.</remarks>
    public void Clear()
    {
        _versions.Clear();
    }

    /// <summary>
    /// Gets the number of consent versions in the store.
    /// </summary>
    public int Count => _versions.Count;
}
