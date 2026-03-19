using System.Collections.Concurrent;
using System.Security.Cryptography;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// In-memory implementation of <see cref="ITemporalKeyProvider"/> for testing and development scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This provider is designed for:
/// <list type="bullet">
/// <item><description>Unit and integration testing</description></item>
/// <item><description>Development and local debugging</description></item>
/// <item><description>Prototyping temporal crypto-shredding features</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Keys are stored in process memory and lost when the
/// process restarts. For production use, use <see cref="MartenTemporalKeyProvider"/>
/// which persists keys in Marten's PostgreSQL document store.
/// </para>
/// <para>
/// Thread-safe: Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for concurrent access
/// and <see cref="System.Threading.Lock"/> for per-period synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System, logger);
///
/// // Create or retrieve a key for a period
/// var keyResult = await keyProvider.GetOrCreateKeyAsync("2026-03");
///
/// // Destroy keys for crypto-shredding
/// var destroyResult = await keyProvider.DestroyKeysBeforeAsync(
///     DateTime.UtcNow.AddYears(-7), TemporalKeyGranularity.Monthly);
/// </code>
/// </example>
public sealed class InMemoryTemporalKeyProvider : ITemporalKeyProvider
{
    /// <summary>
    /// Required key size in bytes for AES-256 (256 bits).
    /// </summary>
    private const int DefaultKeySizeInBytes = 32;

    private readonly ConcurrentDictionary<string, PeriodState> _periods = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryTemporalKeyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTemporalKeyProvider"/> class.
    /// </summary>
    /// <param name="timeProvider">Provider for testable time-dependent logic.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public InMemoryTemporalKeyProvider(
        TimeProvider timeProvider,
        ILogger<InMemoryTemporalKeyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TemporalKeyInfo>> GetOrCreateKeyAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                Left(MartenAuditErrors.StoreUnavailable("GetOrCreateKey")));
        }

        try
        {
            var state = _periods.GetOrAdd(period, static _ => new PeriodState());

            lock (state.SyncRoot)
            {
                if (state.IsDestroyed)
                {
                    return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                        Left(MartenAuditErrors.KeyNotFound(period)));
                }

                if (state.Keys.Count > 0)
                {
                    var activeEntry = state.Keys[^1];
                    return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                        Right(MapToKeyInfo(period, activeEntry)));
                }

                // Create the first key for this period
                var keyMaterial = new byte[DefaultKeySizeInBytes];
                RandomNumberGenerator.Fill(keyMaterial);
                var version = 1;
                var now = _timeProvider.GetUtcNow();

                var entry = new TemporalKeyEntry(keyMaterial, version, TemporalKeyStatus.Active, now);
                state.Keys.Add(entry);
                state.CreatedAtUtc = now;

                _logger.LogDebug(
                    "Created initial temporal encryption key for period {Period}, version {Version}",
                    period,
                    version);

                return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                    Right(MapToKeyInfo(period, entry)));
            }
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                Left(MartenAuditErrors.StoreUnavailable("GetOrCreateKey", ex)));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TemporalKeyInfo>> GetKeyAsync(
        string period,
        int? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                Left(MartenAuditErrors.StoreUnavailable("GetKey")));
        }

        if (!_periods.TryGetValue(period, out var state))
        {
            return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                Left(MartenAuditErrors.KeyNotFound(period)));
        }

        lock (state.SyncRoot)
        {
            if (state.IsDestroyed)
            {
                return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                    Left(MartenAuditErrors.KeyNotFound(period)));
            }

            if (version.HasValue)
            {
                var entry = state.Keys.Find(k => k.Version == version.Value);
                if (entry is null)
                {
                    return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                        Left(MartenAuditErrors.KeyNotFound(period)));
                }

                return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                    Right(MapToKeyInfo(period, entry)));
            }

            // Return active (latest) key
            if (state.Keys.Count == 0)
            {
                return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                    Left(MartenAuditErrors.KeyNotFound(period)));
            }

            return ValueTask.FromResult<Either<EncinaError, TemporalKeyInfo>>(
                Right(MapToKeyInfo(period, state.Keys[^1])));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, int>> DestroyKeysBeforeAsync(
        DateTime olderThanUtc,
        TemporalKeyGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, int>>(
                Left(MartenAuditErrors.KeyDestructionFailed(olderThanUtc)));
        }

        try
        {
            var cutoff = new DateTimeOffset(olderThanUtc, TimeSpan.Zero);
            var destroyedCount = 0;
            var now = _timeProvider.GetUtcNow();

            foreach (var kvp in _periods)
            {
                var period = kvp.Key;
                var state = kvp.Value;

                lock (state.SyncRoot)
                {
                    if (state.IsDestroyed)
                    {
                        continue;
                    }

                    // Check if all keys in this period are older than cutoff
                    if (state.Keys.Count == 0 || !state.Keys.TrueForAll(k => k.CreatedAtUtc < cutoff))
                    {
                        continue;
                    }

                    // Destroy all keys
                    foreach (var key in state.Keys)
                    {
                        CryptographicOperations.ZeroMemory(key.KeyMaterial);
                    }

                    state.Keys.Clear();
                    state.IsDestroyed = true;
                    state.DestroyedAtUtc = now;
                    destroyedCount++;

                    _logger.LogInformation(
                        "Destroyed temporal keys for period {Period} (crypto-shredding)",
                        period);
                }
            }

            return ValueTask.FromResult<Either<EncinaError, int>>(Right(destroyedCount));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, int>>(
                Left(MartenAuditErrors.KeyDestructionFailed(olderThanUtc, ex)));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> IsKeyDestroyedAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(
                Left(MartenAuditErrors.StoreUnavailable("IsKeyDestroyed")));
        }

        if (!_periods.TryGetValue(period, out var state))
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(Right(false));
        }

        lock (state.SyncRoot)
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(Right(state.IsDestroyed));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<TemporalKeyInfo>>> GetActiveKeysAsync(
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<TemporalKeyInfo>>>(
                Left(MartenAuditErrors.StoreUnavailable("GetActiveKeys")));
        }

        var result = new List<TemporalKeyInfo>();

        foreach (var kvp in _periods)
        {
            var period = kvp.Key;
            var state = kvp.Value;

            lock (state.SyncRoot)
            {
                if (state.IsDestroyed)
                {
                    continue;
                }

                foreach (var entry in state.Keys.Where(k => k.Status == TemporalKeyStatus.Active))
                {
                    result.Add(MapToKeyInfo(period, entry));
                }
            }
        }

        result.Sort((a, b) => string.Compare(a.Period, b.Period, StringComparison.Ordinal));

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<TemporalKeyInfo>>>(
            Right<IReadOnlyList<TemporalKeyInfo>>(result));
    }

    /// <summary>
    /// Gets the number of periods tracked by the provider.
    /// </summary>
    /// <remarks>
    /// Intended for testing and diagnostics only.
    /// </remarks>
    public int PeriodCount => _periods.Count;

    /// <summary>
    /// Clears all periods and resets state.
    /// </summary>
    /// <remarks>
    /// Intended for testing only to reset state between tests.
    /// </remarks>
    public void Clear()
    {
        foreach (var kvp in _periods)
        {
            lock (kvp.Value.SyncRoot)
            {
                foreach (var key in kvp.Value.Keys)
                {
                    CryptographicOperations.ZeroMemory(key.KeyMaterial);
                }
            }
        }

        _periods.Clear();
    }

    /// <summary>
    /// Maps a <see cref="TemporalKeyEntry"/> to a <see cref="TemporalKeyInfo"/> record.
    /// </summary>
    private static TemporalKeyInfo MapToKeyInfo(string period, TemporalKeyEntry entry) => new()
    {
        Period = period,
        KeyMaterial = entry.KeyMaterial,
        Version = entry.Version,
        Status = entry.Status,
        CreatedAtUtc = entry.CreatedAtUtc
    };

    /// <summary>
    /// Per-period state container protected by its own <see cref="System.Threading.Lock"/>.
    /// </summary>
    private sealed class PeriodState
    {
        public Lock SyncRoot { get; } = new();
        public List<TemporalKeyEntry> Keys { get; } = [];
        public bool IsDestroyed { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? DestroyedAtUtc { get; set; }
    }

    /// <summary>
    /// Represents a single versioned encryption key for a time period.
    /// </summary>
    private sealed record TemporalKeyEntry(
        byte[] KeyMaterial,
        int Version,
        TemporalKeyStatus Status,
        DateTimeOffset CreatedAtUtc);
}
