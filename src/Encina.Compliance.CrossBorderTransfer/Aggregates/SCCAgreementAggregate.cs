using Encina.Compliance.CrossBorderTransfer.Events;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.CrossBorderTransfer.Aggregates;

/// <summary>
/// Event-sourced aggregate representing the lifecycle of a Standard Contractual Clauses (SCC) agreement.
/// </summary>
/// <remarks>
/// <para>
/// An SCC agreement documents the execution of Standard Contractual Clauses between a data exporter
/// and a data importer under GDPR Art. 46(2)(c). The agreement specifies the applicable module,
/// version, supplementary measures, and validity period.
/// </para>
/// <para>
/// The lifecycle is: Registered → (optionally add supplementary measures) → Active until
/// Revoked or Expired. The <see cref="IsValid"/> method determines current validity.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// </remarks>
public sealed class SCCAgreementAggregate : AggregateBase
{
    private readonly List<SupplementaryMeasure> _supplementaryMeasures = [];

    /// <summary>
    /// Identifier of the data processor/importer party to the agreement.
    /// </summary>
    public string ProcessorId { get; private set; } = string.Empty;

    /// <summary>
    /// The SCC module applicable to this transfer relationship.
    /// </summary>
    public SCCModule Module { get; private set; }

    /// <summary>
    /// Version of the SCC clauses used (e.g., "2021/914").
    /// </summary>
    public string SCCVersion { get; private set; } = string.Empty;

    /// <summary>
    /// Timestamp when the agreement was executed (UTC).
    /// </summary>
    public DateTimeOffset ExecutedAtUtc { get; private set; }

    /// <summary>
    /// Optional expiration date of the agreement (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the agreement has no fixed expiration date.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Indicates whether the agreement has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Timestamp when the agreement was revoked (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the agreement has not been revoked.
    /// </remarks>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Indicates whether the agreement has expired.
    /// </summary>
    public bool IsExpired { get; private set; }

    /// <summary>
    /// Supplementary measures associated with this SCC agreement.
    /// </summary>
    public IReadOnlyList<SupplementaryMeasure> SupplementaryMeasures => _supplementaryMeasures.AsReadOnly();

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Registers a new SCC agreement with the specified terms.
    /// </summary>
    /// <param name="id">Unique identifier for the agreement.</param>
    /// <param name="processorId">Identifier of the data processor/importer.</param>
    /// <param name="module">The SCC module applicable to this transfer relationship.</param>
    /// <param name="version">Version of the SCC clauses used.</param>
    /// <param name="executedAtUtc">Timestamp when the agreement was executed.</param>
    /// <param name="expiresAtUtc">Optional expiration date of the agreement.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="SCCAgreementAggregate"/>.</returns>
    public static SCCAgreementAggregate Register(
        Guid id,
        string processorId,
        SCCModule module,
        string version,
        DateTimeOffset executedAtUtc,
        DateTimeOffset? expiresAtUtc = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var aggregate = new SCCAgreementAggregate();
        aggregate.RaiseEvent(new SCCAgreementRegistered(id, processorId, module, version, executedAtUtc, expiresAtUtc, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Adds a supplementary measure to this SCC agreement.
    /// </summary>
    /// <param name="measureId">Unique identifier for the supplementary measure.</param>
    /// <param name="type">Category of the measure.</param>
    /// <param name="description">Human-readable description of the measure.</param>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is revoked or expired.</exception>
    public void AddSupplementaryMeasure(Guid measureId, SupplementaryMeasureType type, string description)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Cannot add supplementary measures to a revoked SCC agreement.");
        }

        if (IsExpired)
        {
            throw new InvalidOperationException("Cannot add supplementary measures to an expired SCC agreement.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        RaiseEvent(new SCCSupplementaryMeasureAdded(Id, measureId, type, description));
    }

    /// <summary>
    /// Revokes the SCC agreement.
    /// </summary>
    /// <param name="reason">Explanation of why the agreement is being revoked.</param>
    /// <param name="revokedBy">Identifier of the person revoking the agreement.</param>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is already revoked.</exception>
    public void Revoke(string reason, string revokedBy)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Cannot revoke an SCC agreement that is already revoked.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedBy);

        RaiseEvent(new SCCAgreementRevoked(Id, reason, revokedBy));
    }

    /// <summary>
    /// Expires the SCC agreement.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the agreement is already expired or revoked.</exception>
    public void Expire()
    {
        if (IsExpired)
        {
            throw new InvalidOperationException("Cannot expire an SCC agreement that is already expired.");
        }

        if (IsRevoked)
        {
            throw new InvalidOperationException("Cannot expire an SCC agreement that has been revoked.");
        }

        RaiseEvent(new SCCAgreementExpired(Id));
    }

    /// <summary>
    /// Determines whether this SCC agreement is currently valid.
    /// </summary>
    /// <param name="nowUtc">The current UTC time for expiration evaluation.</param>
    /// <returns><c>true</c> if the agreement is active, not revoked, and not expired; otherwise <c>false</c>.</returns>
    public bool IsValid(DateTimeOffset nowUtc)
    {
        if (IsRevoked || IsExpired)
        {
            return false;
        }

        if (ExpiresAtUtc.HasValue && nowUtc >= ExpiresAtUtc.Value)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case SCCAgreementRegistered e:
                Id = e.AgreementId;
                ProcessorId = e.ProcessorId;
                Module = e.Module;
                SCCVersion = e.Version;
                ExecutedAtUtc = e.ExecutedAtUtc;
                ExpiresAtUtc = e.ExpiresAtUtc;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case SCCSupplementaryMeasureAdded e:
                _supplementaryMeasures.Add(new SupplementaryMeasure
                {
                    Id = e.MeasureId,
                    Type = e.MeasureType,
                    Description = e.Description,
                    IsImplemented = false
                });
                break;

            case SCCAgreementRevoked:
                IsRevoked = true;
                RevokedAtUtc = DateTimeOffset.UtcNow;
                break;

            case SCCAgreementExpired:
                IsExpired = true;
                break;
        }
    }
}
