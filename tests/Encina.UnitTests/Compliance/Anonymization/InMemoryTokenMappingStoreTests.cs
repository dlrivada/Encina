#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="InMemoryTokenMappingStore"/>.
/// </summary>
public class InMemoryTokenMappingStoreTests
{
    private readonly InMemoryTokenMappingStore _store = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeEmpty()
    {
        // Assert
        _store.Count.Should().Be(0);
    }

    #endregion

    #region StoreAsync Tests

    [Fact]
    public async Task StoreAsync_ValidMapping_ShouldSucceed()
    {
        // Arrange
        var mapping = CreateMapping();

        // Act
        var result = await _store.StoreAsync(mapping);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task StoreAsync_DuplicateToken_ShouldOverwriteExisting()
    {
        // Arrange
        var mapping1 = CreateMapping(token: "tok-1", hash: "hash-1", keyId: "key-1");
        var mapping2 = CreateMapping(token: "tok-1", hash: "hash-2", keyId: "key-2");

        // Act
        await _store.StoreAsync(mapping1);
        var result = await _store.StoreAsync(mapping2);

        // Assert - in-memory store uses indexer, so duplicate overwrites
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);

        // Verify the overwritten mapping has the new values
        var getResult = await _store.GetByTokenAsync("tok-1");
        getResult.IsRight.Should().BeTrue();
        getResult.Match(
            Right: option => option.Match(
                Some: m => m.KeyId.Should().Be("key-2"),
                None: () => Assert.Fail("Expected Some")),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task StoreAsync_MultipleMappings_ShouldIncrementCount()
    {
        // Arrange & Act
        await _store.StoreAsync(CreateMapping(token: "tok-1", hash: "hash-1"));
        await _store.StoreAsync(CreateMapping(token: "tok-2", hash: "hash-2"));
        await _store.StoreAsync(CreateMapping(token: "tok-3", hash: "hash-3"));

        // Assert
        _store.Count.Should().Be(3);
    }

    #endregion

    #region GetByTokenAsync Tests

    [Fact]
    public async Task GetByTokenAsync_ExistingToken_ShouldReturnSome()
    {
        // Arrange
        var mapping = CreateMapping(token: "tok-existing");
        await _store.StoreAsync(mapping);

        // Act
        var result = await _store.GetByTokenAsync("tok-existing");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: option => option.IsSome.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetByTokenAsync_NonExistingToken_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetByTokenAsync("non-existing");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: option => option.IsNone.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnCorrectMapping()
    {
        // Arrange
        var mapping = CreateMapping(token: "tok-verify", hash: "hash-verify", keyId: "key-verify");
        await _store.StoreAsync(mapping);

        // Act
        var result = await _store.GetByTokenAsync("tok-verify");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = result.Match(Right: o => o, Left: _ => Option<TokenMapping>.None);
        option.Match(
            Some: m =>
            {
                m.Token.Should().Be("tok-verify");
                m.OriginalValueHash.Should().Be("hash-verify");
                m.KeyId.Should().Be("key-verify");
                m.EncryptedOriginalValue.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
            },
            None: () => Assert.Fail("Expected Some"));
    }

    #endregion

    #region GetByOriginalValueHashAsync Tests

    [Fact]
    public async Task GetByOriginalValueHashAsync_ExistingHash_ShouldReturnSome()
    {
        // Arrange
        var mapping = CreateMapping(hash: "hash-existing");
        await _store.StoreAsync(mapping);

        // Act
        var result = await _store.GetByOriginalValueHashAsync("hash-existing");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: option => option.IsSome.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetByOriginalValueHashAsync_NonExistingHash_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetByOriginalValueHashAsync("non-existing-hash");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: option => option.IsNone.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetByOriginalValueHashAsync_ShouldReturnCorrectMapping()
    {
        // Arrange
        var mapping = CreateMapping(token: "tok-hash-lookup", hash: "hash-lookup", keyId: "key-lookup");
        await _store.StoreAsync(mapping);

        // Act
        var result = await _store.GetByOriginalValueHashAsync("hash-lookup");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = result.Match(Right: o => o, Left: _ => Option<TokenMapping>.None);
        option.Match(
            Some: m =>
            {
                m.Token.Should().Be("tok-hash-lookup");
                m.OriginalValueHash.Should().Be("hash-lookup");
                m.KeyId.Should().Be("key-lookup");
            },
            None: () => Assert.Fail("Expected Some"));
    }

    #endregion

    #region DeleteByKeyIdAsync Tests

    [Fact]
    public async Task DeleteByKeyIdAsync_ExistingKeyId_ShouldRemoveMappings()
    {
        // Arrange
        await _store.StoreAsync(CreateMapping(token: "tok-a", hash: "hash-a", keyId: "key-shared"));
        await _store.StoreAsync(CreateMapping(token: "tok-b", hash: "hash-b", keyId: "key-shared"));
        _store.Count.Should().Be(2);

        // Act
        var result = await _store.DeleteByKeyIdAsync("key-shared");

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteByKeyIdAsync_NonExistingKeyId_ShouldSucceed()
    {
        // Act
        var result = await _store.DeleteByKeyIdAsync("non-existing-key");

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteByKeyIdAsync_ShouldOnlyDeleteMatchingKeyId()
    {
        // Arrange
        await _store.StoreAsync(CreateMapping(token: "tok-a", hash: "hash-a", keyId: "key-1"));
        await _store.StoreAsync(CreateMapping(token: "tok-b", hash: "hash-b", keyId: "key-2"));
        _store.Count.Should().Be(2);

        // Act
        await _store.DeleteByKeyIdAsync("key-1");

        // Assert
        _store.Count.Should().Be(1);

        // Verify the correct one was kept
        var remaining = await _store.GetByTokenAsync("tok-b");
        remaining.Match(
            Right: option => option.IsSome.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));

        // Verify the deleted one is gone
        var deleted = await _store.GetByTokenAsync("tok-a");
        deleted.Match(
            Right: option => option.IsNone.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyStore_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithMappings_ShouldReturnAll()
    {
        // Arrange
        await _store.StoreAsync(CreateMapping(token: "tok-1", hash: "hash-1"));
        await _store.StoreAsync(CreateMapping(token: "tok-2", hash: "hash-2"));
        await _store.StoreAsync(CreateMapping(token: "tok-3", hash: "hash-3"));

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Should().HaveCount(3);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_ShouldRemoveAllMappings()
    {
        // Arrange
        await _store.StoreAsync(CreateMapping(token: "tok-1", hash: "hash-1"));
        await _store.StoreAsync(CreateMapping(token: "tok-2", hash: "hash-2"));
        _store.Count.Should().Be(2);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task ConcurrentStoreAsync_ShouldNotLoseData()
    {
        // Arrange
        const int count = 50;
        var tasks = Enumerable.Range(0, count)
            .Select(i => _store.StoreAsync(
                CreateMapping(token: $"tok-{i}", hash: $"hash-{i}", keyId: $"key-{i}")).AsTask());

        // Act
        await Task.WhenAll(tasks);

        // Assert
        _store.Count.Should().Be(count);
    }

    #endregion

    #region Helpers

    private static TokenMapping CreateMapping(
        string token = "tok-1",
        string hash = "hash-1",
        string keyId = "key-1")
    {
        return TokenMapping.Create(
            token: token,
            originalValueHash: hash,
            encryptedOriginalValue: [0x01, 0x02, 0x03],
            keyId: keyId);
    }

    #endregion
}
