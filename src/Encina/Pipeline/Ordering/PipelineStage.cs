namespace Encina;

/// <summary>
/// Closed set of pipeline stages. A stage fixes where a pipeline step runs relative to the others;
/// lower values are further out (they run first on the way in and last on the way out).
/// </summary>
/// <remarks>
/// <para>
/// A behavior or processor declares its stage with <see cref="PipelineStageAttribute"/>. Inside one stage the
/// <see cref="PipelineStageAttribute.Order"/> decides (lower is further out). A step without a declared stage
/// is placed in <see cref="Application"/>, which sits inside every security, compliance and transaction stage,
/// so an undeclared step can never run around a gate.
/// </para>
/// <para>
/// The numeric values leave room between members and are part of the contract: they are never renumbered.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [PipelineStage(PipelineStage.Authorization, Order = 5)]
/// public sealed class TenantGateBehavior&lt;TRequest, TResponse&gt; : IPipelineBehavior&lt;TRequest, TResponse&gt;
///     where TRequest : IRequest&lt;TResponse&gt;
/// {
///     public ValueTask&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(
///         TRequest request,
///         IRequestContext context,
///         RequestHandlerCallback&lt;TResponse&gt; nextStep,
///         CancellationToken cancellationToken) => nextStep();
/// }
/// </code>
/// </example>
public enum PipelineStage
{
    /// <summary>
    /// Tracing, metrics and request-scoped diagnostics. Outermost, so it observes every outcome,
    /// including requests rejected by an inner gate.
    /// </summary>
    Observability = 100,

    /// <summary>
    /// Load shedding: bulkhead and rate limiting. Runs before identity work so overload is rejected cheaply;
    /// it sits outside <see cref="Audit"/> because a rejected-for-overload request is not an access attempt.
    /// </summary>
    Admission = 200,

    /// <summary>
    /// Operation and access auditing. Outside the security gates so denied attempts are recorded.
    /// </summary>
    Audit = 300,

    /// <summary>
    /// Establishes or verifies the caller identity (message signatures, tokens, schemes).
    /// Runs before any authorization decision.
    /// </summary>
    Authentication = 400,

    /// <summary>
    /// Decides whether the authenticated caller may perform the request (roles, policies, ABAC).
    /// Fails closed when context is missing.
    /// </summary>
    Authorization = 500,

    /// <summary>
    /// Regulatory gates that need the caller authorized first: consent, lawful basis, residency, retention.
    /// </summary>
    Compliance = 600,

    /// <summary>
    /// Presentation concerns of the request itself: sanitization, content negotiation, localization.
    /// </summary>
    Presentation = 700,

    /// <summary>
    /// Prepares the incoming payload before it is validated: decrypting received data, normalization.
    /// </summary>
    InputPreparation = 800,

    /// <summary>
    /// Validates the (decrypted, normalized) request. Nothing inside runs on an invalid request.
    /// </summary>
    Validation = 900,

    /// <summary>
    /// Query caching. After validation so only valid requests are cached; before the transaction so a
    /// cache hit opens no connection.
    /// </summary>
    Caching = 1000,

    /// <summary>
    /// Retry, timeout and circuit breaking around the work that follows.
    /// </summary>
    Resilience = 1100,

    /// <summary>
    /// Chooses the data source for the request (read/write routing, tenant or shard selection).
    /// Runs before the transaction opens its connection.
    /// </summary>
    DataRouting = 1200,

    /// <summary>
    /// Unit of work: opens the transaction and commits or rolls back from the result.
    /// Post-commit processors default here, outside the transaction behavior.
    /// </summary>
    Transaction = 1300,

    /// <summary>
    /// Deduplication of repeated requests (inbox, idempotency keys), inside the transaction so the
    /// deduplication record commits with the work.
    /// </summary>
    Idempotency = 1400,

    /// <summary>
    /// Default stage for application behaviors and processors that declare none. Inside every gate,
    /// transaction and idempotency step.
    /// </summary>
    Application = 1500,

    /// <summary>
    /// Last step before the handler: encrypting request properties, injecting secrets, final enrichment.
    /// Innermost, so validators and application steps see plain values.
    /// </summary>
    HandlerPreparation = 1600
}
