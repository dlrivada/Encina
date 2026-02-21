using Encina.Security.AntiTampering.Nonce;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Security.AntiTampering;

/// <summary>
/// Unit tests for <see cref="InMemoryNonceStore"/>.
/// Verifies add, duplicate detection, expiration, and cleanup behavior.
/// </summary>
public sealed class InMemoryNonceStoreTests : IDisposable
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly InMemoryNonceStore _sut;

    public InMemoryNonceStoreTests()
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _sut = new InMemoryNonceStore(_timeProvider);
    }

    public void Dispose() => _sut.Dispose();

    #region TryAddAsync

    [Fact]
    public async Task TryAddAsync_NewNonce_ReturnsTrue()
    {
        // Act
        var result = await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAddAsync_DuplicateNonce_ReturnsFalse()
    {
        // Arrange
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10));

        // Act
        var result = await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAddAsync_ExpiredNonce_CanBeReused()
    {
        // Arrange
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(5));

        // Advance time past expiry
        _timeProvider.Advance(TimeSpan.FromMinutes(6));

        // Act
        var result = await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(5));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAddAsync_MultipleDistinctNonces_AllSucceed()
    {
        // Act & Assert
        (await _sut.TryAddAsync("nonce-a", TimeSpan.FromMinutes(10))).Should().BeTrue();
        (await _sut.TryAddAsync("nonce-b", TimeSpan.FromMinutes(10))).Should().BeTrue();
        (await _sut.TryAddAsync("nonce-c", TimeSpan.FromMinutes(10))).Should().BeTrue();
    }

    #endregion

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_ExistingNonce_ReturnsTrue()
    {
        // Arrange
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(10));

        // Act
        var result = await _sut.ExistsAsync("nonce-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonexistentNonce_ReturnsFalse()
    {
        // Act
        var result = await _sut.ExistsAsync("nonce-unknown");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ExpiredNonce_ReturnsFalse()
    {
        // Arrange
        await _sut.TryAddAsync("nonce-1", TimeSpan.FromMinutes(5));

        // Advance past expiry
        _timeProvider.Advance(TimeSpan.FromMinutes(6));

        // Act
        var result = await _sut.ExistsAsync("nonce-1");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Guard Clauses (inline)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryAddAsync_NullOrWhitespaceNonce_ThrowsArgumentException(string? nonce)
    {
        // Act
        var act = async () => await _sut.TryAddAsync(nonce!, TimeSpan.FromMinutes(10));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsAsync_NullOrWhitespaceNonce_ThrowsArgumentException(string? nonce)
    {
        // Act
        var act = async () => await _sut.ExistsAsync(nonce!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Default TimeProvider

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        // Act
        using var store = new InMemoryNonceStore(null);

        // Assert - no exception, store is functional
        store.Should().NotBeNull();
    }

    #endregion
}
