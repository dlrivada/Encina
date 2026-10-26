using System.Security.Cryptography;

using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Issue #2770 (D15, Q1): <c>[SHREDDED]</c> only for a key destruction recorded with an erasure reference; any other
/// destruction fails typed. Covers <see cref="ErasureReference"/>, the marker, the classification, the key providers
/// and the propagation of the failure through both projections.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class ErasureReferenceTests
{
    private static readonly DateTimeOffset Requested = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static ErasureReference ValidReference() => new("erasure-42", Requested, "dpo@example.test");

    private static TemporalKeyDestroyedMarker Marker(string? id, DateTimeOffset? at, string? actor) => new()
    {
        Id = "temporal-destroyed:2020-01",
        Period = "2020-01",
        DestroyedAtUtc = Requested,
        KeyVersionsDestroyed = 1,
        ErasureRequestId = id,
        ErasureRequestedAtUtc = at,
        ErasureActor = actor
    };

    // ---- ErasureReference.IsValid ----

    [Fact]
    public void IsValid_CompleteReference_ReturnsTrue() => ValidReference().IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_BlankRequestId_ReturnsFalse(string? requestId) =>
        new ErasureReference(requestId!, Requested, "actor").IsValid.ShouldBeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_BlankActor_ReturnsFalse(string? actor) =>
        new ErasureReference("id", Requested, actor!).IsValid.ShouldBeFalse();

    [Fact]
    public void IsValid_DefaultDate_ReturnsFalse() =>
        new ErasureReference("id", default, "actor").IsValid.ShouldBeFalse();

    // ---- TemporalKeyDestroyedMarker.HasErasureReference ----

    [Fact]
    public void HasErasureReference_AllThreeFields_ReturnsTrue() =>
        Marker("erasure-1", Requested, "dpo").HasErasureReference().ShouldBeTrue();

    [Fact]
    public void HasErasureReference_NoFields_ReturnsFalse() =>
        Marker(null, null, null).HasErasureReference().ShouldBeFalse();

    [Theory]
    [InlineData(null, true, "actor")]
    [InlineData("  ", true, "actor")]
    [InlineData("id", false, "actor")]
    [InlineData("id", true, null)]
    [InlineData("id", true, "  ")]
    public void HasErasureReference_AnyFieldMissing_ReturnsFalse(string? id, bool hasDate, string? actor) =>
        Marker(id, hasDate ? Requested : null, actor).HasErasureReference().ShouldBeFalse();

    // ---- ClassifyTemporalKeyLookup ----

    [Fact]
    public void Classify_ActiveKey_ReturnsKey()
    {
        var material = RandomNumberGenerator.GetBytes(32);
        var key = new TemporalKeyDocument { Id = "k", Period = "2026-03", KeyMaterial = material, Version = 1, Status = TemporalKeyStatus.Active };

        var (resultKey, shredded) = OperationAuditEntryProjection.ClassifyTemporalKeyLookup(key, null, "2026-03");

        resultKey.ShouldBeSameAs(material);
        shredded.ShouldBeFalse();
    }

    [Fact]
    public void Classify_MarkerWithReference_IsShredded()
    {
        var (resultKey, shredded) = OperationAuditEntryProjection.ClassifyTemporalKeyLookup(
            null, Marker("erasure-1", Requested, "dpo"), "2020-01");

        resultKey.ShouldBeNull();
        shredded.ShouldBeTrue();
    }

    [Fact]
    public void Classify_MarkerWithoutReference_FailsTypedWithDestroyedWithoutRecord()
    {
        var ex = Should.Throw<EncryptedFieldDecryptionException>(() =>
            OperationAuditEntryProjection.ClassifyTemporalKeyLookup(null, Marker(null, null, null), "2020-01"));

        ex.Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.KeyDestroyedWithoutErasureRecord);
        ex.Message.ShouldContain(EncryptedFieldDecryptionException.Code);
        ex.Message.ShouldNotContain("2020-01");
        ex.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Classify_MarkerWithPartialReference_FailsTyped() =>
        Should.Throw<EncryptedFieldDecryptionException>(() =>
            OperationAuditEntryProjection.ClassifyTemporalKeyLookup(null, Marker("erasure-1", Requested, null), "2020-01"))
            .Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.KeyDestroyedWithoutErasureRecord);

    [Fact]
    public void Classify_NoKeyNoMarker_StillThrowsKeyNotFound_NotTyped()
    {
        var ex = Should.Throw<KeyNotFoundException>(() =>
            OperationAuditEntryProjection.ClassifyTemporalKeyLookup(null, null, "2026-03"));

        ex.ShouldNotBeOfType<EncryptedFieldDecryptionException>();
    }

    // ---- MartenAuditErrors.InvalidErasureReference ----

    [Fact]
    public void InvalidErasureReference_HasCodeAndNoReferenceData()
    {
        var error = MartenAuditErrors.InvalidErasureReference();

        error.GetCode().IfNone(string.Empty).ShouldBe(MartenAuditErrors.InvalidErasureReferenceCode);
        MartenAuditErrors.InvalidErasureReferenceCode.ShouldBe("audit.marten.invalid_erasure_reference");
    }

    // ---- InMemoryTemporalKeyProvider.DestroyKeysBeforeAsync ----

    private static (FakeTimeProvider Time, InMemoryTemporalKeyProvider Provider) InMemory()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2020, 1, 15, 0, 0, 0, TimeSpan.Zero));
        return (time, new InMemoryTemporalKeyProvider(time, NullLogger<InMemoryTemporalKeyProvider>.Instance));
    }

    [Fact]
    public async Task InMemory_DestroyWithValidReference_DestroysKeys()
    {
        var (time, sut) = InMemory();
        (await sut.GetOrCreateKeyAsync("2020-01")).IsRight.ShouldBeTrue();
        time.Advance(TimeSpan.FromDays(3650));

        var result = await sut.DestroyKeysBeforeAsync(
            time.GetUtcNow().UtcDateTime, TemporalKeyGranularity.Monthly, ValidReference());

        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));
        (await sut.GetKeyAsync("2020-01", 1)).IsRight.ShouldBeFalse();
    }

    [Fact]
    public async Task InMemory_DestroyWithoutReference_DestroysKeys()
    {
        var (time, sut) = InMemory();
        await sut.GetOrCreateKeyAsync("2020-01");
        time.Advance(TimeSpan.FromDays(3650));

        var result = await sut.DestroyKeysBeforeAsync(time.GetUtcNow().UtcDateTime, TemporalKeyGranularity.Monthly);

        result.IfRight(count => count.ShouldBe(1));
        result.IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "actor", true)]
    [InlineData("id", " ", true)]
    [InlineData("id", "actor", false)]
    public async Task InMemory_DestroyWithInvalidReference_ReturnsLeftAndDestroysNothing(string id, string actor, bool hasDate)
    {
        var (time, sut) = InMemory();
        await sut.GetOrCreateKeyAsync("2020-01");
        time.Advance(TimeSpan.FromDays(3650));
        var reference = new ErasureReference(id, hasDate ? Requested : default, actor);

        var result = await sut.DestroyKeysBeforeAsync(
            time.GetUtcNow().UtcDateTime, TemporalKeyGranularity.Monthly, reference);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(MartenAuditErrors.InvalidErasureReferenceCode));
        (await sut.GetKeyAsync("2020-01", 1)).IsRight.ShouldBeTrue("nothing may be destroyed with an invalid reference");
    }

    // ---- MartenTemporalKeyProvider.DestroyKeysBeforeAsync (reference validation happens before any query) ----

    [Fact]
    public async Task Marten_DestroyWithInvalidReference_ReturnsLeftWithoutTouchingTheSession()
    {
        var session = Substitute.For<IDocumentSession>();
        var sut = new MartenTemporalKeyProvider(session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(
            DateTime.UtcNow, TemporalKeyGranularity.Monthly, new ErasureReference(" ", Requested, "actor"));

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(MartenAuditErrors.InvalidErasureReferenceCode));
        session.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task InMemory_DestroyWithCancelledToken_ReturnsLeft()
    {
        var (_, sut) = InMemory();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await sut.DestroyKeysBeforeAsync(DateTime.UtcNow, TemporalKeyGranularity.Monthly, null, cts.Token);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemory_DestroyKeepsPeriodsNotOlderThanCutoffAndSkipsAlreadyDestroyed()
    {
        var (time, sut) = InMemory();
        await sut.GetOrCreateKeyAsync("2020-01");
        time.Advance(TimeSpan.FromDays(3650));
        await sut.GetOrCreateKeyAsync("2030-01");
        var cutoff = time.GetUtcNow().UtcDateTime.AddDays(-30);

        var first = await sut.DestroyKeysBeforeAsync(cutoff, TemporalKeyGranularity.Monthly);
        var second = await sut.DestroyKeysBeforeAsync(cutoff, TemporalKeyGranularity.Monthly);

        first.IfRight(count => count.ShouldBe(1));
        second.IfRight(count => count.ShouldBe(0));
        (await sut.GetKeyAsync("2030-01", 1)).IsRight.ShouldBeTrue();
    }

    // ---- MartenTemporalKeyProvider helpers ----

    [Fact]
    public void BuildMarker_WithReference_CarriesItInUtc()
    {
        var reference = new ErasureReference("erasure-7", new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.FromHours(2)), "dpo");

        var marker = MartenTemporalKeyProvider.BuildMarker("temporal-destroyed:2020-01", "2020-01", Requested, 2, reference);

        marker.Id.ShouldBe("temporal-destroyed:2020-01");
        marker.Period.ShouldBe("2020-01");
        marker.KeyVersionsDestroyed.ShouldBe(2);
        marker.ErasureRequestId.ShouldBe("erasure-7");
        marker.ErasureActor.ShouldBe("dpo");
        marker.ErasureRequestedAtUtc.ShouldBe(new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero));
        marker.ErasureRequestedAtUtc!.Value.Offset.ShouldBe(TimeSpan.Zero);
        marker.HasErasureReference().ShouldBeTrue();
    }

    [Fact]
    public void BuildMarker_WithoutReference_HasNoReference()
    {
        var marker = MartenTemporalKeyProvider.BuildMarker("temporal-destroyed:2020-01", "2020-01", Requested, 1, null);

        marker.ErasureRequestId.ShouldBeNull();
        marker.ErasureRequestedAtUtc.ShouldBeNull();
        marker.ErasureActor.ShouldBeNull();
        marker.HasErasureReference().ShouldBeFalse();
    }

    [Fact]
    public void SelectPeriodsBefore_ReturnsOnlyPeriodsStartingBeforeTheCutoff()
    {
        TemporalKeyDocument Key(string period, int v) => new() { Id = $"k{period}{v}", Period = period, Version = v };
        var keys = new[] { Key("2020-01", 1), Key("2020-01", 2), Key("2020-03", 1), Key("garbage", 1) };

        var periods = MartenTemporalKeyProvider.SelectPeriodsBefore(
            keys, new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc), TemporalKeyGranularity.Monthly);

        periods.ShouldBe(["2020-01"]);
    }

    [Fact]
    public async Task Marten_DestroyWhenTheSessionFails_ReturnsLeftInsteadOfThrowing()
    {
        var session = Substitute.For<IDocumentSession>();
        session.When(s => s.Query<TemporalKeyDocument>()).Do(_ => throw new InvalidOperationException("database down"));
        var sut = new MartenTemporalKeyProvider(session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(DateTime.UtcNow, TemporalKeyGranularity.Monthly, ValidReference());

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(MartenAuditErrors.KeyDestructionFailedCode));
    }

    // ---- both projections propagate the failure ----

    private static EncryptedField Tampered(byte[] key)
    {
        var field = EncryptedField.Encrypt("secret-value", key, "temporal:2026-03:v1");
        return field with { Value = field.Value!.Replace("\"t\":\"", "\"t\":\"AAAA", StringComparison.Ordinal) };
    }

    [Fact]
    public void OperationProjection_TamperedEnvelope_Propagates()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var projection = new OperationAuditEntryProjection("[SHREDDED]", NullLogger<OperationAuditEntryProjection>.Instance);
        var evt = new OperationAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "c",
            Action = "a",
            EntityType = "E",
            Outcome = 0,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            EncryptedUserId = Tampered(key),
            TemporalKeyPeriod = "2026-03"
        };

        Should.Throw<EncryptedFieldDecryptionException>(() => projection.MapToReadModel(evt, key, isShredded: false));
    }

    [Fact]
    public void ReadProjection_UnmarkedValue_Propagates()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var projection = new ReadAuditEntryProjection("[SHREDDED]", NullLogger<ReadAuditEntryProjection>.Instance);
        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = "p-1",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = 0,
            EntityCount = 1,
            EncryptedUserId = new EncryptedField { Value = "{\"plain\":true}", KeyId = "k" },
            TemporalKeyPeriod = "2026-03"
        };

        Should.Throw<EncryptedFieldDecryptionException>(() => projection.MapToReadModel(evt, key, isShredded: false))
            .Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.NotEncrypted);
    }
}
