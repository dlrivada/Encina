namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Context provided to each <see cref="Abstractions.INIS2MeasureEvaluator"/> during compliance evaluation.
/// </summary>
/// <remarks>
/// <para>
/// Provides access to the configured <see cref="NIS2Options"/>, a <see cref="TimeProvider"/> for
/// deterministic time operations, and the <see cref="IServiceProvider"/> for resolving
/// runtime dependencies needed by specific evaluators.
/// </para>
/// <para>
/// The context is created by the <see cref="Abstractions.INIS2ComplianceValidator"/> and
/// shared across all evaluator invocations within a single validation run.
/// </para>
/// </remarks>
public sealed record NIS2MeasureContext
{
    /// <summary>
    /// The NIS2 configuration options for the current entity.
    /// </summary>
    public required NIS2Options Options { get; init; }

    /// <summary>
    /// Time provider for deterministic time operations.
    /// </summary>
    /// <remarks>
    /// Use this instead of <c>DateTimeOffset.UtcNow</c> to enable deterministic testing
    /// of time-dependent compliance checks (e.g., supplier assessment expiry).
    /// </remarks>
    public required TimeProvider TimeProvider { get; init; }

    /// <summary>
    /// Service provider for resolving runtime dependencies within evaluators.
    /// </summary>
    /// <remarks>
    /// Evaluators may need to resolve services that are not directly injected
    /// (e.g., checking if <c>IBreachNotificationService</c> is registered for the
    /// incident handling measure, or verifying that <c>IAuditStore</c> is available
    /// for the risk analysis measure).
    /// </remarks>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// The current tenant identifier, if multi-tenancy is active.
    /// </summary>
    /// <remarks>
    /// When set, evaluators may use this to scope compliance checks per tenant.
    /// Resolved from <see cref="IRequestContext.TenantId"/> when available.
    /// </remarks>
    public string? TenantId { get; init; }
}
