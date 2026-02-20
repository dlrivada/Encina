using System.Security.Cryptography;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionOrchestratorTests : IDisposable
{
    private readonly InMemoryKeyProvider _keyProvider;
    private readonly AesGcmFieldEncryptor _fieldEncryptor;
    private readonly EncryptionOrchestrator _sut;
    private readonly IRequestContext _context;

    public EncryptionOrchestratorTests()
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

    #region EncryptAsync

    [Fact]
    public async Task EncryptAsync_WithEncryptedProperties_EncryptsValues()
    {
        var command = new TestEncryptCommand { Email = "user@test.com", Name = "John" };

        var result = await _sut.EncryptAsync(command, _context);

        result.IsRight.Should().BeTrue();
        command.Email.Should().StartWith("ENC:v1:");
        command.Name.Should().Be("John"); // Not encrypted
    }

    [Fact]
    public async Task EncryptAsync_WithMultipleEncryptedProperties_EncryptsAll()
    {
        var command = new TestMultiEncryptCommand
        {
            Email = "user@test.com",
            Phone = "+1234567890",
            Name = "John"
        };

        var result = await _sut.EncryptAsync(command, _context);

        result.IsRight.Should().BeTrue();
        command.Email.Should().StartWith("ENC:v1:");
        command.Phone.Should().StartWith("ENC:v1:");
        command.Name.Should().Be("John");
    }

    [Fact]
    public async Task EncryptAsync_NoEncryptedProperties_ReturnsSuccess()
    {
        var command = new TestPlainCommand { Name = "John" };

        var result = await _sut.EncryptAsync(command, _context);

        result.IsRight.Should().BeTrue();
        command.Name.Should().Be("John");
    }

    [Fact]
    public async Task EncryptAsync_NullPropertyValue_SkipsProperty()
    {
        var command = new TestEncryptCommand { Email = null!, Name = "John" };

        var result = await _sut.EncryptAsync(command, _context);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task EncryptAsync_CancelledToken_ReturnsError()
    {
        var command = new TestEncryptCommand { Email = "user@test.com", Name = "John" };
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _sut.EncryptAsync(command, _context, cts.Token);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region DecryptAsync

    [Fact]
    public async Task DecryptAsync_AfterEncrypt_ReturnsOriginalValues()
    {
        var command = new TestEncryptCommand { Email = "user@test.com", Name = "John" };

        await _sut.EncryptAsync(command, _context);
        var result = await _sut.DecryptAsync(command, _context);

        result.IsRight.Should().BeTrue();
        command.Email.Should().Be("user@test.com");
        command.Name.Should().Be("John");
    }

    [Fact]
    public async Task DecryptAsync_InvalidCiphertext_WithFailOnError_ReturnsError()
    {
        var command = new TestEncryptCommand { Email = "not-encrypted", Name = "John" };

        var result = await _sut.DecryptAsync(command, _context);

        // "not-encrypted" doesn't start with "ENC:v1:", so FailOnError=true → Left
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task DecryptAsync_InvalidCiphertext_WithoutFailOnError_ReturnsSuccess()
    {
        var command = new TestNoFailEncryptCommand { Email = "not-encrypted", Name = "John" };

        var result = await _sut.DecryptAsync(command, _context);

        // FailOnError=false → continues, leaves value unchanged
        result.IsRight.Should().BeTrue();
        command.Email.Should().Be("not-encrypted");
    }

    [Fact]
    public async Task DecryptAsync_NoEncryptedProperties_ReturnsSuccess()
    {
        var command = new TestPlainCommand { Name = "John" };

        var result = await _sut.DecryptAsync(command, _context);

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Test Types

    private sealed class TestEncryptCommand
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestMultiEncryptCommand
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;

        [Encrypt(Purpose = "Phone")]
        public string Phone { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestPlainCommand
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestNoFailEncryptCommand
    {
        [Encrypt(Purpose = "Email", FailOnError = false)]
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
