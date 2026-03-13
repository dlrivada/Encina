namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Factory methods for processor agreement-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>processor.{category}</c>.
/// All errors include structured metadata for observability and GDPR article references.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 28(1) — Obligation to use only processors providing sufficient guarantees.
/// Article 28(2) — Prior authorization for sub-processor engagement.
/// Article 28(3) — Mandatory contractual provisions (a)-(h).
/// Article 28(4) — Sub-processor obligations mirror the original processor's.
/// Article 46    — Appropriate safeguards for cross-border transfers (SCCs).
/// </para>
/// </remarks>
public static class ProcessorAgreementErrors
{
    private const string MetadataKeyProcessorId = "processorId";
    private const string MetadataKeyDPAId = "dpaId";
    private const string MetadataKeyStage = "processor_agreements";

    // --- Error codes ---

    /// <summary>Error code when a processor is not found in the registry.</summary>
    public const string NotFoundCode = "processor.not_found";

    /// <summary>Error code when a processor with the same ID already exists in the registry.</summary>
    public const string AlreadyExistsCode = "processor.already_exists";

    /// <summary>Error code when a DPA is not found by its ID.</summary>
    public const string DPANotFoundCode = "processor.dpa_not_found";

    /// <summary>Error code when no active DPA exists for a processor.</summary>
    public const string DPAMissingCode = "processor.dpa_missing";

    /// <summary>Error code when a DPA has expired.</summary>
    public const string DPAExpiredCode = "processor.dpa_expired";

    /// <summary>Error code when a DPA has been terminated.</summary>
    public const string DPATerminatedCode = "processor.dpa_terminated";

    /// <summary>Error code when a DPA is pending renewal.</summary>
    public const string DPAPendingRenewalCode = "processor.dpa_pending_renewal";

    /// <summary>Error code when mandatory DPA terms are not fully met.</summary>
    public const string DPAIncompleteCode = "processor.dpa_incomplete";

    /// <summary>Error code when a sub-processor is not authorized per Article 28(2).</summary>
    public const string SubProcessorUnauthorizedCode = "processor.sub_processor_unauthorized";

    /// <summary>Error code when registering a sub-processor would exceed the configured maximum depth.</summary>
    public const string SubProcessorDepthExceededCode = "processor.sub_processor_depth_exceeded";

    /// <summary>Error code when SCCs are required for a cross-border transfer but not present.</summary>
    public const string SCCRequiredCode = "processor.scc_required";

    /// <summary>Error code when a store operation fails.</summary>
    public const string StoreErrorCode = "processor.store_error";

    /// <summary>Error code for general validation failures.</summary>
    public const string ValidationFailedCode = "processor.validation_failed";

    // --- Processor lifecycle errors ---

    /// <summary>
    /// Creates an error when a processor is not found in the registry.
    /// </summary>
    /// <param name="processorId">The identifier of the processor that was not found.</param>
    /// <returns>An error indicating the processor was not found.</returns>
    /// <remarks>
    /// Per GDPR Article 28(1), the controller must use only processors that are registered
    /// and provide sufficient guarantees. A missing processor indicates a configuration or
    /// workflow issue.
    /// </remarks>
    public static EncinaError NotFound(string processorId) =>
        EncinaErrors.Create(
            code: NotFoundCode,
            message: $"Processor '{processorId}' was not found in the registry. "
                + "Per Article 28(1), only registered processors with sufficient guarantees may be used.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_28_1_registered_processor"
            });

    /// <summary>
    /// Creates an error when a processor with the same ID already exists in the registry.
    /// </summary>
    /// <param name="processorId">The identifier of the duplicate processor.</param>
    /// <returns>An error indicating the processor already exists.</returns>
    public static EncinaError AlreadyExists(string processorId) =>
        EncinaErrors.Create(
            code: AlreadyExistsCode,
            message: $"A processor with ID '{processorId}' already exists in the registry.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- DPA lifecycle errors ---

    /// <summary>
    /// Creates an error when a DPA is not found by its ID.
    /// </summary>
    /// <param name="dpaId">The identifier of the DPA that was not found.</param>
    /// <returns>An error indicating the DPA was not found.</returns>
    public static EncinaError DPANotFound(string dpaId) =>
        EncinaErrors.Create(
            code: DPANotFoundCode,
            message: $"Data Processing Agreement '{dpaId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyDPAId] = dpaId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when no active DPA exists for a processor.
    /// </summary>
    /// <param name="processorId">The identifier of the processor without a DPA.</param>
    /// <returns>An error indicating no active DPA exists.</returns>
    /// <remarks>
    /// Per GDPR Article 28(3), processing by a processor shall be governed by a contract.
    /// Without an active DPA, processing operations must be blocked or warned depending
    /// on the configured enforcement mode.
    /// </remarks>
    public static EncinaError DPAMissing(string processorId) =>
        EncinaErrors.Create(
            code: DPAMissingCode,
            message: $"No active Data Processing Agreement exists for processor '{processorId}'. "
                + "Per Article 28(3), a binding contract is required before processing can proceed.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_28_3_binding_contract"
            });

    /// <summary>
    /// Creates an error when a DPA has expired.
    /// </summary>
    /// <param name="processorId">The identifier of the processor.</param>
    /// <param name="dpaId">The identifier of the expired DPA.</param>
    /// <param name="expiredAtUtc">The UTC timestamp when the DPA expired.</param>
    /// <returns>An error indicating the DPA has expired.</returns>
    /// <remarks>
    /// An expired agreement means the contractual basis required by Article 28(3) is no longer
    /// valid. Processing operations should be blocked until a new agreement is signed.
    /// </remarks>
    public static EncinaError DPAExpired(string processorId, string dpaId, DateTimeOffset expiredAtUtc) =>
        EncinaErrors.Create(
            code: DPAExpiredCode,
            message: $"Data Processing Agreement '{dpaId}' for processor '{processorId}' expired at {expiredAtUtc:O}. "
                + "A renewed or new agreement is required per Article 28(3).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                [MetadataKeyDPAId] = dpaId,
                ["expiredAtUtc"] = expiredAtUtc,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_28_3_binding_contract"
            });

    /// <summary>
    /// Creates an error when a DPA has been terminated.
    /// </summary>
    /// <param name="processorId">The identifier of the processor.</param>
    /// <param name="dpaId">The identifier of the terminated DPA.</param>
    /// <returns>An error indicating the DPA was terminated.</returns>
    /// <remarks>
    /// Per Article 28(3)(g), upon termination the processor must delete or return all personal
    /// data and certify that it has done so.
    /// </remarks>
    public static EncinaError DPATerminated(string processorId, string dpaId) =>
        EncinaErrors.Create(
            code: DPATerminatedCode,
            message: $"Data Processing Agreement '{dpaId}' for processor '{processorId}' has been terminated. "
                + "Per Article 28(3)(g), the processor must delete or return all personal data.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                [MetadataKeyDPAId] = dpaId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_28_3_g_data_deletion"
            });

    /// <summary>
    /// Creates an error when a DPA is pending renewal.
    /// </summary>
    /// <param name="processorId">The identifier of the processor.</param>
    /// <param name="dpaId">The identifier of the DPA pending renewal.</param>
    /// <returns>An error indicating the DPA is pending renewal.</returns>
    public static EncinaError DPAPendingRenewal(string processorId, string dpaId) =>
        EncinaErrors.Create(
            code: DPAPendingRenewalCode,
            message: $"Data Processing Agreement '{dpaId}' for processor '{processorId}' is pending renewal. "
                + "The agreement should be renewed before it expires to maintain compliance per Article 28(3).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                [MetadataKeyDPAId] = dpaId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_28_3_binding_contract"
            });

    /// <summary>
    /// Creates an error when mandatory DPA terms are not fully met.
    /// </summary>
    /// <param name="processorId">The identifier of the processor.</param>
    /// <param name="dpaId">The identifier of the incomplete DPA.</param>
    /// <param name="missingTerms">The list of missing mandatory term names from Article 28(3)(a)-(h).</param>
    /// <returns>An error indicating the DPA is incomplete.</returns>
    /// <remarks>
    /// Per Article 28(3), the contract must include all eight mandatory provisions.
    /// The <paramref name="missingTerms"/> parameter lists which provisions are missing,
    /// enabling targeted remediation.
    /// </remarks>
    public static EncinaError DPAIncomplete(
        string processorId,
        string dpaId,
        IReadOnlyList<string> missingTerms) =>
        EncinaErrors.Create(
            code: DPAIncompleteCode,
            message: $"Data Processing Agreement '{dpaId}' for processor '{processorId}' is missing "
                + $"{missingTerms.Count} mandatory term(s) per Article 28(3): {string.Join(", ", missingTerms)}.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                [MetadataKeyDPAId] = dpaId,
                ["missingTerms"] = missingTerms,
                ["missingTermCount"] = missingTerms.Count,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_28_3_mandatory_terms"
            });

    // --- Sub-processor errors ---

    /// <summary>
    /// Creates an error when a sub-processor is not authorized per Article 28(2).
    /// </summary>
    /// <param name="processorId">The identifier of the parent processor.</param>
    /// <param name="subProcessorId">The identifier of the unauthorized sub-processor.</param>
    /// <returns>An error indicating the sub-processor is not authorized.</returns>
    /// <remarks>
    /// Per Article 28(2), "the processor shall not engage another processor without prior
    /// specific or general written authorisation of the controller."
    /// </remarks>
    public static EncinaError SubProcessorUnauthorized(string processorId, string subProcessorId) =>
        EncinaErrors.Create(
            code: SubProcessorUnauthorizedCode,
            message: $"Sub-processor '{subProcessorId}' is not authorized under processor '{processorId}'. "
                + "Per Article 28(2), prior written authorisation from the controller is required.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                ["subProcessorId"] = subProcessorId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_28_2_prior_authorization"
            });

    /// <summary>
    /// Creates an error when registering a sub-processor would exceed the configured maximum depth.
    /// </summary>
    /// <param name="processorId">The identifier of the parent processor.</param>
    /// <param name="requestedDepth">The depth that was requested.</param>
    /// <param name="maxDepth">The configured maximum sub-processor depth.</param>
    /// <returns>An error indicating the depth limit would be exceeded.</returns>
    /// <remarks>
    /// The sub-processor hierarchy is bounded by <c>MaxSubProcessorDepth</c> to prevent
    /// unbounded processing chains, which would complicate compliance oversight per
    /// Article 28(4). This limit ensures that liability chains remain manageable.
    /// </remarks>
    public static EncinaError SubProcessorDepthExceeded(
        string processorId,
        int requestedDepth,
        int maxDepth) =>
        EncinaErrors.Create(
            code: SubProcessorDepthExceededCode,
            message: $"Registering a sub-processor under '{processorId}' at depth {requestedDepth} "
                + $"would exceed the configured maximum depth of {maxDepth}. "
                + "Per Article 28(4), sub-processor chains must remain bounded for compliance oversight.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                ["requestedDepth"] = requestedDepth,
                ["maxDepth"] = maxDepth,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_28_4_sub_processor_obligations"
            });

    // --- Cross-border transfer errors ---

    /// <summary>
    /// Creates an error when Standard Contractual Clauses are required but not present.
    /// </summary>
    /// <param name="processorId">The identifier of the processor.</param>
    /// <param name="country">The country of the processor that triggers the SCC requirement.</param>
    /// <returns>An error indicating SCCs are required.</returns>
    /// <remarks>
    /// Per Articles 46(2)(c) and 46(2)(d), Standard Contractual Clauses are required for
    /// cross-border data transfers to countries without an EU adequacy decision.
    /// </remarks>
    public static EncinaError SCCRequired(string processorId, string country) =>
        EncinaErrors.Create(
            code: SCCRequiredCode,
            message: $"Standard Contractual Clauses are required for processor '{processorId}' in '{country}' "
                + "but are not included in the Data Processing Agreement. "
                + "Per Articles 46(2)(c)-(d), SCCs are required for cross-border transfers without an adequacy decision.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                ["country"] = country,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_46_2_standard_contractual_clauses"
            });

    // --- Store errors (persistence layer) ---

    /// <summary>
    /// Creates an error when a store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "RegisterProcessor", "AddDPA").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(
        string operation,
        string message,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Processor agreement store operation '{operation}' failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- General validation errors ---

    /// <summary>
    /// Creates a general validation failure error.
    /// </summary>
    /// <param name="processorId">The identifier of the processor.</param>
    /// <param name="message">The validation failure message.</param>
    /// <returns>An error indicating a validation failure.</returns>
    public static EncinaError ValidationFailed(string processorId, string message) =>
        EncinaErrors.Create(
            code: ValidationFailedCode,
            message: $"Processor agreement validation failed for '{processorId}': {message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProcessorId] = processorId,
                [MetadataKeyStage] = MetadataKeyStage
            });
}
