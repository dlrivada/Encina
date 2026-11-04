using System.Linq.Expressions;

namespace Encina.DomainModeling;

/// <summary>
/// Keyset pagination helpers that every repository provider shares, so they page in the same direction.
/// </summary>
public static class QuerySpecificationKeysetExtensions
{
    extension<T>(IQuerySpecification<T> specification)
    {
        /// <summary>
        /// Gets whether the keyset property is sorted in descending order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// With a descending keyset property the next page holds the keys below
        /// <see cref="IQuerySpecification{T}.LastKeyValue"/>, so the keyset filter compares with <c>&lt;</c>;
        /// otherwise it holds the keys above it and compares with <c>&gt;</c>.
        /// </para>
        /// <para>
        /// The keyset property is looked up in the primary ordering, then in
        /// <see cref="IQuerySpecification{T}.ThenByExpressions"/> and
        /// <see cref="IQuerySpecification{T}.ThenByDescendingExpressions"/>, by its member path.
        /// A keyset property that is not part of the ordering pages in ascending order.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// ApplyOrderByDescending(o =&gt; o.Id);
        /// ApplyKeysetPagination(o =&gt; o.Id, lastId, 10); // IsKeysetDescending is true: next page is Id &lt; lastId
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Thrown when the specification is <see langword="null"/>.</exception>
        public bool IsKeysetDescending
        {
            get
            {
                ArgumentNullException.ThrowIfNull(specification);

                var keyset = MemberPath(specification.KeysetProperty);
                if (keyset is null || MemberPath(specification.OrderBy) == keyset)
                {
                    return false;
                }

                return MemberPath(specification.OrderByDescending) == keyset
                    || (!specification.ThenByExpressions.Any(e => MemberPath(e) == keyset)
                        && specification.ThenByDescendingExpressions.Any(e => MemberPath(e) == keyset));
            }
        }
    }

    // crap-exempt: single-question switch — the member path an ordering or keyset selector reads, if it reads one
    private static string? MemberPath(Expression? expression) => expression switch
    {
        LambdaExpression lambda => MemberPath(lambda.Body),
        UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert => MemberPath(convert.Operand),
        MemberExpression { Expression: ParameterExpression } member => member.Member.Name,
        MemberExpression { Expression: { } inner } member when MemberPath(inner) is { } parent => $"{parent}.{member.Member.Name}",
        _ => null,
    };
}
