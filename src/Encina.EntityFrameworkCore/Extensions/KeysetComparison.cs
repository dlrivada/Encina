using System.Linq.Expressions;
using System.Reflection;

namespace Encina.EntityFrameworkCore.Extensions;

/// <summary>
/// Builds the comparison between a keyset member and the last key, for the keyset filters of the specification
/// evaluator and of cursor pagination.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="string"/> and <see cref="Guid"/> define no comparison operators, so <see cref="Expression.GreaterThan(Expression, Expression)"/>
/// and <see cref="Expression.LessThan(Expression, Expression)"/> throw for them. They compare through
/// <see cref="string.Compare(string, string)"/> and <see cref="Guid.CompareTo(Guid)"/> against zero instead, which
/// EF Core translates to the database's own comparison: the same one its <c>ORDER BY</c> uses, so the filter and
/// the ordering agree.
/// </para>
/// <para>
/// Every other key type keeps the comparison operators.
/// </para>
/// </remarks>
internal static class KeysetComparison
{
    private static readonly MethodInfo StringCompare =
        typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

    private static readonly MethodInfo GuidCompareTo =
        typeof(Guid).GetMethod(nameof(Guid.CompareTo), [typeof(Guid)])!;

    private static readonly ConstantExpression Zero = Expression.Constant(0);

    /// <summary>
    /// Builds <c>key &gt; lastKey</c>, or <c>key &lt; lastKey</c> when <paramref name="greaterThan"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="key">The keyset member of the entity.</param>
    /// <param name="lastKey">The last key, of the same type as <paramref name="key"/>.</param>
    /// <param name="greaterThan">Whether the rows after the last key hold the greater keys.</param>
    /// <returns>The comparison expression.</returns>
    internal static Expression After(Expression key, Expression lastKey, bool greaterThan)
    {
        var (left, right) = Operands(key, lastKey);
        return greaterThan ? Expression.GreaterThan(left, right) : Expression.LessThan(left, right);
    }

    // crap-exempt: single-question switch — which operands a key type is compared through
    private static (Expression Left, Expression Right) Operands(Expression key, Expression lastKey) => key.Type switch
    {
        var type when type == typeof(string) => (Expression.Call(StringCompare, key, lastKey), Zero),
        var type when type == typeof(Guid) => (Expression.Call(key, GuidCompareTo, lastKey), Zero),
        _ => (key, lastKey),
    };
}
