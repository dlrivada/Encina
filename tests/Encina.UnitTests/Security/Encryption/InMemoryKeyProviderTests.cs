using Encina.Security.Encryption;
using Shouldly;

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

        result.IsRight.ShouldBeTrue();
        var retrieved = result.Match(Right: k => k, Left: _ => []);
        retrieved.ShouldBe(key);
    }

    [Fact]
    public async Task GetKeyAsync_NonExistingKey_ReturnsError()
    {
        var result = await _sut.GetKeyAsync("nonexistent");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetKeyAsync_CancelledToken_ReturnsError()
    {
        _sut.AddKey("key-1", new byte[32]);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _sut.GetKeyAsync("key-1", cts.Token);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetCurrentKeyIdAsync

    [Fact]
    public async Task GetCurrentKeyIdAsync_WithCurrentKey_ReturnsKeyId()
    {
        _sut.AddKey("key-1", new byte[32]);
        _sut.SetCurrentKey("key-1");

        var result = await _sut.GetCurrentKeyIdAsync();

        result.IsRight.ShouldBeTrue();
        var keyId = result.Match(Right: id => id, Left: _ => string.Empty);
        keyId.ShouldBe("key-1");
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_NoCurrentKey_ReturnsError()
    {
        var result = await _sut.GetCurrentKeyIdAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_CancelledToken_ReturnsError()
    {
        _sut.AddKey("key-1", new byte[32]);
        _sut.SetCurrentKey("key-1");
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _sut.GetCurrentKeyIdAsync(cts.Token);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region RotateKeyAsync

    [Fact]
    public async Task RotateKeyAsync_GeneratesNewKeyAndSetsCurrent()
    {
        var result = await _sut.RotateKeyAsync();

        result.IsRight.ShouldBeTrue();
        var keyId = result.Match(Right: id => id, Left: _ => string.Empty);
        keyId.ShouldStartWith("key-");
        _sut.Count.ShouldBe(1);

        var currentResult = await _sut.GetCurrentKeyIdAsync();
        var currentKeyId = currentResult.Match(Right: id => id, Left: _ => string.Empty);
        currentKeyId.ShouldBe(keyId);
    }

    [Fact]
    public async Task RotateKeyAsync_MultipleCalls_KeepsAllKeys()
    {
        await _sut.RotateKeyAsync();
        await _sut.RotateKeyAsync();
        await _sut.RotateKeyAsync();

        _sut.Count.ShouldBe(3);
    }

    [Fact]
    public async Task RotateKeyAsync_GeneratedKeyIsValidFor256BitAes()
    {
        var result = await _sut.RotateKeyAsync();
        var keyId = result.Match(Right: id => id, Left: _ => string.Empty);

        var keyResult = await _sut.GetKeyAsync(keyId);
        var key = keyResult.Match(Right: k => k, Left: _ => []);
        key.Length.ShouldBe(32);
    }

    [Fact]
    public async Task RotateKeyAsync_CancelledToken_ReturnsError()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _sut.RotateKeyAsync(cts.Token);

        result.IsLeft.ShouldBeTrue();
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

        _sut.Count.ShouldBe(1);
    }

    [Fact]
    public void Count_EmptyProvider_ReturnsZero()
    {
        _sut.Count.ShouldBe(0);
    }

    [Fact]
    public void Clear_RemovesAllKeysAndCurrentKey()
    {
        _sut.AddKey("key-1", new byte[32]);
        _sut.SetCurrentKey("key-1");

        _sut.Clear();

        _sut.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Clear_GetCurrentKeyIdAsync_ReturnsError()
    {
        _sut.AddKey("key-1", new byte[32]);
        _sut.SetCurrentKey("key-1");

        _sut.Clear();

        var result = await _sut.GetCurrentKeyIdAsync();
        result.IsLeft.ShouldBeTrue();
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

        keyIds.Distinct().Count().ShouldBe(keyIds.Count);
        _sut.Count.ShouldBe(50);
    }

    #endregion
}
