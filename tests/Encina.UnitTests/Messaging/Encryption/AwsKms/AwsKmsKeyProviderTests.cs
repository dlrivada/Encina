#pragma warning disable CA2012 // ValueTask consumed by NSubstitute mock setup

using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.AwsKms;
using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Encryption.AwsKms;

public class AwsKmsKeyProviderTests
{
    private readonly IAmazonKeyManagementService _kmsClient = Substitute.For<IAmazonKeyManagementService>();
    private readonly ILogger<AwsKmsKeyProvider> _logger = NullLogger<AwsKmsKeyProvider>.Instance;

    // --- Constructor null guard tests ---

    [Fact]
    public void Constructor_NullKmsClient_ThrowsArgumentNullException()
    {
        var options = Options.Create(new AwsKmsOptions { KeyId = "k" });

        Should.Throw<ArgumentNullException>(() => new AwsKmsKeyProvider(null!, options, _logger))
            .ParamName.ShouldBe("kmsClient");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new AwsKmsKeyProvider(_kmsClient, null!, _logger))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = Options.Create(new AwsKmsOptions { KeyId = "k" });

        Should.Throw<ArgumentNullException>(() => new AwsKmsKeyProvider(_kmsClient, options, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        var options = Options.Create(new AwsKmsOptions { KeyId = "test" });

        Should.NotThrow(() => new AwsKmsKeyProvider(_kmsClient, options, _logger));
    }

    // --- GetKeyAsync null guard tests ---

    [Fact]
    public async Task GetKeyAsync_NullKeyId_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var ex = await Should.ThrowAsync<ArgumentException>(async () => await provider.GetKeyAsync(null!));
        ex.ParamName.ShouldBe("keyId");
    }

    [Fact]
    public async Task GetKeyAsync_EmptyKeyId_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var ex = await Should.ThrowAsync<ArgumentException>(async () => await provider.GetKeyAsync(string.Empty));
        ex.ParamName.ShouldBe("keyId");
    }

    [Fact]
    public async Task GetKeyAsync_WhitespaceKeyId_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var ex = await Should.ThrowAsync<ArgumentException>(async () => await provider.GetKeyAsync("   "));
        ex.ParamName.ShouldBe("keyId");
    }

    // --- GetKeyAsync success path ---

    [Fact]
    public async Task GetKeyAsync_ValidKeyId_ReturnsRightWithKeyMaterial()
    {
        var expectedKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var response = new GenerateDataKeyResponse
        {
            Plaintext = new MemoryStream(expectedKey)
        };

        _kmsClient.GenerateDataKeyAsync(
                Arg.Any<GenerateDataKeyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(response);

        var provider = CreateProvider();

        var result = await provider.GetKeyAsync("arn:aws:kms:us-east-1:123:key/abc");

        result.IsRight.ShouldBeTrue();
        var keyBytes = result.Match(Right: b => b, Left: _ => default);
        keyBytes.ShouldBe(expectedKey);
    }

    [Fact]
    public async Task GetKeyAsync_ValidKeyId_UsesAes256KeySpec()
    {
        _kmsClient.GenerateDataKeyAsync(
                Arg.Any<GenerateDataKeyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GenerateDataKeyResponse
            {
                Plaintext = new MemoryStream(new byte[32])
            });

        var provider = CreateProvider();
        await provider.GetKeyAsync("test-key");

        await _kmsClient.Received(1).GenerateDataKeyAsync(
            Arg.Is<GenerateDataKeyRequest>(r => r.KeySpec == DataKeySpec.AES_256),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetKeyAsync_ValidKeyId_PassesKeyIdToRequest()
    {
        const string keyId = "arn:aws:kms:us-east-1:123:key/abc-def";

        _kmsClient.GenerateDataKeyAsync(
                Arg.Any<GenerateDataKeyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GenerateDataKeyResponse
            {
                Plaintext = new MemoryStream(new byte[32])
            });

        var provider = CreateProvider();
        await provider.GetKeyAsync(keyId);

        await _kmsClient.Received(1).GenerateDataKeyAsync(
            Arg.Is<GenerateDataKeyRequest>(r => r.KeyId == keyId),
            Arg.Any<CancellationToken>());
    }

    // --- GetKeyAsync error paths ---

    [Fact]
    public async Task GetKeyAsync_NotFoundException_ReturnsLeftKeyNotFound()
    {
        _kmsClient.GenerateDataKeyAsync(
                Arg.Any<GenerateDataKeyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<GenerateDataKeyResponse>(x => throw new NotFoundException("Key not found"));

        var provider = CreateProvider();

        var result = await provider.GetKeyAsync("nonexistent-key");

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task GetKeyAsync_DisabledException_ReturnsLeftProviderUnavailable()
    {
        _kmsClient.GenerateDataKeyAsync(
                Arg.Any<GenerateDataKeyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<GenerateDataKeyResponse>(x => throw new DisabledException("Key is disabled"));

        var provider = CreateProvider();

        var result = await provider.GetKeyAsync("disabled-key");

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("disabled");
    }

    [Fact]
    public async Task GetKeyAsync_InvalidKeyUsageException_ReturnsLeftProviderUnavailable()
    {
        _kmsClient.GenerateDataKeyAsync(
                Arg.Any<GenerateDataKeyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<GenerateDataKeyResponse>(x => throw new InvalidKeyUsageException("Invalid usage"));

        var provider = CreateProvider();

        var result = await provider.GetKeyAsync("invalid-usage-key");

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("unavailable");
    }

    [Fact]
    public async Task GetKeyAsync_AmazonKmsException_ReturnsLeftProviderUnavailable()
    {
        _kmsClient.GenerateDataKeyAsync(
                Arg.Any<GenerateDataKeyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<GenerateDataKeyResponse>(x => throw new AmazonKeyManagementServiceException("Service error"));

        var provider = CreateProvider();

        var result = await provider.GetKeyAsync("some-key");

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("unavailable");
    }

    // --- GetCurrentKeyIdAsync tests ---

    [Fact]
    public async Task GetCurrentKeyIdAsync_KeyIdConfigured_ReturnsRightWithKeyId()
    {
        const string expectedKeyId = "arn:aws:kms:us-east-1:123:key/abc";
        var provider = CreateProvider(keyId: expectedKeyId);

        var result = await provider.GetCurrentKeyIdAsync();

        result.IsRight.ShouldBeTrue();
        var keyId = result.Match(Right: k => k, Left: _ => string.Empty);
        keyId.ShouldBe(expectedKeyId);
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_KeyIdNotConfigured_ReturnsLeft()
    {
        var provider = CreateProvider(keyId: null);

        var result = await provider.GetCurrentKeyIdAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("KeyId");
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_EmptyKeyId_ReturnsLeft()
    {
        var provider = CreateProvider(keyId: "");

        var result = await provider.GetCurrentKeyIdAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_WhitespaceKeyId_ReturnsLeft()
    {
        var provider = CreateProvider(keyId: "   ");

        var result = await provider.GetCurrentKeyIdAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_DoesNotCallKmsClient()
    {
        var provider = CreateProvider(keyId: "arn:aws:kms:us-east-1:123:key/abc");

        await provider.GetCurrentKeyIdAsync();

        // GetCurrentKeyIdAsync reads from options, should NOT call KMS
        await _kmsClient.DidNotReceive().GenerateDataKeyAsync(
            Arg.Any<GenerateDataKeyRequest>(),
            Arg.Any<CancellationToken>());
    }

    // --- RotateKeyAsync tests ---

    [Fact]
    public async Task RotateKeyAsync_KeyIdNotConfigured_ReturnsLeft()
    {
        var provider = CreateProvider(keyId: null);

        var result = await provider.RotateKeyAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("KeyId");
    }

    [Fact]
    public async Task RotateKeyAsync_EmptyKeyId_ReturnsLeft()
    {
        var provider = CreateProvider(keyId: "");

        var result = await provider.RotateKeyAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RotateKeyAsync_Success_ReturnsRightWithNewKeyId()
    {
        const string originalKeyId = "arn:aws:kms:us-east-1:123:key/abc";
        const string rotatedKeyId = "arn:aws:kms:us-east-1:123:key/rotated";

        _kmsClient.RotateKeyOnDemandAsync(
                Arg.Any<RotateKeyOnDemandRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new RotateKeyOnDemandResponse { KeyId = rotatedKeyId });

        var provider = CreateProvider(keyId: originalKeyId);

        var result = await provider.RotateKeyAsync();

        result.IsRight.ShouldBeTrue();
        var newKeyId = result.Match(Right: k => k, Left: _ => string.Empty);
        newKeyId.ShouldBe(rotatedKeyId);
    }

    [Fact]
    public async Task RotateKeyAsync_Success_PassesKeyIdToRequest()
    {
        const string keyId = "arn:aws:kms:us-east-1:123:key/abc";

        _kmsClient.RotateKeyOnDemandAsync(
                Arg.Any<RotateKeyOnDemandRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new RotateKeyOnDemandResponse { KeyId = keyId });

        var provider = CreateProvider(keyId: keyId);
        await provider.RotateKeyAsync();

        await _kmsClient.Received(1).RotateKeyOnDemandAsync(
            Arg.Is<RotateKeyOnDemandRequest>(r => r.KeyId == keyId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateKeyAsync_KmsException_ReturnsLeftProviderUnavailable()
    {
        const string keyId = "arn:aws:kms:us-east-1:123:key/abc";

        _kmsClient.RotateKeyOnDemandAsync(
                Arg.Any<RotateKeyOnDemandRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<RotateKeyOnDemandResponse>(x => throw new AmazonKeyManagementServiceException("Rotation failed"));

        var provider = CreateProvider(keyId: keyId);

        var result = await provider.RotateKeyAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("rotation failed");
    }

    // --- IKeyProvider interface conformance ---

    [Fact]
    public void ImplementsIKeyProvider()
    {
        var provider = CreateProvider();
        provider.ShouldBeAssignableTo<IKeyProvider>();
    }

    // --- CancellationToken propagation ---

    [Fact]
    public async Task GetKeyAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        _kmsClient.GenerateDataKeyAsync(
                Arg.Any<GenerateDataKeyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GenerateDataKeyResponse
            {
                Plaintext = new MemoryStream(new byte[32])
            });

        var provider = CreateProvider();
        await provider.GetKeyAsync("test-key", cts.Token);

        await _kmsClient.Received(1).GenerateDataKeyAsync(
            Arg.Any<GenerateDataKeyRequest>(),
            cts.Token);
    }

    [Fact]
    public async Task RotateKeyAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        const string keyId = "test-key";

        _kmsClient.RotateKeyOnDemandAsync(
                Arg.Any<RotateKeyOnDemandRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new RotateKeyOnDemandResponse { KeyId = keyId });

        var provider = CreateProvider(keyId: keyId);
        await provider.RotateKeyAsync(cts.Token);

        await _kmsClient.Received(1).RotateKeyOnDemandAsync(
            Arg.Any<RotateKeyOnDemandRequest>(),
            cts.Token);
    }

    // --- Helpers ---

    private AwsKmsKeyProvider CreateProvider(string? keyId = "arn:aws:kms:us-east-1:123:key/default")
    {
        var options = Options.Create(new AwsKmsOptions { KeyId = keyId });
        return new AwsKmsKeyProvider(_kmsClient, options, _logger);
    }
}
