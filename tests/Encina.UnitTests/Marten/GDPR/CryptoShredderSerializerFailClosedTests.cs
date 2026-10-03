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

        var ex = Should.Throw<InvalidOperationException>(() => _sut.ToJson(evt));

        ex.Message.ShouldContain(nameof(StringSubjectEvent));
        ex.Message.ShouldContain(nameof(StringSubjectEvent.Email));
        _inner.ReceivedCalls().ShouldBeEmpty();
        evt.Email.ShouldBe(PlainEmail);
    }

    [Fact]
    public void ToJson_EmptyGuidSubjectId_ThrowsAndNeverSerializesThePlaintext()
    {
        var evt = new GuidSubjectEvent { PatientId = Guid.Empty, Email = PlainEmail };

        var ex = Should.Throw<InvalidOperationException>(() => _sut.ToJson(evt));

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
            .Returns(_ => ValueTask.FromResult(Left<EncinaError, byte[]>(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey"))));
        var evt = new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail };

        var ex = Should.Throw<InvalidOperationException>(() => _sut.ToJson(evt));

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
            .Returns(_ => ValueTask.FromResult(Left<EncinaError, byte[]>(CryptoShreddingErrors.SubjectForgotten(SubjectId))));

        Should.Throw<InvalidOperationException>(() => _sut.ToJson(new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail }));

        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void ToJson_KeyProviderThrows_ThrowsAndLogsTheExceptionRedacted()
    {
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(Sentinel + " " + SubjectId));
        var evt = new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail };

        var ex = Should.Throw<InvalidOperationException>(() => _sut.ToJson(evt));

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
            .Returns(_ => ValueTask.FromResult(Left<EncinaError, byte[]>(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey"))));
        var evt = new StringSubjectEvent { UserId = SubjectId, Email = PlainEmail };

        Should.Throw<InvalidOperationException>(() => Invoke(entryPoint, evt));

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

        var ex = Should.Throw<InvalidOperationException>(() => _sut.ToJson(evt));

        ex.Message.ShouldContain(nameof(GetterOnlyEvent));
        ex.Message.ShouldContain(nameof(GetterOnlyEvent.Email));
        _inner.ReceivedCalls().ShouldBeEmpty();
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
        var real =new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);
        _keys.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(_ => real.GetOrCreateSubjectKeyAsync(SubjectId));
        _keys.GetSubjectInfoAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(_ => real.GetSubjectInfoAsync(SubjectId));
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
