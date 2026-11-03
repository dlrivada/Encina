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

    private static AuditResolution CreateDocument(Guid eventId) => new()
    {
        Id = AuditResolutionIds.Format(AuditResolutionTrail.Operation, eventId, AuditResolutionField.UserId),
        Trail = AuditResolutionTrail.Operation,
        EventId = eventId,
        Field = AuditResolutionField.UserId,
        Kind = AuditResolutionKind.Shredded,
        AuditEntryId = Guid.NewGuid(),
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

        document.Id.ShouldBe(string.Empty);
        document.ResolvedBy.ShouldBe(string.Empty);
        document.Kind.ShouldBe(AuditResolutionKind.Unreadable);
        document.Trail.ShouldBe(AuditResolutionTrail.Operation);
        document.Field.ShouldBe(AuditResolutionField.All);
        document.AuditEntryId.ShouldBeNull();
        document.TenantId.ShouldBeNull();
        document.ErasureRequestId.ShouldBeNull();
        document.RetentionPolicyId.ShouldBeNull();
    }

    [Fact]
    public void Document_EveryProperty_RoundTrips()
    {
        var eventId = Guid.NewGuid();

        var document = CreateDocument(eventId);

        document.Id.ShouldBe($"audit-resolution:Operation:{eventId:D}:UserId");
        document.Trail.ShouldBe(AuditResolutionTrail.Operation);
        document.EventId.ShouldBe(eventId);
        document.Field.ShouldBe(AuditResolutionField.UserId);
        document.Kind.ShouldBe(AuditResolutionKind.Shredded);
        document.AuditEntryId.ShouldNotBeNull();
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
        var document = CreateDocument(Guid.NewGuid());

        var record = new AuditResolutionRecord(
            document.Trail, document.EventId, document.Field, document.Kind, document.AuditEntryId, document.TenantId,
            document.ErasureRequestId, document.ErasureRequestedAtUtc, document.ErasureActor,
            document.RetentionPolicyId, document.RetentionCutoffUtc, document.RetentionExecutedAtUtc,
            document.RetentionActor, document.ResolvedBy, document.ResolvedAtUtc);

        record.Trail.ShouldBe(document.Trail);
        record.EventId.ShouldBe(document.EventId);
        record.Field.ShouldBe(document.Field);
        record.Kind.ShouldBe(document.Kind);
        record.AuditEntryId.ShouldBe(document.AuditEntryId);
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
            AuditResolutionTrail.Read, Guid.NewGuid(), AuditResolutionField.Purpose, AuditResolutionKind.Unreadable,
            null, null, null, null, null, null, null, null, null, "operator", At);

        var same = a with { };
        var other = a with { Kind = AuditResolutionKind.Excluded };

        same.ShouldBe(a);
        same.GetHashCode().ShouldBe(a.GetHashCode());
        other.ShouldNotBe(a);
        a.Kind.ShouldBe(AuditResolutionKind.Unreadable);
        a.ToString().ShouldContain("Unreadable");
        (same == a).ShouldBeTrue();
    }
}
