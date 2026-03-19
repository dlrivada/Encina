// ============================================================================
// DRAFT PROJECTION DESIGN — Spike #799 output
// This file is a design artifact, NOT production code.
// Final implementation will live in src/Encina.Audit.Marten/
// ============================================================================

using Encina.Security.Audit;

namespace Encina.Audit.Marten.Projections;

/// <summary>
/// Read model for audit entries, maintained by the inline projection.
/// </summary>
/// <remarks>
/// <para>
/// This read model stores decrypted audit entry data for efficient querying.
/// When a temporal key is shredded, the <see cref="IsShredded"/> flag is set to <c>true</c>
/// and all PII fields are replaced with <c>[SHREDDED]</c>.
/// </para>
/// <para>
/// Non-PII fields (Action, EntityType, Outcome, timestamps) remain queryable
/// even after shredding, preserving operational audit trail visibility.
/// </para>
/// </remarks>
public sealed class AuditEntryReadModel
{
    // --- Identity & Technical ---
    public required Guid Id { get; set; }
    public required string CorrelationId { get; set; }

    // --- PII Fields (encrypted, shreddable) ---
    /// <summary>
    /// User identifier. Contains <c>[SHREDDED]</c> after temporal key deletion.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Client IP address. Contains <c>[SHREDDED]</c> after temporal key deletion.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Browser/client info. Contains <c>[SHREDDED]</c> after temporal key deletion.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Request payload JSON. Contains <c>[SHREDDED]</c> after temporal key deletion.
    /// </summary>
    public string? RequestPayload { get; set; }

    /// <summary>
    /// Response payload JSON. Contains <c>[SHREDDED]</c> after temporal key deletion.
    /// </summary>
    public string? ResponsePayload { get; set; }

    // --- Non-PII Fields (always readable) ---
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public required AuditOutcome Outcome { get; set; }
    public string? ErrorMessage { get; set; }
    public required DateTime TimestampUtc { get; set; }
    public required DateTimeOffset StartedAtUtc { get; set; }
    public required DateTimeOffset CompletedAtUtc { get; set; }
    public string? TenantId { get; set; }
    public string? RequestPayloadHash { get; set; }

    // --- Shredding Metadata ---
    /// <summary>
    /// Indicates whether PII fields have been crypto-shredded.
    /// </summary>
    public bool IsShredded { get; set; }

    /// <summary>
    /// The temporal key period ID used for encryption (e.g., "audit-temporal:monthly:2026-03").
    /// </summary>
    public required string TemporalPeriodId { get; set; }

    /// <summary>
    /// When the entry was shredded, if applicable.
    /// </summary>
    public DateTime? ShreddedAtUtc { get; set; }
}

// =============================================================================
// PROJECTION PSEUDOCODE — How MartenAuditStoreProjection handles events
// =============================================================================
//
// 1. ON AuditEntryRecordedEvent:
//    - Get temporal key for event.TimestampUtc via ITemporalKeyProvider
//    - Decrypt PII fields
//    - Upsert AuditEntryReadModel with decrypted values
//    - Store TemporalPeriodId for later shredding reference
//
// 2. ON TemporalKeyShreddedEvent:
//    - Query all AuditEntryReadModel where TemporalPeriodId == event.PeriodId
//    - Set IsShredded = true, ShreddedAtUtc = event.ShreddedAtUtc
//    - Replace PII fields with "[SHREDDED]"
//    - This is a batch operation on the read model, NOT on the event store
//
// 3. ON PROJECTION REBUILD:
//    - For each AuditEntryRecordedEvent:
//      - Try to get temporal key via ITemporalKeyProvider
//      - If key exists: decrypt normally
//      - If key is deleted (IsPeriodShreddedAsync = true):
//        - Set IsShredded = true
//        - Use "[SHREDDED]" for PII fields
//      - Non-PII fields always populated from event
//
// 4. QUERY BEHAVIOR:
//    - QueryAsync filters work on all non-PII fields (always available)
//    - UserId filter: works only for non-shredded entries
//    - Pagination: shredded entries included, page sizes predictable
//    - Client can filter IsShredded == false if they want only readable entries
// =============================================================================
