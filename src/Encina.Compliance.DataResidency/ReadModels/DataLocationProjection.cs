using Encina.Compliance.DataResidency.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.DataResidency.ReadModels;

/// <summary>
/// Marten inline projection that transforms data location aggregate events into
/// <see cref="DataLocationReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for data location tracking.
/// It handles all 6 data location event types, creating or updating the
/// <see cref="DataLocationReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="DataLocationRegistered"/> — Creates a new read model with initial storage details (first event in stream)</description></item>
///   <item><description><see cref="DataLocationMigrated"/> — Updates region code to the new region</description></item>
///   <item><description><see cref="DataLocationVerified"/> — Records the latest verification timestamp</description></item>
///   <item><description><see cref="DataLocationRemoved"/> — Marks the location as removed</description></item>
///   <item><description><see cref="SovereigntyViolationDetected"/> — Records active violation with details</description></item>
///   <item><description><see cref="SovereigntyViolationResolved"/> — Clears the active violation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class DataLocationProjection :
    IProjection<DataLocationReadModel>,
    IProjectionCreator<DataLocationRegistered, DataLocationReadModel>,
    IProjectionHandler<DataLocationMigrated, DataLocationReadModel>,
    IProjectionHandler<DataLocationVerified, DataLocationReadModel>,
    IProjectionHandler<DataLocationRemoved, DataLocationReadModel>,
    IProjectionHandler<SovereigntyViolationDetected, DataLocationReadModel>,
    IProjectionHandler<SovereigntyViolationResolved, DataLocationReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "DataLocationProjection";

    /// <summary>
    /// Creates a new <see cref="DataLocationReadModel"/> from a <see cref="DataLocationRegistered"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a data location aggregate stream. It initializes all fields
    /// including entity identity, data category, region, storage type, and metadata per
    /// GDPR Article 30 records of processing activities.
    /// </remarks>
    /// <param name="domainEvent">The data location registered event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="DataLocationReadModel"/> in active (non-removed) status.</returns>
    public DataLocationReadModel Create(DataLocationRegistered domainEvent, ProjectionContext context)
    {
        return new DataLocationReadModel
        {
            Id = domainEvent.LocationId,
            EntityId = domainEvent.EntityId,
            DataCategory = domainEvent.DataCategory,
            RegionCode = domainEvent.RegionCode,
            StorageType = domainEvent.StorageType,
            StoredAtUtc = domainEvent.StoredAtUtc,
            Metadata = domainEvent.Metadata,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            CreatedAtUtc = domainEvent.StoredAtUtc,
            LastModifiedAtUtc = domainEvent.StoredAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when data is migrated from one region to another.
    /// </summary>
    /// <remarks>
    /// Data migration between regions is a cross-border transfer under GDPR Chapter V.
    /// This event captures the new region, while the event stream preserves the complete
    /// migration history for GDPR Article 5(2) accountability.
    /// </remarks>
    /// <param name="domainEvent">The data location migrated event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with the new region code.</returns>
    public DataLocationReadModel Apply(DataLocationMigrated domainEvent, DataLocationReadModel current, ProjectionContext context)
    {
        current.RegionCode = domainEvent.NewRegionCode;
        current.LastModifiedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a data location is periodically verified.
    /// </summary>
    /// <remarks>
    /// Regular verification helps organizations demonstrate ongoing compliance with data residency
    /// requirements under GDPR Article 5(2) accountability.
    /// </remarks>
    /// <param name="domainEvent">The data location verified event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with the latest verification timestamp.</returns>
    public DataLocationReadModel Apply(DataLocationVerified domainEvent, DataLocationReadModel current, ProjectionContext context)
    {
        current.LastVerifiedAtUtc = domainEvent.VerifiedAtUtc;
        current.LastModifiedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a data location record is removed.
    /// </summary>
    /// <remarks>
    /// Removal may occur due to data deletion (GDPR Art. 17 right to erasure), migration completion,
    /// or cache/replica cleanup. The event stream preserves the full location history even after removal.
    /// </remarks>
    /// <param name="domainEvent">The data location removed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with <see cref="DataLocationReadModel.IsRemoved"/> set to <see langword="true"/>.</returns>
    public DataLocationReadModel Apply(DataLocationRemoved domainEvent, DataLocationReadModel current, ProjectionContext context)
    {
        current.IsRemoved = true;
        current.LastModifiedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a sovereignty violation is detected on this location.
    /// </summary>
    /// <remarks>
    /// A sovereignty violation occurs when the data is stored in a region not permitted by the
    /// applicable residency policy. Per GDPR Article 33, certain violations may trigger breach
    /// notification obligations to the supervisory authority within 72 hours.
    /// </remarks>
    /// <param name="domainEvent">The sovereignty violation detected event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with an active violation.</returns>
    public DataLocationReadModel Apply(SovereigntyViolationDetected domainEvent, DataLocationReadModel current, ProjectionContext context)
    {
        current.HasViolation = true;
        current.ViolationDetails = domainEvent.Details;
        current.LastModifiedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a previously detected sovereignty violation is resolved.
    /// </summary>
    /// <remarks>
    /// Resolution typically involves migrating data to an allowed region, updating the residency
    /// policy, or removing data from the violating location. The event stream provides a complete
    /// audit trail of violation detection and resolution for GDPR Article 5(2) accountability.
    /// </remarks>
    /// <param name="domainEvent">The sovereignty violation resolved event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with no active violation.</returns>
    public DataLocationReadModel Apply(SovereigntyViolationResolved domainEvent, DataLocationReadModel current, ProjectionContext context)
    {
        current.HasViolation = false;
        current.ViolationDetails = null;
        current.LastModifiedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero);
        current.Version++;
        return current;
    }
}
