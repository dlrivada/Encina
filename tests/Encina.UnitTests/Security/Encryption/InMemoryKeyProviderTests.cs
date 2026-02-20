using Encina.Security.Encryption;
using FluentAssertions;

namespace Encina.UnitTests.Security.Encryption;

public sealed class InMemoryKeyProviderTests
{
    private readonly InMemoryKeyProvider _sut = new();

    #region GetKeyAsync

    [Fact]
    public async Task GetKeyAsync_ExistingKey_ReturnsKey()
    {
        var key = new byte[32];
        _sut.AddKey("key-1", key);

        var result = await _sut.GetKeyAsync("key-1");

        result.IsRight.Should().BeTrue();
        var retrieved = result.Match(Right: k => k, Left: _ => []);
        retrieved.Should().BeEquivalentTo(key);
    }

    [Fact]
    public async Task GetKeyAsync_NonExistingKey_ReturnsError()
    {
        var result = await _sut.GetKeyAsync("nonexistent");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task GetKeyAsync_CancelledToken_ReturnsError()
    {
        _sut.AddKey("key-1", new byte[32]);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _sut.GetKeyAsync("key-1", cts.Token);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region GetCurrentKeyIdAsync

    [Fact]
    public async Task GetCurrentKeyIdAsync_WithCurrentKey_ReturnsKeyId()
    {
        _sut.AddKey("key-1", new byte[32]);
        _sut.SetCurrentKey("key-1");

        var result = await _sut.GetCurrentKeyIdAsync();

        result.IsRight.Should().BeTrue();
        var keyId = result.Match(Right: id => id, Left: _ => string.Empty);
        keyId.Should().Be("key-1");
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_NoCurrentKey_ReturnsError()
    {
        var result = await _sut.GetCurrentKeyIdAsync();

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_CancelledToken_ReturnsError()
    {
        _sut.AddKey("key-1", new byte[32]);
        _sut.SetCurrentKey("key-1");
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _sut.GetCurrentKeyIdAsync(cts.Token);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region RotateKeyAsync

    [Fact]
    public async Task RotateKeyAsync_GeneratesNewKeyAndSetsCurrent()
    {
        var result = await _sut.RotateKeyAsync();

        result.IsRight.Should().BeTrue();
        var keyId = result.Match(Right: id => id, Left: _ => string.Empty);
        keyId.Should().StartWith("key-");
        _sut.Count.Should().Be(1);

        var currentResult = await _sut.GetCurrentKeyIdAsync();
        var currentKeyId = currentResult.Match(Right: id => id, Left: _ => string.Empty);
        currentKeyId.Should().Be(keyId);
    }

    [Fact]
    public async Task RotateKeyAsync_MultipleCalls_KeepsAllKeys()
    {
        await _sut.RotateKeyAsync();
        await _sut.RotateKeyAsync();
        await _sut.RotateKeyAsync();

        _sut.Count.Should().Be(3);
    }

    [Fact]
    public async Task RotateKeyAsync_GeneratedKeyIsValidFor256BitAes()
    {
        var result = await _sut.RotateKeyAsync();
        var keyId = result.Match(Right: id => id, Left: _ => string.Empty);

        var keyResult = await _sut.GetKeyAsync(keyId);
        var key = keyResult.Match(Right: k => k, Left: _ => []);
        key.Length.Should().Be(32);
    }

    [Fact]
    public async Task RotateKeyAsync_CancelledToken_ReturnsError()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _sut.RotateKeyAsync(cts.Token);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region AddKey / SetCurrentKey / Count / Clear

    [Fact]
    public void AddKey_OverwritesExistingKey()
    {
        var key1 = new byte[32];
        var key2 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };

        _sut.AddKey("key-1", key1);
        _sut.AddKey("key-1", key2);

        _sut.Count.Should().Be(1);
    }

    [Fact]
    public void Count_EmptyProvider_ReturnsZero()
    {
        _sut.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_RemovesAllKeysAndCurrentKey()
    {
        _sut.AddKey("key-1", new byte[32]);
        _sut.SetCurrentKey("key-1");

        _sut.Clear();

        _sut.Count.Should().Be(0);
    }

    [Fact]
    public async Task Clear_GetCurrentKeyIdAsync_ReturnsError()
    {
        _sut.AddKey("key-1", new byte[32]);
        _sut.SetCurrentKey("key-1");

        _sut.Clear();

        var result = await _sut.GetCurrentKeyIdAsync();
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Concurrency

    [Fact]
    public async Task ConcurrentRotation_AllKeysAreUnique()
    {
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _sut.RotateKeyAsync().AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var keyIds = results
            .Where(r => r.IsRight)
            .Select(r => r.Match(Right: id => id, Left: _ => string.Empty))
            .ToList();

        keyIds.Distinct().Count().Should().Be(keyIds.Count);
        _sut.Count.Should().Be(50);
    }

    #endregion
}
