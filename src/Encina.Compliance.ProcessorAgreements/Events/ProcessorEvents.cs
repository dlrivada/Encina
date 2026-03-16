using Encina.Compliance.ProcessorAgreements.Model;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

namespace Encina.Compliance.ProcessorAgreements.Events;

/// <summary>
/// Raised when a new data processor is registered in the processing chain.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 28(1), the controller must use only processors providing sufficient guarantees
/// to implement appropriate technical and organisational measures. Registration captures the processor's
/// identity, hierarchical position, and authorization type for sub-processor engagement.
/// </para>
/// <para>
/// This event initiates the processor lifecycle. The processor can subsequently have sub-processors
/// added or removed, be updated, or be removed from the registry.
/// </para>
/// </remarks>
/// <param name="ProcessorId">The unique identifier for the processor.</param>
/// <param name="Name">The display name of the processor (e.g., "Stripe", "AWS").</param>
/// <param name="Country">The country where the processor is established (ISO 3166-1).</param>
/// <param name="ContactEmail">The contact email for the processor's data protection representative, or <see langword="null"/> if not provided.</param>
/// <param name="ParentProcessorId">The identifier of the parent processor, or <see langword="null"/> for top-level processors.</param>
/// <param name="Depth">The depth in the sub-processor hierarchy (0 = top-level).</param>
/// <param name="AuthorizationType">The type of written authorization for sub-processor engagement per Article 28(2).</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the registration occurred.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record ProcessorRegistered(
    Guid ProcessorId,
    string Name,
    string Country,
    string? ContactEmail,
    Guid? ParentProcessorId,
    int Depth,
    SubProcessorAuthorizationType AuthorizationType,
    DateTimeOffset OccurredAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when an existing processor's information is updated.
/// </summary>
/// <remarks>
/// Updates to processor identity information (name, country, contact, authorization type) are tracked
/// as events for GDPR Art. 5(2) accountability. Changes to hierarchical position are handled
/// separately via <see cref="SubProcessorAdded"/> and <see cref="SubProcessorRemoved"/>.
/// </remarks>
/// <param name="ProcessorId">The identifier of the processor being updated.</param>
/// <param name="Name">The updated display name of the processor.</param>
/// <param name="Country">The updated country where the processor is established.</param>
/// <param name="ContactEmail">The updated contact email, or <see langword="null"/> if not provided.</param>
/// <param name="AuthorizationType">The updated authorization type for sub-processor engagement.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the update occurred.</param>
public sealed record ProcessorUpdated(
    Guid ProcessorId,
    string Name,
    string Country,
    string? ContactEmail,
    SubProcessorAuthorizationType AuthorizationType,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a processor is removed from the processing chain.
/// </summary>
/// <remarks>
/// Per GDPR Article 28(3)(g), upon termination the processor must delete or return all personal data.
/// Removal captures the reason for decommissioning the processor relationship.
/// </remarks>
/// <param name="ProcessorId">The identifier of the processor being removed.</param>
/// <param name="Reason">The reason for removing the processor from the registry.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the removal occurred.</param>
public sealed record ProcessorRemoved(
    Guid ProcessorId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a sub-processor is added to a processor's hierarchy.
/// </summary>
/// <remarks>
/// Per GDPR Article 28(2), the processor shall not engage another processor without prior written
/// authorisation of the controller. Under general authorization, the processor must inform the
/// controller of intended changes to give the opportunity to object.
/// </remarks>
/// <param name="ProcessorId">The identifier of the parent processor.</param>
/// <param name="SubProcessorId">The identifier of the sub-processor being added.</param>
/// <param name="SubProcessorName">The display name of the sub-processor being added.</param>
/// <param name="Depth">The depth of the sub-processor in the hierarchy.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the sub-processor was added.</param>
public sealed record SubProcessorAdded(
    Guid ProcessorId,
    Guid SubProcessorId,
    string SubProcessorName,
    int Depth,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a sub-processor is removed from a processor's hierarchy.
/// </summary>
/// <remarks>
/// Tracks removal of sub-processors from the processing chain. Per Article 28(4),
/// sub-processor obligations must be maintained throughout the processing relationship.
/// </remarks>
/// <param name="ProcessorId">The identifier of the parent processor.</param>
/// <param name="SubProcessorId">The identifier of the sub-processor being removed.</param>
/// <param name="Reason">The reason for removing the sub-processor.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the sub-processor was removed.</param>
public sealed record SubProcessorRemoved(
    Guid ProcessorId,
    Guid SubProcessorId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : INotification;
