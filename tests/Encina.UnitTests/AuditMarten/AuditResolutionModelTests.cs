using Encina.Audit.Marten.Resolution;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for the <see cref="AuditResolution"/> document and the <see cref="AuditResolutionRecord"/> snapshot
/// (issue #3049, phase 1).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditResolutionModelTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    private static AuditResolution CreateDocument(Guid eventId, Guid supersededId) => new()
    {
        Id = Guid.CreateVersion7(At),
        Trail = AuditResolutionTrail.Operation,
        EventId = eventId,
        Field = AuditResolutionField.UserId,
        Kind = AuditResolutionKind.Shredded,
        Sequence = 3,
        Supersedes = [supersededId],
        EventAuditEntryId = Guid.NewGuid(),
        ResolutionAuditEntryId = Guid.NewGuid(),
        TenantId = "tenant-1",
        ErasureRequestId = "erasure-42",
        ErasureRequestedAtUtc = At,
        ErasureActor = "dpo",
        RetentionPolicyId = "policy-7",
        RetentionCutoffUtc = At.AddDays(-30),
        RetentionExecutedAtUtc = At.AddDays(-1),
        RetentionActor = "system",
        ResolvedBy = "operator",
        ResolvedAtUtc = At
    };

    [Fact]
    public void Document_NewInstance_HasEmptyIdentityAndNoReferences()
    {
        var document = new AuditResolution();

        document.Id.ShouldBe(Guid.Empty);
        document.ResolvedBy.ShouldBe(string.Empty);
        document.Kind.ShouldBe(AuditResolutionKind.Unreadable);
        document.Trail.ShouldBe(AuditResolutionTrail.Operation);
        document.Field.ShouldBe(AuditResolutionField.All);
        document.Sequence.ShouldBe(0);
        document.Supersedes.ShouldBeEmpty();
        document.EventAuditEntryId.ShouldBeNull();
        document.ResolutionAuditEntryId.ShouldBeNull();
        document.TenantId.ShouldBeNull();
        document.ErasureRequestId.ShouldBeNull();
        document.RetentionPolicyId.ShouldBeNull();
    }

    [Fact]
    public void Document_EveryProperty_RoundTrips()
    {
        var eventId = Guid.NewGuid();
        var superseded = Guid.NewGuid();

        var document = CreateDocument(eventId, superseded);

        document.Id.Version.ShouldBe(7);
        document.Trail.ShouldBe(AuditResolutionTrail.Operation);
        document.EventId.ShouldBe(eventId);
        document.Field.ShouldBe(AuditResolutionField.UserId);
        document.Kind.ShouldBe(AuditResolutionKind.Shredded);
        document.Sequence.ShouldBe(3);
        document.Supersedes.ShouldBe([superseded]);
        document.EventAuditEntryId.ShouldNotBeNull();
        document.ResolutionAuditEntryId.ShouldNotBeNull();
        document.TenantId.ShouldBe("tenant-1");
        document.ErasureRequestId.ShouldBe("erasure-42");
        document.ErasureRequestedAtUtc.ShouldBe(At);
        document.ErasureActor.ShouldBe("dpo");
        document.RetentionPolicyId.ShouldBe("policy-7");
        document.RetentionCutoffUtc.ShouldBe(At.AddDays(-30));
        document.RetentionExecutedAtUtc.ShouldBe(At.AddDays(-1));
        document.RetentionActor.ShouldBe("system");
        document.ResolvedBy.ShouldBe("operator");
        document.ResolvedAtUtc.ShouldBe(At);
    }

    [Fact]
    public void Document_HasNoReasonProperty() =>
        typeof(AuditResolution).GetProperties()
            .ShouldNotContain(p => p.Name.Contains("Reason", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Record_BuiltFromADocument_CarriesEveryValue()
    {
        var superseded = Guid.NewGuid();
        var document = CreateDocument(Guid.NewGuid(), superseded);

        var record = new AuditResolutionRecord(
            document.Id, document.Trail, document.EventId, document.Field, document.Kind, document.Sequence,
            document.Supersedes, document.EventAuditEntryId, document.ResolutionAuditEntryId, document.TenantId,
            document.ErasureRequestId, document.ErasureRequestedAtUtc, document.ErasureActor,
            document.RetentionPolicyId, document.RetentionCutoffUtc, document.RetentionExecutedAtUtc,
            document.RetentionActor, document.ResolvedBy, document.ResolvedAtUtc);

        record.Id.ShouldBe(document.Id);
        record.Trail.ShouldBe(document.Trail);
        record.EventId.ShouldBe(document.EventId);
        record.Field.ShouldBe(document.Field);
        record.Kind.ShouldBe(document.Kind);
        record.Sequence.ShouldBe(3);
        record.Supersedes.ShouldBe([superseded]);
        record.EventAuditEntryId.ShouldBe(document.EventAuditEntryId);
        record.ResolutionAuditEntryId.ShouldBe(document.ResolutionAuditEntryId);
        record.TenantId.ShouldBe("tenant-1");
        record.ErasureRequestId.ShouldBe("erasure-42");
        record.ErasureRequestedAtUtc.ShouldBe(At);
        record.ErasureActor.ShouldBe("dpo");
        record.RetentionPolicyId.ShouldBe("policy-7");
        record.RetentionCutoffUtc.ShouldBe(At.AddDays(-30));
        record.RetentionExecutedAtUtc.ShouldBe(At.AddDays(-1));
        record.RetentionActor.ShouldBe("system");
        record.ResolvedBy.ShouldBe("operator");
        record.ResolvedAtUtc.ShouldBe(At);
    }

    [Fact]
    public void Record_ValueEquality_AndWith_BehaveAsAnImmutableSnapshot()
    {
        var a = new AuditResolutionRecord(
            Guid.CreateVersion7(At), AuditResolutionTrail.Read, Guid.NewGuid(), AuditResolutionField.Purpose,
            AuditResolutionKind.Unreadable, 1, [], null, null, null, null, null, null, null, null, null, null,
            "operator", At);

        var same = a with { };
        var other = a with { Kind = AuditResolutionKind.Withdrawn };

        same.ShouldBe(a);
        same.GetHashCode().ShouldBe(a.GetHashCode());
        other.ShouldNotBe(a);
        a.Kind.ShouldBe(AuditResolutionKind.Unreadable);
        a.ToString().ShouldContain("Unreadable");
        (same == a).ShouldBeTrue();
    }
}
