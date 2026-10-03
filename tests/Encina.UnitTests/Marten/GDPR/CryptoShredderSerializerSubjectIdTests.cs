using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

using ISerializer = Marten.ISerializer;

namespace Encina.UnitTests.Marten.GDPR;

/// <summary>
/// Regression tests for #1174: <c>[CryptoShredded]</c> must resolve non-string subject ids
/// (<see cref="Guid"/>, integers, strongly-typed ids) exactly like #1149, and encrypt each field under the
/// key of the right subject instead of silently storing plaintext.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShredderSerializerSubjectIdTests : IDisposable
{
    private const string PlainEmail = "a@example.com";
    private const string EncryptedPrefix = "{\"__enc\":true";
    private static readonly Guid SampleGuid = Guid.Parse("7f3a2c1e-4b5d-4e6f-8a9b-0c1d2e3f4a5b");
    private const string SampleGuidText = "7f3a2c1e-4b5d-4e6f-8a9b-0c1d2e3f4a5b";

    private readonly ISerializer _inner = Substitute.For<ISerializer>();
    private readonly ISubjectKeyProvider _keys = Substitute.For<ISubjectKeyProvider>();
    private readonly CryptoShredderSerializer _sut;

    public CryptoShredderSerializerSubjectIdTests()
    {
        _sut = new CryptoShredderSerializer(
            _inner,
            _keys,
            Substitute.For<IForgottenSubjectHandler>(),
            NullLogger<CryptoShredderSerializer>.Instance);
    }

    public void Dispose() => CryptoShreddedPropertyCache.ClearCache();

    [Fact]
    public async Task ToJson_GuidSubjectId_EncryptsFieldUnderTheGuidKey()
    {
        var captured = ArrangeKeys(SampleGuidText);
        _inner.ToJson(Arg.Any<GuidSubjectEvent>()).Returns(ci => Capture(captured, ci.Arg<GuidSubjectEvent>().Email));
        var evt = new GuidSubjectEvent { PatientId = SampleGuid, Email = "a@example.com" };

        _sut.ToJson(evt);

        captured.Value.ShouldNotBeNull();
        captured.Value.ShouldStartWith(EncryptedPrefix);
        evt.Email.ShouldBe("a@example.com");
        await _keys.Received().GetOrCreateSubjectKeyAsync(SampleGuidText, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToJson_StronglyTypedSubjectId_EncryptsFieldUnderTheWrappedValueKey()
    {
        var captured = ArrangeKeys(SampleGuidText);
        _inner.ToJson(Arg.Any<WrappedSubjectEvent>()).Returns(ci => Capture(captured, ci.Arg<WrappedSubjectEvent>().Email));
        var evt = new WrappedSubjectEvent { PatientId = new PatientId(SampleGuid), Email = "a@example.com" };

        _sut.ToJson(evt);

        captured.Value.ShouldNotBeNull();
        captured.Value.ShouldStartWith(EncryptedPrefix);
        await _keys.Received().GetOrCreateSubjectKeyAsync(SampleGuidText, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToJson_IntegerSubjectId_EncryptsFieldUnderTheInvariantKey()
    {
        var captured = ArrangeKeys("42");
        _inner.ToJson(Arg.Any<IntSubjectEvent>()).Returns(ci => Capture(captured, ci.Arg<IntSubjectEvent>().Email));

        _sut.ToJson(new IntSubjectEvent { PatientId = 42, Email = "a@example.com" });

        captured.Value.ShouldNotBeNull();
        captured.Value.ShouldStartWith(EncryptedPrefix);
        await _keys.Received().GetOrCreateSubjectKeyAsync("42", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ToJson_UnsupportedSubjectIdType_FailsClosedNamingTypeAndProperty()
    {
        var evt = new DoubleSubjectEvent { PatientId = 1.5, Email = "a@example.com" };

        var ex = Should.Throw<InvalidOperationException>(() => _sut.ToJson(evt));

        ex.Message.ShouldContain(nameof(DoubleSubjectEvent));
        ex.Message.ShouldContain(nameof(DoubleSubjectEvent.PatientId));
        evt.Email.ShouldBe("a@example.com");
        _inner.DidNotReceiveWithAnyArgs().ToJson(default);
    }

    [Fact]
    public void FromJson_GuidSubjectIdWithKey_DecryptsFieldAfterRoundTrip()
    {
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        ArrangeKeys(SampleGuidText, keyMaterial);
        string? stored = null;
        _inner.ToJson(Arg.Any<GuidSubjectEvent>()).Returns(ci =>
        {
            stored = ci.Arg<GuidSubjectEvent>().Email;
            return "{}";
        });
        _sut.ToJson(new GuidSubjectEvent { PatientId = SampleGuid, Email = "a@example.com" });
        stored.ShouldNotBeNull();
        stored.ShouldStartWith(EncryptedPrefix);
        _keys.GetSubjectKeyAsync(SampleGuidText, Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _inner.FromJson(typeof(GuidSubjectEvent), Arg.Any<Stream>())
            .Returns(new GuidSubjectEvent { PatientId = SampleGuid, Email = stored! });

        var result = (GuidSubjectEvent)_sut.FromJson(typeof(GuidSubjectEvent), new MemoryStream());

        result.Email.ShouldBe("a@example.com");
    }

    [Fact]
    public void RoundTrip_GuidSubjectId_UsesInvariantKeyIdAndDecryptsBack() =>
        AssertEnvelopeKeyAndRoundTrip(
            new GuidSubjectEvent { PatientId = SampleGuid, Email = PlainEmail },
            SampleGuidText,
            e => e.Email,
            stored => new GuidSubjectEvent { PatientId = SampleGuid, Email = stored });

    [Fact]
    public void RoundTrip_LongSubjectId_UsesInvariantKeyIdAndDecryptsBack() =>
        AssertEnvelopeKeyAndRoundTrip(
            new LongSubjectEvent { PatientId = 9_000_000_000L, Email = PlainEmail },
            "9000000000",
            e => e.Email,
            stored => new LongSubjectEvent { PatientId = 9_000_000_000L, Email = stored });

    [Fact]
    public void RoundTrip_FormattableSubjectId_UsesInvariantKeyIdAndDecryptsBack() =>
        AssertEnvelopeKeyAndRoundTrip(
            new FormattableSubjectEvent { PatientId = new OpaqueId(77), Email = PlainEmail },
            "opaque-77",
            e => e.Email,
            stored => new FormattableSubjectEvent { PatientId = new OpaqueId(77), Email = stored });

    [Fact]
    public void RoundTrip_ValueWrapperSubjectId_UsesInvariantKeyIdAndDecryptsBack() =>
        AssertEnvelopeKeyAndRoundTrip(
            new WrappedSubjectEvent { PatientId = new PatientId(SampleGuid), Email = PlainEmail },
            SampleGuidText,
            e => e.Email,
            stored => new WrappedSubjectEvent { PatientId = new PatientId(SampleGuid), Email = stored });

    [Fact]
    public async Task RoundTrip_StringSubjectId_KeepsTheRawStringInTheKeyId()
    {
        const string rawSubject = " User-1 ";
        AssertEnvelopeKeyAndRoundTrip(
            new StringSubjectEvent { UserId = rawSubject, Email = PlainEmail },
            rawSubject,
            e => e.Email,
            stored => new StringSubjectEvent { UserId = rawSubject, Email = stored });

        await _keys.Received().GetOrCreateSubjectKeyAsync(rawSubject, Arg.Any<CancellationToken>());
        await _keys.DidNotReceive().GetOrCreateSubjectKeyAsync(
            Arg.Is<string>(s => s != rawSubject), Arg.Any<CancellationToken>());
    }

    private void AssertEnvelopeKeyAndRoundTrip<TEvent>(
        TEvent evt,
        string invariantSubjectId,
        Func<TEvent, string> readEmail,
        Func<string, TEvent> rebuild)
        where TEvent : class
    {
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        ArrangeKeys(invariantSubjectId, keyMaterial);
        string? stored = null;
        _inner.ToJson(Arg.Any<TEvent>()).Returns(ci =>
        {
            stored = readEmail(ci.Arg<TEvent>());
            return "{}";
        });

        _sut.ToJson(evt);

        var envelope = EncryptedFieldJsonConverter.TryParse(stored);
        envelope.HasValue.ShouldBeTrue();
        envelope!.Value.KeyId.ShouldBe($"subject:{invariantSubjectId}:v1");

        _keys.GetSubjectKeyAsync(invariantSubjectId, Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _inner.FromJson<TEvent>(Arg.Any<Stream>()).Returns(rebuild(stored!));

        var result = _sut.FromJson<TEvent>(new MemoryStream());

        readEmail(result).ShouldBe(PlainEmail);
    }

    private StringHolder ArrangeKeys(string subjectId, byte[]? keyMaterial = null)
    {
        keyMaterial ??= new byte[32];
        _keys.GetOrCreateSubjectKeyAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionKey>(new SubjectEncryptionKey { Version = 1, KeyMaterial = keyMaterial }));
        return new StringHolder();
    }

    private static string Capture(StringHolder holder, string? value)
    {
        holder.Value = value;
        return "{}";
    }

    private sealed class StringHolder
    {
        public string? Value { get; set; }
    }

    public sealed record PatientId(Guid Value);

    /// <summary>A strongly-typed id that is <see cref="IFormattable"/> and has no <c>Value</c> property.</summary>
    public readonly struct OpaqueId(int number) : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) =>
            "opaque-" + number.ToString(formatProvider);
    }

    public sealed class StringSubjectEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class LongSubjectEvent
    {
        public long PatientId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class FormattableSubjectEvent
    {
        public OpaqueId PatientId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class GuidSubjectEvent
    {
        public Guid PatientId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class WrappedSubjectEvent
    {
        public PatientId PatientId { get; set; } = new(Guid.Empty);

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class IntSubjectEvent
    {
        public int PatientId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class DoubleSubjectEvent
    {
        public double PatientId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }
}
