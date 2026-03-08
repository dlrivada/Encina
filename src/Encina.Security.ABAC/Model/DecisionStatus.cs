namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 Status — additional information about the result of a policy evaluation,
/// particularly when the decision is <see cref="Effect.Indeterminate"/>.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.10 — The status provides diagnostic information about why a particular
/// decision was reached. It is most useful for <see cref="Effect.Indeterminate"/> decisions,
/// where the <see cref="StatusCode"/> indicates the category of error and
/// <see cref="StatusMessage"/> provides a human-readable description.
/// </para>
/// <para>
/// Standard status codes include <c>"urn:oasis:names:tc:xacml:1.0:status:ok"</c>,
/// <c>"urn:oasis:names:tc:xacml:1.0:status:missing-attribute"</c>,
/// <c>"urn:oasis:names:tc:xacml:1.0:status:syntax-error"</c>, and
/// <c>"urn:oasis:names:tc:xacml:1.0:status:processing-error"</c>.
/// </para>
/// </remarks>
public sealed record DecisionStatus
{
    /// <summary>
    /// The status code indicating the category of the evaluation result.
    /// </summary>
    /// <remarks>
    /// Standard XACML status codes are defined as URN strings. Encina uses simplified
    /// identifiers (e.g., <c>"ok"</c>, <c>"missing-attribute"</c>, <c>"processing-error"</c>).
    /// </remarks>
    public required string StatusCode { get; init; }

    /// <summary>
    /// An optional human-readable message providing additional details about the status.
    /// </summary>
    public string? StatusMessage { get; init; }
}
