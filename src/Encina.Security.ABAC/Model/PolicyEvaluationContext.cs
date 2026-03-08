namespace Encina.Security.ABAC;

/// <summary>
/// Represents the attribute context used during XACML policy evaluation, containing
/// all resolved attributes organized by XACML 3.0 attribute categories.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.2 — The evaluation context carries all attribute bags needed for
/// policy evaluation. Attributes are organized by the four standard categories:
/// <see cref="SubjectAttributes"/>, <see cref="ResourceAttributes"/>,
/// <see cref="EnvironmentAttributes"/>, and <see cref="ActionAttributes"/>.
/// </para>
/// <para>
/// Each attribute category is represented as an <see cref="AttributeBag"/>, supporting
/// multi-valued attributes as required by the XACML specification.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new PolicyEvaluationContext
/// {
///     SubjectAttributes = AttributeBag.Of(
///         new AttributeValue { DataType = "string", Value = "admin" }),
///     ResourceAttributes = AttributeBag.Of(
///         new AttributeValue { DataType = "string", Value = "financial-report" }),
///     EnvironmentAttributes = AttributeBag.Empty,
///     ActionAttributes = AttributeBag.Of(
///         new AttributeValue { DataType = "string", Value = "read" }),
///     RequestType = typeof(GetFinancialReportQuery)
/// };
/// </code>
/// </example>
public sealed record PolicyEvaluationContext
{
    /// <summary>
    /// Attributes describing the subject (user or service) making the access request.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.2 — Includes attributes such as user ID, roles, department,
    /// clearance level, and group memberships.
    /// </remarks>
    public required AttributeBag SubjectAttributes { get; init; }

    /// <summary>
    /// Attributes describing the resource being accessed.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.3 — Includes attributes such as resource type, classification,
    /// owner, and sensitivity label.
    /// </remarks>
    public required AttributeBag ResourceAttributes { get; init; }

    /// <summary>
    /// Attributes describing the current environmental conditions.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.5 — Includes attributes such as current time, IP address,
    /// business hours, and tenant ID.
    /// </remarks>
    public required AttributeBag EnvironmentAttributes { get; init; }

    /// <summary>
    /// Attributes describing the action being performed on the resource.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.4 — Includes attributes such as action name (read, write, delete),
    /// HTTP method, and operation type.
    /// </remarks>
    public required AttributeBag ActionAttributes { get; init; }

    /// <summary>
    /// The type of the Encina request being evaluated (e.g., the command or query type).
    /// </summary>
    /// <remarks>
    /// Used by the PEP to correlate the ABAC evaluation with the CQRS pipeline request.
    /// </remarks>
    public required Type RequestType { get; init; }

    /// <summary>
    /// Whether to include advice expressions in the decision response.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Set to <c>false</c> to exclude advice from the response
    /// for performance optimization when advice is not needed.
    /// </remarks>
    public bool IncludeAdvice { get; init; } = true;
}
