using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Core.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="InMemoryReferenceTableStateStore"/>.
/// </summary>
public sealed class InMemoryReferenceTableStateStoreTests
{
    private readonly InMemoryReferenceTableStateStore _sut = new();

    [Fact]
    public async Task GetLastHashAsync_WhenNoHash_ReturnsNull()
    {
        // Act
        var hash = await _sut.GetLastHashAsync(typeof(string));

        // Assert
        hash.ShouldBeNull();
    }

    [Fact]
    public async Task SaveHashAsync_ThenGetLastHashAsync_ReturnsStoredHash()
    {
        // Arrange
        var entityType = typeof(int);
        await _sut.SaveHashAsync(entityType, "abc123");

        // Act
        var hash = await _sut.GetLastHashAsync(entityType);

        // Assert
        hash.ShouldBe("abc123");
    }

    [Fact]
    public async Task SaveHashAsync_Overwrites_PreviousHash()
    {
        // Arrange
        var entityType = typeof(int);
        await _sut.SaveHashAsync(entityType, "hash1");
        await _sut.SaveHashAsync(entityType, "hash2");

        // Act
        var hash = await _sut.GetLastHashAsync(entityType);

        // Assert
        hash.ShouldBe("hash2");
    }

    [Fact]
    public async Task GetLastReplicationTimeAsync_WhenNoTime_ReturnsNull()
    {
        // Act
        var time = await _sut.GetLastReplicationTimeAsync(typeof(string));

        // Assert
        time.ShouldBeNull();
    }

    [Fact]
    public async Task SaveReplicationTimeAsync_ThenGet_ReturnsStoredTime()
    {
        // Arrange
        var entityType = typeof(int);
        var timeUtc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        await _sut.SaveReplicationTimeAsync(entityType, timeUtc);

        // Act
        var time = await _sut.GetLastReplicationTimeAsync(entityType);

        // Assert
        time.ShouldBe(timeUtc);
    }

    [Fact]
    public async Task SaveReplicationTimeAsync_Overwrites_PreviousTime()
    {
        // Arrange
        var entityType = typeof(int);
        var time1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var time2 = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        await _sut.SaveReplicationTimeAsync(entityType, time1);
        await _sut.SaveReplicationTimeAsync(entityType, time2);

        // Act
        var time = await _sut.GetLastReplicationTimeAsync(entityType);

        // Assert
        time.ShouldBe(time2);
    }

    [Fact]
    public void GetLastHashAsync_WithNullType_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.GetLastHashAsync(null!));
    }

    [Fact]
    public void SaveHashAsync_WithNullType_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.SaveHashAsync(null!, "hash"));
    }

    [Fact]
    public void SaveHashAsync_WithNullHash_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.SaveHashAsync(typeof(int), null!));
    }

    [Fact]
    public void GetLastReplicationTimeAsync_WithNullType_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.GetLastReplicationTimeAsync(null!));
    }

    [Fact]
    public void SaveReplicationTimeAsync_WithNullType_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.SaveReplicationTimeAsync(null!, DateTime.UtcNow));
    }

    [Fact]
    public async Task DifferentEntityTypes_HaveSeparateState()
    {
        // Arrange
        await _sut.SaveHashAsync(typeof(int), "int-hash");
        await _sut.SaveHashAsync(typeof(string), "string-hash");

        // Act & Assert
        (await _sut.GetLastHashAsync(typeof(int))).ShouldBe("int-hash");
        (await _sut.GetLastHashAsync(typeof(string))).ShouldBe("string-hash");
    }
}
