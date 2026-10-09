using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// Unit tests for the decision audit model of #751 Phase 1: the record, the schema constants,
/// the resource identity and the new <see cref="ABACErrors"/> factories.
/// </summary>
public sealed class ABACDecisionAuditModelTests
{
    private sealed record OrderQuery(Guid OrderId) : IABACResourceIdentity
    {
        public string? ResourceId => OrderId.ToString();
    }

    private static ABACDecisionRecord MinimalRecord() => new()
    {
        DecisionId = Guid.CreateVersion7(),
        IdentityKind = IdentityKind.User,
        RequestType = typeof(OrderQuery).FullName!,
        EnforcedOutcome = ABACEnforcedOutcome.Granted,
        ReasonCode = ABACDecisionAuditSchema.PermitReasonCode,
        EnforcementMode = ABACEnforcementMode.Block,
        StartedAtUtc = new DateTimeOffset(2026, 10, 9, 8, 0, 0, TimeSpan.Zero),
        CompletedAtUtc = new DateTimeOffset(2026, 10, 9, 8, 0, 1, TimeSpan.Zero)
    };

    private static string Code(EncinaError error) => error.GetCode().IfNone("<none>");

    private static object? Detail(EncinaError error, string key) =>
        error.GetDetails().TryGetValue(key, out var value) ? value : null;

    // ── ABACDecisionRecord ───────────────────────────────────────────

    [Fact]
    public void Record_OptionalMembers_DefaultToEmptyNotNull()
    {
        var record = MinimalRecord();

        record.UserId.ShouldBeNull();
        record.ResourceId.ShouldBeNull();
        record.Effect.ShouldBeNull();
        record.EvaluatedPolicies.ShouldBeEmpty();
        record.ObligationIds.ShouldBeEmpty();
        record.AdviceIds.ShouldBeEmpty();
        record.AttributeNames.ShouldBeEmpty();
        record.RecordedValues.ShouldBeEmpty();
        record.TraceTruncated.ShouldBeFalse();
    }

    [Fact]
    public void Record_WithExpression_ChangesOnlyTheNamedMember()
    {
        var original = MinimalRecord();

        var denied = original with { EnforcedOutcome = ABACEnforcedOutcome.Denied, ReasonCode = ABACErrors.AccessDeniedCode };

        denied.DecisionId.ShouldBe(original.DecisionId);
        denied.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.Denied);
        denied.ReasonCode.ShouldBe("encina.authorization.abac_access_denied");
        original.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.Granted);
    }

    [Fact]
    public void Record_CarriesAttributeNamesPerCategory()
    {
        var record = MinimalRecord() with
        {
            AttributeNames = new Dictionary<AttributeCategory, IReadOnlyList<string>>
            {
                [AttributeCategory.Subject] = ["department"],
                [AttributeCategory.Resource] = ["classification", "owner"]
            }
        };

        record.AttributeNames[AttributeCategory.Resource].ShouldBe(["classification", "owner"]);
    }

    [Theory]
    [InlineData(ABACEnforcedOutcome.Granted, 0)]
    [InlineData(ABACEnforcedOutcome.Denied, 1)]
    [InlineData(ABACEnforcedOutcome.DeniedNotEnforced, 2)]
    public void EnforcedOutcome_HasStableValues(ABACEnforcedOutcome outcome, int value) =>
        ((int)outcome).ShouldBe(value);

    // ── IABACResourceIdentity ────────────────────────────────────────

    [Fact]
    public void ResourceIdentity_ExposesTheDeclaredResourceId()
    {
        var id = Guid.NewGuid();
        IABACResourceIdentity request = new OrderQuery(id);

        request.ResourceId.ShouldBe(id.ToString());
    }

    // ── ABACDecisionAuditSchema ──────────────────────────────────────

    [Fact]
    public void Schema_ActionAndVersionAreStable()
    {
        ABACDecisionAuditSchema.Action.ShouldBe("ABACDecision");
        ABACDecisionAuditSchema.SchemaVersion.ShouldBe("encina.abac.decision/1");
        ABACDecisionAuditSchema.MetadataIdentityKind.ShouldBe("abac.identity_kind");
    }

    [Fact]
    public void Schema_MetadataKeysAreDistinctAndNamespaced()
    {
        string[] keys =
        [
            ABACDecisionAuditSchema.MetadataSchema, ABACDecisionAuditSchema.MetadataStage,
            ABACDecisionAuditSchema.MetadataEnforced, ABACDecisionAuditSchema.MetadataEnforcementMode,
            ABACDecisionAuditSchema.MetadataIdentityKind, ABACDecisionAuditSchema.MetadataEffect,
            ABACDecisionAuditSchema.MetadataPolicyId, ABACDecisionAuditSchema.MetadataRuleId,
            ABACDecisionAuditSchema.MetadataModuleId, ABACDecisionAuditSchema.MetadataTrace,
            ABACDecisionAuditSchema.MetadataTraceTruncated, ABACDecisionAuditSchema.MetadataObligations,
            ABACDecisionAuditSchema.MetadataAdvice, ABACDecisionAuditSchema.MetadataAttributeNames,
            ABACDecisionAuditSchema.MetadataAttributeValuePrefix, ABACDecisionAuditSchema.MetadataStartedAtUtc,
            ABACDecisionAuditSchema.MetadataCompletedAtUtc, ABACDecisionAuditSchema.MetadataHashedFields,
            ABACDecisionAuditSchema.MetadataDroppedFields, ABACDecisionAuditSchema.MetadataTruncatedFields
        ];

        keys.Distinct().Count().ShouldBe(keys.Length);
        keys.ShouldAllBe(key => key.StartsWith("abac.", StringComparison.Ordinal));
    }

    [Fact]
    public void Schema_HashedValueFitsEveryIdColumn()
    {
        ABACDecisionAuditSchema.HashedValueLength.ShouldBe(ABACDecisionAuditSchema.HashPrefix.Length + 64);
        new[]
        {
            ABACDecisionAuditSchema.UserIdMaxLength, ABACDecisionAuditSchema.EntityTypeMaxLength,
            ABACDecisionAuditSchema.EntityIdMaxLength, ABACDecisionAuditSchema.CorrelationIdMaxLength,
            ABACDecisionAuditSchema.TenantIdMaxLength
        }.ShouldAllBe(limit => limit >= ABACDecisionAuditSchema.HashedValueLength);
    }

    [Fact]
    public void Schema_ColumnLimitsMatchTheOperationAuditColumns()
    {
        ABACDecisionAuditSchema.UserIdMaxLength.ShouldBe(256);
        ABACDecisionAuditSchema.TenantIdMaxLength.ShouldBe(128);
        ABACDecisionAuditSchema.ActionMaxLength.ShouldBe(128);
        ABACDecisionAuditSchema.ErrorMessageMaxLength.ShouldBe(2048);
        ABACDecisionAuditSchema.IpAddressMaxLength.ShouldBe(45);
        ABACDecisionAuditSchema.UserAgentMaxLength.ShouldBe(512);
        ABACDecisionAuditSchema.Action.Length.ShouldBeLessThanOrEqualTo(ABACDecisionAuditSchema.ActionMaxLength);
    }

    // ── ABACErrors (decision audit) ──────────────────────────────────

    [Fact]
    public void DecisionAuditFailed_UsesFixedMessageAndOnlyTheCauseCode()
    {
        var error = ABACErrors.DecisionAuditFailed(typeof(OrderQuery), "store.timeout");

        Code(error).ShouldBe("abac.decision_audit_failed");
        error.Message.ShouldBe("The ABAC decision could not be recorded in the audit trail. Access denied.");
        Detail(error, "cause").ShouldBe("store.timeout");
        Detail(error, "requestType").ShouldBe(typeof(OrderQuery).FullName);
        Detail(error, "stage").ShouldBe("abac");
    }

    [Fact]
    public void DecisionAuditFailed_WithoutCause_KeepsTheCauseEmpty() =>
        Detail(ABACErrors.DecisionAuditFailed(typeof(OrderQuery), null), "cause").ShouldBeNull();

    [Fact]
    public void InvalidDecisionAuditQuery_UsesFixedMessage()
    {
        var error = ABACErrors.InvalidDecisionAuditQuery("pageSize");

        Code(error).ShouldBe("abac.invalid_decision_audit_query");
        error.Message.ShouldBe("The decision audit query is invalid.");
        Detail(error, "reason").ShouldBe("pageSize");
    }

    [Fact]
    public void DecisionAuditStoreUnavailable_NamesTheMissingRequirement()
    {
        var error = ABACErrors.DecisionAuditStoreUnavailable();

        Code(error).ShouldBe("abac.decision_audit_store_unavailable");
        Detail(error, "requirement").ShouldBe("IOperationAuditStore");
    }

    [Fact]
    public void DecisionAuditTenantDenials_AreAuthorizationCodesWithFixedMessages()
    {
        var required = ABACErrors.DecisionAuditTenantRequired();
        var mismatch = ABACErrors.DecisionAuditTenantMismatch();

        Code(required).ShouldStartWith(EncinaErrorCodes.AuthorizationPrefix);
        Code(mismatch).ShouldStartWith(EncinaErrorCodes.AuthorizationPrefix);
        Code(required).ShouldBe(ABACErrors.DecisionAuditTenantRequiredCode);
        Code(mismatch).ShouldBe(ABACErrors.DecisionAuditTenantMismatchCode);
        required.Message.ShouldBe("Reading the decision audit trail requires a tenant. Access denied.");
        mismatch.Message.ShouldBe("The decision audit query names a different tenant than the request. Access denied.");
    }

    [Fact]
    public void DecisionAuditServerSideCodes_StayInTheAbacFamily()
    {
        ABACErrors.DecisionAuditFailedCode.ShouldStartWith("abac.");
        ABACErrors.InvalidDecisionAuditQueryCode.ShouldStartWith("abac.");
        ABACErrors.DecisionAuditStoreUnavailableCode.ShouldStartWith("abac.");
    }
}
