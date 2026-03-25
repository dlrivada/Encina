using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.Security.AntiTampering;

/// <summary>
/// Unit tests for <see cref="InMemoryKeyProvider"/>.
/// </summary>
public sealed class InMemoryKeyProviderTests
{
    #region Constructor

    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new InMemoryKeyProvider(null!));
    }

    [Fact]
    public void Constructor_WithTestKeys_ShouldLoadKeys()
    {
        // Arrange
        var options = new AntiTamperingOptions();
        options.AddKey("key-1", "secret-1");
        options.AddKey("key-2", "secret-2");

        // Act
        var provider = new InMemoryKeyProvider(Options.Create(options));

        // Assert
        provider.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_NoTestKeys_ShouldBeEmpty()
    {
        var provider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));
        provider.Count.ShouldBe(0);
    }

    #endregion

    #region GetKeyAsync

    [Fact]
    public async Task GetKeyAsync_ExistingKey_ShouldReturnRight()
    {
        // Arrange
        var options = new AntiTamperingOptions();
        options.AddKey("test-key", "my-secret");
        var provider = new InMemoryKeyProvider(Options.Create(options));

        // Act
        var result = await provider.GetKeyAsync("test-key");

        // Assert
        result.Match(
            Right: key => key.ShouldNotBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetKeyAsync_NonExistentKey_ShouldReturnLeft()
    {
        // Arrange
        var provider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        // Act
        var result = await provider.GetKeyAsync("missing-key");

        // Assert
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldNotBeNullOrWhiteSpace());
    }

    [Fact]
    public async Task GetKeyAsync_NullKeyId_ShouldThrow()
    {
        var provider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        await Should.ThrowAsync<ArgumentException>(
            async () => await provider.GetKeyAsync(null!));
    }

    [Fact]
    public async Task GetKeyAsync_WhitespaceKeyId_ShouldThrow()
    {
        var provider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        await Should.ThrowAsync<ArgumentException>(
            async () => await provider.GetKeyAsync("   "));
    }

    [Fact]
    public async Task GetKeyAsync_CancelledToken_ShouldReturnLeft()
    {
        // Arrange
        var options = new AntiTamperingOptions();
        options.AddKey("key-1", "secret");
        var provider = new InMemoryKeyProvider(Options.Create(options));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await provider.GetKeyAsync("key-1", cts.Token);

        // Assert
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("cancelled"));
    }

    #endregion

    #region AddKey

    [Fact]
    public void AddKey_ValidInput_ShouldIncrementCount()
    {
        // Arrange
        var provider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        // Act
        provider.AddKey("new-key", new byte[] { 1, 2, 3 });

        // Assert
        provider.Count.ShouldBe(1);
    }

    [Fact]
    public void AddKey_NullKeyId_ShouldThrow()
    {
        var provider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        Should.Throw<ArgumentException>(() => provider.AddKey(null!, new byte[] { 1 }));
    }

    [Fact]
    public void AddKey_NullKeyBytes_ShouldThrow()
    {
        var provider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        Should.Throw<ArgumentNullException>(() => provider.AddKey("key-1", null!));
    }

    [Fact]
    public void AddKey_DuplicateId_ShouldReplace()
    {
        // Arrange
        var provider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));
        provider.AddKey("key-1", new byte[] { 1, 2, 3 });

        // Act
        provider.AddKey("key-1", new byte[] { 4, 5, 6 });

        // Assert
        provider.Count.ShouldBe(1);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_ShouldRemoveAllKeys()
    {
        // Arrange
        var options = new AntiTamperingOptions();
        options.AddKey("key-1", "secret-1");
        options.AddKey("key-2", "secret-2");
        var provider = new InMemoryKeyProvider(Options.Create(options));

        // Act
        provider.Clear();

        // Assert
        provider.Count.ShouldBe(0);
    }

    #endregion
}
