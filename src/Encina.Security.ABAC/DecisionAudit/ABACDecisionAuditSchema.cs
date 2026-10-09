namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// The constants of the decision audit entry shape: the action name, the schema version, the metadata
/// keys and the column limits the mapper must respect.
/// </summary>
/// <remarks>
/// <para>
/// Decision audit entries are <c>OperationAuditEntry</c> rows distinguished by <see cref="Action"/>
/// and their metadata. Every metadata value is a string, so relational JSON columns, MongoDB and
/// Marten encryption round-trip them identically.
/// </para>
/// <para>
/// The column limits mirror the bounded columns of the operation audit store (the EF Core
/// <c>OperationAuditEntryEntityConfiguration</c> and the ADO.NET, Dapper and MongoDB schemas).
/// </para>
/// </remarks>
public static class ABACDecisionAuditSchema
{
    // ── Entry shape ──────────────────────────────────────────────────

    /// <summary>The <c>Action</c> of every decision audit entry: one indexed filter for all ABAC decisions.</summary>
    public const string Action = "ABACDecision";

    /// <summary>The version of the entry shape and of the JSON Lines export (<c>encina.abac.decision/1</c>).</summary>
    public const string SchemaVersion = "encina.abac.decision/1";

    /// <summary>The reason code of a permitted request.</summary>
    public const string PermitReasonCode = "abac.permit";

    // ── Metadata keys ────────────────────────────────────────────────

    /// <summary>Metadata key: the schema version (<see cref="SchemaVersion"/>).</summary>
    public const string MetadataSchema = "abac.schema";

    /// <summary>Metadata key: the stage that made the decision (<see cref="StagePep"/>).</summary>
    public const string MetadataStage = "abac.stage";

    /// <summary>Metadata value of <see cref="MetadataStage"/>: the Policy Enforcement Point.</summary>
    public const string StagePep = "pep";

    /// <summary>Metadata key: <c>true</c> when ABAC blocked or allowed the request as decided, <c>false</c> for a would-deny verdict in Warn mode.</summary>
    public const string MetadataEnforced = "abac.enforced";

    /// <summary>Metadata key: the enforcement mode (<see cref="ABACEnforcementMode"/>).</summary>
    public const string MetadataEnforcementMode = "abac.enforcement_mode";

    /// <summary>Metadata key: the kind of the caller (<see cref="IdentityKind"/>), stored so it is queryable without parsing a list.</summary>
    public const string MetadataIdentityKind = "abac.identity_kind";

    /// <summary>Metadata key: the effect the Policy Decision Point answered.</summary>
    public const string MetadataEffect = "abac.effect";

    /// <summary>Metadata key: the deciding policy id.</summary>
    public const string MetadataPolicyId = "abac.policy_id";

    /// <summary>Metadata key: the representative decisive rule id.</summary>
    public const string MetadataRuleId = "abac.rule_id";

    /// <summary>Metadata key: the module id (there is no queryable module column).</summary>
    public const string MetadataModuleId = "abac.module_id";

    /// <summary>Metadata key: the evaluation trace as a JSON string.</summary>
    public const string MetadataTrace = "abac.trace";

    /// <summary>Metadata key: <c>true</c> when the trace is incomplete.</summary>
    public const string MetadataTraceTruncated = "abac.trace_truncated";

    /// <summary>Metadata key: the obligation ids, as a JSON array of strings.</summary>
    public const string MetadataObligations = "abac.obligations";

    /// <summary>Metadata key: the advice ids, as a JSON array of strings.</summary>
    public const string MetadataAdvice = "abac.advice";

    /// <summary>Metadata key: the attribute names by category, as a JSON string.</summary>
    public const string MetadataAttributeNames = "abac.attribute_names";

    /// <summary>Metadata key prefix of a recorded attribute value; the attribute name follows the prefix.</summary>
    public const string MetadataAttributeValuePrefix = "abac.attr.";

    /// <summary>Metadata key: when the decision started (round-trip format).</summary>
    public const string MetadataStartedAtUtc = "abac.started_at_utc";

    /// <summary>Metadata key: when the decision completed (round-trip format).</summary>
    public const string MetadataCompletedAtUtc = "abac.completed_at_utc";

    /// <summary>Metadata key: the entry fields replaced by a hash because they exceeded their column limit.</summary>
    public const string MetadataHashedFields = "abac.hashed_fields";

    /// <summary>Metadata key: the entry fields stored as <c>null</c> because they could not be bounded without misleading.</summary>
    public const string MetadataDroppedFields = "abac.dropped_fields";

    /// <summary>Metadata key: the entry fields (or lists) truncated to their limit.</summary>
    public const string MetadataTruncatedFields = "abac.truncated_fields";

    // ── Column limits (OperationAuditEntries) ────────────────────────

    /// <summary>Maximum length of the <c>UserId</c> column.</summary>
    public const int UserIdMaxLength = 256;

    /// <summary>Maximum length of the <c>EntityType</c> column.</summary>
    public const int EntityTypeMaxLength = 256;

    /// <summary>Maximum length of the <c>EntityId</c> column.</summary>
    public const int EntityIdMaxLength = 256;

    /// <summary>Maximum length of the <c>CorrelationId</c> column.</summary>
    public const int CorrelationIdMaxLength = 256;

    /// <summary>Maximum length of the <c>TenantId</c> column.</summary>
    public const int TenantIdMaxLength = 128;

    /// <summary>Maximum length of the <c>Action</c> column.</summary>
    public const int ActionMaxLength = 128;

    /// <summary>Maximum length of the <c>ErrorMessage</c> column (which holds only a reason code).</summary>
    public const int ErrorMessageMaxLength = 2048;

    /// <summary>Maximum length of the <c>IpAddress</c> column (the longest IPv6 text form).</summary>
    public const int IpAddressMaxLength = 45;

    /// <summary>Maximum length of the <c>UserAgent</c> column.</summary>
    public const int UserAgentMaxLength = 512;

    // ── Metadata bounds ──────────────────────────────────────────────

    /// <summary>The prefix of a hashed value (<c>sha256:&lt;64 hex&gt;</c>).</summary>
    public const string HashPrefix = "sha256:";

    /// <summary>The length of a hashed value: <see cref="HashPrefix"/> plus 64 hexadecimal characters; it fits every id column.</summary>
    public const int HashedValueLength = 71;

    /// <summary>The maximum number of attribute names stored in <see cref="MetadataAttributeNames"/>.</summary>
    public const int MaxAttributeNames = 128;

    /// <summary>The maximum length of a recorded attribute value.</summary>
    public const int MaxAttributeValueLength = 256;
}
