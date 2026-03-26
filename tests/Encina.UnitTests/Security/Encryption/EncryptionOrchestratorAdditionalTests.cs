using System.Security.Cryptography;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Encryption;

/// <summary>
/// Additional tests for <see cref="EncryptionOrchestrator"/> covering error paths and edge cases.
/// </summary>
public sealed class EncryptionOrchestratorAdditionalTests : IDisposable
{
    private readonly InMemoryKeyProvider _keyProvider;
    private readonly AesGcmFieldEncryptor _fieldEncryptor;
    private readonly EncryptionOrchestrator _sut;
    private readonly IRequestContext _context;

    public EncryptionOrchestratorAdditionalTests()
    {
        _keyProvider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        _keyProvider.AddKey("test-key", key);
        _keyProvider.SetCurrentKey("test-key");

        _fieldEncryptor = new AesGcmFieldEncryptor(_keyProvider);
        _sut = new EncryptionOrchestrator(_fieldEncryptor, NullLogger<EncryptionOrchestrator>.Instance);
        _context = RequestContext.CreateForTest(tenantId: "tenant-1");

        EncryptedPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        _keyProvider.Clear();
        EncryptedPropertyCache.ClearCache();
    }

    [Fact]
    public async Task EncryptAsync_WithNullInstance_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.EncryptAsync<TestNoEncryptCommand>(null!, _context));
    }

    [Fact]
    public async Task EncryptAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var cmd = new TestNoEncryptCommand { Name = "test" };
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.EncryptAsync(cmd, null!));
    }

    [Fact]
    public async Task EncryptAsync_WithNoEncryptedProperties_ReturnsOriginalInstance()
    {
        var cmd = new TestNoEncryptCommand { Name = "test" };
        var result = await _sut.EncryptAsync(cmd, _context);

        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.Name.ShouldBe("test"), Left: _ => { });
    }

    [Fact]
    public async Task EncryptAsync_WhenCancelled_ReturnsError()
    {
        var cmd = new TestEncryptableCommand { Email = "test@test.com" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _sut.EncryptAsync(cmd, _context, cts.Token);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DecryptAsync_WithNullInstance_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.DecryptAsync<TestNoEncryptCommand>(null!, _context));
    }

    [Fact]
    public async Task DecryptAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var cmd = new TestNoEncryptCommand { Name = "test" };
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.DecryptAsync(cmd, null!));
    }

    [Fact]
    public async Task DecryptAsync_WithNoEncryptedProperties_ReturnsOriginalInstance()
    {
        var cmd = new TestNoEncryptCommand { Name = "test" };
        var result = await _sut.DecryptAsync(cmd, _context);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DecryptAsync_WhenCancelled_ReturnsError()
    {
        var cmd = new TestEncryptableCommand { Email = "test@test.com" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _sut.DecryptAsync(cmd, _context, cts.Token);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DecryptAsync_WithNonEncryptedValue_FailsOnError()
    {
        // A property marked [Encrypt(FailOnError=true)] with non-ENC:v1: value
        var cmd = new TestEncryptableCommand { Email = "plain-text-not-encrypted" };

        var result = await _sut.DecryptAsync(cmd, _context);

        // Should return error because the value is not a valid encrypted format
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DecryptAsync_WithNonEncryptedValue_SkipsWhenFailOnErrorFalse()
    {
        var cmd = new TestSoftFailCommand { SoftField = "plain-text" };

        var result = await _sut.DecryptAsync(cmd, _context);

        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.SoftField.ShouldBe("plain-text"), Left: _ => { });
    }

    [Fact]
    public async Task EncryptAsync_WithNullPropertyValue_SkipsProperty()
    {
        var cmd = new TestEncryptableCommand { Email = null! };

        var result = await _sut.EncryptAsync(cmd, _context);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task EncryptDecrypt_Roundtrip_PreservesOriginalValue()
    {
        var original = "sensitive-email@test.com";
        var cmd = new TestEncryptableCommand { Email = original };

        var encryptResult = await _sut.EncryptAsync(cmd, _context);
        encryptResult.IsRight.ShouldBeTrue();
        cmd.Email.ShouldStartWith("ENC:v1:");

        var decryptResult = await _sut.DecryptAsync(cmd, _context);
        decryptResult.IsRight.ShouldBeTrue();
        cmd.Email.ShouldBe(original);
    }

    // Test models
    private sealed class TestNoEncryptCommand
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TestEncryptableCommand
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    private sealed class TestSoftFailCommand
    {
        [Encrypt(Purpose = "Soft", FailOnError = false)]
        public string SoftField { get; set; } = string.Empty;
    }
}
