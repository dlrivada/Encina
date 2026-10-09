#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Encina.PropertyTests.Security.ABAC;

/// <summary>
/// Invariants of <see cref="ABACDecisionAuditEntryMapper"/> (#751 Phase 3): whatever the lengths of
/// the bounded values, no mapped column exceeds its limit, a hashed value is exactly
/// <c>sha256:</c> plus 64 hex characters, the markers list exactly the modified fields, the mapper
/// never throws, and a stored entry reads back with every bounded value it kept.
/// </summary>
public sealed class ABACDecisionAuditEntryMapperProperties
{
    private static readonly char[] Hex = "0123456789abcdef".ToCharArray();
    private static readonly string[] UserAgentOnly = ["UserAgent"];
    private static readonly string[] IpAddressOnly = ["IpAddress"];

    // Each address with whether it is canonical (kept) or not (dropped).
    private static readonly (string Address, bool Valid)[] IpCases =
    [
        ("10.0.0.1", true), ("::1", true), ("2001:db8::8a2e:370:7334", true), ("::ffff:192.0.2.1", true),
        ("", false), ("999.1.1.1", false), ("host.example", false), (new string('1', 46), false),
        ("fe80::1%eth0", false), ("123", false), ("10.1", false), ("2001:0db8::1", false)
    ];

    private static string Text(int length, char fill) => new(fill, length);

    private static ABACDecisionRecord Record(int user, int type, int resource, int correlation, int tenant, int agent) => new()
    {
        DecisionId = Guid.CreateVersion7(),
        UserId = Text(user, 'u'),
        IdentityKind = IdentityKind.User,
        TenantId = Text(tenant, 't'),
        CorrelationId = Text(Math.Max(correlation, 1), 'c'),
        RequestType = Text(Math.Max(type, 1), 'q'),
        ResourceId = Text(resource, 'r'),
        UserAgent = Text(agent, 'a'),
        IpAddress = "198.51.100.1",
        EnforcedOutcome = ABACEnforcedOutcome.Granted,
        ReasonCode = ABACDecisionAuditSchema.PermitReasonCode,
        EnforcementMode = ABACEnforcementMode.Block,
        StartedAtUtc = DateTimeOffset.UnixEpoch,
        CompletedAtUtc = DateTimeOffset.UnixEpoch
    };

    private static Arbitrary<int> Length() => Gen.Choose(0, 700).ToArbitrary();

    private static bool IsHash(string? value) =>
        value is { Length: ABACDecisionAuditSchema.HashedValueLength }
        && value.StartsWith(ABACDecisionAuditSchema.HashPrefix, StringComparison.Ordinal)
        && value[ABACDecisionAuditSchema.HashPrefix.Length..].All(c => Hex.Contains(c));

    private static string[] Marker(OperationAuditEntry entry, string key) =>
        entry.Metadata.TryGetValue(key, out var value) && value is string text ? text.Split(',') : [];

    [Property(MaxTest = 200)]
    public Property EveryBoundedColumn_FitsItsLimit_AndTheMarkersListExactlyTheModifiedFields() =>
        Prop.ForAll(Gen.Choose(0, 700).ArrayOf(6).ToArbitrary(), lengths =>
        {
            var (user, type, resource, correlation, tenant, agent) = (lengths[0], lengths[1], lengths[2], lengths[3], lengths[4], lengths[5]);
            var record = Record(user, type, resource, correlation, tenant, agent);

            var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record);

            var expectedHashed = new List<string>();
            if (record.CorrelationId.Length > ABACDecisionAuditSchema.CorrelationIdMaxLength) expectedHashed.Add("CorrelationId");
            if (user > ABACDecisionAuditSchema.UserIdMaxLength) expectedHashed.Add("UserId");
            if (tenant > ABACDecisionAuditSchema.TenantIdMaxLength) expectedHashed.Add("TenantId");
            if (record.RequestType.Length > ABACDecisionAuditSchema.EntityTypeMaxLength) expectedHashed.Add("EntityType");
            if (resource > ABACDecisionAuditSchema.EntityIdMaxLength) expectedHashed.Add("EntityId");

            var withinLimits =
                entry.UserId!.Length <= ABACDecisionAuditSchema.UserIdMaxLength
                && entry.EntityType.Length <= ABACDecisionAuditSchema.EntityTypeMaxLength
                && entry.EntityId!.Length <= ABACDecisionAuditSchema.EntityIdMaxLength
                && entry.CorrelationId.Length <= ABACDecisionAuditSchema.CorrelationIdMaxLength
                && entry.TenantId!.Length <= ABACDecisionAuditSchema.TenantIdMaxLength
                && entry.UserAgent!.Length <= ABACDecisionAuditSchema.UserAgentMaxLength
                && entry.Action.Length <= ABACDecisionAuditSchema.ActionMaxLength
                && entry.ErrorMessage!.Length <= ABACDecisionAuditSchema.ErrorMessageMaxLength;

            var hashesWellFormed =
                (user <= ABACDecisionAuditSchema.UserIdMaxLength || IsHash(entry.UserId))
                && (tenant <= ABACDecisionAuditSchema.TenantIdMaxLength || IsHash(entry.TenantId))
                && (resource <= ABACDecisionAuditSchema.EntityIdMaxLength || IsHash(entry.EntityId));

            var truncatedExpected = agent > ABACDecisionAuditSchema.UserAgentMaxLength ? UserAgentOnly : [];

            return withinLimits
                && hashesWellFormed
                && Marker(entry, ABACDecisionAuditSchema.MetadataHashedFields).SequenceEqual(expectedHashed)
                && Marker(entry, ABACDecisionAuditSchema.MetadataTruncatedFields).SequenceEqual(truncatedExpected)
                && Marker(entry, ABACDecisionAuditSchema.MetadataDroppedFields).Length == 0;
        });

    [Property(MaxTest = 200)]
    public Property IpAddress_IsKeptOnlyWhenItFitsAndParses() =>
        Prop.ForAll(Gen.Elements(IpCases).ToArbitrary(), pair =>
        {
            var (address, valid) = pair;
            var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record(1, 1, 1, 1, 1, 1) with { IpAddress = address });

            var dropped = Marker(entry, ABACDecisionAuditSchema.MetadataDroppedFields);

            return valid
                ? entry.IpAddress == address && dropped.Length == 0
                : entry.IpAddress is null && dropped.SequenceEqual(IpAddressOnly);
        });

    [Property(MaxTest = 50)]
    public Property AStoreErrorMessage_NeverLeavesTheRecorder() =>
        Prop.ForAll(Gen.Elements("Secret patient Alice", "connection string=pwd", "Object reference not set").ToArbitrary(), message =>
        {
            var store = NSubstitute.Substitute.For<IOperationAuditStore>();
            store.RecordAsync(NSubstitute.Arg.Any<OperationAuditEntry>(), NSubstitute.Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(LanguageExt.Prelude.Left<EncinaError, LanguageExt.Unit>(
                    EncinaErrors.Create("store.down", message, new InvalidOperationException(message)))));
            store.GetByCorrelationIdAsync(NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([])));
            var scopes = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
                .AddScoped(_ => store)
                .BuildServiceProvider()
                .GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
            var recorder = new AuditStoreABACDecisionRecorder(
                scopes,
                Microsoft.Extensions.Options.Options.Create(new ABACOptions()),
                TimeProvider.System,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditStoreABACDecisionRecorder>.Instance);

            var result = recorder.RecordAsync(Record(5, 5, 5, 5, 5, 5)).AsTask().GetAwaiter().GetResult();

            return result.Match(
                Right: _ => false,
                Left: error => !error.Message.Contains(message, StringComparison.Ordinal)
                    && !error.Exception.Map(exception => exception.ToString()).IfNone(string.Empty).Contains(message, StringComparison.Ordinal));
        });

    [Property(MaxTest = 100)]
    public Property EntryToRecord_KeepsEveryValueTheEntryStores() =>
        Prop.ForAll(Length(), Length(), Length(), (user, resource, agent) =>
        {
            var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(Record(user, 3, resource, 3, 3, agent));

            var read = ABACDecisionAuditEntryMapper.ToDecisionAuditRecord(entry);

            return read.DecisionId == entry.Id
                && read.UserId == entry.UserId
                && read.ResourceId == entry.EntityId
                && read.RequestType == entry.EntityType
                && read.TenantId == entry.TenantId
                && read.UserAgent == entry.UserAgent
                && read.ReasonCode == entry.ErrorMessage
                && read.Outcome == entry.Outcome
                && read.HashedFields.SequenceEqual(Marker(entry, ABACDecisionAuditSchema.MetadataHashedFields))
                && read.TruncatedFields.SequenceEqual(Marker(entry, ABACDecisionAuditSchema.MetadataTruncatedFields));
        });

    [Property(MaxTest = 100)]
    public Property OutcomeMapping_IsTotal() =>
        Prop.ForAll(
            Gen.Elements(ABACEnforcedOutcome.Granted, ABACEnforcedOutcome.Denied, ABACEnforcedOutcome.DeniedNotEnforced).ToArbitrary(),
            Gen.Elements(ABACDecisionAuditSchema.PermitReasonCode, ABACErrors.AccessDeniedCode, ABACErrors.IndeterminateCode,
                ABACErrors.EvaluationFailedCode, ABACErrors.ObligationFailedCode, ABACErrors.ConditionNotMetCode,
                ABACErrors.RequiredPolicyNotFoundCode, EncinaErrorCodes.AuthorizationUnauthenticated).ToArbitrary(),
            (enforced, reason) =>
            {
                var outcome = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(
                    Record(1, 1, 1, 1, 1, 1) with { EnforcedOutcome = enforced, ReasonCode = reason }).Outcome;

                return enforced == ABACEnforcedOutcome.Denied
                    ? outcome is AuditOutcome.Denied or AuditOutcome.Error
                    : outcome == AuditOutcome.Success;
            });
}
