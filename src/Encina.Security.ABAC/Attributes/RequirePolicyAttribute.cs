namespace Encina.Security.ABAC;

/// <summary>
/// Requires the request to be authorized by a specific ABAC policy.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request class, the ABAC pipeline behavior evaluates the policy set or
/// policy whose identifier is <see cref="PolicyName"/>, read from the policy store, against the
/// current subject, resource, action, and environment attributes. The named policies decide the
/// request on their own: the rest of the policy store is not evaluated.
/// </para>
/// <para>
/// A required policy passes only when it returns <see cref="Effect.Permit"/>. Deny and
/// NotApplicable do not pass (a policy that is required but does not apply cannot authorize);
/// Indeterminate is handled as an Indeterminate decision. A name that is neither a policy set nor a policy in the
/// store denies the request with <see cref="ABACErrors.PolicyNotFoundCode"/>. When a policy set
/// and a policy share the name, the policy set is evaluated.
/// </para>
/// <para>
/// Multiple <see cref="RequirePolicyAttribute"/> instances can be applied to a single request.
/// Every policy with <see cref="AllMustPass"/> = <c>true</c> (the default) must permit (AND); when
/// at least one attribute has <see cref="AllMustPass"/> = <c>false</c>, at least one of those
/// policies must permit (OR); with both kinds present, both groups must hold. The obligations and
/// advice of the policies that decided the outcome are combined and executed by the pipeline.
/// </para>
/// <para>
/// When the request also carries <see cref="RequireConditionAttribute"/>, the policies and the
/// conditions combine with AND: the request proceeds only when every requirement passes.
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
    /// <param name="policyName">The identifier of the ABAC policy set or policy to evaluate.</param>
    public RequirePolicyAttribute(string policyName)
    {
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the name of the ABAC policy to evaluate.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// Gets or sets whether this policy belongs to the group in which every policy must permit
    /// (AND logic) or to the group in which one permitting policy suffices (OR logic).
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c> (AND logic — this policy must permit). With both groups present on a
    /// request, both must hold.
    /// </remarks>
    public bool AllMustPass { get; set; } = true;
}
