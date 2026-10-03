#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern
using System.Buffers;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;
using Encina.UnitTests.Support;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

using Npgsql;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

using ISerializer = Marten.ISerializer;

namespace Encina.UnitTests.Marten.GDPR;

/// <summary>
/// Regression tests for #1646: <see cref="CryptoShredderSerializer"/> never hands a non-null
/// <c>[CryptoShredded]</c> value to the inner serializer unencrypted. A missing subject id, a key the
/// provider cannot return, and a property the serializer cannot overwrite all make serialization throw,
/// so Marten's append fails and nothing is stored. The key version written to the envelope is the
/// version of the key that encrypted the value, never a guess.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShredderSerializerFailClosedTests : IDisposable
{
    private const string SubjectId = "subject-7b1d";
    private const string PlainEmail = "patient@example.com";
    private const string Sentinel = "SENTINEL-KEY-STORE-DOWN-31c";

    private readonly ISerializer _inner = Substitute.For<ISerializer>();
    private readonly ISubjectKeyProvider _keys = Substitute.For<ISubjectKeyProvider>();
    private readonly FakeLogger<CryptoShredderSerializer> _logger = new();
    private readonly CryptoShredderSerializer _sut;

    public CryptoShredderSerializerFailClosedTests()
    {
        _sut = new CryptoShredderSerializer(
            _inner,
            _keys,
            Substitute.For<IForgottenSubjectHandler>(),
            _logger);
    }

    public void Dispose() => CryptoShreddedPropertyCache.ClearCache();

    // -- Missing subject id --

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToJson_MissingStringSubjectId_ThrowsAndNeverSerializesThePlaintext(string? subjectId)
    {
        var evt = new StringSubjectEvent { UserId = subjectId!, Email = PlainEmail };

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _sut.ToJson(evt));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.SubjectIdMissing);
        ex.Message.ShouldContain(nameof(StringSubjectEvent));
        ex.Message.ShouldContain(nameof(StringSubjectEvent.Email));
        _inner.ReceivedCalls().ShouldBeEmpty();
        _keys.ReceivedCalls().ShouldBeEmpty();
        evt.Email.ShouldBe(PlainEmail);
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Error && r.Id.Id == 8466);
    }

    [Fact]
    public void ToJson_EmptyGuidSubjectId_ThrowsAndNeverSerializesThePlaintext()
    {
        var evt = new GuidSubjectEvent { PatientId = Guid.Empty, Email = PlainEmail };

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _sut.ToJson(evt));

        ex.Message.ShouldContain(nameof(GuidSubjectEvent));
        ex.Message.ShouldContain(nameof(GuidSubjectEvent.Email));
        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void ToJson_MissingSubjectIdButNullValue_SerializesWithoutAKeyLookup()
    {
        _inner.ToJson(Arg.Any<StringSubjectEvent>()).Returns("{}");

        _sut.ToJson(new StringSubjectEvent { UserId = null!, Email = null! }).ShouldBe("{}");

        _keys.ReceivedCalls().ShouldBeEmpty();
    }

    // -- Key provider failure --

    [Fact]
    public void ToJson_KeyProviderReturnsLeft_ThrowsAndLogsTheErrorCodeOnly()
    {
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SubjectEncryptionKey>(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey")));
        var evt = new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail };

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _sut.ToJson(evt));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.KeyUnavailable);
        ex.ErrorCode.ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
        ex.PropertyName.ShouldBe(nameof(StringSubjectEvent.Email));
        ex.EventTypeName.ShouldBe(typeof(StringSubjectEvent).FullName);
        ex.Message.ShouldContain(nameof(StringSubjectEvent));
        ex.Message.ShouldContain(nameof(StringSubjectEvent.Email));
        ex.Message.ShouldNotContain(SubjectId);
        _inner.ReceivedCalls().ShouldBeEmpty();
        evt.Email.ShouldBe(PlainEmail);

        var records = _logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Level == LogLevel.Error && r.Message.Contains(CryptoShreddingErrors.KeyStoreErrorCode));
        records.ShouldAllBe(r => !r.Message.Contains(SubjectId));
    }

    [Fact]
    public void ToJson_ForgottenSubject_Throws()
    {
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SubjectEncryptionKey>(CryptoShreddingErrors.SubjectForgotten(SubjectId)));

        var ex = Should.Throw<CryptoShreddingEncryptionException>(
            () => _sut.ToJson(new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail }));

        ex.ErrorCode.ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(1, 16)]
    public void ToJson_KeyProviderReturnsAnUnusableKey_Throws(int version, int keyLength)
    {
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionKey>(new SubjectEncryptionKey { Version = version, KeyMaterial = new byte[keyLength] }));

        var ex = Should.Throw<CryptoShreddingEncryptionException>(
            () => _sut.ToJson(new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail }));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.KeyUnavailable);
        ex.ErrorCode.ShouldBe(CryptoShreddingErrors.EncryptionFailedCode);
        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void ToJson_SecondFieldFails_RestoresTheFirstFieldAndNeverSerializes()
    {
        var keyMaterial = new byte[32];
        _keys.GetOrCreateSubjectKeyAsync("first", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionKey>(new SubjectEncryptionKey { Version = 1, KeyMaterial = keyMaterial }));
        _keys.GetOrCreateSubjectKeyAsync("second", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SubjectEncryptionKey>(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey")));
        var evt = new TwoSubjectEvent { FirstId = "first", First = PlainEmail, SecondId = "second", Second = PlainEmail };

        Should.Throw<CryptoShreddingEncryptionException>(() => _sut.ToJson(evt));

        _inner.ReceivedCalls().ShouldBeEmpty();
        evt.First.ShouldBe(PlainEmail);
        evt.Second.ShouldBe(PlainEmail);
    }

    [Fact]
    public void ToJson_KeyProviderThrows_ThrowsAndLogsTheExceptionRedacted()
    {
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(Sentinel + " " + SubjectId));
        var evt = new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail };

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _sut.ToJson(evt));

        ex.Message.ShouldNotContain(Sentinel);
        ex.Message.ShouldNotContain(SubjectId);
        ex.ToString().ShouldNotContain(Sentinel);
        _inner.ReceivedCalls().ShouldBeEmpty();
        evt.Email.ShouldBe(PlainEmail);
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
        _logger.Collector.GetSnapshot().ShouldAllBe(r => !r.Message.Contains(SubjectId));
    }

    [Theory]
    [InlineData("ToCleanJson")]
    [InlineData("ToJsonWithTypes")]
    [InlineData("WriteTo")]
    [InlineData("WriteToCleanJson")]
    [InlineData("WriteToJsonWithTypes")]
    [InlineData("WriteToParameter")]
    public void EveryWritePath_KeyProviderReturnsLeft_ThrowsAndNeverReachesTheInnerSerializer(string entryPoint)
    {
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SubjectEncryptionKey>(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey")));
        var evt = new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail };

        Should.Throw<CryptoShreddingEncryptionException>(() => Invoke(entryPoint, evt));

        _inner.ReceivedCalls().ShouldBeEmpty();
        evt.Email.ShouldBe(PlainEmail);
    }

    // -- Key version --

    [Fact]
    public async Task ToJson_SubjectInfoLookupFails_WritesTheVersionOfTheKeyThatEncrypted()
    {
        var real = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);
        await real.GetOrCreateSubjectKeyAsync(SubjectId);
        await real.RotateSubjectKeyAsync(SubjectId);
        await real.RotateSubjectKeyAsync(SubjectId);
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(_ => real.GetOrCreateSubjectKeyAsync(SubjectId));
        _keys.GetSubjectKeyAsync(SubjectId, Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(ci => real.GetSubjectKeyAsync(SubjectId, ci.ArgAt<int?>(1)));
        _keys.GetSubjectInfoAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(Left<EncinaError, SubjectEncryptionInfo>(CryptoShreddingErrors.KeyStoreError("GetSubjectInfo"))));
        string? stored = null;
        _inner.ToJson(Arg.Any<StringSubjectEvent>()).Returns(ci =>
        {
            stored = ci.Arg<StringSubjectEvent>().Email;
            return "{}";
        });

        _sut.ToJson(new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail });

        var envelope = EncryptedFieldJsonConverter.TryParse(stored);
        envelope.HasValue.ShouldBeTrue();
        envelope!.Value.KeyId.ShouldBe($"subject:{SubjectId}:v3");

        _inner.FromJson<StringSubjectEvent>(Arg.Any<Stream>())
            .Returns(new StringSubjectEvent { UserId = SubjectId, Email = stored! });
        _sut.FromJson<StringSubjectEvent>(new MemoryStream()).Email.ShouldBe(PlainEmail);
    }

    // -- Property the serializer cannot overwrite --

    [Fact]
    public void ToJson_GetterOnlyCryptoShreddedProperty_ThrowsNamingPropertyAndType()
    {
        var evt = new GetterOnlyEvent(SubjectId, PlainEmail);

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _sut.ToJson(evt));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.PropertyMisconfigured);
        ex.Message.ShouldContain(nameof(GetterOnlyEvent));
        ex.Message.ShouldContain(nameof(GetterOnlyEvent.Email));
        _inner.ReceivedCalls().ShouldBeEmpty();
        _keys.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void WriteTo_GetterOnlyCryptoShreddedProperty_Throws()
    {
        Should.Throw<CryptoShreddingEncryptionException>(
            () => _sut.WriteTo(new ArrayBufferWriter<byte>(), new GetterOnlyEvent(SubjectId, PlainEmail)));

        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void ToJson_CryptoShreddedWithoutPersonalData_ThrowsInsteadOfStoringPlaintext()
    {
        var ex = Should.Throw<CryptoShreddingEncryptionException>(
            () => _sut.ToJson(new MissingPersonalDataEvent { UserId = SubjectId, Email = PlainEmail }));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.PropertyMisconfigured);
        ex.PropertyName.ShouldBe(nameof(MissingPersonalDataEvent.Email));
        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void FromJson_GetterOnlyCryptoShreddedProperty_DelegatesToInnerUnchanged()
    {
        // Decryption is unchanged: an unencryptable property has nothing to decrypt
        var stored = new GetterOnlyEvent(SubjectId, PlainEmail);
        _inner.FromJson<GetterOnlyEvent>(Arg.Any<Stream>()).Returns(stored);

        _sut.FromJson<GetterOnlyEvent>(new MemoryStream()).ShouldBeSameAs(stored);
    }

    [Fact]
    public void ToJson_PositionalRecord_IsEncrypted()
    {
        ArrangeKey();
        string? stored = null;
        _inner.ToJson(Arg.Any<PositionalEvent>()).Returns(ci =>
        {
            stored = ci.Arg<PositionalEvent>().Email;
            return "{}";
        });

        _sut.ToJson(new PositionalEvent(SubjectId, PlainEmail));

        stored.ShouldNotBeNull();
        stored.ShouldStartWith("{\"__enc\":true");
    }

    private void ArrangeKey()
    {
        var real = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(_ => real.GetOrCreateSubjectKeyAsync(SubjectId));
    }

    private void Invoke(string entryPoint, object evt)
    {
        switch (entryPoint)
        {
            case "ToCleanJson":
                _sut.ToCleanJson(evt);
                break;
            case "ToJsonWithTypes":
                _sut.ToJsonWithTypes(evt);
                break;
            case "WriteTo":
                _sut.WriteTo(new ArrayBufferWriter<byte>(), evt);
                break;
            case "WriteToCleanJson":
                _sut.WriteToCleanJson(new ArrayBufferWriter<byte>(), evt);
                break;
            case "WriteToJsonWithTypes":
                _sut.WriteToJsonWithTypes(new ArrayBufferWriter<byte>(), evt);
                break;
            default:
                _sut.WriteToParameter(new NpgsqlParameter(), evt);
                break;
        }
    }

    public sealed class StringSubjectEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class GuidSubjectEvent
    {
        public Guid PatientId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class TwoSubjectEvent
    {
        public string FirstId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(FirstId))]
        public string First { get; set; } = string.Empty;

        public string SecondId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(SecondId))]
        public string Second { get; set; } = string.Empty;
    }

    public sealed class MissingPersonalDataEvent
    {
        public string UserId { get; set; } = string.Empty;

        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class GetterOnlyEvent(string userId, string email)
    {
        public string UserId { get; } = userId;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; } = email;
    }

    public sealed record PositionalEvent(
        string UserId,
        [property: PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [property: CryptoShredded(SubjectIdProperty = nameof(PositionalEvent.UserId))]
        string Email);
}
