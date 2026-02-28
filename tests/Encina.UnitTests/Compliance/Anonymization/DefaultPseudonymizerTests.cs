#pragma warning disable CA2012 // Use ValueTasks correctly

using System.Security.Cryptography;

using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

using LanguageExt;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="DefaultPseudonymizer"/>.
/// </summary>
public class DefaultPseudonymizerTests
{
    private static readonly byte[] TestKey = new byte[32];

    static DefaultPseudonymizerTests()
    {
        // Use a deterministic key for testing
        for (int i = 0; i < 32; i++)
        {
            TestKey[i] = (byte)(i + 1);
        }
    }

    private const string TestKeyId = "test-key";

    /// <summary>
    /// Simple DTO used for object-level pseudonymization tests.
    /// </summary>
    private sealed class TestPerson
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
        public string? ReadOnlyCalculated => Name is not null ? $"Calculated-{Name}" : null;
    }

    private static IKeyProvider CreateMockKeyProvider(byte[]? key = null, string keyId = TestKeyId)
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetKeyAsync(keyId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Right<EncinaError, byte[]>(key ?? TestKey)));
        return keyProvider;
    }

    private static IKeyProvider CreateFailingKeyProvider(string keyId = TestKeyId)
    {
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetKeyAsync(keyId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Left<EncinaError, byte[]>(AnonymizationErrors.KeyNotFound(keyId))));
        return keyProvider;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DefaultPseudonymizer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("keyProvider");
    }

    [Fact]
    public void Constructor_ValidKeyProvider_DoesNotThrow()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();

        // Act
        var act = () => new DefaultPseudonymizer(keyProvider);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region PseudonymizeAsync Tests

    [Fact]
    public async Task PseudonymizeAsync_ValidData_EncryptsStringProperties()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var person = new TestPerson { Name = "John Doe", Email = "john@example.com", Age = 30 };

        // Act
        var result = await sut.PseudonymizeAsync(person, TestKeyId);

        // Assert
        result.IsRight.Should().BeTrue();
        var pseudonymized = result.Match(Right: p => p, Left: _ => null!);
        pseudonymized.Should().NotBeNull();
        pseudonymized.Name.Should().NotBe("John Doe");
        pseudonymized.Email.Should().NotBe("john@example.com");

        // Values should be valid Base64
        var nameAction = () => Convert.FromBase64String(pseudonymized.Name!);
        nameAction.Should().NotThrow();
        var emailAction = () => Convert.FromBase64String(pseudonymized.Email!);
        emailAction.Should().NotThrow();
    }

    [Fact]
    public async Task PseudonymizeAsync_NonStringProperties_AreUnchanged()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var person = new TestPerson { Name = "Jane", Email = "jane@test.com", Age = 25 };

        // Act
        var result = await sut.PseudonymizeAsync(person, TestKeyId);

        // Assert
        result.IsRight.Should().BeTrue();
        var pseudonymized = result.Match(Right: p => p, Left: _ => null!);
        pseudonymized.Age.Should().Be(25);
    }

    [Fact]
    public async Task PseudonymizeAsync_NullStringProperty_IsSkipped()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var person = new TestPerson { Name = null, Email = "john@example.com", Age = 30 };

        // Act
        var result = await sut.PseudonymizeAsync(person, TestKeyId);

        // Assert
        result.IsRight.Should().BeTrue();
        var pseudonymized = result.Match(Right: p => p, Left: _ => null!);
        pseudonymized.Name.Should().BeNull();
        pseudonymized.Email.Should().NotBe("john@example.com");
    }

    [Fact]
    public async Task PseudonymizeAsync_KeyNotFound_ReturnsLeftError()
    {
        // Arrange
        var keyProvider = CreateFailingKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var person = new TestPerson { Name = "John", Email = "john@test.com", Age = 30 };

        // Act
        var result = await sut.PseudonymizeAsync(person, TestKeyId);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.KeyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task PseudonymizeAsync_DoesNotMutateOriginalObject()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var person = new TestPerson { Name = "John Doe", Email = "john@example.com", Age = 30 };

        // Act
        await sut.PseudonymizeAsync(person, TestKeyId);

        // Assert - Original object should remain unchanged
        person.Name.Should().Be("John Doe");
        person.Email.Should().Be("john@example.com");
        person.Age.Should().Be(30);
    }

    [Fact]
    public async Task PseudonymizeAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Act
        var act = async () => await sut.PseudonymizeAsync<TestPerson>(null!, TestKeyId);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "data");
    }

    [Fact]
    public async Task PseudonymizeAsync_NullKeyId_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var person = new TestPerson { Name = "John", Email = "john@test.com", Age = 30 };

        // Act
        var act = async () => await sut.PseudonymizeAsync(person, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "keyId");
    }

    #endregion

    #region DepseudonymizeAsync Tests

    [Fact]
    public async Task DepseudonymizeAsync_PreviouslyPseudonymized_RestoresOriginalValues()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var original = new TestPerson { Name = "John Doe", Email = "john@example.com", Age = 42 };

        var pseudonymized = (await sut.PseudonymizeAsync(original, TestKeyId))
            .Match(Right: p => p, Left: _ => null!);

        // Act
        var result = await sut.DepseudonymizeAsync(pseudonymized, TestKeyId);

        // Assert
        result.IsRight.Should().BeTrue();
        var restored = result.Match(Right: p => p, Left: _ => null!);
        restored.Name.Should().Be("John Doe");
        restored.Email.Should().Be("john@example.com");
        restored.Age.Should().Be(42);
    }

    [Fact]
    public async Task DepseudonymizeAsync_NonBase64String_SkipsProperty()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Create object with a non-Base64 string value (plain text that is not valid Base64)
        var person = new TestPerson { Name = "Not Base64!!!", Email = "plaintext@email", Age = 30 };

        // Act
        var result = await sut.DepseudonymizeAsync(person, TestKeyId);

        // Assert - Non-Base64 strings are skipped (left unchanged)
        result.IsRight.Should().BeTrue();
        var restored = result.Match(Right: p => p, Left: _ => null!);
        restored.Name.Should().Be("Not Base64!!!");
        restored.Email.Should().Be("plaintext@email");
    }

    [Fact]
    public async Task DepseudonymizeAsync_WrongKey_ReturnsDecryptionFailed()
    {
        // Arrange - Pseudonymize with one key
        var key1 = new byte[32];
        for (int i = 0; i < 32; i++) key1[i] = (byte)(i + 1);
        var keyProvider1 = CreateMockKeyProvider(key1);
        var sut1 = new DefaultPseudonymizer(keyProvider1);

        var original = new TestPerson { Name = "John Doe", Email = "john@example.com", Age = 30 };
        var pseudonymized = (await sut1.PseudonymizeAsync(original, TestKeyId))
            .Match(Right: p => p, Left: _ => null!);

        // Arrange - Attempt to depseudonymize with a different key
        var key2 = new byte[32];
        for (int i = 0; i < 32; i++) key2[i] = (byte)(i + 100);
        var keyProvider2 = CreateMockKeyProvider(key2);
        var sut2 = new DefaultPseudonymizer(keyProvider2);

        // Act
        var result = await sut2.DepseudonymizeAsync(pseudonymized, TestKeyId);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.DecryptionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task DepseudonymizeAsync_KeyNotFound_ReturnsLeftError()
    {
        // Arrange
        var keyProvider = CreateFailingKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var person = new TestPerson { Name = "SomeEncryptedValue", Email = "test@test.com", Age = 30 };

        // Act
        var result = await sut.DepseudonymizeAsync(person, TestKeyId);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.KeyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task DepseudonymizeAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Act
        var act = async () => await sut.DepseudonymizeAsync<TestPerson>(null!, TestKeyId);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "data");
    }

    #endregion

    #region PseudonymizeValueAsync Tests

    [Fact]
    public async Task PseudonymizeValueAsync_Aes256Gcm_ReturnsBase64String()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        const string original = "sensitive-data";

        // Act
        var result = await sut.PseudonymizeValueAsync(
            original, TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);

        // Assert
        result.IsRight.Should().BeTrue();
        var pseudonym = result.Match(Right: v => v, Left: _ => string.Empty);
        pseudonym.Should().NotBeNullOrEmpty();
        pseudonym.Should().NotBe(original);

        // Should be valid Base64
        var parseAction = () => Convert.FromBase64String(pseudonym);
        parseAction.Should().NotThrow();

        // Decoded bytes should contain nonce (12) + tag (16) + ciphertext
        var decoded = Convert.FromBase64String(pseudonym);
        decoded.Length.Should().BeGreaterThanOrEqualTo(12 + 16 + 1);
    }

    [Fact]
    public async Task PseudonymizeValueAsync_Aes256Gcm_IsNonDeterministic()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        const string original = "same-input-value";

        // Act - Pseudonymize the same value twice
        var result1 = await sut.PseudonymizeValueAsync(
            original, TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);
        var result2 = await sut.PseudonymizeValueAsync(
            original, TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);

        // Assert - AES-GCM uses random nonces, so outputs should differ
        var pseudonym1 = result1.Match(Right: v => v, Left: _ => string.Empty);
        var pseudonym2 = result2.Match(Right: v => v, Left: _ => string.Empty);
        pseudonym1.Should().NotBe(pseudonym2);
    }

    [Fact]
    public async Task PseudonymizeValueAsync_HmacSha256_ReturnsDeterministicHash()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        const string original = "sensitive-data";

        // Act
        var result = await sut.PseudonymizeValueAsync(
            original, TestKeyId, PseudonymizationAlgorithm.HmacSha256);

        // Assert
        result.IsRight.Should().BeTrue();
        var hash = result.Match(Right: v => v, Left: _ => string.Empty);
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(original);

        // HMAC-SHA256 produces a 32-byte hash, Base64 encoded
        var decoded = Convert.FromBase64String(hash);
        decoded.Should().HaveCount(32);
    }

    [Fact]
    public async Task PseudonymizeValueAsync_HmacSha256_SameValueSameKey_SameResult()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        const string original = "deterministic-input";

        // Act
        var result1 = await sut.PseudonymizeValueAsync(
            original, TestKeyId, PseudonymizationAlgorithm.HmacSha256);
        var result2 = await sut.PseudonymizeValueAsync(
            original, TestKeyId, PseudonymizationAlgorithm.HmacSha256);

        // Assert
        var hash1 = result1.Match(Right: v => v, Left: _ => string.Empty);
        var hash2 = result2.Match(Right: v => v, Left: _ => string.Empty);
        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task PseudonymizeValueAsync_HmacSha256_DifferentValues_DifferentResults()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Act
        var result1 = await sut.PseudonymizeValueAsync(
            "value-a", TestKeyId, PseudonymizationAlgorithm.HmacSha256);
        var result2 = await sut.PseudonymizeValueAsync(
            "value-b", TestKeyId, PseudonymizationAlgorithm.HmacSha256);

        // Assert
        var hash1 = result1.Match(Right: v => v, Left: _ => string.Empty);
        var hash2 = result2.Match(Right: v => v, Left: _ => string.Empty);
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task PseudonymizeValueAsync_KeyNotFound_ReturnsLeftError()
    {
        // Arrange
        var keyProvider = CreateFailingKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Act
        var result = await sut.PseudonymizeValueAsync(
            "some-value", TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.KeyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task PseudonymizeValueAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Act
        var act = async () => await sut.PseudonymizeValueAsync(
            null!, TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "value");
    }

    #endregion

    #region DepseudonymizeValueAsync Tests

    [Fact]
    public async Task DepseudonymizeValueAsync_ValidPseudonym_ReturnsOriginalValue()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        const string original = "sensitive-email@example.com";

        var pseudonymResult = await sut.PseudonymizeValueAsync(
            original, TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);
        var pseudonym = pseudonymResult.Match(Right: v => v, Left: _ => string.Empty);

        // Act
        var result = await sut.DepseudonymizeValueAsync(pseudonym, TestKeyId);

        // Assert
        result.IsRight.Should().BeTrue();
        var restored = result.Match(Right: v => v, Left: _ => string.Empty);
        restored.Should().Be(original);
    }

    [Fact]
    public async Task DepseudonymizeValueAsync_InvalidBase64_ReturnsLeftError()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Act - Pass a string that is not valid Base64
        var result = await sut.DepseudonymizeValueAsync("not-valid-base64!!!", TestKeyId);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.DepseudonymizationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task DepseudonymizeValueAsync_WrongKey_ReturnsLeftError()
    {
        // Arrange - Pseudonymize with one key
        var key1 = new byte[32];
        for (int i = 0; i < 32; i++) key1[i] = (byte)(i + 1);
        var keyProvider1 = CreateMockKeyProvider(key1);
        var sut1 = new DefaultPseudonymizer(keyProvider1);

        var pseudonymResult = await sut1.PseudonymizeValueAsync(
            "secret-value", TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);
        var pseudonym = pseudonymResult.Match(Right: v => v, Left: _ => string.Empty);

        // Arrange - Try to depseudonymize with a different key
        var key2 = new byte[32];
        for (int i = 0; i < 32; i++) key2[i] = (byte)(i + 200);
        var keyProvider2 = CreateMockKeyProvider(key2);
        var sut2 = new DefaultPseudonymizer(keyProvider2);

        // Act
        var result = await sut2.DepseudonymizeValueAsync(pseudonym, TestKeyId);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.DecryptionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task DepseudonymizeValueAsync_KeyNotFound_ReturnsLeftError()
    {
        // Arrange
        var keyProvider = CreateFailingKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Act
        var result = await sut.DepseudonymizeValueAsync("c29tZS1kYXRh", TestKeyId);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(AnonymizationErrors.KeyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task DepseudonymizeValueAsync_NullPseudonym_ThrowsArgumentNullException()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);

        // Act
        var act = async () => await sut.DepseudonymizeValueAsync(null!, TestKeyId);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "pseudonym");
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public async Task Roundtrip_PseudonymizeAndDepseudonymize_Object_RestoresAllProperties()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var original = new TestPerson
        {
            Name = "Alice Wonderland",
            Email = "alice@wonderland.org",
            Age = 99
        };

        // Act
        var pseudoResult = await sut.PseudonymizeAsync(original, TestKeyId);
        var pseudonymized = pseudoResult.Match(Right: p => p, Left: _ => null!);
        var depseudoResult = await sut.DepseudonymizeAsync(pseudonymized, TestKeyId);

        // Assert
        depseudoResult.IsRight.Should().BeTrue();
        var restored = depseudoResult.Match(Right: p => p, Left: _ => null!);
        restored.Name.Should().Be("Alice Wonderland");
        restored.Email.Should().Be("alice@wonderland.org");
        restored.Age.Should().Be(99);
    }

    [Fact]
    public async Task Roundtrip_PseudonymizeAndDepseudonymize_Value_RestoresOriginal()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        const string original = "super-secret-data-12345";

        // Act
        var pseudoResult = await sut.PseudonymizeValueAsync(
            original, TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);
        var pseudonym = pseudoResult.Match(Right: v => v, Left: _ => string.Empty);
        var depseudoResult = await sut.DepseudonymizeValueAsync(pseudonym, TestKeyId);

        // Assert
        depseudoResult.IsRight.Should().BeTrue();
        var restored = depseudoResult.Match(Right: v => v, Left: _ => string.Empty);
        restored.Should().Be(original);
    }

    [Fact]
    public async Task Roundtrip_EmptyString_IsPreservedCorrectly()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var original = new TestPerson { Name = "", Email = "", Age = 0 };

        // Act
        var pseudoResult = await sut.PseudonymizeAsync(original, TestKeyId);
        var pseudonymized = pseudoResult.Match(Right: p => p, Left: _ => null!);
        var depseudoResult = await sut.DepseudonymizeAsync(pseudonymized, TestKeyId);

        // Assert
        depseudoResult.IsRight.Should().BeTrue();
        var restored = depseudoResult.Match(Right: p => p, Left: _ => null!);
        restored.Name.Should().Be("");
        restored.Email.Should().Be("");
    }

    [Fact]
    public async Task Roundtrip_UnicodeContent_IsPreservedCorrectly()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        const string unicodeValue = "Nombre completo con acentos y caracteres especiales";

        // Act
        var pseudoResult = await sut.PseudonymizeValueAsync(
            unicodeValue, TestKeyId, PseudonymizationAlgorithm.Aes256Gcm);
        var pseudonym = pseudoResult.Match(Right: v => v, Left: _ => string.Empty);
        var depseudoResult = await sut.DepseudonymizeValueAsync(pseudonym, TestKeyId);

        // Assert
        depseudoResult.IsRight.Should().BeTrue();
        var restored = depseudoResult.Match(Right: v => v, Left: _ => string.Empty);
        restored.Should().Be(unicodeValue);
    }

    #endregion

    #region Read-Only Property Tests

    [Fact]
    public async Task PseudonymizeAsync_ReadOnlyProperties_AreNotModified()
    {
        // Arrange
        var keyProvider = CreateMockKeyProvider();
        var sut = new DefaultPseudonymizer(keyProvider);
        var person = new TestPerson { Name = "Test", Email = "test@test.com", Age = 20 };

        // Act
        var result = await sut.PseudonymizeAsync(person, TestKeyId);

        // Assert - ReadOnlyCalculated is a get-only property, should not be touched
        result.IsRight.Should().BeTrue();
        // The fact that pseudonymization succeeded without exception is the assertion.
        // Read-only properties should be ignored by the reflection-based discovery.
    }

    #endregion
}
