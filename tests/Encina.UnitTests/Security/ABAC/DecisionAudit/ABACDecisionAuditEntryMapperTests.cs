using System.Text.Json;

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// Unit tests for <see cref="ABACDecisionAuditEntryMapper"/> (#751 Phase 3): the entry shape, the
/// outcome mapping, the per-column over-limit rule (one test per bounded column) and the read-back.
/// </summary>
public sealed class ABACDecisionAuditEntryMapperTests
{
    private static readonly DateTimeOffset Started = new(2026, 10, 9, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Completed = new(2026, 10, 9, 8, 0, 1, 250, TimeSpan.Zero);

    private static ABACDecisionRecord Record(
        ABACEnforcedOutcome outcome = ABACEnforcedOutcome.Granted,
        string reason = ABACDecisionAuditSchema.PermitReasonCode) => new()
        {
            DecisionId = Guid.CreateVersion7(Started),
            UserId = "user-1",
            IdentityKind = IdentityKind.User,
            TenantId = "tenant-1",
            CorrelationId = "corr-1",
            ModuleId = "orders",
            IpAddress = "203.0.113.7",
            UserAgent = "agent/1.0",
            RequestType = "GetOrderQuery",
            ResourceId = "order-9",
            EnforcedOutcome = outcome,
            ReasonCode = reason,
            EnforcementMode = ABACEnforcementMode.Block,
            Effect = Effect.Permit,
            PolicyId = "orders-policy",
            RuleId = "rule-1",
            StartedAtUtc = Started,
            CompletedAtUtc = Completed
        };

    private static string? Meta(OperationAuditEntry entry, string key) =>
        entry.Metadata.TryGetValue(key, out var value) ? (string?)value : null;

    // A marker is stored as a JSON array of strings; joined with commas here for compact assertions.
    private static string? Markers(OperationAuditEntry entry, string key) =>
        Meta(entry, key) is { } json ? string.Join(",", JsonSerializer.Deserialize<List<string>>(json)!) : null;

    private static List<string> JsonArray(OperationAuditEntry entry, string key) =>
        JsonSerializer.Deserialize<List<string>>(Meta(entry, key)!)!;

    [Fact]
    public void ToOperationAuditEntry_Null_Throws() =>
        Should.Throw<ArgumentNullException>(() => ABACDecisionAuditEntryMapper.ToOperationAuditEntry(null!));

    [Fact]
    public void ToOperationAuditEntry_MapsColumnsFromTheRecord()
    {
        var record = Record();

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record);

        entry.Id.ShouldBe(record.DecisionId);
        entry.Action.ShouldBe(ABACDecisionAuditSchema.Action);
        entry.EntityType.ShouldBe("GetOrderQuery");
        entry.EntityId.ShouldBe("order-9");
        entry.UserId.ShouldBe("user-1");
        entry.TenantId.ShouldBe("tenant-1");
        entry.CorrelationId.ShouldBe("corr-1");
        entry.ErrorMessage.ShouldBe(ABACDecisionAuditSchema.PermitReasonCode);
        entry.IpAddress.ShouldBe("203.0.113.7");
        entry.UserAgent.ShouldBe("agent/1.0");
        entry.RequestPayload.ShouldBeNull();
        entry.ResponsePayload.ShouldBeNull();
        entry.RequestPayloadHash.ShouldBeNull();
    }

    [Fact]
    public void ToOperationAuditEntry_TakesTheThreeTimestampsFromTheTwoClockReads()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record());

        entry.StartedAtUtc.ShouldBe(Started);
        entry.CompletedAtUtc.ShouldBe(Completed);
        entry.TimestampUtc.ShouldBe(Completed.UtcDateTime);
        entry.TimestampUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void ToOperationAuditEntry_WritesStringOnlyMetadataUnderAbacKeys()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with
        {
            ObligationIds = ["o1", "o2"],
            AdviceIds = ["a1"],
            TraceTruncated = true
        });

        entry.Metadata.Values.ShouldAllBe(value => value is string);
        entry.Metadata.Keys.ShouldAllBe(key => key.StartsWith("abac.", StringComparison.Ordinal));
        Meta(entry, ABACDecisionAuditSchema.MetadataSchema).ShouldBe(ABACDecisionAuditSchema.SchemaVersion);
        Meta(entry, ABACDecisionAuditSchema.MetadataStage).ShouldBe(ABACDecisionAuditSchema.StagePep);
        Meta(entry, ABACDecisionAuditSchema.MetadataEnforced).ShouldBe("true");
        Meta(entry, ABACDecisionAuditSchema.MetadataEnforcementMode).ShouldBe("Block");
        Meta(entry, ABACDecisionAuditSchema.MetadataIdentityKind).ShouldBe("User");
        Meta(entry, ABACDecisionAuditSchema.MetadataEffect).ShouldBe("Permit");
        Meta(entry, ABACDecisionAuditSchema.MetadataPolicyId).ShouldBe("orders-policy");
        Meta(entry, ABACDecisionAuditSchema.MetadataRuleId).ShouldBe("rule-1");
        Meta(entry, ABACDecisionAuditSchema.MetadataModuleId).ShouldBe("orders");
        Meta(entry, ABACDecisionAuditSchema.MetadataObligations).ShouldBe("""["o1","o2"]""");
        Meta(entry, ABACDecisionAuditSchema.MetadataAdvice).ShouldBe("""["a1"]""");
        Meta(entry, ABACDecisionAuditSchema.MetadataTraceTruncated).ShouldBe("true");
        Meta(entry, ABACDecisionAuditSchema.MetadataStartedAtUtc).ShouldBe(Started.ToString("O"));
        Meta(entry, ABACDecisionAuditSchema.MetadataCompletedAtUtc).ShouldBe(Completed.ToString("O"));
    }

    [Fact]
    public void ToOperationAuditEntry_WithinLimits_WritesNoMarkers()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record());

        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataHashedFields);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataDroppedFields);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataTruncatedFields);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataTrace);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataAttributeNames);
    }

    [Fact]
    public void ToOperationAuditEntry_UnauthenticatedCaller_StoresNoSubjectAndNoEffect()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record(
            ABACEnforcedOutcome.Denied, EncinaErrorCodes.AuthorizationUnauthenticated) with
        {
            UserId = null,
            IdentityKind = IdentityKind.Anonymous,
            Effect = null,
            PolicyId = null,
            RuleId = null
        });

        entry.UserId.ShouldBeNull();
        entry.Outcome.ShouldBe(AuditOutcome.Denied);
        entry.ErrorMessage.ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        Meta(entry, ABACDecisionAuditSchema.MetadataIdentityKind).ShouldBe("Anonymous");
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataEffect);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataPolicyId);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataRuleId);
    }

    // ── Outcome mapping ──────────────────────────────────────────────

    [Theory]
    [InlineData(ABACEnforcedOutcome.Granted, ABACDecisionAuditSchema.PermitReasonCode, AuditOutcome.Success)]
    [InlineData(ABACEnforcedOutcome.DeniedNotEnforced, ABACErrors.AccessDeniedCode, AuditOutcome.Success)]
    [InlineData(ABACEnforcedOutcome.DeniedNotEnforced, ABACErrors.RequiredPolicyNotFoundCode, AuditOutcome.Success)]
    [InlineData(ABACEnforcedOutcome.DeniedNotEnforced, ABACErrors.ConditionNotMetCode, AuditOutcome.Success)]
    [InlineData(ABACEnforcedOutcome.Denied, ABACErrors.AccessDeniedCode, AuditOutcome.Denied)]
    [InlineData(ABACEnforcedOutcome.Denied, ABACErrors.ObligationFailedCode, AuditOutcome.Denied)]
    [InlineData(ABACEnforcedOutcome.Denied, ABACErrors.RequiredPolicyNotFoundCode, AuditOutcome.Denied)]
    [InlineData(ABACEnforcedOutcome.Denied, ABACErrors.ConditionNotMetCode, AuditOutcome.Denied)]
    [InlineData(ABACEnforcedOutcome.Denied, EncinaErrorCodes.AuthorizationUnauthenticated, AuditOutcome.Denied)]
    [InlineData(ABACEnforcedOutcome.Denied, ABACErrors.IndeterminateCode, AuditOutcome.Error)]
    [InlineData(ABACEnforcedOutcome.Denied, ABACErrors.EvaluationFailedCode, AuditOutcome.Error)]
    public void ToOperationAuditEntry_MapsEveryDecisionPathToItsAuditOutcome(
        ABACEnforcedOutcome enforced, string reason, AuditOutcome expected)
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record(enforced, reason));

        entry.Outcome.ShouldBe(expected);
        entry.ErrorMessage.ShouldBe(reason);
    }

    [Fact]
    public void ToOperationAuditEntry_WarnModeWouldDeny_IsMarkedNotEnforced()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(
            Record(ABACEnforcedOutcome.DeniedNotEnforced, ABACErrors.AccessDeniedCode));

        Meta(entry, ABACDecisionAuditSchema.MetadataEnforced).ShouldBe("false");
    }

    // ── Per-column over-limit rule ───────────────────────────────────

    private static void ShouldBeHash(string? value, string original)
    {
        value.ShouldNotBeNull();
        value.Length.ShouldBe(ABACDecisionAuditSchema.HashedValueLength);
        value.ShouldStartWith(ABACDecisionAuditSchema.HashPrefix);
        value.ShouldBe(ABACDecisionAuditEntryMapper.Hash(original));
    }

    [Fact]
    public void ToOperationAuditEntry_UserIdOverLimit_IsHashedAndMarked()
    {
        var longValue = new string('u', ABACDecisionAuditSchema.UserIdMaxLength + 1);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { UserId = longValue });

        ShouldBeHash(entry.UserId, longValue);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe("UserId");
    }

    [Fact]
    public void ToOperationAuditEntry_UserIdAtLimit_IsKept()
    {
        var atLimit = new string('u', ABACDecisionAuditSchema.UserIdMaxLength);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { UserId = atLimit });

        entry.UserId.ShouldBe(atLimit);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataHashedFields);
    }

    [Fact]
    public void ToOperationAuditEntry_EntityTypeOverLimit_IsHashedAndMarked()
    {
        var longValue = new string('t', ABACDecisionAuditSchema.EntityTypeMaxLength + 1);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { RequestType = longValue });

        ShouldBeHash(entry.EntityType, longValue);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe("EntityType");
    }

    [Fact]
    public void ToOperationAuditEntry_EntityIdOverLimit_IsHashedAndMarked()
    {
        var longValue = new string('e', ABACDecisionAuditSchema.EntityIdMaxLength + 1);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { ResourceId = longValue });

        ShouldBeHash(entry.EntityId, longValue);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe("EntityId");
    }

    [Fact]
    public void ToOperationAuditEntry_CorrelationIdOverLimit_IsHashedAndMarked()
    {
        var longValue = new string('c', ABACDecisionAuditSchema.CorrelationIdMaxLength + 1);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { CorrelationId = longValue });

        ShouldBeHash(entry.CorrelationId, longValue);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe("CorrelationId");
    }

    [Fact]
    public void ToOperationAuditEntry_TenantIdOverLimit_IsHashedAndMarked()
    {
        var longValue = new string('n', ABACDecisionAuditSchema.TenantIdMaxLength + 1);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { TenantId = longValue });

        ShouldBeHash(entry.TenantId, longValue);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe("TenantId");
    }

    [Fact]
    public void ToOperationAuditEntry_ReasonCodeOverLimit_IsHashedSoTheInsertNeverFails()
    {
        var longValue = new string('r', ABACDecisionAuditSchema.ErrorMessageMaxLength + 1);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { ReasonCode = longValue });

        ShouldBeHash(entry.ErrorMessage, longValue);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe("ErrorMessage");
    }

    [Fact]
    public void ToOperationAuditEntry_PolicyAndRuleIdsOverTheValueLimit_AreHashedAndMarked()
    {
        var longPolicy = new string('p', ABACDecisionAuditSchema.MaxAttributeValueLength + 1);
        var longRule = new string('r', ABACDecisionAuditSchema.MaxAttributeValueLength + 1);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { PolicyId = longPolicy, RuleId = longRule });

        ShouldBeHash(Meta(entry, ABACDecisionAuditSchema.MetadataPolicyId), longPolicy);
        ShouldBeHash(Meta(entry, ABACDecisionAuditSchema.MetadataRuleId), longRule);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields)
            .ShouldBe($"{ABACDecisionAuditSchema.MetadataPolicyId},{ABACDecisionAuditSchema.MetadataRuleId}");
    }

    [Fact]
    public void ToOperationAuditEntry_IpAddressOverLimit_IsDroppedAndMarked()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(
            Record() with { IpAddress = new string('1', ABACDecisionAuditSchema.IpAddressMaxLength + 1) });

        entry.IpAddress.ShouldBeNull();
        Markers(entry, ABACDecisionAuditSchema.MetadataDroppedFields).ShouldBe("IpAddress");
    }

    [Fact]
    public void ToOperationAuditEntry_UnparseableIpAddress_IsDroppedAndMarked()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { IpAddress = "not-an-address" });

        entry.IpAddress.ShouldBeNull();
        Markers(entry, ABACDecisionAuditSchema.MetadataDroppedFields).ShouldBe("IpAddress");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("10.1")]
    [InlineData("0x7f.1")]
    [InlineData("fe80::1%eth0")]
    [InlineData("fe80::1%3")]
    [InlineData("2001:0db8::1")]
    [InlineData("2001:db8:0:0:0:0:0:1")]
    public void ToOperationAuditEntry_NonCanonicalIpAddress_IsDroppedAndMarked(string address)
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { IpAddress = address });

        entry.IpAddress.ShouldBeNull();
        Markers(entry, ABACDecisionAuditSchema.MetadataDroppedFields).ShouldBe("IpAddress");
    }

    [Theory]
    [InlineData("198.51.100.23")]
    [InlineData("::1")]
    [InlineData("2001:DB8::1")]
    [InlineData("::ffff:192.0.2.1")]
    public void ToOperationAuditEntry_CanonicalIpAddress_IsKept(string address)
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { IpAddress = address });

        entry.IpAddress.ShouldBe(address);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataDroppedFields);
    }

    [Fact]
    public void ToOperationAuditEntry_IPv6Address_IsKept()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { IpAddress = "2001:db8::1" });

        entry.IpAddress.ShouldBe("2001:db8::1");
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataDroppedFields);
    }

    [Fact]
    public void ToOperationAuditEntry_UserAgentOverLimit_IsTruncatedAndMarked()
    {
        var longValue = new string('a', ABACDecisionAuditSchema.UserAgentMaxLength + 10);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { UserAgent = longValue });

        entry.UserAgent.ShouldBe(longValue[..ABACDecisionAuditSchema.UserAgentMaxLength]);
        Markers(entry, ABACDecisionAuditSchema.MetadataTruncatedFields).ShouldBe("UserAgent");
    }

    [Fact]
    public void ToOperationAuditEntry_TruncationAtASurrogatePair_NeverSplitsIt()
    {
        var value = new string('a', ABACDecisionAuditSchema.UserAgentMaxLength - 1) + "\U0001F600" + "tail";

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { UserAgent = value });

        entry.UserAgent!.Length.ShouldBe(ABACDecisionAuditSchema.UserAgentMaxLength - 1);
        char.IsHighSurrogate(entry.UserAgent[^1]).ShouldBeFalse();
    }

    [Fact]
    public void ToOperationAuditEntry_ModuleIdOverTheValueLimit_IsHashedAndMarked()
    {
        var longModule = new string('m', ABACDecisionAuditSchema.MaxAttributeValueLength + 1);

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { ModuleId = longModule });

        ShouldBeHash(Meta(entry, ABACDecisionAuditSchema.MetadataModuleId), longModule);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe(ABACDecisionAuditSchema.MetadataModuleId);
    }

    [Theory]
    [InlineData(ABACDecisionAuditSchema.MetadataObligations)]
    [InlineData(ABACDecisionAuditSchema.MetadataAdvice)]
    public void ToOperationAuditEntry_IdListOverTheCount_KeepsTheFirstIdsAndIsMarkedTruncated(string key)
    {
        var ids = Enumerable.Range(0, ABACDecisionAuditSchema.MaxListedIds + 6).Select(i => $"id-{i}").ToList();
        var record = key == ABACDecisionAuditSchema.MetadataObligations ? Record() with { ObligationIds = ids } : Record() with { AdviceIds = ids };

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record);

        JsonArray(entry, key).ShouldBe(ids.Take(ABACDecisionAuditSchema.MaxListedIds));
        Markers(entry, ABACDecisionAuditSchema.MetadataTruncatedFields).ShouldBe(key);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataHashedFields);
    }

    [Theory]
    [InlineData(ABACDecisionAuditSchema.MetadataObligations)]
    [InlineData(ABACDecisionAuditSchema.MetadataAdvice)]
    public void ToOperationAuditEntry_IdOverTheValueLimit_IsHashedAndMarked(string key)
    {
        var longId = new string('o', ABACDecisionAuditSchema.MaxAttributeValueLength + 1);
        List<string> ids = ["short", longId];
        var record = key == ABACDecisionAuditSchema.MetadataObligations ? Record() with { ObligationIds = ids } : Record() with { AdviceIds = ids };

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record);

        var stored = JsonArray(entry, key);
        stored[0].ShouldBe("short");
        ShouldBeHash(stored[1], longId);
        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe(key);
        entry.Metadata.ShouldNotContainKey(ABACDecisionAuditSchema.MetadataTruncatedFields);
    }

    [Fact]
    public void ToOperationAuditEntry_MarkedNameWithAComma_StaysOneMarkerEntry()
    {
        var key = ABACDecisionAuditSchema.MetadataAttributeValuePrefix + "unit,floor";

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with
        {
            RecordedValues = new Dictionary<string, string> { ["unit,floor"] = new string('v', ABACDecisionAuditSchema.MaxAttributeValueLength + 1) }
        });

        JsonArray(entry, ABACDecisionAuditSchema.MetadataTruncatedFields).ShouldBe([key]);
        ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(entry).TruncatedFields.ShouldBe([key]);
    }

    [Fact]
    public void ToOperationAuditEntry_IdsWithCommas_RoundTripIntact()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with { ObligationIds = ["notify,manager"], AdviceIds = ["a,b", "c"] });

        var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(entry);

        read.ObligationIds.ShouldBe(["notify,manager"]);
        read.AdviceIds.ShouldBe(["a,b", "c"]);
    }

    [Fact]
    public void ToOperationAuditEntry_AttributeNamesOverTheCap_AreCappedAndMarked()
    {
        var names = Enumerable.Range(0, ABACDecisionAuditSchema.MaxAttributeNames + 5).Select(i => $"n{i:D3}").ToList();

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with
        {
            AttributeNames = new Dictionary<AttributeCategory, IReadOnlyList<string>>
            {
                [AttributeCategory.Subject] = names.Take(100).ToList(),
                [AttributeCategory.Resource] = names.Skip(100).ToList()
            }
        });

        var stored = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(Meta(entry, ABACDecisionAuditSchema.MetadataAttributeNames)!)!;
        stored.Values.Sum(list => list.Count).ShouldBe(ABACDecisionAuditSchema.MaxAttributeNames);
        Markers(entry, ABACDecisionAuditSchema.MetadataTruncatedFields).ShouldBe(ABACDecisionAuditSchema.MetadataAttributeNames);
    }

    [Fact]
    public void ToOperationAuditEntry_RecordedValueOverTheCap_IsTruncatedAndMarked()
    {
        var longValue = new string('v', ABACDecisionAuditSchema.MaxAttributeValueLength + 1);
        var key = ABACDecisionAuditSchema.MetadataAttributeValuePrefix + "department";

        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with
        {
            RecordedValues = new Dictionary<string, string> { ["department"] = longValue, ["role"] = "nurse" }
        });

        Meta(entry, key)!.Length.ShouldBe(ABACDecisionAuditSchema.MaxAttributeValueLength);
        Meta(entry, ABACDecisionAuditSchema.MetadataAttributeValuePrefix + "role").ShouldBe("nurse");
        Markers(entry, ABACDecisionAuditSchema.MetadataTruncatedFields).ShouldBe(key);
    }

    [Fact]
    public void ToOperationAuditEntry_SeveralOverLimitFields_ListsEachInItsMarker()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record() with
        {
            UserId = new string('u', 300),
            TenantId = new string('t', 200),
            IpAddress = "bad",
            UserAgent = new string('a', 600)
        });

        Markers(entry, ABACDecisionAuditSchema.MetadataHashedFields).ShouldBe("UserId,TenantId");
        Markers(entry, ABACDecisionAuditSchema.MetadataDroppedFields).ShouldBe("IpAddress");
        Markers(entry, ABACDecisionAuditSchema.MetadataTruncatedFields).ShouldBe("UserAgent");
    }

    // ── Read-back ────────────────────────────────────────────────────

    [Fact]
    public void ToDecisionAuditRecord_RoundTripsEveryStoredField()
    {
        var trace = new PolicyEvaluationTrace
        {
            PolicyId = "orders-policy",
            IsPolicySet = true,
            Effect = Effect.Permit,
            Reason = PolicyTraceReason.Evaluated,
            DecisiveRuleIds = ["rule-1"],
            Children = [new PolicyEvaluationTrace { PolicyId = "child", IsPolicySet = false, Effect = Effect.NotApplicable, Reason = PolicyTraceReason.TargetNotMatched }],
            Version = "v2"
        };
        var record = Record() with
        {
            EvaluatedPolicies = [trace],
            TraceTruncated = true,
            ObligationIds = ["o1"],
            AdviceIds = ["a1", "a2"],
            AttributeNames = new Dictionary<AttributeCategory, IReadOnlyList<string>>
            {
                [AttributeCategory.Subject] = ["department"],
                [AttributeCategory.Environment] = ["time"]
            },
            RecordedValues = new Dictionary<string, string> { ["department"] = "HR" },
            UserAgent = new string('a', 600)
        };

        var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record));

        read.Schema.ShouldBe(ABACDecisionAuditSchema.SchemaVersion);
        read.DecisionId.ShouldBe(record.DecisionId);
        read.CorrelationId.ShouldBe("corr-1");
        read.UserId.ShouldBe("user-1");
        read.IdentityKind.ShouldBe(IdentityKind.User);
        read.TenantId.ShouldBe("tenant-1");
        read.ModuleId.ShouldBe("orders");
        read.RequestType.ShouldBe("GetOrderQuery");
        read.ResourceId.ShouldBe("order-9");
        read.Outcome.ShouldBe(AuditOutcome.Success);
        read.ReasonCode.ShouldBe(ABACDecisionAuditSchema.PermitReasonCode);
        read.Enforced.ShouldBeTrue();
        read.EnforcementMode.ShouldBe(ABACEnforcementMode.Block);
        read.Effect.ShouldBe(Effect.Permit);
        read.PolicyId.ShouldBe("orders-policy");
        read.RuleId.ShouldBe("rule-1");
        read.EvaluatedPolicies.ShouldHaveSingleItem().ShouldBe(trace with { DecisiveRuleIds = read.EvaluatedPolicies[0].DecisiveRuleIds, Children = read.EvaluatedPolicies[0].Children });
        read.EvaluatedPolicies[0].DecisiveRuleIds.ShouldBe(["rule-1"]);
        read.EvaluatedPolicies[0].Children.ShouldHaveSingleItem().Reason.ShouldBe(PolicyTraceReason.TargetNotMatched);
        read.TraceTruncated.ShouldBeTrue();
        read.ObligationIds.ShouldBe(["o1"]);
        read.AdviceIds.ShouldBe(["a1", "a2"]);
        read.AttributeNames["Subject"].ShouldBe(["department"]);
        read.AttributeNames["Environment"].ShouldBe(["time"]);
        read.RecordedValues["department"].ShouldBe("HR");
        read.IpAddress.ShouldBe("203.0.113.7");
        read.UserAgent!.Length.ShouldBe(ABACDecisionAuditSchema.UserAgentMaxLength);
        read.TruncatedFields.ShouldBe(["UserAgent"]);
        read.HashedFields.ShouldBeEmpty();
        read.DroppedFields.ShouldBeEmpty();
        read.StartedAtUtc.ShouldBe(Started);
        read.CompletedAtUtc.ShouldBe(Completed);
    }

    [Fact]
    public void ToDecisionAuditRecord_ReadsJsonElementMetadataAsStoresReturnIt()
    {
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(
            Record(ABACEnforcedOutcome.DeniedNotEnforced, ABACErrors.AccessDeniedCode));
        var roundTripped = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(entry.Metadata))!;
        roundTripped["abac.extra_number"] = JsonSerializer.Deserialize<JsonElement>("42");
        roundTripped["abac.extra_null"] = null;

        var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(entry with { Metadata = roundTripped });

        read.Enforced.ShouldBeFalse();
        read.IdentityKind.ShouldBe(IdentityKind.User);
        read.PolicyId.ShouldBe("orders-policy");
    }

    [Fact]
    public void ToDecisionAuditRecord_EntryWithoutAbacMetadata_ReadsDefaults()
    {
        var entry = new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "c",
            Action = ABACDecisionAuditSchema.Action,
            EntityType = "T",
            Outcome = AuditOutcome.Denied,
            TimestampUtc = Completed.UtcDateTime,
            StartedAtUtc = Started,
            CompletedAtUtc = Completed,
            Metadata = new Dictionary<string, object?> { [ABACDecisionAuditSchema.MetadataEnforcementMode] = "NotAMode" }
        };

        var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(entry);

        read.Enforced.ShouldBeTrue();
        read.EnforcementMode.ShouldBeNull();
        read.IdentityKind.ShouldBeNull();
        read.EvaluatedPolicies.ShouldBeEmpty();
        read.AttributeNames.ShouldBeEmpty();
        read.RecordedValues.ShouldBeEmpty();
        read.ObligationIds.ShouldBeEmpty();
    }

    [Fact]
    public void ToDecisionAuditRecord_Null_Throws() =>
        Should.Throw<ArgumentNullException>(() => ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(null!));

    [Fact]
    public void NormalizeIdentifier_HashesOnlyOverTheLimit()
    {
        ABACDecisionAuditEntryMapper.NormalizeIdentifier(null, 5).ShouldBeNull();
        ABACDecisionAuditEntryMapper.NormalizeIdentifier("12345", 5).ShouldBe("12345");
        ABACDecisionAuditEntryMapper.NormalizeIdentifier("123456", 5).ShouldBe(ABACDecisionAuditEntryMapper.Hash("123456"));
    }

    [Fact]
    public void DecisionAuditRecord_ToString_HidesPersonalData()
    {
        var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record()));

        var text = read.ToString();

        text.ShouldContain(read.DecisionId.ToString());
        text.ShouldNotContain("user-1");
        text.ShouldNotContain("tenant-1");
        text.ShouldNotContain("203.0.113.7");
    }

    [Fact]
    public void DecisionAuditQuery_ToString_PrintsOnlyThePaging()
    {
        var text = new ABACDecisionAuditQuery { UserId = "user-1", TenantId = "tenant-1", PageNumber = 2, PageSize = 10 }.ToString();

        text.ShouldContain("PageNumber = 2");
        text.ShouldNotContain("user-1");
        text.ShouldNotContain("tenant-1");
    }
}
