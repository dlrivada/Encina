#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Security.Cryptography;
using System.Text;
using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.HMAC;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Options;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.AntiTampering;

/// <summary>
/// Unit tests for <see cref="HMACSigner"/>.
/// Verifies signing, verification, key resolution, and timing-safe comparison.
/// </summary>
public sealed class HMACSignerTests
{
    private readonly byte[] _testKey;
    private readonly AntiTamperingOptions _options;
    private readonly IKeyProvider _keyProvider;

    public HMACSignerTests()
    {
        _testKey = new byte[32];
        RandomNumberGenerator.Fill(_testKey);

        _options = new AntiTamperingOptions { Algorithm = HMACAlgorithm.SHA256 };
        _keyProvider = Substitute.For<IKeyProvider>();
    }

    private HMACSigner CreateSut() =>
        new(_keyProvider, Options.Create(_options));

    private static SigningContext CreateContext(string keyId = "test-key") => new()
    {
        KeyId = keyId,
        HttpMethod = "POST",
        RequestPath = "/api/orders",
        Timestamp = DateTimeOffset.UtcNow,
        Nonce = Guid.NewGuid().ToString("N")
    };

    #region SignAsync

    [Fact]
    public async Task SignAsync_ValidPayload_ReturnsBase64Signature()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext();
        var payload = Encoding.UTF8.GetBytes("{\"amount\":42.00}");

        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(_testKey)));

        // Act
        var result = await sut.SignAsync(payload.AsMemory(), context);

        // Assert
        result.IsRight.Should().BeTrue();
        var signature = (string)result;
        signature.Should().NotBeNullOrWhiteSpace();

        // Verify it's valid Base64
        var act = () => Convert.FromBase64String(signature);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SignAsync_EmptyPayload_ReturnsValidSignature()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext();

        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(_testKey)));

        // Act
        var result = await sut.SignAsync(ReadOnlyMemory<byte>.Empty, context);

        // Assert
        result.IsRight.Should().BeTrue();
        ((string)result).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignAsync_KeyNotFound_ReturnsLeftError()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext("missing-key");
        var error = AntiTamperingErrors.KeyNotFound("missing-key");

        _keyProvider.GetKeyAsync("missing-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Left(error)));

        // Act
        var result = await sut.SignAsync(ReadOnlyMemory<byte>.Empty, context);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Theory]
    [InlineData(HMACAlgorithm.SHA256)]
    [InlineData(HMACAlgorithm.SHA384)]
    [InlineData(HMACAlgorithm.SHA512)]
    public async Task SignAsync_AllAlgorithms_ProduceValidSignatures(HMACAlgorithm algorithm)
    {
        // Arrange
        _options.Algorithm = algorithm;
        var sut = CreateSut();
        var context = CreateContext();
        var payload = Encoding.UTF8.GetBytes("test-payload");

        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(_testKey)));

        // Act
        var result = await sut.SignAsync(payload.AsMemory(), context);

        // Assert
        result.IsRight.Should().BeTrue();
        var signature = (string)result;
        Convert.FromBase64String(signature).Should().NotBeEmpty();
    }

    #endregion

    #region VerifyAsync

    [Fact]
    public async Task VerifyAsync_CorrectSignature_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext();
        var payload = Encoding.UTF8.GetBytes("{\"amount\":42.00}");

        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(_testKey)));

        // Sign first
        var signResult = await sut.SignAsync(payload.AsMemory(), context);
        signResult.IsRight.Should().BeTrue();
        var signature = (string)signResult;

        // Act
        var verifyResult = await sut.VerifyAsync(payload.AsMemory(), signature, context);

        // Assert
        verifyResult.IsRight.Should().BeTrue();
        ((bool)verifyResult).Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WrongSignature_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext();
        var payload = Encoding.UTF8.GetBytes("{\"amount\":42.00}");

        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(_testKey)));

        // Create a fake Base64 signature (wrong bytes)
        var fakeSignature = Convert.ToBase64String(new byte[32]);

        // Act
        var verifyResult = await sut.VerifyAsync(payload.AsMemory(), fakeSignature, context);

        // Assert
        verifyResult.IsRight.Should().BeTrue();
        ((bool)verifyResult).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_TamperedPayload_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext();
        var originalPayload = Encoding.UTF8.GetBytes("{\"amount\":42.00}");
        var tamperedPayload = Encoding.UTF8.GetBytes("{\"amount\":99.99}");

        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(_testKey)));

        // Sign the original
        var signResult = await sut.SignAsync(originalPayload.AsMemory(), context);
        var signature = (string)signResult;

        // Act - verify with tampered payload
        var verifyResult = await sut.VerifyAsync(tamperedPayload.AsMemory(), signature, context);

        // Assert
        verifyResult.IsRight.Should().BeTrue();
        ((bool)verifyResult).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_KeyNotFound_ReturnsLeftError()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext("missing-key");
        var error = AntiTamperingErrors.KeyNotFound("missing-key");

        _keyProvider.GetKeyAsync("missing-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Left(error)));

        var fakeSignature = Convert.ToBase64String(new byte[32]);

        // Act
        var result = await sut.VerifyAsync(ReadOnlyMemory<byte>.Empty, fakeSignature, context);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_DifferentKey_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext();
        var payload = Encoding.UTF8.GetBytes("test-payload");

        // Sign with key A
        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(_testKey)));

        var signResult = await sut.SignAsync(payload.AsMemory(), context);
        var signature = (string)signResult;

        // Verify with key B (different key)
        var otherKey = new byte[32];
        RandomNumberGenerator.Fill(otherKey);

        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(otherKey)));

        // Act
        var verifyResult = await sut.VerifyAsync(payload.AsMemory(), signature, context);

        // Assert
        verifyResult.IsRight.Should().BeTrue();
        ((bool)verifyResult).Should().BeFalse();
    }

    #endregion

    #region Determinism

    [Fact]
    public async Task SignAsync_SameInput_ProducesSameSignature()
    {
        // Arrange
        var sut = CreateSut();
        var context = CreateContext();
        var payload = Encoding.UTF8.GetBytes("deterministic-test");

        _keyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(_testKey)));

        // Act
        var result1 = await sut.SignAsync(payload.AsMemory(), context);
        var result2 = await sut.SignAsync(payload.AsMemory(), context);

        // Assert
        var sig1 = (string)result1;
        var sig2 = (string)result2;
        sig1.Should().Be(sig2);
    }

    #endregion
}
