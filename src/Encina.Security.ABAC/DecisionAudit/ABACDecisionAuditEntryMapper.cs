using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Encina.Security.Audit;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Maps an <see cref="ABACDecisionRecord"/> to the <see cref="OperationAuditEntry"/> the operation
/// audit store persists, and a stored entry back to an <see cref="ABACDecisionAuditRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Entry shape</b>: <c>Action</c> is <see cref="ABACDecisionAuditSchema.Action"/>, <c>EntityType</c>
/// the request type name, <c>EntityId</c> the declared resource id, <c>UserId</c> the caller,
/// <c>ErrorMessage</c> the reason code only, and every metadata value a string under an
/// <c>abac.*</c> key. <c>StartedAtUtc</c> is the decision start; <c>CompletedAtUtc</c> and
/// <c>TimestampUtc</c> are the single clock read at the end.
/// </para>
/// <para>
/// <b>Never stored</b>: <see cref="DecisionStatus.StatusMessage"/>, any <see cref="EncinaError.Message"/>,
/// exception messages and request or response payloads.
/// </para>
/// <para>
/// <b>Column limits</b>: a value never fails the insert. An identifier over its column limit
/// (<c>UserId</c>, <c>EntityType</c>, <c>EntityId</c>, <c>CorrelationId</c> 256; <c>TenantId</c> 128)
/// is replaced by <c>sha256:&lt;64 hex&gt;</c> and named in <see cref="ABACDecisionAuditSchema.MetadataHashedFields"/>;
/// a client address over 45 characters or that does not parse is stored as <c>null</c> and named in
/// <see cref="ABACDecisionAuditSchema.MetadataDroppedFields"/>; a user agent over 512 characters, the
/// attribute-name list over 128 names and a recorded value over 256 characters are truncated and named
/// in <see cref="ABACDecisionAuditSchema.MetadataTruncatedFields"/>. The decision audit reader applies
/// the same rule to its filters, so a hashed value is still found.
/// </para>
/// </remarks>
public static class ABACDecisionAuditEntryMapper
{
    private const string ListSeparator = ",";

    /// <summary>
    /// Builds the operation audit entry of a decision record, applying the column-limit rule.
    /// </summary>
    /// <param name="record">The decision record.</param>
    /// <returns>The entry to persist; its id is the decision id, so a retried write is idempotent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="record"/> is <c>null</c>.</exception>
    public static OperationAuditEntry ToOperationAuditEntry(ABACDecisionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var bounds = new EntryBounds();
        var metadata = BuildMetadata(record, bounds);
        var entry = new OperationAuditEntry
        {
            Id = record.DecisionId,
            CorrelationId = bounds.Identifier(nameof(OperationAuditEntry.CorrelationId), record.CorrelationId, ABACDecisionAuditSchema.CorrelationIdMaxLength)!,
            UserId = bounds.Identifier(nameof(OperationAuditEntry.UserId), record.UserId, ABACDecisionAuditSchema.UserIdMaxLength),
            TenantId = bounds.Identifier(nameof(OperationAuditEntry.TenantId), record.TenantId, ABACDecisionAuditSchema.TenantIdMaxLength),
            Action = ABACDecisionAuditSchema.Action,
            EntityType = bounds.Identifier(nameof(OperationAuditEntry.EntityType), record.RequestType, ABACDecisionAuditSchema.EntityTypeMaxLength)!,
            EntityId = bounds.Identifier(nameof(OperationAuditEntry.EntityId), record.ResourceId, ABACDecisionAuditSchema.EntityIdMaxLength),
            Outcome = OutcomeOf(record),
            ErrorMessage = bounds.Identifier(nameof(OperationAuditEntry.ErrorMessage), record.ReasonCode, ABACDecisionAuditSchema.ErrorMessageMaxLength),
            TimestampUtc = record.CompletedAtUtc.UtcDateTime,
            StartedAtUtc = record.StartedAtUtc,
            CompletedAtUtc = record.CompletedAtUtc,
            IpAddress = bounds.IpAddress(record.IpAddress),
            UserAgent = bounds.Truncated(nameof(OperationAuditEntry.UserAgent), record.UserAgent, ABACDecisionAuditSchema.UserAgentMaxLength),
            Metadata = metadata
        };

        // The markers go in last: every bounded field above has been through the rule.
        bounds.WriteMarkers(metadata);
        return entry;
    }

    /// <summary>
    /// The stored audit outcome of a decision: <see cref="AuditOutcome.Success"/> when ABAC let the
    /// request through (a Warn-mode would-deny included), <see cref="AuditOutcome.Error"/> for an
    /// indeterminate decision or an evaluation failure, <see cref="AuditOutcome.Denied"/> for every
    /// other denial.
    /// </summary>
    // crap-exempt: single-question switch — the audit outcome of one enforced outcome and reason code.
    internal static AuditOutcome OutcomeOf(ABACDecisionRecord record) => record.EnforcedOutcome switch
    {
        ABACEnforcedOutcome.Denied when IsError(record.ReasonCode) => AuditOutcome.Error,
        ABACEnforcedOutcome.Denied => AuditOutcome.Denied,
        _ => AuditOutcome.Success
    };

    private static bool IsError(string reasonCode) =>
        reasonCode is ABACErrors.IndeterminateCode or ABACErrors.EvaluationFailedCode;

    /// <summary>
    /// Applies the identifier rule of a bounded column: the value itself when it fits, otherwise
    /// <c>sha256:&lt;64 hex&gt;</c>. The reader normalizes its filters with it.
    /// </summary>
    internal static string? NormalizeIdentifier(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : Hash(value);

    /// <summary>The <c>sha256:&lt;64 hex&gt;</c> form of a value (SHA-256 of its UTF-8 bytes, lowercase hex).</summary>
    internal static string Hash(string value) =>
        ABACDecisionAuditSchema.HashPrefix + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>
    /// Reads a stored decision audit entry back as a typed record. Metadata values are read as text
    /// whatever the store returns them as (a string or a JSON string element).
    /// </summary>
    internal static ABACDecisionAuditRecord ToDecisionAuditRecord(OperationAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var metadata = new StoredMetadata(entry.Metadata);
        return new ABACDecisionAuditRecord
        {
            DecisionId = entry.Id,
            CorrelationId = entry.CorrelationId,
            UserId = entry.UserId,
            IdentityKind = metadata.Enum<IdentityKind>(ABACDecisionAuditSchema.MetadataIdentityKind),
            TenantId = entry.TenantId,
            ModuleId = metadata.Text(ABACDecisionAuditSchema.MetadataModuleId),
            RequestType = entry.EntityType,
            ResourceId = entry.EntityId,
            Outcome = entry.Outcome,
            ReasonCode = entry.ErrorMessage,
            Enforced = metadata.Text(ABACDecisionAuditSchema.MetadataEnforced) != "false",
            EnforcementMode = metadata.Enum<ABACEnforcementMode>(ABACDecisionAuditSchema.MetadataEnforcementMode),
            Effect = metadata.Enum<Effect>(ABACDecisionAuditSchema.MetadataEffect),
            PolicyId = metadata.Text(ABACDecisionAuditSchema.MetadataPolicyId),
            RuleId = metadata.Text(ABACDecisionAuditSchema.MetadataRuleId),
            EvaluatedPolicies = metadata.Trace(),
            TraceTruncated = metadata.Text(ABACDecisionAuditSchema.MetadataTraceTruncated) == "true",
            ObligationIds = metadata.List(ABACDecisionAuditSchema.MetadataObligations),
            AdviceIds = metadata.List(ABACDecisionAuditSchema.MetadataAdvice),
            AttributeNames = metadata.AttributeNames(),
            RecordedValues = metadata.RecordedValues(),
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            HashedFields = metadata.List(ABACDecisionAuditSchema.MetadataHashedFields),
            DroppedFields = metadata.List(ABACDecisionAuditSchema.MetadataDroppedFields),
            TruncatedFields = metadata.List(ABACDecisionAuditSchema.MetadataTruncatedFields),
            StartedAtUtc = entry.StartedAtUtc,
            CompletedAtUtc = entry.CompletedAtUtc
        };
    }

    // ── Metadata ─────────────────────────────────────────────────────

    private static Dictionary<string, object?> BuildMetadata(ABACDecisionRecord record, EntryBounds bounds)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [ABACDecisionAuditSchema.MetadataSchema] = ABACDecisionAuditSchema.SchemaVersion,
            [ABACDecisionAuditSchema.MetadataStage] = ABACDecisionAuditSchema.StagePep,
            [ABACDecisionAuditSchema.MetadataEnforced] = record.EnforcedOutcome == ABACEnforcedOutcome.DeniedNotEnforced ? "false" : "true",
            [ABACDecisionAuditSchema.MetadataEnforcementMode] = record.EnforcementMode.ToString(),
            [ABACDecisionAuditSchema.MetadataIdentityKind] = record.IdentityKind.ToString(),
            [ABACDecisionAuditSchema.MetadataStartedAtUtc] = record.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            [ABACDecisionAuditSchema.MetadataCompletedAtUtc] = record.CompletedAtUtc.ToString("O", CultureInfo.InvariantCulture)
        };

        AddDecision(metadata, record, bounds);
        AddTrace(metadata, record);
        AddAttributes(metadata, record, bounds);
        return metadata;
    }

    private static void AddDecision(Dictionary<string, object?> metadata, ABACDecisionRecord record, EntryBounds bounds)
    {
        AddIfPresent(metadata, ABACDecisionAuditSchema.MetadataEffect, record.Effect?.ToString());
        AddIfPresent(metadata, ABACDecisionAuditSchema.MetadataPolicyId,
            bounds.Identifier(ABACDecisionAuditSchema.MetadataPolicyId, record.PolicyId, ABACDecisionAuditSchema.MaxAttributeValueLength));
        AddIfPresent(metadata, ABACDecisionAuditSchema.MetadataRuleId,
            bounds.Identifier(ABACDecisionAuditSchema.MetadataRuleId, record.RuleId, ABACDecisionAuditSchema.MaxAttributeValueLength));
        AddIfPresent(metadata, ABACDecisionAuditSchema.MetadataModuleId, record.ModuleId);
        AddList(metadata, ABACDecisionAuditSchema.MetadataObligations, record.ObligationIds);
        AddList(metadata, ABACDecisionAuditSchema.MetadataAdvice, record.AdviceIds);
    }

    private static void AddTrace(Dictionary<string, object?> metadata, ABACDecisionRecord record)
    {
        if (record.EvaluatedPolicies.Count > 0)
        {
            metadata[ABACDecisionAuditSchema.MetadataTrace] = JsonSerializer.Serialize(
                record.EvaluatedPolicies.ToList(), ABACDecisionAuditJsonContext.Default.ListPolicyEvaluationTrace);
        }

        if (record.TraceTruncated)
        {
            metadata[ABACDecisionAuditSchema.MetadataTraceTruncated] = "true";
        }
    }

    private static void AddAttributes(Dictionary<string, object?> metadata, ABACDecisionRecord record, EntryBounds bounds)
    {
        var names = bounds.CappedNames(record.AttributeNames);
        if (names.Count > 0)
        {
            metadata[ABACDecisionAuditSchema.MetadataAttributeNames] =
                JsonSerializer.Serialize(names, ABACDecisionAuditJsonContext.Default.DictionaryStringListString);
        }

        foreach (var (name, value) in record.RecordedValues)
        {
            var key = ABACDecisionAuditSchema.MetadataAttributeValuePrefix + name;
            metadata[key] = bounds.Truncated(key, value, ABACDecisionAuditSchema.MaxAttributeValueLength);
        }
    }

    private static void AddIfPresent(Dictionary<string, object?> metadata, string key, string? value)
    {
        if (value is not null)
        {
            metadata[key] = value;
        }
    }

    private static void AddList(Dictionary<string, object?> metadata, string key, IReadOnlyCollection<string> values)
    {
        if (values.Count > 0)
        {
            metadata[key] = string.Join(ListSeparator, values);
        }
    }

    // ── Column-limit rule ────────────────────────────────────────────

    /// <summary>Applies the per-column rule and remembers which fields it changed.</summary>
    private sealed class EntryBounds
    {
        private readonly List<string> _hashed = [];
        private readonly List<string> _dropped = [];
        private readonly List<string> _truncated = [];

        public string? Identifier(string field, string? value, int maxLength)
        {
            var bounded = NormalizeIdentifier(value, maxLength);
            if (!ReferenceEquals(bounded, value))
            {
                _hashed.Add(field);
            }

            return bounded;
        }

        public string? Truncated(string field, string? value, int maxLength)
        {
            if (value is null || value.Length <= maxLength)
            {
                return value;
            }

            _truncated.Add(field);
            return value[..maxLength];
        }

        // A hash cannot fit 45 characters and a truncated address is a different address, so an
        // over-length or unparseable address is dropped.
        public string? IpAddress(string? value)
        {
            if (value is null || (value.Length <= ABACDecisionAuditSchema.IpAddressMaxLength && System.Net.IPAddress.TryParse(value, out _)))
            {
                return value;
            }

            _dropped.Add(nameof(OperationAuditEntry.IpAddress));
            return null;
        }

        public Dictionary<string, List<string>> CappedNames(
            IReadOnlyDictionary<AttributeCategory, IReadOnlyList<string>> names)
        {
            var capped = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var remaining = ABACDecisionAuditSchema.MaxAttributeNames;

            foreach (var (category, list) in names.OrderBy(pair => pair.Key))
            {
                var kept = list.Take(remaining).ToList();
                remaining -= kept.Count;
                if (kept.Count > 0)
                {
                    capped[category.ToString()] = kept;
                }
            }

            if (names.Values.Sum(list => list.Count) > ABACDecisionAuditSchema.MaxAttributeNames)
            {
                _truncated.Add(ABACDecisionAuditSchema.MetadataAttributeNames);
            }

            return capped;
        }

        public void WriteMarkers(Dictionary<string, object?> metadata)
        {
            AddList(metadata, ABACDecisionAuditSchema.MetadataHashedFields, _hashed);
            AddList(metadata, ABACDecisionAuditSchema.MetadataDroppedFields, _dropped);
            AddList(metadata, ABACDecisionAuditSchema.MetadataTruncatedFields, _truncated);
        }
    }

    // ── Reading stored metadata ──────────────────────────────────────

    /// <summary>Reads the string metadata of a stored entry, whatever object the store returns.</summary>
    private readonly struct StoredMetadata(IReadOnlyDictionary<string, object?> values)
    {
        public string? Text(string key) =>
            values.TryGetValue(key, out var value) ? AsText(value) : null;

        public T? Enum<T>(string key)
            where T : struct, System.Enum =>
            System.Enum.TryParse<T>(Text(key), ignoreCase: false, out var parsed) ? parsed : null;

        public string[] List(string key) =>
            Text(key) is { Length: > 0 } text ? text.Split(ListSeparator) : [];

        public List<PolicyEvaluationTrace> Trace() =>
            Text(ABACDecisionAuditSchema.MetadataTrace) is { Length: > 0 } json
                ? JsonSerializer.Deserialize(json, ABACDecisionAuditJsonContext.Default.ListPolicyEvaluationTrace) ?? []
                : [];

        public Dictionary<string, IReadOnlyList<string>> AttributeNames()
        {
            var parsed = Text(ABACDecisionAuditSchema.MetadataAttributeNames) is { Length: > 0 } json
                ? JsonSerializer.Deserialize(json, ABACDecisionAuditJsonContext.Default.DictionaryStringListString)
                : null;

            return (parsed ?? []).ToDictionary(
                pair => pair.Key, IReadOnlyList<string> (pair) => pair.Value, StringComparer.Ordinal);
        }

        public Dictionary<string, string> RecordedValues()
        {
            var prefix = ABACDecisionAuditSchema.MetadataAttributeValuePrefix;
            return values
                .Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal) && AsText(pair.Value) is not null)
                .ToDictionary(pair => pair.Key[prefix.Length..], pair => AsText(pair.Value)!, StringComparer.Ordinal);
        }

        // crap-exempt: single-question switch — the text of one stored metadata value.
        private static string? AsText(object? value) => value switch
        {
            null => null,
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement { ValueKind: JsonValueKind.Null } => null,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }
}
