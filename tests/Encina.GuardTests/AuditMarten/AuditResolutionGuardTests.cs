using Encina.Audit.Marten;
using Encina.Audit.Marten.Resolution;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests for the resolution model (issue #3049, phase 1): the document and snapshot types are plain data
/// holders with no hidden guard and the document carries no reason text.
/// </summary>
public sealed class AuditResolutionGuardTests
{
    [Fact]
    public void ResolutionErrorFactories_NeverThrowAndCarryTheirCodes()
    {
        MartenAuditErrors.ResolutionConflict().Message.ShouldNotBeNullOrWhiteSpace();
        MartenAuditErrors.ResolutionPrincipalRequired().Message.ShouldNotBeNullOrWhiteSpace();
        MartenAuditErrors.InvalidResolutionReason().Message.ShouldNotBeNullOrWhiteSpace();
        MartenAuditErrors.ResolutionEventNotFound().Message.ShouldNotBeNullOrWhiteSpace();
        MartenAuditErrors.InvalidResolutionField().GetCode().IfNone(string.Empty)
            .ShouldBe(MartenAuditErrors.InvalidResolutionFieldCode);
    }

    [Fact]
    public void Document_DefaultsAreEmptyAndCarryNoReasonProperty()
    {
        var document = new AuditResolution();

        document.Id.ShouldBe(string.Empty);
        document.ResolvedBy.ShouldBe(string.Empty);
        typeof(AuditResolution).GetProperties().ShouldNotContain(p => p.Name.Contains("Reason", StringComparison.Ordinal));
    }

    [Fact]
    public void Document_AssignedValuesAreReadBack()
    {
        var id = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;

        var document = new AuditResolution
        {
            Id = "x", Trail = AuditResolutionTrail.Read, EventId = id, Field = AuditResolutionField.Purpose,
            Kind = AuditResolutionKind.Expired, AuditEntryId = id, TenantId = "t", ErasureRequestId = "e",
            ErasureRequestedAtUtc = at, ErasureActor = "a", RetentionPolicyId = "p", RetentionCutoffUtc = at,
            RetentionExecutedAtUtc = at, RetentionActor = "r", ResolvedBy = "op", ResolvedAtUtc = at
        };

        document.EventId.ShouldBe(id);
        document.Kind.ShouldBe(AuditResolutionKind.Expired);
        document.RetentionActor.ShouldBe("r");
        document.ResolvedAtUtc.ShouldBe(at);
    }

    [Fact]
    public void Record_CarriesTheSnapshotValuesAndIsValueEqual()
    {
        var id = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;
        var a = new AuditResolutionRecord(
            AuditResolutionTrail.Read, id, AuditResolutionField.Purpose, AuditResolutionKind.Unreadable,
            null, "t1", null, null, null, null, null, null, null, "operator", at);
        var b = a with { };

        b.ShouldBe(a);
        a.EventId.ShouldBe(id);
        a.ResolvedBy.ShouldBe("operator");
    }
}
