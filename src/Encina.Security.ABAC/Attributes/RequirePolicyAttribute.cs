namespace Encina.Security.ABAC;

/// <summary>
/// Requires the request to be authorized by a specific ABAC policy.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request class, the ABAC pipeline behavior evaluates the
/// named policy against the current subject, resource, action, and environment
/// attributes. If the policy denies access, the request is rejected.
/// </para>
/// <para>
/// Multiple <see cref="RequirePolicyAttribute"/> instances can be applied to a
/// single request. The <see cref="AllMustPass"/> property controls whether all
/// policies must permit (AND logic, the default) or any single policy suffices
/// (OR logic).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Single policy — must permit
/// [RequirePolicy("financial-data-access")]
/// public sealed record GetFinancialReportQuery(Guid ReportId) : IQuery&lt;ReportDto&gt;;
///
/// // Multiple policies — all must permit (default AND logic)
/// [RequirePolicy("data-classification")]
/// [RequirePolicy("department-access")]
/// public sealed record GetClassifiedDocumentQuery(Guid DocumentId) : IQuery&lt;DocumentDto&gt;;
///
/// // Multiple policies — any single policy permitting suffices (OR logic)
/// [RequirePolicy("admin-override", AllMustPass = false)]
/// [RequirePolicy("standard-access", AllMustPass = false)]
/// public sealed record GetResourceQuery(Guid ResourceId) : IQuery&lt;ResourceDto&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequirePolicyAttribute : SecurityAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePolicyAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The name of the ABAC policy to evaluate.</param>
    public RequirePolicyAttribute(string policyName)
    {
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the name of the ABAC policy to evaluate.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// Gets or sets whether all applied policies must permit access (AND logic)
    /// or any single policy permitting suffices (OR logic).
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c> (AND logic — all policies must permit).
    /// </remarks>
    public bool AllMustPass { get; set; } = true;
}
