#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;

using Shouldly;

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
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("mappingStore");
    }

    [Fact]
    public void Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DefaultTokenizer(_mappingStore, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("keyProvider");
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
        result.IsRight.ShouldBeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);
        Guid.TryParse(token, out _).ShouldBeTrue();
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
        result.IsRight.ShouldBeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);
        token.ShouldStartWith("cc_");
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
        result.IsRight.ShouldBeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);
        token.ShouldStartWith("tok_");
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
        result.IsRight.ShouldBeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);
        token.Length.ShouldBe(originalValue.Length);
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
        result.IsRight.ShouldBeTrue();
        var token = result.Match(Right: t => t, Left: _ => string.Empty);

        token.Length.ShouldBe(originalValue.Length);
        char.IsUpper(token[0]).ShouldBeTrue("position 0 should be uppercase");
        char.IsLower(token[1]).ShouldBeTrue("position 1 should be lowercase");
        char.IsDigit(token[2]).ShouldBeTrue("position 2 should be digit");
        token[3].ShouldBe('-', "position 3 should preserve the separator");
        char.IsUpper(token[4]).ShouldBeTrue("position 4 should be uppercase");
        char.IsLower(token[5]).ShouldBeTrue("position 5 should be lowercase");
        char.IsDigit(token[6]).ShouldBeTrue("position 6 should be digit");
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
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        var token1 = result1.Match(Right: t => t, Left: _ => string.Empty);
        var token2 = result2.Match(Right: t => t, Left: _ => string.Empty);

        token1.ShouldBe(token2, "tokenizing the same value should return the same token (deduplication)");
        _mappingStore.Count.ShouldBe(1, "only one mapping should exist for the same value");
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
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        var token1 = result1.Match(Right: t => t, Left: _ => string.Empty);
        var token2 = result2.Match(Right: t => t, Left: _ => string.Empty);

        token1.ShouldNotBe(token2, "different values should produce different tokens");
        _mappingStore.Count.ShouldBe(2, "two distinct mappings should be stored");
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
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("No active cryptographic key");
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
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("missing-key");
    }

    [Fact]
    public async Task TokenizeAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var act = async () => await _sut.TokenizeAsync(null!, options);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("value");
    }

    [Fact]
    public async Task TokenizeAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.TokenizeAsync("some-value", null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public async Task TokenizeAsync_StoresEncryptedOriginalValue()
    {
        // Arrange
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        // Act
        var result = await _sut.TokenizeAsync("secret-data", options);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify the mapping was stored
        _mappingStore.Count.ShouldBe(1);

        var allResult = await _mappingStore.GetAllAsync();
        var all = allResult.Match(Right: l => l, Left: _ => []);
        all.Count.ShouldBe(1);
        all[0].EncryptedOriginalValue.ShouldNotBeEmpty();
        all[0].KeyId.ShouldBe("active-key");
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
        result.IsRight.ShouldBeTrue();
        var originalValue = result.Match(Right: v => v, Left: _ => string.Empty);
        originalValue.ShouldBe("my-original-value");
    }

    [Fact]
    public async Task DetokenizeAsync_UnknownToken_ReturnsTokenNotFoundError()
    {
        // Act
        var result = await _sut.DetokenizeAsync("non-existent-token");

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("non-existent-token");
        error.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DetokenizeAsync_NullToken_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.DetokenizeAsync(null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("token");
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
            result.IsRight.ShouldBeTrue();
            var original = result.Match(Right: v => v, Left: _ => string.Empty);
            original.ShouldBe(values[i]);
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
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("active-key");
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
        result.IsRight.ShouldBeTrue();
        var isToken = result.Match(Right: b => b, Left: _ => false);
        isToken.ShouldBeTrue();
    }

    [Fact]
    public async Task IsTokenAsync_UnknownValue_ReturnsFalse()
    {
        // Act
        var result = await _sut.IsTokenAsync("not-a-token");

        // Assert
        result.IsRight.ShouldBeTrue();
        var isToken = result.Match(Right: b => b, Left: _ => true);
        isToken.ShouldBeFalse();
    }

    [Fact]
    public async Task IsTokenAsync_NullValue_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.IsTokenAsync(null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("value");
    }

    #endregion
}
