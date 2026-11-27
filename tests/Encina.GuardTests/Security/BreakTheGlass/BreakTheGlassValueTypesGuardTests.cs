using System.Diagnostics.Metrics;
using System.Reflection;
using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Security.BreakTheGlass.Diagnostics;
using LanguageExt;
using NSubstitute;
using Shouldly;

#pragma warning disable CA2012 // NSubstitute setups of a ValueTask-returning member: the call configures the double, it consumes nothing

namespace Encina.GuardTests.Security.BreakTheGlass;

/// <summary>A request type marked eligible, whose subject id is a string property.</summary>
[BreakTheGlassEligible("clinical", SubjectIdProperty = nameof(PatientId))]
public sealed record GuardEligibleRequest(string PatientId);

/// <summary>A request type that carries no eligibility attribute.</summary>
public sealed record GuardPlainRequest(string PatientId);

/// <summary>Carries a value the conversion helper reads.</summary>
public sealed class GuardSubjectCarrier
{
    public object? Subject { get; set; }
}

/// <summary>
/// Guard-flag tests of the break-the-glass helpers, factories and records whose methods take
/// reference-type parameters. None of them validates its arguments with a throw: each is a
/// total function over any value, so the tests run the declared members with valid and edge
/// arguments and assert the result.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassValueTypesGuardTests
{
    private static readonly PropertyInfo SubjectProperty = typeof(GuardSubjectCarrier).GetProperty(nameof(GuardSubjectCarrier.Subject))!;

    // ── BreakTheGlassErrors ─────────────────────────────────────────────

    [Fact]
    public void Errors_ValidationFailed_CarriesTheReasonKeyword()
    {
        var error = BreakTheGlassErrors.ValidationFailed("justification_missing");

        error.GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.ValidationFailedCode);
    }

    [Fact]
    public void Errors_ValidationFailed_AcceptsAnEmptyReason()
    {
        var error = BreakTheGlassErrors.ValidationFailed(string.Empty);

        error.GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.ValidationFailedCode);
    }

    [Fact]
    public void Errors_FixedFactories_CarryTheirCodes()
    {
        BreakTheGlassErrors.ScopeUnknown().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.ScopeUnknownCode);
        BreakTheGlassErrors.AlreadyActive().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.AlreadyActiveCode);
        BreakTheGlassErrors.NotFound().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.NotFoundCode);
        BreakTheGlassErrors.NotActive().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.NotActiveCode);
        BreakTheGlassErrors.AlreadyReviewed().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.AlreadyReviewedCode);
        BreakTheGlassErrors.SelfReview().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.SelfReviewCode);
        BreakTheGlassErrors.AuditFailed().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.AuditFailedCode);
        BreakTheGlassErrors.TenantRequired().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.TenantRequiredCode);
    }

    [Fact]
    public void Errors_StoreFailed_AcceptsANullAndAnExceptionArgument()
    {
        BreakTheGlassErrors.StoreFailed().GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.StoreFailedCode);
        BreakTheGlassErrors.StoreFailed(null).GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.StoreFailedCode);
        BreakTheGlassErrors.StoreFailed(new InvalidOperationException("x")).GetCode().IfNone(string.Empty)
            .ShouldBe(BreakTheGlassErrors.StoreFailedCode);
    }

    [Fact]
    public void Errors_EligibilityRefusals_AreForbidden()
    {
        BreakTheGlassErrors.NotEligible().GetCode().IfNone(string.Empty).ShouldNotBeNullOrEmpty();
        BreakTheGlassErrors.ReviewerNotEligible().GetCode().IfNone(string.Empty).ShouldNotBeNullOrEmpty();
    }

    // ── BreakTheGlassDiagnostics ────────────────────────────────────────

    [Fact]
    public void Diagnostics_RecordAccess_EmitsTheScopeAndGateTags()
    {
        var measurements = new List<(long Value, string? Scope, string? Gate)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == BreakTheGlassDiagnostics.MeterName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name != "encina.break_glass.accesses")
            {
                return;
            }

            string? scope = null;
            string? gate = null;
            foreach (var tag in tags)
            {
                if (tag.Key == BreakTheGlassDiagnostics.TagScope) scope = tag.Value as string;
                if (tag.Key == BreakTheGlassDiagnostics.TagGate) gate = tag.Value as string;
            }

            lock (measurements)
            {
                measurements.Add((value, scope, gate));
            }
        });
        listener.Start();

        BreakTheGlassDiagnostics.RecordAccess("guard-scope-unique", "guard-gate-unique");
        listener.RecordObservableInstruments();

        lock (measurements)
        {
            measurements.ShouldContain(m => m.Value == 1 && m.Scope == "guard-scope-unique" && m.Gate == "guard-gate-unique");
        }
    }

    [Fact]
    public void Diagnostics_RecordStoreFailure_EmitsTheOperationTag()
    {
        string? operation = null;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == BreakTheGlassDiagnostics.MeterName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "encina.break_glass.store_failures")
            {
                return;
            }

            foreach (var tag in tags)
            {
                if (tag.Key == BreakTheGlassDiagnostics.TagOperation && tag.Value is "guard-op-unique")
                {
                    operation = tag.Value as string;
                }
            }
        });
        listener.Start();

        BreakTheGlassDiagnostics.RecordStoreFailure("guard-op-unique");

        operation.ShouldBe("guard-op-unique");
    }

    // ── BreakTheGlassAudit ──────────────────────────────────────────────

    [Fact]
    public void Audit_Create_BuildsTheEntryFromItsArguments()
    {
        var elevationId = Guid.NewGuid();
        var now = DateTimeOffset.UnixEpoch.AddDays(10);
        var metadata = new Dictionary<string, object?> { ["scope"] = "clinical" };

        var entry = BreakTheGlassAudit.Create(
            BreakTheGlassAudit.ActionOpened, AuditOutcome.Success, elevationId, "corr-1", "user-1", "tenant-1", now, metadata);

        entry.Action.ShouldBe(BreakTheGlassAudit.ActionOpened);
        entry.EntityType.ShouldBe(BreakTheGlassAudit.EntityType);
        entry.EntityId.ShouldBe(elevationId.ToString("D"));
        entry.CorrelationId.ShouldBe("corr-1");
        entry.UserId.ShouldBe("user-1");
        entry.TenantId.ShouldBe("tenant-1");
        entry.Outcome.ShouldBe(AuditOutcome.Success);
        entry.StartedAtUtc.ShouldBe(now);
        entry.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void Audit_Create_AcceptsANullUserAndTenant()
    {
        var entry = BreakTheGlassAudit.Create(
            BreakTheGlassAudit.ActionExpired, AuditOutcome.Failure, Guid.NewGuid(), "corr", null, null,
            DateTimeOffset.UnixEpoch, new Dictionary<string, object?>());

        entry.UserId.ShouldBeNull();
        entry.TenantId.ShouldBeNull();
    }

    [Fact]
    public async Task Audit_WriteAsync_ReturnsRightWhenTheStoreRecords()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));
        var entry = SampleEntry();

        var result = await BreakTheGlassAudit.WriteAsync(store, entry, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Audit_WriteAsync_ReturnsAuditFailedWhenTheStoreReturnsLeft()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(EncinaErrors.Create("store.failed", "boom")));

        var result = await BreakTheGlassAudit.WriteAsync(store, SampleEntry(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => string.Empty, e => e.GetCode().IfNone(string.Empty)).ShouldBe(BreakTheGlassErrors.AuditFailedCode);
    }

    [Fact]
    public async Task Audit_WriteAsync_ReturnsAuditFailedWhenTheStoreThrows()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("down"));

        var result = await BreakTheGlassAudit.WriteAsync(store, SampleEntry(), CancellationToken.None);

        result.Match(_ => string.Empty, e => e.GetCode().IfNone(string.Empty)).ShouldBe(BreakTheGlassErrors.AuditFailedCode);
    }

    [Fact]
    public async Task Audit_WriteAsync_PropagatesCancellation()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await BreakTheGlassAudit.WriteAsync(store, SampleEntry(), CancellationToken.None));
    }

    private static OperationAuditEntry SampleEntry() => BreakTheGlassAudit.Create(
        BreakTheGlassAudit.ActionAccess, AuditOutcome.Success, Guid.NewGuid(), "corr", "u", "t",
        DateTimeOffset.UnixEpoch, new Dictionary<string, object?>());

    // ── BreakTheGlassEligibility ────────────────────────────────────────

    [Fact]
    public void Eligibility_Info_IsNullForATypeWithoutTheAttribute() =>
        BreakTheGlassEligibility<GuardPlainRequest>.Info.ShouldBeNull();

    [Fact]
    public void Eligibility_Info_ReadsTheScopeAndTheSubjectId()
    {
        var info = BreakTheGlassEligibility<GuardEligibleRequest>.Info;

        info.ShouldNotBeNull();
        info.Scope.ShouldBe("clinical");
        info.GetSubjectId(new GuardEligibleRequest("patient-1")).ShouldBe("patient-1");
        info.GetSubjectId(new GuardEligibleRequest(" ")).ShouldBeNull();
    }

    [Fact]
    public void Eligibility_InfoRecord_KeepsItsConstructorArguments()
    {
        Func<GuardPlainRequest, string?> read = r => r.PatientId;

        var info = new BreakTheGlassEligibilityInfo<GuardPlainRequest>("scope-x", read);

        info.Scope.ShouldBe("scope-x");
        info.GetSubjectId.ShouldBeSameAs(read);
    }

    // ── BreakTheGlassSubjectIdConversion ────────────────────────────────

    [Fact]
    public void SubjectIdConversion_NullValue_MeansSubjectMissing() =>
        BreakTheGlassSubjectIdConversion.ToInvariantString(null, SubjectProperty).ShouldBeNull();

    [Fact]
    public void SubjectIdConversion_ConvertsPrimitiveShapes()
    {
        var guid = Guid.NewGuid();

        BreakTheGlassSubjectIdConversion.ToInvariantString("p-1", SubjectProperty).ShouldBe("p-1");
        BreakTheGlassSubjectIdConversion.ToInvariantString("  ", SubjectProperty).ShouldBeNull();
        BreakTheGlassSubjectIdConversion.ToInvariantString(guid, SubjectProperty).ShouldBe(guid.ToString("D"));
        BreakTheGlassSubjectIdConversion.ToInvariantString(Guid.Empty, SubjectProperty).ShouldBeNull();
        BreakTheGlassSubjectIdConversion.ToInvariantString(42, SubjectProperty).ShouldBe("42");
    }

    [Fact]
    public void SubjectIdConversion_UnsupportedType_ThrowsInvalidOperation() =>
        Should.Throw<InvalidOperationException>(() =>
            BreakTheGlassSubjectIdConversion.ToInvariantString(new object(), SubjectProperty));

    // ── Records ─────────────────────────────────────────────────────────

    [Fact]
    public void Request_KeepsItsArguments_AndToStringOmitsTheSubjectAndJustification()
    {
        var request = new BreakTheGlassRequest("patient-secret", "clinical", "crisis-secret", TimeSpan.FromMinutes(30));

        request.SubjectId.ShouldBe("patient-secret");
        request.Scope.ShouldBe("clinical");
        request.Justification.ShouldBe("crisis-secret");
        request.Duration.ShouldBe(TimeSpan.FromMinutes(30));
        var text = request.ToString();
        text.ShouldContain("clinical");
        text.ShouldNotContain("patient-secret");
        text.ShouldNotContain("crisis-secret");
    }

    [Fact]
    public void OpenedNotification_KeepsItsArguments_AndAcceptsANullTenant()
    {
        var id = Guid.NewGuid();
        var opened = DateTimeOffset.UnixEpoch;
        var expires = opened.AddHours(1);

        var withTenant = new BreakTheGlassOpenedNotification(id, "t1", "clinical", opened, expires);
        var withoutTenant = new BreakTheGlassOpenedNotification(id, null, "clinical", opened, expires);

        withTenant.ElevationId.ShouldBe(id);
        withTenant.TenantId.ShouldBe("t1");
        withTenant.Scope.ShouldBe("clinical");
        withTenant.OpenedAtUtc.ShouldBe(opened);
        withTenant.ExpiresAtUtc.ShouldBe(expires);
        withoutTenant.TenantId.ShouldBeNull();
        withTenant.ShouldNotBe(withoutTenant);
    }

    [Fact]
    public void ReviewedNotification_KeepsItsArguments_AndAcceptsANullTenant()
    {
        var id = Guid.NewGuid();
        var reviewed = DateTimeOffset.UnixEpoch.AddDays(1);

        var withTenant = new BreakTheGlassReviewedNotification(id, "t1", "clinical", BreakTheGlassReviewOutcome.Unjustified, reviewed);
        var withoutTenant = new BreakTheGlassReviewedNotification(id, null, "clinical", BreakTheGlassReviewOutcome.Justified, reviewed);

        withTenant.ElevationId.ShouldBe(id);
        withTenant.TenantId.ShouldBe("t1");
        withTenant.Scope.ShouldBe("clinical");
        withTenant.Outcome.ShouldBe(BreakTheGlassReviewOutcome.Unjustified);
        withTenant.ReviewedAtUtc.ShouldBe(reviewed);
        withoutTenant.TenantId.ShouldBeNull();
        withTenant.ShouldNotBe(withoutTenant);
    }
}
