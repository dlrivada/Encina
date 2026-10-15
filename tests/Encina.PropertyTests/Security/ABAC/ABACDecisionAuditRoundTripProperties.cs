using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Security.ABAC;

/// <summary>
/// Round-trip invariants of the decision audit entry (#751 Phase 7): a record whose lists and
/// metadata fit their caps reads back from the stored entry with nothing lost (trace, decisive ids,
/// obligation and advice ids, attribute names and recorded values), a record that exceeds a cap keeps
/// the first items in order and names the list in the truncated marker, and the printed form of the
/// record and of a query never contains personal data.
/// </summary>
public sealed class ABACDecisionAuditRoundTripProperties
{
    private static readonly AttributeCategory[] Categories =
        [AttributeCategory.Subject, AttributeCategory.Resource, AttributeCategory.Environment, AttributeCategory.Action];

    private static ABACDecisionRecord Record(
        int obligations, int advice, int namesPerCategory, int recordedValues, int traceNodes, bool truncatedTrace) => new()
    {
        DecisionId = Guid.CreateVersion7(),
        UserId = "user-1",
        IdentityKind = IdentityKind.User,
        CorrelationId = "corr-1",
        ModuleId = "module-1",
        RequestType = "GetOrderQuery",
        ResourceId = "order-1",
        EnforcedOutcome = ABACEnforcedOutcome.Denied,
        ReasonCode = ABACErrors.AccessDeniedCode,
        EnforcementMode = ABACEnforcementMode.Block,
        Effect = Effect.Deny,
        PolicyId = "policy-a",
        RuleId = "rule-1",
        EvaluatedPolicies = Enumerable.Range(0, traceNodes).Select(Node).ToList(),
        TraceTruncated = truncatedTrace,
        ObligationIds = Enumerable.Range(0, obligations).Select(i => $"obligation-{i}").ToList(),
        AdviceIds = Enumerable.Range(0, advice).Select(i => $"advice-{i}").ToList(),
        AttributeNames = Categories.ToDictionary(
            category => category,
            category => (IReadOnlyList<string>)Enumerable.Range(0, namesPerCategory).Select(i => $"{category}-attr-{i}").ToList()),
        RecordedValues = Enumerable.Range(0, recordedValues).ToDictionary(i => $"attr{i}", i => $"value-{i}"),
        StartedAtUtc = DateTimeOffset.UnixEpoch,
        CompletedAtUtc = DateTimeOffset.UnixEpoch.AddMilliseconds(3)
    };

    private static PolicyEvaluationTrace Node(int index) => new()
    {
        PolicyId = $"policy-{index}",
        IsPolicySet = index % 2 == 0,
        Effect = index % 3 == 0 ? Effect.Permit : Effect.Deny,
        Reason = PolicyTraceReason.Evaluated,
        DecisiveRuleIds = [$"rule-{index}"],
        Children = index % 2 == 0 ? [new PolicyEvaluationTrace { PolicyId = $"child-{index}", IsPolicySet = false, Effect = Effect.Deny, Reason = PolicyTraceReason.Evaluated }] : [],
        Version = "1.0"
    };

    private static Arbitrary<int> Count(int max) => Gen.Choose(0, max).ToArbitrary();

    [Property(MaxTest = 150)]
    public Property ARecordWithinTheCaps_ReadsBackWithNothingLost() =>
        Prop.ForAll(Gen.Choose(0, ABACDecisionAuditSchema.MaxListedIds).ArrayOf(2).ToArbitrary(), Gen.Choose(0, 30).ArrayOf(3).ToArbitrary(),
            (listCounts, otherCounts) =>
            {
                var (obligations, advice) = (listCounts[0], listCounts[1]);
                var (names, values, nodes) = (otherCounts[0], otherCounts[1], otherCounts[2]);
                var record = Record(obligations, advice, names, values, nodes, truncatedTrace: nodes % 2 == 1);

                var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record));

                return read.ObligationIds.SequenceEqual(record.ObligationIds)
                    && read.AdviceIds.SequenceEqual(record.AdviceIds)
                    && read.TraceTruncated == record.TraceTruncated
                    && read.EvaluatedPolicies.Select(node => (node.PolicyId, node.Effect, node.Children.Count))
                        .SequenceEqual(record.EvaluatedPolicies.Select(node => (node.PolicyId, node.Effect, node.Children.Count)))
                    && read.AttributeNames.Count == (names == 0 ? 0 : Categories.Length)
                    && read.AttributeNames.Values.All(list => list.Count == names)
                    && read.RecordedValues.OrderBy(pair => pair.Key).SequenceEqual(record.RecordedValues.OrderBy(pair => pair.Key))
                    && read.PolicyId == record.PolicyId
                    && read.RuleId == record.RuleId
                    && read.ModuleId == record.ModuleId
                    && read.Effect == record.Effect
                    && read.HashedFields.Count == 0
                    && read.TruncatedFields.Count == 0;
            });

    [Property(MaxTest = 100)]
    public Property AListOverItsCap_KeepsTheFirstItemsInOrderAndIsMarkedTruncated() =>
        Prop.ForAll(Count(200), Count(200), Count(80), (obligations, advice, names) =>
        {
            var record = Record(obligations, advice, names, 0, 0, truncatedTrace: false);

            var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record);
            var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(entry);

            var cap = ABACDecisionAuditSchema.MaxListedIds;
            var totalNames = names * Categories.Length;
            var expectedTruncated = new List<string>();
            if (obligations > cap) expectedTruncated.Add(ABACDecisionAuditSchema.MetadataObligations);
            if (advice > cap) expectedTruncated.Add(ABACDecisionAuditSchema.MetadataAdvice);
            if (totalNames > ABACDecisionAuditSchema.MaxAttributeNames) expectedTruncated.Add(ABACDecisionAuditSchema.MetadataAttributeNames);

            return read.ObligationIds.SequenceEqual(record.ObligationIds.Take(cap))
                && read.AdviceIds.SequenceEqual(record.AdviceIds.Take(cap))
                && read.AttributeNames.Values.Sum(list => list.Count) == Math.Min(totalNames, ABACDecisionAuditSchema.MaxAttributeNames)
                && read.TruncatedFields.Order().SequenceEqual(expectedTruncated.Order());
        });

    [Property(MaxTest = 100)]
    public Property ThePrintedRecordAndQuery_NeverContainPersonalData() =>
        Prop.ForAll(Gen.Elements("Alice", "Bob Smith", "ünï", "12345", "a,b|c").ToArbitrary(), Gen.Elements("acme", "Tenant 7", "x").ToArbitrary(), (user, tenant) =>
        {
            var userId = "USER-SENTINEL-" + user;
            var tenantId = "TENANT-SENTINEL-" + tenant;

            var record = Record(1, 1, 1, 1, 0, false) with { UserId = userId, TenantId = tenantId, IpAddress = "198.51.100.7" };
            var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record));
            var query = new ABACDecisionAuditQuery { UserId = userId, TenantId = tenantId, ResourceId = userId };

            var printed = record + "|" + read + "|" + query;

            return !printed.Contains("SENTINEL", StringComparison.Ordinal)
                && !printed.Contains("198.51.100.7", StringComparison.Ordinal);
        });
}
