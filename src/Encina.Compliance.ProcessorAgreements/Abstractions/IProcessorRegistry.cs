using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Registry for managing processor identities and their hierarchical relationships.
/// </summary>
/// <remarks>
/// <para>
/// The processor registry manages <see cref="Processor"/> entities — the long-lived identity
/// records representing data processors and sub-processors in the processing chain. Per
/// GDPR Article 28(1), "the controller shall use only processors providing sufficient guarantees
/// to implement appropriate technical and organisational measures."
/// </para>
/// <para>
/// This interface manages processor identity and hierarchy only. Contractual state
/// (Data Processing Agreements, terms, expiration) is managed separately by
/// <see cref="IDPAStore"/>, following the design principle of separating identity
/// from contractual state (DC 2).
/// </para>
/// <para>
/// The sub-processor hierarchy is bounded by <c>MaxSubProcessorDepth</c> in configuration.
/// <see cref="GetSubProcessorsAsync"/> returns direct children, while
/// <see cref="GetFullSubProcessorChainAsync"/> recursively traverses the entire
/// sub-processor chain up to the configured depth limit (DC 5).
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store processors in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register a top-level processor
/// var processor = new Processor
/// {
///     Id = "stripe-payments",
///     Name = "Stripe Inc.",
///     Country = "US",
///     Depth = 0,
///     SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
///     CreatedAtUtc = DateTimeOffset.UtcNow,
///     LastUpdatedAtUtc = DateTimeOffset.UtcNow
/// };
/// await registry.RegisterProcessorAsync(processor, ct);
///
/// // Query the sub-processor chain
/// var chain = await registry.GetFullSubProcessorChainAsync("stripe-payments", ct);
/// </code>
/// </example>
public interface IProcessorRegistry
{
    /// <summary>
    /// Registers a new processor in the registry.
    /// </summary>
    /// <param name="processor">The processor to register.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if registration fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Registration validates:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The processor ID does not already exist (<c>processor.already_exists</c>).</description></item>
    /// <item><description>If <see cref="Processor.ParentProcessorId"/> is set, the parent must exist in the registry.</description></item>
    /// <item><description>The <see cref="Processor.Depth"/> must not exceed <c>MaxSubProcessorDepth</c>
    /// (<c>processor.sub_processor_depth_exceeded</c>).</description></item>
    /// </list>
    /// <para>
    /// Per Article 28(2), engaging a sub-processor requires prior authorization from the controller.
    /// The authorization type is captured in <see cref="Processor.SubProcessorAuthorizationType"/>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RegisterProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a processor by its unique identifier.
    /// </summary>
    /// <param name="processorId">The unique identifier of the processor.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Option{Processor}"/> containing the processor if found;
    /// <c>None</c> if no processor exists with the given ID;
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<Processor>>> GetProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered processors.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all processors, or an <see cref="EncinaError"/> on failure.
    /// Returns an empty list if no processors are registered.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting, administrative dashboards, and regulatory audits.
    /// For large datasets, consider implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing processor's details in the registry.
    /// </summary>
    /// <param name="processor">The processor with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the processor
    /// is not found (<c>processor.not_found</c>) or the update fails.
    /// </returns>
    /// <remarks>
    /// The processor is matched by <see cref="Processor.Id"/>. All mutable fields are overwritten.
    /// Changes to <see cref="Processor.ParentProcessorId"/> or <see cref="Processor.Depth"/>
    /// are validated against the hierarchy constraints.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdateProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a processor from the registry.
    /// </summary>
    /// <param name="processorId">The unique identifier of the processor to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the processor
    /// is not found (<c>processor.not_found</c>) or the removal fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Removing a processor does not automatically remove its sub-processors or DPAs.
    /// Callers should handle cascading cleanup (e.g., terminating DPAs, removing
    /// sub-processors) before or after removal.
    /// </para>
    /// <para>
    /// Per Article 28(3)(g), upon termination the processor must delete or return all
    /// personal data. Consider publishing a <c>SubProcessorRemovedNotification</c> to
    /// trigger downstream cleanup workflows.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the direct sub-processors of a given processor.
    /// </summary>
    /// <param name="processorId">The unique identifier of the parent processor.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of processors whose <see cref="Processor.ParentProcessorId"/>
    /// matches the given <paramref name="processorId"/> (i.e., <c>Depth = parent.Depth + 1</c>),
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no sub-processors exist.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns only direct children (one level down). For the entire sub-processor tree,
    /// use <see cref="GetFullSubProcessorChainAsync"/>.
    /// </para>
    /// <para>
    /// Per Article 28(2), the processor must have prior authorization from the controller
    /// before engaging sub-processors. Under general authorization, the processor must
    /// inform the controller of any intended changes.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetSubProcessorsAsync(
        string processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full sub-processor chain for a given processor, recursively traversing
    /// the hierarchy up to <c>MaxSubProcessorDepth</c>.
    /// </summary>
    /// <param name="processorId">The unique identifier of the root processor.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all descendant processors in the sub-processor chain,
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no
    /// sub-processors exist. The list includes all levels of nesting up to the
    /// configured <c>MaxSubProcessorDepth</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method recursively traverses <see cref="Processor.ParentProcessorId"/> links
    /// starting from the given processor, collecting all descendants. The traversal is
    /// bounded by <c>MaxSubProcessorDepth</c> to prevent unbounded chains (DC 5).
    /// </para>
    /// <para>
    /// Per Article 28(4), "where [a sub-processor] fails to fulfil its data protection
    /// obligations, the initial processor shall remain fully liable to the controller
    /// for the performance of that other processor's obligations." This method enables
    /// compliance oversight of the entire processing chain.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetFullSubProcessorChainAsync(
        string processorId,
        CancellationToken cancellationToken = default);
}
