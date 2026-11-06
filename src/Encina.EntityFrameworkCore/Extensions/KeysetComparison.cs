using System.Linq.Expressions;
using System.Reflection;

namespace Encina.EntityFrameworkCore.Extensions;

/// <summary>
/// Builds the comparisons between a keyset member and the last key, for the keyset filters of the specification
/// evaluator and of cursor pagination.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="string"/> defines no comparison operators, so <see cref="Expression.GreaterThan(Expression, Expression)"/>
/// and <see cref="Expression.LessThan(Expression, Expression)"/> throw for it. A string key compares through
/// <see cref="string.Compare(string, string)"/> against zero instead, which EF Core translates to the database's own
/// comparison: the same one, under the column's collation, that its <c>ORDER BY</c> uses, so the filter and the
/// ordering agree. Every other key type, <see cref="Guid"/> included, keeps the comparison operators.
/// </para>
/// <para>
/// A string key must be declared non-nullable. Databases order <c>NULL</c> keys differently (SQL Server and MySQL
/// first in ascending order, PostgreSQL last) and a <c>NULL</c> key never satisfies the comparison, so pages over a
/// nullable string key would skip the <c>NULL</c> rows or repeat them.
/// </para>
/// </remarks>
internal static class KeysetComparison
{
    private static readonly MethodInfo StringCompare =
        typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

    private static readonly ConstantExpression Zero = Expression.Constant(0);

    /// <summary>
    /// Builds <c>key &gt; lastKey</c>, or <c>key &lt; lastKey</c> when <paramref name="greaterThan"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="key">The keyset member of the entity.</param>
    /// <param name="lastKey">The last key, of the same type as <paramref name="key"/>.</param>
    /// <param name="greaterThan">Whether the rows after the last key hold the greater keys.</param>
    /// <returns>The comparison expression.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="key"/> is a string member declared nullable.</exception>
    internal static Expression After(Expression key, Expression lastKey, bool greaterThan)
    {
        if (key.Type != typeof(string))
        {
            return greaterThan ? Expression.GreaterThan(key, lastKey) : Expression.LessThan(key, lastKey);
        }

        EnsureNotNullable(key);
        var compare = Expression.Call(StringCompare, key, lastKey);
        return greaterThan ? Expression.GreaterThan(compare, Zero) : Expression.LessThan(compare, Zero);
    }

    /// <summary>
    /// Builds <c>key == lastKey</c> for an earlier member of a composite key, under the same string comparison
    /// as <see cref="After"/>.
    /// </summary>
    /// <remarks>
    /// A string key also matches when <see cref="string.Compare(string, string)"/> is zero. In memory that is the
    /// culture comparison the ordering uses, under which canonically equivalent strings (such as a composed and a
    /// decomposed "é") are equal although <c>==</c> is ordinal; a relational database translates both to its
    /// collation. The <c>==</c> stays, so a <c>NULL</c> key still matches.
    /// </remarks>
    /// <param name="key">The keyset member of the entity.</param>
    /// <param name="lastKey">The last key, of the same type as <paramref name="key"/>.</param>
    /// <returns>The equality expression.</returns>
    internal static Expression Equal(Expression key, Expression lastKey)
        => key.Type == typeof(string)
            ? Expression.OrElse(Expression.Equal(key, lastKey), Expression.Equal(Expression.Call(StringCompare, key, lastKey), Zero))
            : Expression.Equal(key, lastKey);

    private static void EnsureNotNullable(Expression key)
    {
        if (key is MemberExpression { Member: var member } && IsDeclaredNullable(member))
        {
            throw new NotSupportedException(
                $"Keyset pagination on {member.DeclaringType?.Name}.{member.Name} needs a string key declared non-nullable: " +
                "databases order NULL keys differently and a NULL key never satisfies the keyset comparison, so pages would skip or repeat rows.");
        }
    }

    // crap-exempt: single-question switch — whether a property or field is declared nullable
    private static bool IsDeclaredNullable(MemberInfo member) => member switch
    {
        PropertyInfo property => new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable,
        FieldInfo field => new NullabilityInfoContext().Create(field).ReadState == NullabilityState.Nullable,
        _ => false,
    };
}
