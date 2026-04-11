using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using FsCheck;
using FsCheck.Xunit;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Weasel.Core;

namespace Encina.PropertyTests.Marten.GDPR;

/// <summary>
/// Property-based tests for <see cref="CryptoShredderSerializer"/>. These tests use
/// <see cref="InMemorySubjectKeyProvider"/> as a real key store and an NSubstitute
/// <see cref="ISerializer"/> stub for the inner Marten serializer so that the
/// encryption/decryption pipeline is exercised end-to-end.
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "Marten")]
public sealed class CryptoShredderSerializerPropertyTests : IDisposable
{
    private readonly InMemorySubjectKeyProvider _keyProvider;
    private readonly DefaultForgottenSubjectHandler _forgottenHandler;
    private readonly ISerializer _innerSerializer;
    private readonly CryptoShredderSerializer _sut;

    public CryptoShredderSerializerPropertyTests()
    {
        _keyProvider = new InMemorySubjectKeyProvider(
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<InMemorySubjectKeyProvider>.Instance);
        _forgottenHandler = new DefaultForgottenSubjectHandler(
            NullLogger<DefaultForgottenSubjectHandler>.Instance);

        _innerSerializer = Substitute.For<ISerializer>();
        _innerSerializer.EnumStorage.Returns(EnumStorage.AsString);
        _innerSerializer.Casing.Returns(Casing.CamelCase);
        _innerSerializer.ValueCasting.Returns(ValueCasting.Strict);

        // The inner serializer just captures the document's current field values into JSON
        // by reading the object's Email property; this is enough to observe the encryption
        // pipeline without pulling in a real JSON library.
        _innerSerializer.ToJson(Arg.Any<object?>())
            .Returns(ci => CaptureJson(ci.Arg<object?>()));
        _innerSerializer.ToCleanJson(Arg.Any<object?>())
            .Returns(ci => CaptureJson(ci.Arg<object?>()));
        _innerSerializer.ToJsonWithTypes(Arg.Any<object>())
            .Returns(ci => CaptureJson(ci.Arg<object>()));

        _sut = new CryptoShredderSerializer(
            _innerSerializer,
            _keyProvider,
            _forgottenHandler,
            NullLogger<CryptoShredderSerializer>.Instance);
    }

    public void Dispose()
    {
        _keyProvider.Clear();
        CryptoShreddedPropertyCache.ClearCache();
    }

    private static string CaptureJson(object? document)
    {
        if (document is null) return "null";
        if (document is PiiSampleEvent pii)
        {
            return $"{{\"userId\":\"{pii.UserId}\",\"email\":\"{pii.Email}\"}}";
        }
        return "{\"other\":true}";
    }

    // ─── Passthrough invariants (non-PII types) ───

    [Property(MaxTest = 50)]
    public bool ToJson_NonPiiType_PassesThroughToInner(NonEmptyString id)
    {
        var evt = new NonPiiSampleEvent { Id = id.Get };
        var json = _sut.ToJson(evt);
        return json == "{\"other\":true}";
    }

    [Property(MaxTest = 50)]
    public bool ToCleanJson_NonPiiType_PassesThroughToInner(NonEmptyString id)
    {
        var evt = new NonPiiSampleEvent { Id = id.Get };
        var json = _sut.ToCleanJson(evt);
        return json == "{\"other\":true}";
    }

    [Property(MaxTest = 50)]
    public bool ToJsonWithTypes_NonPiiType_PassesThroughToInner(NonEmptyString id)
    {
        var evt = new NonPiiSampleEvent { Id = id.Get };
        var json = _sut.ToJsonWithTypes(evt);
        return json == "{\"other\":true}";
    }

    [Fact]
    public void ToJson_NullDocument_DelegatesToInner()
    {
        _innerSerializer.ToJson((object?)null).Returns("null");
        var json = _sut.ToJson(null);
        json.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ToCleanJson_NullDocument_DelegatesToInner()
    {
        _innerSerializer.ToCleanJson((object?)null).Returns("null");
        var json = _sut.ToCleanJson(null);
        json.ShouldNotBeNullOrEmpty();
    }

    // ─── Encryption mutation is rolled back ───

    [Property(MaxTest = 50)]
    public bool ToJson_PiiEvent_OriginalValuesRestoredAfterSerialization(NonEmptyString userId, NonEmptyString email)
    {
        var uid = userId.Get.Trim();
        var plaintextEmail = email.Get.Trim();
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(plaintextEmail)) return true;
        // Avoid email values that happen to collide with encrypted envelope marker
        if (plaintextEmail.StartsWith("{\"__enc\":true", StringComparison.Ordinal)) return true;

        var evt = new PiiSampleEvent { UserId = uid, Email = plaintextEmail };
        _sut.ToJson(evt);

        // After serialization, the original plaintext MUST be restored on the object.
        return evt.Email == plaintextEmail && evt.UserId == uid;
    }

    [Property(MaxTest = 50)]
    public bool ToJson_PiiEvent_EncryptsEmailFieldDuringInnerCall(NonEmptyString userId, NonEmptyString email)
    {
        var uid = userId.Get.Trim();
        var plaintext = email.Get.Trim();
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(plaintext)) return true;
        if (plaintext.StartsWith("{\"__enc\":true", StringComparison.Ordinal)) return true;

        string? observedEmail = null;
        _innerSerializer.ToJson(Arg.Any<object?>())
            .Returns(ci =>
            {
                if (ci.Arg<object?>() is PiiSampleEvent p)
                {
                    observedEmail = p.Email;
                }
                return "{}";
            });

        var evt = new PiiSampleEvent { UserId = uid, Email = plaintext };
        _sut.ToJson(evt);

        // During inner.ToJson, the Email should be the encrypted JSON envelope.
        return observedEmail is not null
               && observedEmail.StartsWith("{\"__enc\":true", StringComparison.Ordinal);
    }

    // ─── Forgotten subject: encryption silently skipped ───

    [Property(MaxTest = 30)]
    public bool ToJson_ForgottenSubject_SkipsEncryptionAndRestoresOriginal(NonEmptyString userId)
    {
        var uid = userId.Get.Trim();
        if (string.IsNullOrWhiteSpace(uid)) return true;

        // Pre-forget the subject so the key provider returns Left for GetOrCreate.
        _keyProvider.GetOrCreateSubjectKeyAsync(uid).AsTask().GetAwaiter().GetResult();
        _keyProvider.DeleteSubjectKeysAsync(uid).AsTask().GetAwaiter().GetResult();

        var evt = new PiiSampleEvent { UserId = uid, Email = "secret@example.com" };

        // Should not throw; the encryption branch returns null silently.
        _sut.ToJson(evt);

        // Original values must be preserved.
        return evt.Email == "secret@example.com" && evt.UserId == uid;
    }

    // ─── Decryption pipeline (sync path): round-trip ───

    [Property(MaxTest = 30)]
    public bool FromJson_PiiEvent_WithEncryptedField_DecryptsToOriginal(NonEmptyString userId, NonEmptyString email)
    {
        var uid = userId.Get.Trim();
        var plaintext = email.Get.Trim();
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(plaintext)) return true;
        if (plaintext.StartsWith("{\"__enc\":true", StringComparison.Ordinal)) return true;

        // First encrypt the field via ToJson and capture the encrypted envelope
        string? encryptedEnvelope = null;
        _innerSerializer.ToJson(Arg.Any<object?>())
            .Returns(ci =>
            {
                if (ci.Arg<object?>() is PiiSampleEvent p)
                {
                    encryptedEnvelope = p.Email;
                }
                return "{}";
            });

        var evt = new PiiSampleEvent { UserId = uid, Email = plaintext };
        _sut.ToJson(evt);

        if (encryptedEnvelope is null || !encryptedEnvelope.StartsWith("{\"__enc\":true", StringComparison.Ordinal))
        {
            return false;
        }

        // Simulate the inner serializer returning a fresh object with the encrypted envelope
        var deserialized = new PiiSampleEvent { UserId = uid, Email = encryptedEnvelope };
        _innerSerializer.FromJson<PiiSampleEvent>(Arg.Any<Stream>())
            .Returns(deserialized);

        using var stream = new MemoryStream();
        var roundtrip = _sut.FromJson<PiiSampleEvent>(stream);

        return roundtrip.Email == plaintext && roundtrip.UserId == uid;
    }

    // ─── Delegation invariants ───

    [Fact]
    public void EnumStorage_ReflectsInner()
    {
        _sut.EnumStorage.ShouldBe(EnumStorage.AsString);
    }

    [Fact]
    public void Casing_ReflectsInner()
    {
        _sut.Casing.ShouldBe(Casing.CamelCase);
    }

    [Fact]
    public void ValueCasting_ReflectsInner()
    {
        _sut.ValueCasting.ShouldBe(ValueCasting.Strict);
    }

    // ─── Test event types ───

    public sealed class NonPiiSampleEvent
    {
        public string Id { get; set; } = string.Empty;
    }

    public sealed class PiiSampleEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }
}
