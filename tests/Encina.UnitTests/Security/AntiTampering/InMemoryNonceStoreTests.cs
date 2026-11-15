using Encina.Diagnostics;
using Encina.Security.AntiTampering.Nonce;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Encina.UnitTests.Security.AntiTampering;

/// <summary>
/// Unit tests for <see cref="InMemoryNonceStore"/>.
/// Verifies add, duplicate detection, expiration, and cleanup behavior.
/// </summary>
public sealed class InMemoryNonceStoreTests : IDisposable
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly InMemoryNonceStore _sut;

    public InMemoryNonceStoreTests()
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _sut = new InMemoryNonceStore(_timeProvider);
    }

    public void Dispose() => _sut.Dispose();

    #region TryAddAsync

    [Fact]
    public async Task TryAddAsync_NewNonce_ReturnsTrue()
    {
        // Act
        var result = await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TryAddAsync_DuplicateNonce_ReturnsFalse()
    {
        // Arrange
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10));

        // Act
        var result = await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAddAsync_ExpiredNonce_CanBeReused()
    {
        // Arrange
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(1));

        // Advance past expiry but before the first 5-minute cleanup sweep, so the expired entry is
        // still stored and TryAddAsync must replace it
        _timeProvider.Advance(TimeSpan.FromMinutes(2));
        _sut.Count.ShouldBe(1);

        // Act
        var result = await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(5));

        // Assert
        result.ShouldBeTrue();
        (await _sut.ExistsAsync("nonce-1")).ShouldBeTrue();
    }

    [Fact]
    public async Task TryAddAsync_MultipleDistinctNonces_AllSucceed()
    {
        // Act & Assert
        (await _sut.TryAddAsync("nonce-a", TimeSpan.FromMinutes(10))).ShouldBeTrue();
        (await _sut.TryAddAsync("nonce-b", TimeSpan.FromMinutes(10))).ShouldBeTrue();
        (await _sut.TryAddAsync("nonce-c", TimeSpan.FromMinutes(10))).ShouldBeTrue();
    }

    #endregion

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_ExistingNonce_ReturnsTrue()
    {
        // Arrange
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10));

        // Act
        var result = await _sut.ExistsAsync("nonce-1");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonexistentNonce_ReturnsFalse()
    {
        // Act
        var result = await _sut.ExistsAsync("nonce-unknown");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ExpiredNonce_ReturnsFalse()
    {
        // Arrange
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(1));

        // Advance past expiry but before the first 5-minute cleanup sweep, so the expired entry is
        // still stored and ExistsAsync must drop it lazily
        _timeProvider.Advance(TimeSpan.FromMinutes(2));
        _sut.Count.ShouldBe(1);

        // Act
        var result = await _sut.ExistsAsync("nonce-1");

        // Assert
        result.ShouldBeFalse();
        _sut.Count.ShouldBe(0);
    }

    #endregion

    #region Guard Clauses (inline)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryAddAsync_NullOrWhitespaceNonce_ThrowsArgumentException(string? nonce)
    {
        // Act
        var act = async () => await _sut.TryAddAsync(nonce!, TimeSpan.FromMinutes(10));

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsAsync_NullOrWhitespaceNonce_ThrowsArgumentException(string? nonce)
    {
        // Act
        var act = async () => await _sut.ExistsAsync(nonce!);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion

    #region Cleanup timer

    [Fact]
    public async Task CleanupTimer_InjectedClockReachesTheInterval_RemovesExpiredNonces()
    {
        // Arrange: one nonce that expires before the first sweep, one that outlives it
        await _sut.TryAddAsync("short-lived", TimeSpan.FromMinutes(1));
        await _sut.TryAddAsync("long-lived", TimeSpan.FromMinutes(30));

        // Act: the injected clock, not the system clock, fires the 5-minute sweep
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // Assert
        _sut.Count.ShouldBe(1);
        (await _sut.ExistsAsync("long-lived")).ShouldBeTrue();
    }

    [Fact]
    public async Task CleanupTimer_SweepThrows_LogsTheExceptionTypeWithoutItsMessageAndKeepsRunning()
    {
        // Arrange: a clock whose reads throw while the first sweep runs
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var clock = new ThrowingReadTimeProvider(DateTimeOffset.UtcNow, secret);
        var logger = new FakeLogger<InMemoryNonceStore>();
        using var store = new InMemoryNonceStore(clock, logger);
        await store.TryAddAsync("nonce-1", TimeSpan.FromMinutes(1));

        // Act: the failing sweep must not escape the timer callback
        clock.ThrowOnRead = true;
        Should.NotThrow(() => clock.Advance(TimeSpan.FromMinutes(5)));
        clock.ThrowOnRead = false;

        // Assert: logged once, as an error with EventId 9108, exception type kept, message redacted
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        record.Id.Id.ShouldBe(9108);
        var logged = record.Exception.ShouldBeOfType<RedactedException>();
        logged.ToString().ShouldContain(nameof(InvalidOperationException));
        logged.ToString().ShouldNotContain(secret);
        store.Count.ShouldBe(1);

        // Act: the timer keeps running, so the next sweep removes the expired nonce
        clock.Advance(TimeSpan.FromMinutes(5));

        // Assert
        store.Count.ShouldBe(0);
        logger.Collector.Count.ShouldBe(1);
    }

    [Fact]
    public void Dispose_StopsTheCleanupTimer()
    {
        // Arrange
        var clock = new ThrowingReadTimeProvider(DateTimeOffset.UtcNow, "unused");
        var store = new InMemoryNonceStore(clock);

        // Act
        store.Dispose();
        clock.ThrowOnRead = true;
        clock.Advance(TimeSpan.FromMinutes(10));

        // Assert: no sweep ran after disposal, so the clock was never read
        clock.ReadsWhileThrowing.ShouldBe(0);
    }

    /// <summary>
    /// A fake clock whose <see cref="GetUtcNow"/> throws while <see cref="ThrowOnRead"/> is set,
    /// so a cleanup sweep fails inside the timer callback.
    /// </summary>
    private sealed class ThrowingReadTimeProvider(DateTimeOffset start, string message) : TimeProvider
    {
        private readonly FakeTimeProvider _inner = new(start);
        private int _readsWhileThrowing;

        public bool ThrowOnRead { get; set; }

        public int ReadsWhileThrowing => Volatile.Read(ref _readsWhileThrowing);

        public void Advance(TimeSpan delta) => _inner.Advance(delta);

        public override DateTimeOffset GetUtcNow()
        {
            if (ThrowOnRead)
            {
                Interlocked.Increment(ref _readsWhileThrowing);
                throw new InvalidOperationException(message);
            }

            return _inner.GetUtcNow();
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            _inner.CreateTimer(callback, state, dueTime, period);
    }

    #endregion

    #region Expired entry replaced concurrently (#3836)

    [Fact]
    public async Task ExistsAsync_NonceRefreshedBetweenTheExpiredReadAndTheRemoval_KeepsTheRefreshedNonce()
    {
        // Arrange: an expired nonce, and a clock that refreshes it through TryAddAsync on its next read,
        // which is after ExistsAsync has read the expired entry and before it removes it
        var clock = new ReadHookTimeProvider(DateTimeOffset.UtcNow);
        using var store = new InMemoryNonceStore(clock);
        await store.TryAddAsync("nonce-1", TimeSpan.FromMinutes(1));
        clock.Advance(TimeSpan.FromMinutes(2));
        var refreshed = false;
        clock.OnNextRead = () => refreshed = store.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10)).AsTask().GetAwaiter().GetResult();

        // Act
        var exists = await store.ExistsAsync("nonce-1");

        // Assert: ExistsAsync answers for the expired entry it read, but the refreshed nonce survives
        exists.ShouldBeFalse();
        refreshed.ShouldBeTrue();
        store.Count.ShouldBe(1);
        (await store.ExistsAsync("nonce-1")).ShouldBeTrue();
        (await store.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10))).ShouldBeFalse();
    }

    [Fact]
    public async Task TryRemoveExpired_EntryReplacedAfterTheSweepReadIt_KeepsTheReplacement()
    {
        // Arrange: the entry as the cleanup sweep reads it, then a TryAddAsync that replaces it
        var expiredAt = _timeProvider.GetUtcNow().AddMinutes(1);
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(1));
        _timeProvider.Advance(TimeSpan.FromMinutes(2));
        var observedBySweep = new KeyValuePair<string, DateTimeOffset>("nonce-1", expiredAt);
        (await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10))).ShouldBeTrue();

        // Act
        var removed = _sut.TryRemoveExpired(observedBySweep, _timeProvider.GetUtcNow());

        // Assert: the replay of the refreshed nonce is still rejected
        removed.ShouldBeFalse();
        (await _sut.ExistsAsync("nonce-1")).ShouldBeTrue();
        (await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10))).ShouldBeFalse();
    }

    [Fact]
    public async Task TryRemoveExpired_UnchangedExpiredEntry_RemovesIt()
    {
        // Arrange
        var expiredAt = _timeProvider.GetUtcNow().AddMinutes(1);
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(1));
        _timeProvider.Advance(TimeSpan.FromMinutes(2));

        // Act
        var removed = _sut.TryRemoveExpired(
            new KeyValuePair<string, DateTimeOffset>("nonce-1", expiredAt), _timeProvider.GetUtcNow());

        // Assert
        removed.ShouldBeTrue();
        _sut.Count.ShouldBe(0);
    }

    /// <summary>
    /// A fake clock that runs <see cref="OnNextRead"/> once, inside the next <see cref="GetUtcNow"/>
    /// call, so a test can act between the store's read of an entry and its use of the time.
    /// </summary>
    private sealed class ReadHookTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private readonly FakeTimeProvider _inner = new(start);

        public Action? OnNextRead { get; set; }

        public void Advance(TimeSpan delta) => _inner.Advance(delta);

        public override DateTimeOffset GetUtcNow()
        {
            var hook = OnNextRead;
            OnNextRead = null;
            hook?.Invoke();

            return _inner.GetUtcNow();
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            _inner.CreateTimer(callback, state, dueTime, period);
    }

    #endregion

    #region Default TimeProvider

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        // Act
        using var store = new InMemoryNonceStore(null);

        // Assert - no exception, store is functional
        store.ShouldNotBeNull();
    }

    #endregion
}
