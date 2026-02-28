#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Time.Testing;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="InMemoryKeyProvider"/>.
/// </summary>
public class InMemoryKeyProviderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateInitialKey()
    {
        // Act
        var provider = new InMemoryKeyProvider();

        // Assert
        provider.Count.Should().Be(1);
    }

    [Fact]
    public async Task Constructor_WithTimeProvider_ShouldUseProvidedTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2026, 2, 28, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);

        // Act
        var provider = new InMemoryKeyProvider(timeProvider);

        // Assert
        var listResult = await provider.ListKeysAsync();
        listResult.IsRight.Should().BeTrue();
        var keys = listResult.Match(Right: k => k, Left: _ => []);
        keys.Should().HaveCount(1);
        keys[0].CreatedAtUtc.Should().Be(fixedTime);
    }

    #endregion

    #region GetActiveKeyIdAsync Tests

    [Fact]
    public async Task GetActiveKeyIdAsync_ShouldReturnActiveKeyId()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();

        // Act
        var result = await provider.GetActiveKeyIdAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var keyId = result.Match(Right: id => id, Left: _ => string.Empty);
        keyId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetActiveKeyIdAsync_AfterClear_ShouldReturnError()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();
        provider.Clear();

        // Act
        var result = await provider.GetActiveKeyIdAsync();

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.NoActiveKeyCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region GetKeyAsync Tests

    [Fact]
    public async Task GetKeyAsync_ExistingKey_ShouldReturnKeyBytes()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();
        var activeKeyIdResult = await provider.GetActiveKeyIdAsync();
        var activeKeyId = activeKeyIdResult.Match(Right: id => id, Left: _ => string.Empty);

        // Act
        var result = await provider.GetKeyAsync(activeKeyId);

        // Assert
        result.IsRight.Should().BeTrue();
        var keyBytes = result.Match(Right: b => b, Left: _ => []);
        keyBytes.Should().NotBeEmpty();
        keyBytes.Should().HaveCount(32); // 256-bit key
    }

    [Fact]
    public async Task GetKeyAsync_NonExistingKey_ShouldReturnError()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();

        // Act
        var result = await provider.GetKeyAsync("non-existing-key-id");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.KeyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region RotateKeyAsync Tests

    [Fact]
    public async Task RotateKeyAsync_ShouldCreateNewActiveKey()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();
        var originalKeyIdResult = await provider.GetActiveKeyIdAsync();
        var originalKeyId = originalKeyIdResult.Match(Right: id => id, Left: _ => string.Empty);

        // Act
        var rotateResult = await provider.RotateKeyAsync(originalKeyId);

        // Assert
        rotateResult.IsRight.Should().BeTrue();
        var newKeyInfo = rotateResult.Match(Right: k => k, Left: _ => null!);
        newKeyInfo.Should().NotBeNull();
        newKeyInfo.IsActive.Should().BeTrue();
        newKeyInfo.KeyId.Should().NotBe(originalKeyId);

        // Verify the new key is now the active one
        var activeKeyIdResult = await provider.GetActiveKeyIdAsync();
        var activeKeyId = activeKeyIdResult.Match(Right: id => id, Left: _ => string.Empty);
        activeKeyId.Should().Be(newKeyInfo.KeyId);
    }

    [Fact]
    public async Task RotateKeyAsync_ShouldDeactivatePreviousKey()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();
        var originalKeyIdResult = await provider.GetActiveKeyIdAsync();
        var originalKeyId = originalKeyIdResult.Match(Right: id => id, Left: _ => string.Empty);

        // Act
        await provider.RotateKeyAsync(originalKeyId);

        // Assert - old key should still be accessible but not active
        var listResult = await provider.ListKeysAsync();
        var keys = listResult.Match(Right: k => k, Left: _ => []);
        var oldKey = keys.FirstOrDefault(k => k.KeyId == originalKeyId);
        oldKey.Should().NotBeNull();
        oldKey!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RotateKeyAsync_ShouldIncrementCount()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();
        provider.Count.Should().Be(1);

        var activeKeyIdResult = await provider.GetActiveKeyIdAsync();
        var activeKeyId = activeKeyIdResult.Match(Right: id => id, Left: _ => string.Empty);

        // Act
        await provider.RotateKeyAsync(activeKeyId);

        // Assert
        provider.Count.Should().Be(2);
    }

    [Fact]
    public async Task RotateKeyAsync_NonExistingKey_ShouldReturnError()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();

        // Act
        var result = await provider.RotateKeyAsync("non-existing-key-id");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.KeyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region ListKeysAsync Tests

    [Fact]
    public async Task ListKeysAsync_ShouldReturnAllKeys()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();
        var keyId1Result = await provider.GetActiveKeyIdAsync();
        var keyId1 = keyId1Result.Match(Right: id => id, Left: _ => string.Empty);
        var rotate1Result = await provider.RotateKeyAsync(keyId1);
        var keyId2 = rotate1Result.Match(Right: k => k.KeyId, Left: _ => string.Empty);
        await provider.RotateKeyAsync(keyId2);

        // Act
        var result = await provider.ListKeysAsync();

        // Assert - initial key + 2 rotations = 3 keys
        result.IsRight.Should().BeTrue();
        var keys = result.Match(Right: k => k, Left: _ => []);
        keys.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListKeysAsync_AfterClear_ShouldReturnEmpty()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();
        provider.Clear();

        // Act
        var result = await provider.ListKeysAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var keys = result.Match(Right: k => k, Left: _ => []);
        keys.Should().BeEmpty();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldRemoveAllKeys()
    {
        // Arrange
        var provider = new InMemoryKeyProvider();
        provider.Count.Should().Be(1);

        // Act
        provider.Clear();

        // Assert
        provider.Count.Should().Be(0);
    }

    #endregion
}
