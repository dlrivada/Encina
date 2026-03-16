using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.ProcessorAgreements.ReadModels;

/// <summary>
/// Marten inline projection that transforms processor aggregate events into <see cref="ProcessorReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for processor identity management. It handles all 5
/// processor event types, creating or updating the <see cref="ProcessorReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="ProcessorRegistered"/> — Creates a new read model (first event in stream)</description></item>
///   <item><description><see cref="ProcessorUpdated"/> — Updates identity information (name, country, contact, authorization type)</description></item>
///   <item><description><see cref="ProcessorRemoved"/> — Marks the processor as removed from the processing chain</description></item>
///   <item><description><see cref="SubProcessorAdded"/> — Increments the sub-processor count</description></item>
///   <item><description><see cref="SubProcessorRemoved"/> — Decrements the sub-processor count</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class ProcessorProjection :
    IProjection<ProcessorReadModel>,
    IProjectionCreator<ProcessorRegistered, ProcessorReadModel>,
    IProjectionHandler<ProcessorUpdated, ProcessorReadModel>,
    IProjectionHandler<ProcessorRemoved, ProcessorReadModel>,
    IProjectionHandler<SubProcessorAdded, ProcessorReadModel>,
    IProjectionHandler<SubProcessorRemoved, ProcessorReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "ProcessorProjection";

    /// <summary>
    /// Creates a new <see cref="ProcessorReadModel"/> from a <see cref="ProcessorRegistered"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a processor aggregate stream. It initializes all fields
    /// including the hierarchical position and authorization type per Article 28(2).
    /// </remarks>
    /// <param name="domainEvent">The processor registered event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="ProcessorReadModel"/> with <see cref="ProcessorReadModel.IsRemoved"/> = <c>false</c>.</returns>
    public ProcessorReadModel Create(ProcessorRegistered domainEvent, ProjectionContext context)
    {
        return new ProcessorReadModel
        {
            Id = domainEvent.ProcessorId,
            Name = domainEvent.Name,
            Country = domainEvent.Country,
            ContactEmail = domainEvent.ContactEmail,
            ParentProcessorId = domainEvent.ParentProcessorId,
            Depth = domainEvent.Depth,
            AuthorizationType = domainEvent.AuthorizationType,
            IsRemoved = false,
            SubProcessorCount = 0,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            CreatedAtUtc = domainEvent.OccurredAtUtc,
            LastModifiedAtUtc = domainEvent.OccurredAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when processor identity information changes.
    /// </summary>
    /// <remarks>
    /// Updates name, country, contact email, and authorization type. Hierarchical position
    /// changes are tracked separately via <see cref="SubProcessorAdded"/> and <see cref="SubProcessorRemoved"/>.
    /// </remarks>
    /// <param name="domainEvent">The processor updated event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ProcessorReadModel Apply(ProcessorUpdated domainEvent, ProcessorReadModel current, ProjectionContext context)
    {
        current.Name = domainEvent.Name;
        current.Country = domainEvent.Country;
        current.ContactEmail = domainEvent.ContactEmail;
        current.AuthorizationType = domainEvent.AuthorizationType;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the processor is removed from the processing chain.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 28(3)(g), upon termination the processor must delete or return all personal data.
    /// </remarks>
    /// <param name="domainEvent">The processor removed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with <see cref="ProcessorReadModel.IsRemoved"/> = <c>true</c>.</returns>
    public ProcessorReadModel Apply(ProcessorRemoved domainEvent, ProcessorReadModel current, ProjectionContext context)
    {
        current.IsRemoved = true;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a sub-processor is added to this processor's hierarchy.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 28(2), the processor shall not engage another processor without prior
    /// written authorisation of the controller. The <see cref="ProcessorReadModel.SubProcessorCount"/>
    /// is incremented to reflect the new sub-processor relationship.
    /// </remarks>
    /// <param name="domainEvent">The sub-processor added event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with incremented sub-processor count.</returns>
    public ProcessorReadModel Apply(SubProcessorAdded domainEvent, ProcessorReadModel current, ProjectionContext context)
    {
        current.SubProcessorCount++;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a sub-processor is removed from this processor's hierarchy.
    /// </summary>
    /// <remarks>
    /// Tracks removal of sub-processors from the processing chain. Per Article 28(4),
    /// sub-processor obligations must be maintained throughout the processing relationship.
    /// The <see cref="ProcessorReadModel.SubProcessorCount"/> is decremented.
    /// </remarks>
    /// <param name="domainEvent">The sub-processor removed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with decremented sub-processor count.</returns>
    public ProcessorReadModel Apply(SubProcessorRemoved domainEvent, ProcessorReadModel current, ProjectionContext context)
    {
        current.SubProcessorCount--;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }
}
