#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

using LanguageExt;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="DefaultTokenizer"/>.
/// Uses real <see cref="InMemoryTokenMappingStore"/> and mocked <see cref="IKeyProvider"/>
/// to avoid complex NSubstitute setup for triple-nested MatchAsync calls.
/// </summary>
public class DefaultTokenizerTests
{
    private static readonly byte[] TestKey = new byte[32];

    static DefaultTokenizerTests()
    {
        for (int i = 0; i < 32; i++)
        {
            TestKey[i] = (byte)(i + 1);
        }
    }

    private readonly InMemoryTokenMappingStore _mappingStore = new();
    private readonly IKeyProvider _keyProvider = Substitute.For<IKeyProvider>();
    private readonly DefaultTokenizer _sut;

    public DefaultTokenizerTests()
    {
        _keyProvider.GetActiveKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("active-key")));

        _keyProvider.GetKeyAsync("active-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right<EncinaError, byte[]>(TestKey)));

        _sut = new DefaultTokenizer(_mappingStore, _keyProvider);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullMappingStore_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DefaultTokenizer(null!, _keyProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mappingStore");
    }

    [Fact]
    public void Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DefaultTokenizer(_mappingStore, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("keyProvider");
    }

    #endregion

    #region TokenizeAsync Tests

    [Fact]
    public async Task TokenizeAsync_UuidFormat_ReturnsGuidToken()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var result = await _sut.TokenizeAsync("sensitive-value", options);

        // Assert
        result.IsRight.Should().BeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);
        Guid.TryParse(token, out _).Should().BeTrue("token should be a valid GUID");
    }

    [Fact]
    public async Task TokenizeAsync_PrefixedFormat_ReturnsTokenWithPrefix()
    {
        // Arrange
        var options = new TokenizationOptions
        {
            Format = TokenFormat.Prefixed,
            Prefix = "cc"
        };

        // Act
        var result = await _sut.TokenizeAsync("4111-1111-1111-1111", options);

        // Assert
        result.IsRight.Should().BeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);
        token.Should().StartWith("cc_");
    }

    [Fact]
    public async Task TokenizeAsync_PrefixedFormat_NullPrefix_ReturnsTokenWithDefaultPrefix()
    {
        // Arrange
        var options = new TokenizationOptions
        {
            Format = TokenFormat.Prefixed,
            Prefix = null
        };

        // Act
        var result = await _sut.TokenizeAsync("some-value", options);

        // Assert
        result.IsRight.Should().BeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);
        token.Should().StartWith("tok_");
    }

    [Fact]
    public async Task TokenizeAsync_FormatPreserving_PreservesLength()
    {
        // Arrange
        var originalValue = "1234567890";
        var options = new TokenizationOptions
        {
            Format = TokenFormat.FormatPreserving,
            PreserveLength = true
        };

        // Act
        var result = await _sut.TokenizeAsync(originalValue, options);

        // Assert
        result.IsRight.Should().BeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);
        token.Should().HaveLength(originalValue.Length);
    }

    [Fact]
    public async Task TokenizeAsync_FormatPreserving_PreservesCharacterClasses()
    {
        // Arrange - mixed character classes: digits, uppercase, lowercase, separator
        var originalValue = "Ab1-Cd2";
        var options = new TokenizationOptions
        {
            Format = TokenFormat.FormatPreserving,
            PreserveLength = true
        };

        // Act
        var result = await _sut.TokenizeAsync(originalValue, options);

        // Assert
        result.IsRight.Should().BeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);

        token.Should().HaveLength(originalValue.Length);
        token[0].Should().Match(c => char.IsUpper((char)c), "position 0 should be uppercase");
        token[1].Should().Match(c => char.IsLower((char)c), "position 1 should be lowercase");
        token[2].Should().Match(c => char.IsDigit((char)c), "position 2 should be digit");
        token[3].Should().Be('-', "position 3 should preserve the separator");
        token[4].Should().Match(c => char.IsUpper((char)c), "position 4 should be uppercase");
        token[5].Should().Match(c => char.IsLower((char)c), "position 5 should be lowercase");
        token[6].Should().Match(c => char.IsDigit((char)c), "position 6 should be digit");
    }

    [Fact]
    public async Task TokenizeAsync_SameValueTwice_ReturnsSameToken()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var result1 = await _sut.TokenizeAsync("duplicate-value", options);
        var result2 = await _sut.TokenizeAsync("duplicate-value", options);

        // Assert
        result1.IsRight.Should().BeTrue();
        result2.IsRight.Should().BeTrue();

        var token1 = result1.Match(Right: t => t, Left: _ => string.Empty);
        var token2 = result2.Match(Right: t => t, Left: _ => string.Empty);

        token1.Should().Be(token2, "tokenizing the same value should return the same token (deduplication)");
        _mappingStore.Count.Should().Be(1, "only one mapping should exist for the same value");
    }

    [Fact]
    public async Task TokenizeAsync_DifferentValues_ReturnsDifferentTokens()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var result1 = await _sut.TokenizeAsync("value-one", options);
        var result2 = await _sut.TokenizeAsync("value-two", options);

        // Assert
        result1.IsRight.Should().BeTrue();
        result2.IsRight.Should().BeTrue();

        var token1 = result1.Match(Right: t => t, Left: _ => string.Empty);
        var token2 = result2.Match(Right: t => t, Left: _ => string.Empty);

        token1.Should().NotBe(token2, "different values should produce different tokens");
        _mappingStore.Count.Should().Be(2, "two distinct mappings should be stored");
    }

    [Fact]
    public async Task TokenizeAsync_NoActiveKey_ReturnsLeftError()
    {
        // Arrange
        var noKeyProvider = Substitute.For<IKeyProvider>();
        noKeyProvider.GetActiveKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                Left<EncinaError, string>(AnonymizationErrors.NoActiveKey())));

        var tokenizer = new DefaultTokenizer(_mappingStore, noKeyProvider);
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var result = await tokenizer.TokenizeAsync("some-value", options);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.Should().Contain("No active cryptographic key");
    }

    [Fact]
    public async Task TokenizeAsync_KeyNotFound_ReturnsLeftError()
    {
        // Arrange
        var badKeyProvider = Substitute.For<IKeyProvider>();
        badKeyProvider.GetActiveKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                Right<EncinaError, string>("missing-key")));
        badKeyProvider.GetKeyAsync("missing-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Left<EncinaError, byte[]>(AnonymizationErrors.KeyNotFound("missing-key"))));

        var tokenizer = new DefaultTokenizer(_mappingStore, badKeyProvider);
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var result = await tokenizer.TokenizeAsync("some-value", options);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.Should().Contain("missing-key");
    }

    [Fact]
    public async Task TokenizeAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var act = async () => await _sut.TokenizeAsync(null!, options);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public async Task TokenizeAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.TokenizeAsync("some-value", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task TokenizeAsync_StoresEncryptedOriginalValue()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var result = await _sut.TokenizeAsync("secret-data", options);

        // Assert
        result.IsRight.Should().BeTrue();

        // Verify the mapping was stored
        _mappingStore.Count.Should().Be(1);

        var allResult = await _mappingStore.GetAllAsync();
        var all = allResult.Match(Right: l => l, Left: _ => []);
        all.Should().HaveCount(1);
        all[0].EncryptedOriginalValue.Should().NotBeEmpty();
        all[0].KeyId.Should().Be("active-key");
    }

    #endregion

    #region DetokenizeAsync Tests

    [Fact]
    public async Task DetokenizeAsync_ExistingToken_ReturnsOriginalValue()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };
        var tokenResult = await _sut.TokenizeAsync("my-original-value", options);
        var token = tokenResult.Match(Right: t => t, Left: _ => string.Empty);

        // Act
        var result = await _sut.DetokenizeAsync(token);

        // Assert
        result.IsRight.Should().BeTrue();
        var originalValue = result.Match(Right: v => v, Left: _ => string.Empty);
        originalValue.Should().Be("my-original-value");
    }

    [Fact]
    public async Task DetokenizeAsync_UnknownToken_ReturnsTokenNotFoundError()
    {
        // Act
        var result = await _sut.DetokenizeAsync("non-existent-token");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.Should().Contain("non-existent-token");
        error.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DetokenizeAsync_NullToken_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.DetokenizeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("token");
    }

    [Fact]
    public async Task DetokenizeAsync_RoundTrip_PreservesOriginalValue()
    {
        // Arrange - tokenize multiple values and verify all round-trip correctly
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };
        var values = new[] { "value-1", "value-2", "hello world", "12345", "special!@#$%^&*()" };
        var tokens = new List<string>();

        foreach (var value in values)
        {
            var tokenResult = await _sut.TokenizeAsync(value, options);
            tokens.Add(tokenResult.Match(Right: t => t, Left: _ => string.Empty));
        }

        // Act & Assert
        for (int i = 0; i < values.Length; i++)
        {
            var result = await _sut.DetokenizeAsync(tokens[i]);
            result.IsRight.Should().BeTrue();
            var original = result.Match(Right: v => v, Left: _ => string.Empty);
            original.Should().Be(values[i]);
        }
    }

    [Fact]
    public async Task DetokenizeAsync_KeyProviderFails_ReturnsLeftError()
    {
        // Arrange - tokenize first with working key provider
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };
        var tokenResult = await _sut.TokenizeAsync("value-to-decrypt", options);
        var token = tokenResult.Match(Right: t => t, Left: _ => string.Empty);

        // Now make the key provider fail for GetKeyAsync
        var failingKeyProvider = Substitute.For<IKeyProvider>();
        failingKeyProvider.GetActiveKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("active-key")));
        failingKeyProvider.GetKeyAsync("active-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Left<EncinaError, byte[]>(AnonymizationErrors.KeyNotFound("active-key"))));

        var failingTokenizer = new DefaultTokenizer(_mappingStore, failingKeyProvider);

        // Act
        var result = await failingTokenizer.DetokenizeAsync(token);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.Should().Contain("active-key");
    }

    #endregion

    #region IsTokenAsync Tests

    [Fact]
    public async Task IsTokenAsync_KnownToken_ReturnsTrue()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };
        var tokenResult = await _sut.TokenizeAsync("some-value", options);
        var token = tokenResult.Match(Right: t => t, Left: _ => string.Empty);

        // Act
        var result = await _sut.IsTokenAsync(token);

        // Assert
        result.IsRight.Should().BeTrue();
        var isToken = result.Match(Right: b => b, Left: _ => false);
        isToken.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenAsync_UnknownValue_ReturnsFalse()
    {
        // Act
        var result = await _sut.IsTokenAsync("not-a-token");

        // Assert
        result.IsRight.Should().BeTrue();
        var isToken = result.Match(Right: b => b, Left: _ => true);
        isToken.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenAsync_NullValue_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.IsTokenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("value");
    }

    #endregion
}
