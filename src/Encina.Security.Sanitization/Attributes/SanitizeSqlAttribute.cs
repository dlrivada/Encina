namespace Encina.Security.Sanitization.Attributes;

/// <summary>
/// Marks a property for automatic SQL context sanitization.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a string property, the <c>InputSanitizationPipelineBehavior</c>
/// will sanitize the value for safe use in SQL contexts by escaping single quotes,
/// removing SQL comment sequences, and neutralizing common SQL injection patterns.
/// </para>
/// <para>
/// <b>Important:</b> Parameterized queries are always the preferred defense against
/// SQL injection. This attribute provides defense-in-depth for scenarios where
/// parameterization is not possible (e.g., dynamic column names, ORDER BY clauses).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record SearchProductsQuery(
///     [property: SanitizeSql] string SearchTerm,
///     [property: SanitizeSql] string SortColumn
/// ) : IQuery&lt;IReadOnlyList&lt;ProductDto&gt;&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SanitizeSqlAttribute : SanitizationAttribute
{
    /// <inheritdoc />
    public override SanitizationType SanitizationType => SanitizationType.Sql;
}
