#pragma warning disable CA2012 // ValueTask should not be awaited multiple times - used via .AsTask().Result in sync property tests

using Encina.Security.AntiTampering.Nonce;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Time.Testing;

namespace Encina.PropertyTests.Security.AntiTampering;

/// <summary>
/// Property-based tests for <see cref="InMemoryNonceStore"/> invariants.
/// </summary>
public sealed class NonceStorePropertyTests
{
    #region Uniqueness Invariant

    [Property(MaxTest = 50)]
    public bool TryAdd_SameNonceTwice_SecondAlwaysFails(NonEmptyString nonce)
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var store = new InMemoryNonceStore(timeProvider);
        var expiry = TimeSpan.FromMinutes(10);

        var first = store.TryAddAsync(nonce.Get, expiry).AsTask().Result;
        var second = store.TryAddAsync(nonce.Get, expiry).AsTask().Result;

        return first && !second;
    }

    #endregion

    #region Distinct Nonces Invariant

    [Property(MaxTest = 30)]
    public bool TryAdd_DistinctNonces_AlwaysSucceed(NonEmptyString nonceA, NonEmptyString nonceB)
    {
        // Skip if inputs happen to be equal
        if (nonceA.Get == nonceB.Get) return true;

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var store = new InMemoryNonceStore(timeProvider);
        var expiry = TimeSpan.FromMinutes(10);

        var resultA = store.TryAddAsync(nonceA.Get, expiry).AsTask().Result;
        var resultB = store.TryAddAsync(nonceB.Get, expiry).AsTask().Result;

        return resultA && resultB;
    }

    #endregion

    #region Exists After Add

    [Property(MaxTest = 50)]
    public bool ExistsAsync_AfterSuccessfulAdd_ReturnsTrue(NonEmptyString nonce)
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var store = new InMemoryNonceStore(timeProvider);
        var expiry = TimeSpan.FromMinutes(10);

        var added = store.TryAddAsync(nonce.Get, expiry).AsTask().Result;
        if (!added) return false;

        return store.ExistsAsync(nonce.Get).AsTask().Result;
    }

    #endregion
}
