using Encina.Compliance.CrossBorderTransfer.Model;

namespace Encina.Compliance.CrossBorderTransfer.ReadModels;

/// <summary>
/// Read-only projected view of an SCC agreement, built from SCC aggregate events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the <c>SCCAgreementAggregate</c> event stream
/// by Marten inline projections. It provides an efficient query view without replaying events.
/// </para>
/// <para>
/// Used by <c>ISCCService</c> query methods to return SCC agreement state to consumers.
/// </para>
/// </remarks>
public sealed record SCCAgreementReadModel
{
    /// <summary>
    /// Unique identifier for this SCC agreement.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Identifier of the data processor/importer party to the agreement.
    /// </summary>
    public required string ProcessorId { get; init; }

    /// <summary>
    /// The SCC module applicable to this transfer relationship.
    /// </summary>
    public required SCCModule Module { get; init; }

    /// <summary>
    /// Version of the SCC clauses used (e.g., "2021/914").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Timestamp when the agreement was executed (UTC).
    /// </summary>
    public required DateTimeOffset ExecutedAtUtc { get; init; }

    /// <summary>
    /// Optional expiration date of the agreement (UTC).
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// Indicates whether the agreement has been revoked.
    /// </summary>
    public required bool IsRevoked { get; init; }

    /// <summary>
    /// Timestamp when the agreement was revoked (UTC).
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; init; }

    /// <summary>
    /// Supplementary measures associated with this SCC agreement.
    /// </summary>
    public required IReadOnlyList<SupplementaryMeasure> SupplementaryMeasures { get; init; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// Determines whether this SCC agreement is currently valid.
    /// </summary>
    /// <param name="nowUtc">The current UTC time for expiration evaluation.</param>
    /// <returns><c>true</c> if the agreement is active, not revoked, and not expired; otherwise <c>false</c>.</returns>
    public bool IsValid(DateTimeOffset nowUtc)
    {
        if (IsRevoked)
        {
            return false;
        }

        if (ExpiresAtUtc.HasValue && nowUtc >= ExpiresAtUtc.Value)
        {
            return false;
        }

        return true;
    }
}
