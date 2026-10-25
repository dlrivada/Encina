#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;
using LanguageExt;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Pins the AES-GCM failure handling of <see cref="DefaultTokenizer"/> and
/// <see cref="DefaultPseudonymizer"/> (#2770): every stored-value or key failure returns the same
/// generic error, a wrong-length key returns KeyNotFound without reaching AesGcm, and no
/// exception text ends up in an error message.
/// </summary>
public class AesGcmFailureHandlingTests
{
    private const string KeyId = "key-1";
    private const string Secret = "secret-value";
    private const string DistinctiveText = "DISTINCTIVE-CRYPTO-TEXT-9f3a";

    private static readonly byte[] GoodKey = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
    private static readonly byte[] OtherKey = Enumerable.Range(100, 32).Select(i => (byte)i).ToArray();

    private static IKeyProvider KeyProvider(byte[] key)
    {
        var provider = Substitute.For<IKeyProvider>();
        provider.GetActiveKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>(KeyId)));
        provider.GetKeyAsync(KeyId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right<EncinaError, byte[]>(key)));
        return provider;
    }

    private static IKeyProvider ThrowingKeyProvider()
    {
        var provider = Substitute.For<IKeyProvider>();
        provider.GetActiveKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, string>>>(_ => throw new InvalidOperationException(DistinctiveText));
        provider.GetKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, byte[]>>>(_ => throw new InvalidOperationException(DistinctiveText));
        return provider;
    }

    private sealed class Person
    {
        public string? Name { get; set; }
    }

    private static byte[] Flip(byte[] blob, int index)
    {
        var copy = (byte[])blob.Clone();
        copy[index] ^= 0xFF;
        return copy;
    }

    private static void ShouldBeGenericDecryptionFailure(EncinaError error)
    {
        var expected = AnonymizationErrors.DecryptionFailed();
        error.GetCode().ShouldBe(expected.GetCode());
        error.Message.ShouldBe(expected.Message);
        error.GetDetails().Keys.OrderBy(k => k).ShouldBe(expected.GetDetails().Keys.OrderBy(k => k));
        error.GetDetails().ShouldNotContainKey("keyId");
        ShouldCarryNoInnerException(error);
        error.Message.ShouldNotContain(KeyId);
    }

    // EncinaError.Exception is always the EncinaException wrapper; "no exception" means no inner cause.
    private static void ShouldCarryNoInnerException(EncinaError error) =>
        error.Exception.Match(ex => ex.InnerException.ShouldBeNull(), () => { });

    private static void ShouldBeKeyNotFound(EncinaError error)
    {
        error.GetCode().Match(
            Some: code => code.ShouldBe(AnonymizationErrors.KeyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    private static EncinaError Left<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        return result.LeftAsEnumerable().First();
    }

    // ---------------------------------------------------------------- Blobs

    public static TheoryData<string, int> BlobMutations => new()
    {
        { "short", -1 },
        { "tag", 12 },
        { "ciphertext", 28 },
        { "nonce", 0 },
    };

    private static byte[] Mutate(byte[] blob, string kind, int index) =>
        kind == "short" ? blob[..27] : Flip(blob, index);

    // ------------------------------------------------------------ Pseudonymizer

    [Theory]
    [MemberData(nameof(BlobMutations))]
    public async Task DepseudonymizeValue_CorruptBlob_ReturnsGenericError(string kind, int index)
    {
        var sut = new DefaultPseudonymizer(KeyProvider(GoodKey));
        var blob = Convert.FromBase64String(
            (await sut.PseudonymizeValueAsync(Secret, KeyId, PseudonymizationAlgorithm.Aes256Gcm))
                .Match(Right: v => v, Left: _ => string.Empty));

        var result = await sut.DepseudonymizeValueAsync(Convert.ToBase64String(Mutate(blob, kind, index)), KeyId);

        ShouldBeGenericDecryptionFailure(Left(result));
    }

    [Theory]
    [MemberData(nameof(BlobMutations))]
    public async Task Depseudonymize_CorruptBlob_ReturnsGenericError(string kind, int index)
    {
        var sut = new DefaultPseudonymizer(KeyProvider(GoodKey));
        var pseudonymized = (await sut.PseudonymizeAsync(new Person { Name = Secret }, KeyId))
            .Match(Right: p => p, Left: _ => null!);
        var blob = Convert.FromBase64String(pseudonymized.Name!);
        pseudonymized.Name = Convert.ToBase64String(Mutate(blob, kind, index));

        var result = await sut.DepseudonymizeAsync(pseudonymized, KeyId);

        ShouldBeGenericDecryptionFailure(Left(result));
    }

    [Fact]
    public async Task DepseudonymizeValue_WrongKey_ReturnsSameErrorAsCorruptBlob()
    {
        var pseudonym = (await new DefaultPseudonymizer(KeyProvider(GoodKey))
                .PseudonymizeValueAsync(Secret, KeyId, PseudonymizationAlgorithm.Aes256Gcm))
            .Match(Right: v => v, Left: _ => string.Empty);

        var result = await new DefaultPseudonymizer(KeyProvider(OtherKey)).DepseudonymizeValueAsync(pseudonym, KeyId);

        ShouldBeGenericDecryptionFailure(Left(result));
    }

    [Theory]
    [InlineData("###")]
    [InlineData("not-valid-base64!!!")]
    [InlineData("abc")]
    public async Task DepseudonymizeValue_NonBase64_ReturnsGenericErrorWithoutFormatExceptionText(string input)
    {
        var sut = new DefaultPseudonymizer(KeyProvider(GoodKey));

        var error = Left(await sut.DepseudonymizeValueAsync(input, KeyId));

        ShouldBeGenericDecryptionFailure(error);
        error.Message.ShouldNotContain("Base64", Case.Insensitive);
    }

    [Fact]
    public async Task Depseudonymize_NonBase64_ReturnsGenericError()
    {
        var sut = new DefaultPseudonymizer(KeyProvider(GoodKey));

        var result = await sut.DepseudonymizeAsync(new Person { Name = "###" }, KeyId);

        ShouldBeGenericDecryptionFailure(Left(result));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(33)]
    public async Task Pseudonymizer_WrongLengthKey_ReturnsKeyNotFoundOnEveryAesPath(int length)
    {
        var sut = new DefaultPseudonymizer(KeyProvider(new byte[length]));

        ShouldBeKeyNotFound(Left(await sut.PseudonymizeValueAsync(Secret, KeyId, PseudonymizationAlgorithm.Aes256Gcm)));
        ShouldBeKeyNotFound(Left(await sut.DepseudonymizeValueAsync("AAAA", KeyId)));
        ShouldBeKeyNotFound(Left(await sut.PseudonymizeAsync(new Person { Name = Secret }, KeyId)));
        ShouldBeKeyNotFound(Left(await sut.DepseudonymizeAsync(new Person { Name = "AAAA" }, KeyId)));
    }

    [Fact]
    public async Task PseudonymizeValue_HmacWithNon32ByteKey_StillWorks()
    {
        var sut = new DefaultPseudonymizer(KeyProvider(new byte[31]));

        var result = await sut.PseudonymizeValueAsync(Secret, KeyId, PseudonymizationAlgorithm.HmacSha256);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Pseudonymizer_ProviderThrows_MessageHasNoExceptionText()
    {
        var sut = new DefaultPseudonymizer(ThrowingKeyProvider());

        var errors = new[]
        {
            Left(await sut.PseudonymizeAsync(new Person { Name = Secret }, KeyId)),
            Left(await sut.DepseudonymizeAsync(new Person { Name = Secret }, KeyId)),
            Left(await sut.PseudonymizeValueAsync(Secret, KeyId, PseudonymizationAlgorithm.Aes256Gcm)),
            Left(await sut.DepseudonymizeValueAsync(Secret, KeyId)),
        };

        foreach (var error in errors)
        {
            error.Message.ShouldNotContain(DistinctiveText);
            error.Message.ShouldContain(nameof(InvalidOperationException));
            error.Exception.Match(ex => ex.Message.ShouldNotContain(DistinctiveText), () => { });
        }
    }

    // ---------------------------------------------------------------- Tokenizer

    private static async Task<(DefaultTokenizer Sut, InMemoryTokenMappingStore Store, string Token)> TokenizedAsync()
    {
        var store = new InMemoryTokenMappingStore();
        var sut = new DefaultTokenizer(store, KeyProvider(GoodKey));
        var token = (await sut.TokenizeAsync(Secret, new TokenizationOptions { Format = TokenFormat.Uuid }))
            .Match(Right: t => t, Left: _ => string.Empty);
        return (sut, store, token);
    }

    private static async Task<string> StoreBlobAsync(InMemoryTokenMappingStore store, string token, byte[] blob)
    {
        var newToken = token + "-x";
        var mapping = TokenMapping.Create(newToken, "hash-" + newToken, blob, KeyId);
        (await store.StoreAsync(mapping, CancellationToken.None)).IsRight.ShouldBeTrue();
        return newToken;
    }

    [Theory]
    [MemberData(nameof(BlobMutations))]
    public async Task Detokenize_CorruptBlob_ReturnsGenericError(string kind, int index)
    {
        var (sut, store, token) = await TokenizedAsync();
        var original = (await store.GetByTokenAsync(token, CancellationToken.None))
            .Match(Right: o => o.Match(m => m.EncryptedOriginalValue, () => []), Left: _ => []);
        var corrupt = await StoreBlobAsync(store, token, Mutate(original, kind, index));

        var result = await sut.DetokenizeAsync(corrupt);

        ShouldBeGenericDecryptionFailure(Left(result));
    }

    [Fact]
    public async Task Detokenize_WrongKey_ReturnsSameGenericError()
    {
        var (_, store, token) = await TokenizedAsync();
        var other = new DefaultTokenizer(store, KeyProvider(OtherKey));

        var result = await other.DetokenizeAsync(token);

        ShouldBeGenericDecryptionFailure(Left(result));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    public async Task Tokenizer_WrongLengthKey_ReturnsKeyNotFoundOnEncryptAndDecrypt(int length)
    {
        var (_, store, token) = await TokenizedAsync();
        var sut = new DefaultTokenizer(store, KeyProvider(new byte[length]));

        ShouldBeKeyNotFound(Left(await sut.DetokenizeAsync(token)));
        ShouldBeKeyNotFound(Left(await sut.TokenizeAsync(
            "another-value", new TokenizationOptions { Format = TokenFormat.Uuid })));
    }

    [Fact]
    public async Task Depseudonymize_NullProperty_IsLeftUnchanged()
    {
        var sut = new DefaultPseudonymizer(KeyProvider(GoodKey));

        var result = await sut.DepseudonymizeAsync(new Person { Name = null }, KeyId);

        result.IsRight.ShouldBeTrue();
        result.Match(Right: p => p.Name is null, Left: _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task PseudonymizeValue_UnsupportedAlgorithm_ReturnsPseudonymizationFailed()
    {
        var sut = new DefaultPseudonymizer(KeyProvider(GoodKey));

        var error = Left(await sut.PseudonymizeValueAsync(Secret, KeyId, (PseudonymizationAlgorithm)99));

        error.GetCode().Match(
            Some: code => code.ShouldBe(AnonymizationErrors.PseudonymizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task Tokenize_UnknownFormat_FallsBackToUuid()
    {
        var sut = new DefaultTokenizer(new InMemoryTokenMappingStore(), KeyProvider(GoodKey));

        var result = await sut.TokenizeAsync(Secret, new TokenizationOptions { Format = (TokenFormat)99 });

        Guid.TryParse(result.Match(Right: t => t, Left: _ => string.Empty), out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Tokenizer_ProviderThrows_MessageHasNoExceptionText()
    {
        var sut = new DefaultTokenizer(new InMemoryTokenMappingStore(), ThrowingKeyProvider());
        var failingStore = Substitute.For<ITokenMappingStore>();
        failingStore.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Option<TokenMapping>>>>(_ => throw new InvalidOperationException(DistinctiveText));
        var storeSut = new DefaultTokenizer(failingStore, KeyProvider(GoodKey));

        var errors = new[]
        {
            Left(await sut.TokenizeAsync(Secret, new TokenizationOptions { Format = TokenFormat.Uuid })),
            Left(await storeSut.DetokenizeAsync("t")),
            Left(await storeSut.IsTokenAsync("t")),
        };

        foreach (var error in errors)
        {
            error.Message.ShouldNotContain(DistinctiveText);
            error.Message.ShouldContain(nameof(InvalidOperationException));
            error.Exception.Match(ex => ex.Message.ShouldNotContain(DistinctiveText), () => { });
        }
    }
}
