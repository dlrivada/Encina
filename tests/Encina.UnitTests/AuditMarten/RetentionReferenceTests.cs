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
/// Issue #2770 (Q3): a key destroyed with a retention reference projects as <c>[EXPIRED]</c> without pausing the
/// shard; a marker with neither reference, or with both, fails typed. Covers <see cref="RetentionReference"/>, the
/// marker classification, both key providers, both projections and the stores' metadata mapping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class RetentionReferenceTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static RetentionReference ValidRetention() => new("audit-retention:P2555D", At.AddYears(-7), At, "system");

    private static ErasureReference ValidErasure() => new("erasure-42", At, "dpo");

    private static TemporalKeyDestroyedMarker Marker(bool erasure, bool retention) => new()
    {
        Id = "temporal-destroyed:2020-01",
        Period = "2020-01",
        DestroyedAtUtc = At,
        KeyVersionsDestroyed = 1,
        ErasureRequestId = erasure ? "erasure-1" : null,
        ErasureRequestedAtUtc = erasure ? At : null,
        ErasureActor = erasure ? "dpo" : null,
        RetentionPolicyId = retention ? "policy" : null,
        RetentionCutoffUtc = retention ? At : null,
        RetentionExecutedAtUtc = retention ? At : null,
        RetentionActor = retention ? "system" : null
    };

    // ---- RetentionReference ----

    [Fact]
    public void IsValid_CompleteReference_ReturnsTrue() => ValidRetention().IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(null, "system", true, true)]
    [InlineData("  ", "system", true, true)]
    [InlineData("policy", null, true, true)]
    [InlineData("policy", " ", true, true)]
    [InlineData("policy", "system", false, true)]
    [InlineData("policy", "system", true, false)]
    public void IsValid_AnyFieldMissing_ReturnsFalse(string? policy, string? actor, bool hasCutoff, bool hasExecuted) =>
        new RetentionReference(policy!, hasCutoff ? At : default, hasExecuted ? At : default, actor!).IsValid.ShouldBeFalse();

    [Fact]
    public void ForPurge_BuildsPolicyIdFromRetentionPeriodAndSystemActor()
    {
        var reference = RetentionReference.ForPurge(new MartenOperationAuditOptions { RetentionPeriod = TimeSpan.FromDays(2555) }, At.AddYears(-7), At);

        reference.PolicyId.ShouldBe("audit-retention:P2555D");
        reference.Actor.ShouldBe("system");
        reference.CutoffUtc.ShouldBe(At.AddYears(-7));
        reference.ExecutedAtUtc.ShouldBe(At);
        reference.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ForPurge_ConfiguredRetentionPolicyId_IsUsedAsThePolicyId()
    {
        var options = new MartenOperationAuditOptions { RetentionPolicyId = "policy-2026-gdpr-7y", RetentionPeriod = TimeSpan.FromDays(10) };

        var reference = RetentionReference.ForPurge(options, At.AddYears(-7), At);

        reference.PolicyId.ShouldBe("policy-2026-gdpr-7y");
        reference.IsValid.ShouldBeTrue();
    }

    // ---- marker ----

    [Theory]
    [InlineData(true, false, KeyDestructionKind.Erasure)]
    [InlineData(false, true, KeyDestructionKind.Retention)]
    [InlineData(false, false, KeyDestructionKind.Unrecorded)]
    [InlineData(true, true, KeyDestructionKind.Unrecorded)]
    public void Classify_ReturnsTheKindTheReferencesDefine(bool erasure, bool retention, KeyDestructionKind expected) =>
        Marker(erasure, retention).Classify().ShouldBe(expected);

    [Fact]
    public void HasRetentionReference_PartialFields_ReturnsFalse()
    {
        var marker = Marker(false, true);
        marker.RetentionActor = " ";
        marker.HasRetentionReference().ShouldBeFalse();

        marker = Marker(false, true);
        marker.RetentionCutoffUtc = default(DateTimeOffset);
        marker.HasRetentionReference().ShouldBeFalse();

        marker = Marker(false, true);
        marker.RetentionExecutedAtUtc = null;
        marker.HasRetentionReference().ShouldBeFalse();

        marker = Marker(false, true);
        marker.RetentionPolicyId = null;
        marker.HasRetentionReference().ShouldBeFalse();
    }

    // ---- ClassifyTemporalKeyLookup (3-way rule) ----

    [Fact]
    public void Classify_ActiveKey_ReturnsKeyWithNoPlaceholderFlags()
    {
        var material = RandomNumberGenerator.GetBytes(32);
        var key = new TemporalKeyDocument { Id = "k", Period = "2026-03", KeyMaterial = material, Version = 1, Status = TemporalKeyStatus.Active };

        OperationAuditEntryProjection.ClassifyTemporalKeyLookup(key, Marker(true, true))
            .ShouldBe((material, false, false));
    }

    [Fact]
    public void Classify_ErasureOnly_IsShredded() =>
        OperationAuditEntryProjection.ClassifyTemporalKeyLookup(null, Marker(true, false))
            .ShouldBe((null, true, false));

    [Fact]
    public void Classify_RetentionOnly_IsExpired() =>
        OperationAuditEntryProjection.ClassifyTemporalKeyLookup(null, Marker(false, true))
            .ShouldBe((null, false, true));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Classify_NeitherOrBothReferences_FailsTyped(bool erasure, bool retention)
    {
        var ex = Should.Throw<EncryptedFieldDecryptionException>(() =>
            OperationAuditEntryProjection.ClassifyTemporalKeyLookup(null, Marker(erasure, retention)));

        ex.Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.KeyDestroyedWithoutErasureRecord);
    }

    // ---- errors ----

    [Fact]
    public void InvalidRetentionReference_HasCode() =>
        MartenAuditErrors.InvalidRetentionReference().GetCode().IfNone(string.Empty)
            .ShouldBe(MartenAuditErrors.InvalidRetentionReferenceCode);

    [Fact]
    public void RejectInvalidReference_PicksTheErrorOfTheReferenceKind()
    {
        MartenAuditErrors.RejectInvalidReference(null).ShouldBeNull();
        MartenAuditErrors.RejectInvalidReference(ValidRetention()).ShouldBeNull();
        MartenAuditErrors.RejectInvalidReference(ValidErasure()).ShouldBeNull();
        MartenAuditErrors.RejectInvalidReference(new RetentionReference(" ", At, At, "s"))!.Value.GetCode().IfNone(string.Empty)
            .ShouldBe(MartenAuditErrors.InvalidRetentionReferenceCode);
        MartenAuditErrors.RejectInvalidReference(new ErasureReference(" ", At, "s"))!.Value.GetCode().IfNone(string.Empty)
            .ShouldBe(MartenAuditErrors.InvalidErasureReferenceCode);
    }

    // ---- InMemory provider ----

    private static (FakeTimeProvider Time, InMemoryTemporalKeyProvider Provider) InMemory()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2020, 1, 15, 0, 0, 0, TimeSpan.Zero));
        return (time, new InMemoryTemporalKeyProvider(time, NullLogger<InMemoryTemporalKeyProvider>.Instance));
    }

    [Fact]
    public async Task InMemory_DestroyWithRetentionReference_DestroysKeys()
    {
        var (time, sut) = InMemory();
        await sut.GetOrCreateKeyAsync("2020-01");
        time.Advance(TimeSpan.FromDays(3650));

        var result = await sut.DestroyKeysBeforeAsync(time.GetUtcNow().UtcDateTime, TemporalKeyGranularity.Monthly, ValidRetention());

        result.IfRight(count => count.ShouldBe(1));
        result.IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "system", true, true)]
    [InlineData("p", " ", true, true)]
    [InlineData("p", "system", false, true)]
    [InlineData("p", "system", true, false)]
    public async Task InMemory_DestroyWithInvalidRetentionReference_ReturnsLeftAndDestroysNothing(string policy, string actor, bool hasCutoff, bool hasExecuted)
    {
        var (time, sut) = InMemory();
        await sut.GetOrCreateKeyAsync("2020-01");
        time.Advance(TimeSpan.FromDays(3650));
        var reference = new RetentionReference(policy, hasCutoff ? At : default, hasExecuted ? At : default, actor);

        var result = await sut.DestroyKeysBeforeAsync(time.GetUtcNow().UtcDateTime, TemporalKeyGranularity.Monthly, reference);

        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(MartenAuditErrors.InvalidRetentionReferenceCode));
        result.IsLeft.ShouldBeTrue();
        (await sut.GetKeyAsync("2020-01", 1)).IsRight.ShouldBeTrue();
    }

    // ---- Marten provider ----

    [Fact]
    public async Task Marten_DestroyWithInvalidRetentionReference_ReturnsLeftWithoutTouchingTheSession()
    {
        var session = Substitute.For<IDocumentSession>();
        var sut = new MartenTemporalKeyProvider(session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(
            DateTime.UtcNow, TemporalKeyGranularity.Monthly, new RetentionReference(" ", At, At, "system"));

        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(MartenAuditErrors.InvalidRetentionReferenceCode));
        result.IsLeft.ShouldBeTrue();
        session.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void BuildMarker_WithRetentionReference_CarriesItInUtcAndNoErasure()
    {
        var reference = new RetentionReference("p", new DateTimeOffset(2019, 1, 1, 2, 0, 0, TimeSpan.FromHours(2)), new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.FromHours(2)), "system");

        var marker = MartenTemporalKeyProvider.BuildMarker("temporal-destroyed:2020-01", "2020-01", At, 2, reference);

        marker.RetentionPolicyId.ShouldBe("p");
        marker.RetentionActor.ShouldBe("system");
        marker.RetentionCutoffUtc.ShouldBe(new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));
        marker.RetentionCutoffUtc!.Value.Offset.ShouldBe(TimeSpan.Zero);
        marker.RetentionExecutedAtUtc.ShouldBe(new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero));
        marker.ErasureRequestId.ShouldBeNull();
        marker.Classify().ShouldBe(KeyDestructionKind.Retention);
    }

    [Fact]
    public void BuildMarker_WithErasureReference_HasNoRetentionFields()
    {
        var marker = MartenTemporalKeyProvider.BuildMarker("id", "2020-01", At, 1, ValidErasure());

        marker.RetentionPolicyId.ShouldBeNull();
        marker.Classify().ShouldBe(KeyDestructionKind.Erasure);
    }

    // ---- projections ----

    [Fact]
    public void OperationProjection_Expired_UsesExpiredPlaceholderAndFlag()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var projection = new OperationAuditEntryProjection("[S]", "[E]", NullLogger<OperationAuditEntryProjection>.Instance);
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
            EncryptedUserId = EncryptedField.Encrypt("u", key, "temporal:2020-01:v1"),
            TemporalKeyPeriod = "2020-01"
        };

        var model = projection.MapToReadModel(evt, null, isShredded: false, isExpired: true);

        model.UserId.ShouldBe("[E]");
        model.IsExpired.ShouldBeTrue();
        model.IsShredded.ShouldBeFalse();
        projection.MapToReadModel(evt, null, isShredded: true).UserId.ShouldBe("[S]");
    }

    [Fact]
    public void ReadProjection_Expired_UsesExpiredPlaceholderAndFlag()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var projection = new ReadAuditEntryProjection("[S]", "[E]", NullLogger<ReadAuditEntryProjection>.Instance);
        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = "p-1",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = 0,
            EntityCount = 1,
            EncryptedUserId = EncryptedField.Encrypt("u", key, "temporal:2020-01:v1"),
            TemporalKeyPeriod = "2020-01"
        };

        var model = projection.MapToReadModel(evt, null, isShredded: false, isExpired: true);

        model.UserId.ShouldBe("[E]");
        model.IsExpired.ShouldBeTrue();
        model.IsShredded.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ProjectionConstructors_BlankExpiredPlaceholder_Throw(string? placeholder)
    {
        Should.Throw<ArgumentException>(() => new OperationAuditEntryProjection("[S]", placeholder!, NullLogger<OperationAuditEntryProjection>.Instance));
        Should.Throw<ArgumentException>(() => new ReadAuditEntryProjection("[S]", placeholder!, NullLogger<ReadAuditEntryProjection>.Instance));
    }

    [Fact]
    public void Options_ExpiredPlaceholder_DefaultsToExpired() =>
        new MartenOperationAuditOptions().ExpiredPlaceholder.ShouldBe("[EXPIRED]");

    // ---- stores: an expired placeholder in MetadataJson must not be parsed as JSON ----

    [Fact]
    public void OperationStoreMapping_ExpiredMetadata_YieldsEmptyMetadata()
    {
        var model = new OperationAuditEntryReadModel { Id = Guid.NewGuid(), MetadataJson = "[EXPIRED]", IsExpired = true };

        MartenOperationAuditStore.MapToAuditEntry(model).Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void ReadStoreMapping_ExpiredMetadata_YieldsEmptyMetadata()
    {
        var model = new ReadAuditEntryReadModel { Id = Guid.NewGuid(), MetadataJson = "[EXPIRED]", IsExpired = true };

        MartenReadAuditStore.MapToReadAuditEntry(model).Metadata.ShouldBeEmpty();
    }
}
