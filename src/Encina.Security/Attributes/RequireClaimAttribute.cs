namespace Encina.Security;

/// <summary>
/// Requires the user to have a specific claim, optionally with an exact value.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="ClaimValue"/> is <c>null</c>, only the existence of the claim type
/// is verified. When a value is specified, an exact match is required.
/// </para>
/// <para>
/// Claims are checked against <see cref="ISecurityContext.User"/> principal.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Verify claim type exists (any value)
/// [RequireClaim("department")]
/// public sealed record GetDepartmentReportQuery(string Dept) : IQuery&lt;ReportDto&gt;;
///
/// // Verify claim type with exact value
/// [RequireClaim("department", "finance")]
/// public sealed record GetFinanceReportQuery() : IQuery&lt;ReportDto&gt;;
/// </code>
/// </example>
public sealed class RequireClaimAttribute : SecurityAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireClaimAttribute"/> class
    /// that verifies the existence of a claim type.
    /// </summary>
    /// <param name="claimType">The claim type that must be present.</param>
    public RequireClaimAttribute(string claimType)
    {
        ClaimType = claimType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireClaimAttribute"/> class
    /// that verifies a claim type with an exact value.
    /// </summary>
    /// <param name="claimType">The claim type that must be present.</param>
    /// <param name="claimValue">The exact value the claim must have.</param>
    public RequireClaimAttribute(string claimType, string claimValue)
    {
        ClaimType = claimType;
        ClaimValue = claimValue;
    }

    /// <summary>
    /// Gets the required claim type.
    /// </summary>
    public string ClaimType { get; }

    /// <summary>
    /// Gets the required claim value, or <c>null</c> if only existence is checked.
    /// </summary>
    public string? ClaimValue { get; }
}
