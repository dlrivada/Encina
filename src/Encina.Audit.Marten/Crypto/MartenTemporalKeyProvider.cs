using System.Security.Cryptography;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="ITemporalKeyProvider"/> using Marten's document store.
/// </summary>
/// <remarks>
/// <para>
/// Persists temporal encryption keys as <see cref="TemporalKeyDocument"/> entities in PostgreSQL
/// via Marten's <see cref="IDocumentSession"/>. Each key version is stored as a separate document
/// with the ID convention <c>"temporal:{period}:v{version}"</c>.
/// </para>
/// <para>
/// <b>Recommended for production use.</b> Keys survive process restarts and benefit from
/// PostgreSQL's ACID guarantees.
/// </para>
/// <para>
/// When temporal keys are destroyed via crypto-shredding, all key documents for the affected
/// periods are hard-deleted from PostgreSQL and <see cref="TemporalKeyDestroyedMarker"/> documents
/// are stored to prevent accidental re-creation.
/// </para>
/// </remarks>
public sealed class MartenTemporalKeyProvider : ITemporalKeyProvider
{
    /// <summary>
    /// Required key size in bytes for AES-256 (256 bits).
    /// </summary>
    private const int DefaultKeySizeInBytes = 32;

    private readonly IDocumentSession _session;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MartenTemporalKeyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenTemporalKeyProvider"/> class.
    /// </summary>
    /// <param name="session">The Marten document session for PostgreSQL operations.</param>
    /// <param name="timeProvider">Provider for testable time-dependent logic.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public MartenTemporalKeyProvider(
        IDocumentSession session,
        TimeProvider timeProvider,
        ILogger<MartenTemporalKeyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TemporalKeyInfo>> GetOrCreateKeyAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        try
        {
            // Check if period has been destroyed
            var destroyedDoc = await _session.LoadAsync<TemporalKeyDestroyedMarker>(
                TemporalPeriodHelper.FormatDestroyedMarkerId(period),
                cancellationToken).ConfigureAwait(false);

            if (destroyedDoc is not null)
            {
                return Left(MartenAuditErrors.KeyNotFound(period));
            }

            // Check for existing active key
            var existingKeys = await _session.Query<TemporalKeyDocument>()
                .Where(d => d.Period == period && d.Status == TemporalKeyStatus.Active)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (existingKeys.Count > 0)
            {
                var activeDoc = existingKeys.OrderByDescending(k => k.Version).First();
                return Right(MapToKeyInfo(activeDoc));
            }

            // Create the first key for this period
            var keyMaterial = new byte[DefaultKeySizeInBytes];
            RandomNumberGenerator.Fill(keyMaterial);
            var version = 1;
            var now = _timeProvider.GetUtcNow();

            var doc = new TemporalKeyDocument
            {
                Id = TemporalPeriodHelper.FormatKeyId(period, version),
                Period = period,
                KeyMaterial = keyMaterial,
                Version = version,
                Status = TemporalKeyStatus.Active,
                CreatedAtUtc = now
            };

            _session.Store(doc);
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Created initial temporal encryption key for period {Period}, version {Version}",
                period,
                version);

            return Right(MapToKeyInfo(doc));
        }
        catch (Exception ex)
        {
            return Left(MartenAuditErrors.StoreUnavailable("GetOrCreateKey", ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TemporalKeyInfo>> GetKeyAsync(
        string period,
        int? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        try
        {
            // Check if period has been destroyed
            var destroyedDoc = await _session.LoadAsync<TemporalKeyDestroyedMarker>(
                TemporalPeriodHelper.FormatDestroyedMarkerId(period),
                cancellationToken).ConfigureAwait(false);

            if (destroyedDoc is not null)
            {
                return Left(MartenAuditErrors.KeyNotFound(period));
            }

            if (version.HasValue)
            {
                var keyId = TemporalPeriodHelper.FormatKeyId(period, version.Value);
                var doc = await _session.LoadAsync<TemporalKeyDocument>(
                    keyId, cancellationToken).ConfigureAwait(false);

                if (doc is null)
                {
                    return Left(MartenAuditErrors.KeyNotFound(period));
                }

                return Right(MapToKeyInfo(doc));
            }

            // Return active (latest) key
            var activeKeys = await _session.Query<TemporalKeyDocument>()
                .Where(d => d.Period == period && d.Status == TemporalKeyStatus.Active)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (activeKeys.Count == 0)
            {
                return Left(MartenAuditErrors.KeyNotFound(period));
            }

            var activeKey = activeKeys.OrderByDescending(k => k.Version).First();
            return Right(MapToKeyInfo(activeKey));
        }
        catch (Exception ex)
        {
            return Left(MartenAuditErrors.StoreUnavailable("GetKey", ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> DestroyKeysBeforeAsync(
        DateTime olderThanUtc,
        TemporalKeyGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find all key documents with periods that predate the cutoff
            var allKeys = await _session.Query<TemporalKeyDocument>()
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            // Group by period and filter those whose period date is before the cutoff
            var cutoff = new DateTimeOffset(olderThanUtc, TimeSpan.Zero);
            var periodsToDestroy = allKeys
                .GroupBy(k => k.Period)
                .Where(g => TemporalPeriodHelper.TryParsePeriodToDate(g.Key, granularity, out var periodDate)
                            && periodDate < cutoff)
                .Select(g => g.Key)
                .ToList();

            // Also exclude already-destroyed periods
            var destroyedCount = 0;
            var now = _timeProvider.GetUtcNow();

            foreach (var period in periodsToDestroy)
            {
                var markerId = TemporalPeriodHelper.FormatDestroyedMarkerId(period);
                var existingMarker = await _session.LoadAsync<TemporalKeyDestroyedMarker>(
                    markerId, cancellationToken).ConfigureAwait(false);

                if (existingMarker is not null)
                {
                    continue; // Already destroyed
                }

                // Find all keys for this period
                var periodKeys = allKeys.Where(k => k.Period == period).ToList();

                // Hard-delete all key documents
                foreach (var key in periodKeys)
                {
                    _session.Delete(key);
                }

                // Store a destroyed marker
                var marker = new TemporalKeyDestroyedMarker
                {
                    Id = markerId,
                    Period = period,
                    DestroyedAtUtc = now,
                    KeyVersionsDestroyed = periodKeys.Count
                };

                _session.Store(marker);
                destroyedCount++;

                _logger.LogInformation(
                    "Destroyed temporal keys for period {Period}: {KeyVersionsDestroyed} key version(s) deleted",
                    period,
                    periodKeys.Count);
            }

            if (destroyedCount > 0)
            {
                await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return Right(destroyedCount);
        }
        catch (Exception ex)
        {
            return Left(MartenAuditErrors.KeyDestructionFailed(olderThanUtc, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsKeyDestroyedAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        try
        {
            var destroyedDoc = await _session.LoadAsync<TemporalKeyDestroyedMarker>(
                TemporalPeriodHelper.FormatDestroyedMarkerId(period),
                cancellationToken).ConfigureAwait(false);

            return Right(destroyedDoc is not null);
        }
        catch (Exception ex)
        {
            return Left(MartenAuditErrors.StoreUnavailable("IsKeyDestroyed", ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<TemporalKeyInfo>>> GetActiveKeysAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeKeys = await _session.Query<TemporalKeyDocument>()
                .Where(d => d.Status == TemporalKeyStatus.Active)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var result = activeKeys
                .OrderBy(k => k.Period)
                .ThenByDescending(k => k.Version)
                .Select(MapToKeyInfo)
                .ToList();

            return Right<IReadOnlyList<TemporalKeyInfo>>(result);
        }
        catch (Exception ex)
        {
            return Left(MartenAuditErrors.StoreUnavailable("GetActiveKeys", ex));
        }
    }

    /// <summary>
    /// Maps a <see cref="TemporalKeyDocument"/> to a <see cref="TemporalKeyInfo"/> record.
    /// </summary>
    private static TemporalKeyInfo MapToKeyInfo(TemporalKeyDocument doc) => new()
    {
        Period = doc.Period,
        KeyMaterial = doc.KeyMaterial,
        Version = doc.Version,
        Status = doc.Status,
        CreatedAtUtc = doc.CreatedAtUtc
    };
}
