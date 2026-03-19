using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="InMemoryTemporalKeyProvider"/> key lifecycle management.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class InMemoryTemporalKeyProviderTests : IDisposable
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly InMemoryTemporalKeyProvider _sut;

    public InMemoryTemporalKeyProviderTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        _sut = new InMemoryTemporalKeyProvider(
            _timeProvider,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);
    }

    /// <summary>
    /// Creates a provider starting from the past so we can advance forward to simulate time.
    /// </summary>
    private static (FakeTimeProvider Time, InMemoryTemporalKeyProvider Provider) CreateWithPastTime()
    {
        var tp = new FakeTimeProvider(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var provider = new InMemoryTemporalKeyProvider(tp, NullLogger<InMemoryTemporalKeyProvider>.Instance);
        return (tp, provider);
    }

    public void Dispose() => _sut.Clear();

    [Fact]
    public async Task GetOrCreateKeyAsync_NewPeriod_CreatesKey()
    {
        // Act
        var result = await _sut.GetOrCreateKeyAsync("2026-03");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(info =>
        {
            info.Period.ShouldBe("2026-03");
            info.KeyMaterial.Length.ShouldBe(32);
            info.Version.ShouldBe(1);
            info.Status.ShouldBe(TemporalKeyStatus.Active);
            info.KeyId.ShouldBe("temporal:2026-03:v1");
            info.DestroyedAtUtc.ShouldBeNull();
        });
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_SamePeriod_ReturnsSameKey()
    {
        // Act
        var first = await _sut.GetOrCreateKeyAsync("2026-03");
        var second = await _sut.GetOrCreateKeyAsync("2026-03");

        // Assert
        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();

        byte[] firstKey = null!;
        byte[] secondKey = null!;
        first.IfRight(k => firstKey = k.KeyMaterial);
        second.IfRight(k => secondKey = k.KeyMaterial);

        firstKey.SequenceEqual(secondKey).ShouldBeTrue("Same period should return same key");
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_DifferentPeriods_ReturnsDifferentKeys()
    {
        // Act
        var march = await _sut.GetOrCreateKeyAsync("2026-03");
        var april = await _sut.GetOrCreateKeyAsync("2026-04");

        // Assert
        march.IsRight.ShouldBeTrue();
        april.IsRight.ShouldBeTrue();

        byte[] marchKey = null!;
        byte[] aprilKey = null!;
        march.IfRight(k => marchKey = k.KeyMaterial);
        april.IfRight(k => aprilKey = k.KeyMaterial);

        marchKey.SequenceEqual(aprilKey).ShouldBeFalse("Different periods should have different keys");
    }

    [Fact]
    public async Task GetKeyAsync_ExistingPeriod_ReturnsKey()
    {
        // Arrange
        await _sut.GetOrCreateKeyAsync("2026-03");

        // Act
        var result = await _sut.GetKeyAsync("2026-03");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(info => info.Period.ShouldBe("2026-03"));
    }

    [Fact]
    public async Task GetKeyAsync_NonExistentPeriod_ReturnsLeft()
    {
        // Act
        var result = await _sut.GetKeyAsync("2099-12");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DestroyKeysBeforeAsync_DestroysOlderPeriods()
    {
        // Arrange — use a provider that starts in the past
        var (tp, provider) = CreateWithPastTime();

        // Create keys at past time (2020-01)
        await provider.GetOrCreateKeyAsync("2020-01");
        tp.SetUtcNow(new DateTimeOffset(2020, 6, 15, 12, 0, 0, TimeSpan.Zero));
        await provider.GetOrCreateKeyAsync("2020-06");

        // Create a recent key
        tp.SetUtcNow(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        await provider.GetOrCreateKeyAsync("2026-03");

        // Act — destroy keys created before 2025
        var result = await provider.DestroyKeysBeforeAsync(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TemporalKeyGranularity.Monthly);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));

        // Verify old periods are destroyed
        var isDestroyed = await provider.IsKeyDestroyedAsync("2020-01");
        isDestroyed.IsRight.ShouldBeTrue();
        isDestroyed.IfRight(d => d.ShouldBeTrue());

        // Verify current period is not destroyed
        var currentKey = await provider.GetKeyAsync("2026-03");
        currentKey.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task IsKeyDestroyedAsync_ActivePeriod_ReturnsFalse()
    {
        // Arrange
        await _sut.GetOrCreateKeyAsync("2026-03");

        // Act
        var result = await _sut.IsKeyDestroyedAsync("2026-03");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(d => d.ShouldBeFalse());
    }

    [Fact]
    public async Task IsKeyDestroyedAsync_UnknownPeriod_ReturnsFalse()
    {
        // Act
        var result = await _sut.IsKeyDestroyedAsync("2099-12");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(d => d.ShouldBeFalse());
    }

    [Fact]
    public async Task GetActiveKeysAsync_ReturnsOnlyActiveKeys()
    {
        // Arrange — use a provider that starts in the past
        var (tp, provider) = CreateWithPastTime();

        await provider.GetOrCreateKeyAsync("2020-01");
        tp.SetUtcNow(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        await provider.GetOrCreateKeyAsync("2026-03");

        await provider.DestroyKeysBeforeAsync(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TemporalKeyGranularity.Monthly);

        // Act
        var result = await provider.GetActiveKeysAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(keys =>
        {
            keys.ShouldNotBeEmpty();
            keys.ShouldAllBe(k => k.Status == TemporalKeyStatus.Active);
            keys.ShouldContain(k => k.Period == "2026-03");
            keys.ShouldNotContain(k => k.Period == "2020-01");
        });
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_DestroyedPeriod_ReturnsLeft()
    {
        // Arrange — use a provider that starts in the past
        var (tp, provider) = CreateWithPastTime();

        await provider.GetOrCreateKeyAsync("2020-01");
        tp.SetUtcNow(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));

        await provider.DestroyKeysBeforeAsync(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TemporalKeyGranularity.Monthly);

        // Act — try to create a key for a destroyed period
        var result = await provider.GetOrCreateKeyAsync("2020-01");

        // Assert
        result.IsLeft.ShouldBeTrue("Should not create key for destroyed period");
    }

    [Fact]
    public void PeriodCount_ReflectsActiveKeys()
    {
        // Arrange
        _sut.GetOrCreateKeyAsync("2026-01").AsTask().GetAwaiter().GetResult();
        _sut.GetOrCreateKeyAsync("2026-02").AsTask().GetAwaiter().GetResult();
        _sut.GetOrCreateKeyAsync("2026-03").AsTask().GetAwaiter().GetResult();

        // Assert
        _sut.PeriodCount.ShouldBe(3);
    }

    [Fact]
    public void Clear_RemovesAllKeys()
    {
        // Arrange
        _sut.GetOrCreateKeyAsync("2026-01").AsTask().GetAwaiter().GetResult();
        _sut.GetOrCreateKeyAsync("2026-02").AsTask().GetAwaiter().GetResult();

        // Act
        _sut.Clear();

        // Assert
        _sut.PeriodCount.ShouldBe(0);
    }
}
