using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="InMemoryReferenceTableStateStore"/>.
/// </summary>
/// <remarks>
/// The class is internal but accessible via InternalsVisibleTo from Encina.csproj.
/// </remarks>
public sealed class InMemoryReferenceTableStateStoreTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Country
    {
        public int Id { get; set; }
    }

    private sealed class Currency
    {
        public int Id { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private readonly InMemoryReferenceTableStateStore _store = new();

    // ────────────────────────────────────────────────────────────
    //  GetLastHashAsync
    // ────────────────────────────────────────────────────────────

    #region GetLastHashAsync

    [Fact]
    public async Task GetLastHashAsync_NoHashStored_ReturnsNull()
    {
        // Act
        var hash = await _store.GetLastHashAsync(typeof(Country));

        // Assert
        hash.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastHashAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _store.GetLastHashAsync(null!));
    }

    [Fact]
    public async Task GetLastHashAsync_AfterSave_ReturnsStoredHash()
    {
        // Arrange
        await _store.SaveHashAsync(typeof(Country), "abc123");

        // Act
        var hash = await _store.GetLastHashAsync(typeof(Country));

        // Assert
        hash.ShouldBe("abc123");
    }

    [Fact]
    public async Task GetLastHashAsync_DifferentEntityType_ReturnsNull()
    {
        // Arrange
        await _store.SaveHashAsync(typeof(Country), "abc123");

        // Act
        var hash = await _store.GetLastHashAsync(typeof(Currency));

        // Assert
        hash.ShouldBeNull();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  SaveHashAsync
    // ────────────────────────────────────────────────────────────

    #region SaveHashAsync

    [Fact]
    public async Task SaveHashAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _store.SaveHashAsync(null!, "hash"));
    }

    [Fact]
    public async Task SaveHashAsync_NullHash_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _store.SaveHashAsync(typeof(Country), null!));
    }

    [Fact]
    public async Task SaveHashAsync_OverwritesPreviousHash()
    {
        // Arrange
        await _store.SaveHashAsync(typeof(Country), "hash-v1");

        // Act
        await _store.SaveHashAsync(typeof(Country), "hash-v2");

        // Assert
        var hash = await _store.GetLastHashAsync(typeof(Country));
        hash.ShouldBe("hash-v2");
    }

    [Fact]
    public async Task SaveHashAsync_MultipleEntityTypes_StoresSeparately()
    {
        // Act
        await _store.SaveHashAsync(typeof(Country), "hash-country");
        await _store.SaveHashAsync(typeof(Currency), "hash-currency");

        // Assert
        var countryHash = await _store.GetLastHashAsync(typeof(Country));
        var currencyHash = await _store.GetLastHashAsync(typeof(Currency));
        countryHash.ShouldBe("hash-country");
        currencyHash.ShouldBe("hash-currency");
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  GetLastReplicationTimeAsync
    // ────────────────────────────────────────────────────────────

    #region GetLastReplicationTimeAsync

    [Fact]
    public async Task GetLastReplicationTimeAsync_NoTimeStored_ReturnsNull()
    {
        // Act
        var time = await _store.GetLastReplicationTimeAsync(typeof(Country));

        // Assert
        time.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastReplicationTimeAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _store.GetLastReplicationTimeAsync(null!));
    }

    [Fact]
    public async Task GetLastReplicationTimeAsync_AfterSave_ReturnsStoredTime()
    {
        // Arrange
        var time = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await _store.SaveReplicationTimeAsync(typeof(Country), time);

        // Act
        var result = await _store.GetLastReplicationTimeAsync(typeof(Country));

        // Assert
        result.ShouldBe(time);
    }

    [Fact]
    public async Task GetLastReplicationTimeAsync_DifferentEntityType_ReturnsNull()
    {
        // Arrange
        var time = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await _store.SaveReplicationTimeAsync(typeof(Country), time);

        // Act
        var result = await _store.GetLastReplicationTimeAsync(typeof(Currency));

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  SaveReplicationTimeAsync
    // ────────────────────────────────────────────────────────────

    #region SaveReplicationTimeAsync

    [Fact]
    public async Task SaveReplicationTimeAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _store.SaveReplicationTimeAsync(null!, DateTime.UtcNow));
    }

    [Fact]
    public async Task SaveReplicationTimeAsync_OverwritesPreviousTime()
    {
        // Arrange
        var time1 = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var time2 = new DateTime(2026, 1, 15, 13, 0, 0, DateTimeKind.Utc);
        await _store.SaveReplicationTimeAsync(typeof(Country), time1);

        // Act
        await _store.SaveReplicationTimeAsync(typeof(Country), time2);

        // Assert
        var result = await _store.GetLastReplicationTimeAsync(typeof(Country));
        result.ShouldBe(time2);
    }

    [Fact]
    public async Task SaveReplicationTimeAsync_MultipleEntityTypes_StoresSeparately()
    {
        // Arrange
        var countryTime = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var currencyTime = new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc);

        // Act
        await _store.SaveReplicationTimeAsync(typeof(Country), countryTime);
        await _store.SaveReplicationTimeAsync(typeof(Currency), currencyTime);

        // Assert
        var countryResult = await _store.GetLastReplicationTimeAsync(typeof(Country));
        var currencyResult = await _store.GetLastReplicationTimeAsync(typeof(Currency));
        countryResult.ShouldBe(countryTime);
        currencyResult.ShouldBe(currencyTime);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  IReferenceTableStateStore Interface
    // ────────────────────────────────────────────────────────────

    #region Interface Implementation

    [Fact]
    public void ImplementsIReferenceTableStateStore()
    {
        // Assert
        _store.ShouldBeAssignableTo<IReferenceTableStateStore>();
    }

    #endregion
}
