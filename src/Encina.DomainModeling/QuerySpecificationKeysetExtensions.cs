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
        /// A single-column keyset filter is correct only when the keyset property is the primary ordering
        /// (<see cref="IQuerySpecification{T}.OrderBy"/> or <see cref="IQuerySpecification{T}.OrderByDescending"/>),
        /// matched by member path. With any other ordering first, or the keyset property only in a
        /// <c>ThenBy</c> or <c>ThenByDescending</c>, the next page would skip or repeat rows, so this throws
        /// <see cref="NotSupportedException"/> instead of picking a direction. A specification with no
        /// ordering at all pages in ascending order.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// ApplyOrderByDescending(o =&gt; o.Id);
        /// ApplyKeysetPagination(o =&gt; o.Id, lastId, 10); // IsKeysetDescending is true: next page is Id &lt; lastId
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Thrown when the specification is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when the specification is ordered and the keyset property is not its primary ordering.
        /// </exception>
        public bool IsKeysetDescending
        {
            get
            {
                ArgumentNullException.ThrowIfNull(specification);

                var keyset = MemberPath(specification.KeysetProperty);
                if (keyset is null || !IsOrdered(specification))
                {
                    return false;
                }

                if (MemberPath(specification.OrderBy ?? specification.OrderByDescending) != keyset)
                {
                    throw new NotSupportedException(
                        $"Keyset pagination on {keyset} needs {keyset} as the primary ordering (ApplyOrderBy or ApplyOrderByDescending) " +
                        $"of {specification.GetType().Name}; ordered any other way, the next page would skip or repeat rows.");
                }

                return specification.OrderByDescending is not null;
            }
        }
    }

    private static bool IsOrdered<T>(IQuerySpecification<T> specification)
        => specification.OrderBy is not null
            || specification.OrderByDescending is not null
            || specification.ThenByExpressions.Count > 0
            || specification.ThenByDescendingExpressions.Count > 0;

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
