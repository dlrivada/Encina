using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using LanguageExt;

namespace Encina.Compliance.ProcessorAgreements.Abstractions;

/// <summary>
/// Service interface for managing data processor lifecycle operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean CQRS API for registering, updating, removing, and querying data processors.
/// The implementation wraps the event-sourced <see cref="Aggregates.ProcessorAggregate"/> via
/// <c>IAggregateRepository&lt;ProcessorAggregate&gt;</c> (command side) and
/// <c>IReadModelRepository&lt;ProcessorReadModel&gt;</c> (query side).
/// </para>
/// <para>
/// Per GDPR Article 28(1), the controller must use only processors providing sufficient guarantees
/// to implement appropriate technical and organisational measures. This service tracks processor
/// identity, hierarchical position, and sub-processor relationships per Article 28(2).
/// </para>
/// </remarks>
public interface IProcessorService
{
    // ========================================================================
    // Command operations
    // ========================================================================

    /// <summary>
    /// Registers a new data processor in the processing chain.
    /// </summary>
    /// <param name="name">The display name of the processor (e.g., "Stripe", "AWS").</param>
    /// <param name="country">The country where the processor is established (ISO 3166-1).</param>
    /// <param name="contactEmail">The contact email for the processor's data protection representative, or <c>null</c>.</param>
    /// <param name="parentProcessorId">The parent processor's identifier, or <c>null</c> for top-level processors.</param>
    /// <param name="depth">The depth in the sub-processor hierarchy (0 = top-level).</param>
    /// <param name="authorizationType">The type of written authorization for sub-processor engagement per Article 28(2).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly registered processor.</returns>
    ValueTask<Either<EncinaError, Guid>> RegisterProcessorAsync(
        string name,
        string country,
        string? contactEmail,
        Guid? parentProcessorId,
        int depth,
        SubProcessorAuthorizationType authorizationType,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing processor's identity information.
    /// </summary>
    /// <param name="processorId">The identifier of the processor to update.</param>
    /// <param name="name">The updated display name.</param>
    /// <param name="country">The updated country.</param>
    /// <param name="contactEmail">The updated contact email, or <c>null</c>.</param>
    /// <param name="authorizationType">The updated authorization type.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> UpdateProcessorAsync(
        Guid processorId,
        string name,
        string country,
        string? contactEmail,
        SubProcessorAuthorizationType authorizationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a processor from the processing chain.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 28(3)(g), upon termination the processor must delete or return all personal data.
    /// </remarks>
    /// <param name="processorId">The identifier of the processor to remove.</param>
    /// <param name="reason">The reason for removing the processor.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
        Guid processorId,
        string reason,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <summary>
    /// Retrieves a processor by its identifier.
    /// </summary>
    /// <param name="processorId">The processor identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the processor read model.</returns>
    ValueTask<Either<EncinaError, ProcessorReadModel>> GetProcessorAsync(
        Guid processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered processors (non-removed).
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of active processors.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves direct sub-processors of a given processor.
    /// </summary>
    /// <remarks>
    /// Per Article 28(2), the processor must inform the controller of sub-processors.
    /// This method returns only direct children (depth + 1).
    /// </remarks>
    /// <param name="processorId">The parent processor identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of direct sub-processors.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetSubProcessorsAsync(
        Guid processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full sub-processor chain for a given processor using BFS traversal.
    /// </summary>
    /// <remarks>
    /// Per Article 28(4), sub-processor obligations mirror the original processor's.
    /// This method returns all descendants in the hierarchy (direct and indirect).
    /// </remarks>
    /// <param name="processorId">The root processor identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the full sub-processor chain.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetFullSubProcessorChainAsync(
        Guid processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the event history for a processor aggregate.
    /// </summary>
    /// <param name="processorId">The processor identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of historical events.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetProcessorHistoryAsync(
        Guid processorId,
        CancellationToken cancellationToken = default);
}
