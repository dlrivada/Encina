using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Audit;

/// <summary>
/// Integration tests for <see cref="MartenTemporalKeyProvider"/> using a real PostgreSQL instance.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenTemporalKeyProviderIntegrationTests
{
    private readonly MartenFixture _fixture;

    public MartenTemporalKeyProviderIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_NewPeriod_PersistsToMarten()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);
        var period = $"test-{Guid.NewGuid():N}";

        // Act
        var result = await sut.GetOrCreateKeyAsync(period);

        // Assert
        result.IsRight.ShouldBeTrue("Should create key for new period");
        result.IfRight(info =>
        {
            info.Period.ShouldBe(period);
            info.KeyMaterial.Length.ShouldBe(32);
            info.Version.ShouldBe(1);
            info.Status.ShouldBe(TemporalKeyStatus.Active);
        });
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_ExistingPeriod_ReturnsSameKey()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);
        var period = $"test-{Guid.NewGuid():N}";

        // Act
        var first = await sut.GetOrCreateKeyAsync(period);
        var second = await sut.GetOrCreateKeyAsync(period);

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
    public async Task GetKeyAsync_AfterCreate_ReturnsKey()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);
        var period = $"test-{Guid.NewGuid():N}";

        await sut.GetOrCreateKeyAsync(period);

        // Act
        var result = await sut.GetKeyAsync(period);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(info => info.Period.ShouldBe(period));
    }

    [Fact]
    public async Task GetKeyAsync_NonExistentPeriod_ReturnsLeft()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        // Act
        var result = await sut.GetKeyAsync($"nonexistent-{Guid.NewGuid():N}");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DestroyKeysBeforeAsync_DestroysOlderPeriods()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        await sut.GetOrCreateKeyAsync("2020-01");
        await sut.GetOrCreateKeyAsync("2026-03");

        // Act
        var result = await sut.DestroyKeysBeforeAsync(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TemporalKeyGranularity.Monthly);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBeGreaterThan(0));

        // Verify destroyed
        var isDestroyed = await sut.IsKeyDestroyedAsync("2020-01");
        isDestroyed.IsRight.ShouldBeTrue();
        isDestroyed.IfRight(d => d.ShouldBeTrue());
    }

    [Fact]
    public async Task IsKeyDestroyedAsync_ActivePeriod_ReturnsFalse()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);
        var period = $"test-{Guid.NewGuid():N}";

        await sut.GetOrCreateKeyAsync(period);

        // Act
        var result = await sut.IsKeyDestroyedAsync(period);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(d => d.ShouldBeFalse());
    }

    [Fact]
    public async Task GetActiveKeysAsync_ReturnsOnlyActiveKeys()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        var activePeriod = $"active-{Guid.NewGuid():N}";
        await sut.GetOrCreateKeyAsync(activePeriod);

        // Act
        var result = await sut.GetActiveKeysAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(keys =>
        {
            keys.ShouldNotBeEmpty();
            keys.ShouldContain(k => k.Period == activePeriod);
            keys.ShouldAllBe(k => k.Status == TemporalKeyStatus.Active);
        });
    }
}
