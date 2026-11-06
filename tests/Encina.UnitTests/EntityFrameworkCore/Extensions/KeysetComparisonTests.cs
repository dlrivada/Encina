using System.Linq.Expressions;
using Encina.EntityFrameworkCore.Extensions;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Extensions;

/// <summary>
/// Unit tests for <see cref="KeysetComparison"/>: <see cref="string"/> keys, which have no comparison operators,
/// compare through <see cref="string.Compare(string, string)"/> against zero; other keys, <see cref="Guid"/>
/// included, keep the operators; a nullable string member is rejected (#3201).
/// </summary>
[Trait("Category", "Unit")]
public sealed class KeysetComparisonTests
{
    private static readonly Guid FirstGuid = new(1, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);
    private static readonly Guid SecondGuid = new(2, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);
    private static readonly Guid ThirdGuid = new(3, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);

    [Theory]
    [InlineData(true, ExpressionType.GreaterThan)]
    [InlineData(false, ExpressionType.LessThan)]
    public void After_StringKey_ComparesStringCompareWithZero(bool greaterThan, ExpressionType expectedNode)
    {
        // Arrange
        var key = Expression.Parameter(typeof(string), "key");

        // Act
        var comparison = (BinaryExpression)KeysetComparison.After(key, Expression.Constant("b"), greaterThan);

        // Assert
        comparison.NodeType.ShouldBe(expectedNode);
        var call = comparison.Left.ShouldBeAssignableTo<MethodCallExpression>()!;
        call.Method.ShouldBe(typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)]));
        call.Arguments[0].ShouldBe(key);
        comparison.Right.ShouldBeAssignableTo<ConstantExpression>()!.Value.ShouldBe(0);
    }

    [Theory]
    [InlineData(true, ExpressionType.GreaterThan)]
    [InlineData(false, ExpressionType.LessThan)]
    public void After_GuidKey_KeepsItsComparisonOperator(bool greaterThan, ExpressionType expectedNode)
    {
        // Arrange - Guid has comparison operators, which EF Core translates
        var key = Expression.Parameter(typeof(Guid), "key");
        var lastKey = Expression.Constant(SecondGuid);

        // Act
        var comparison = (BinaryExpression)KeysetComparison.After(key, lastKey, greaterThan);

        // Assert
        comparison.NodeType.ShouldBe(expectedNode);
        comparison.Left.ShouldBe(key);
        comparison.Right.ShouldBe(lastKey);
    }

    [Theory]
    [InlineData(true, ExpressionType.GreaterThan)]
    [InlineData(false, ExpressionType.LessThan)]
    public void After_KeyWithComparisonOperators_KeepsTheOperator(bool greaterThan, ExpressionType expectedNode)
    {
        // Arrange
        var key = Expression.Parameter(typeof(int), "key");
        var lastKey = Expression.Constant(5);

        // Act
        var comparison = (BinaryExpression)KeysetComparison.After(key, lastKey, greaterThan);

        // Assert
        comparison.NodeType.ShouldBe(expectedNode);
        comparison.Left.ShouldBe(key);
        comparison.Right.ShouldBe(lastKey);
    }

    [Fact]
    public void After_StringKey_SelectsTheKeysAfterTheLastOneInEitherDirection()
    {
        // Arrange
        var above = Compile<string>("b", greaterThan: true);
        var below = Compile<string>("b", greaterThan: false);

        // Act & Assert
        above("c").ShouldBeTrue();
        above("b").ShouldBeFalse();
        above("a").ShouldBeFalse();
        below("a").ShouldBeTrue();
        below("b").ShouldBeFalse();
        below("c").ShouldBeFalse();
    }

    [Fact]
    public void After_GuidKey_SelectsTheKeysAfterTheLastOneInEitherDirection()
    {
        // Arrange
        var above = Compile(SecondGuid, greaterThan: true);
        var below = Compile(SecondGuid, greaterThan: false);

        // Act & Assert
        above(ThirdGuid).ShouldBeTrue();
        above(SecondGuid).ShouldBeFalse();
        above(FirstGuid).ShouldBeFalse();
        below(FirstGuid).ShouldBeTrue();
        below(SecondGuid).ShouldBeFalse();
        below(ThirdGuid).ShouldBeFalse();
    }

    [Fact]
    public void After_NullableStringMember_ThrowsNotSupportedException()
    {
        // Arrange - NULL keys skip or repeat rows, differently per database
        var row = Expression.Parameter(typeof(Row), "row");
        var key = Expression.Property(row, nameof(Row.Nickname));

        // Act & Assert
        Should.Throw<NotSupportedException>(() => KeysetComparison.After(key, Expression.Constant("b"), greaterThan: true))
            .Message.ShouldContain("Row.Nickname");
    }

    [Fact]
    public void After_NullableStringField_ThrowsNotSupportedException()
    {
        // Arrange - a field keyset member gets the same check as a property
        var row = Expression.Parameter(typeof(Row), "row");
        var key = Expression.Field(row, nameof(Row.NicknameField));

        // Act & Assert
        Should.Throw<NotSupportedException>(() => KeysetComparison.After(key, Expression.Constant("b"), greaterThan: false));
    }

    [Fact]
    public void After_NonNullableStringMember_ComparesThroughStringCompare()
    {
        // Arrange
        var row = Expression.Parameter(typeof(Row), "row");
        var key = Expression.Property(row, nameof(Row.Name));

        // Act
        var comparison = (BinaryExpression)KeysetComparison.After(key, Expression.Constant("b"), greaterThan: true);

        // Assert
        comparison.Left.ShouldBeAssignableTo<MethodCallExpression>()!.Arguments[0].ShouldBe(key);
    }

    [Fact]
    public void Equal_StringKey_MatchesCanonicallyEquivalentStringsLikeTheOrdering()
    {
        // Arrange - a composed and a decomposed e-acute sort as equal, so they must also match as equal
        var equal = CompileEqual<string?>("\u00e9");

        // Act & Assert
        equal("\u00e9").ShouldBeTrue();
        equal("e\u0301").ShouldBeTrue();
        equal("e").ShouldBeFalse();
    }

    [Fact]
    public void Equal_StringKey_StillMatchesNull()
    {
        // Arrange
        var equal = CompileEqual<string?>(null);

        // Act & Assert
        equal(null).ShouldBeTrue();
        equal("a").ShouldBeFalse();
    }

    [Fact]
    public void Equal_KeyWithEqualityOperator_KeepsTheOperator()
    {
        // Arrange
        var key = Expression.Parameter(typeof(int), "key");
        var lastKey = Expression.Constant(5);

        // Act
        var equality = (BinaryExpression)KeysetComparison.Equal(key, lastKey);

        // Assert
        equality.NodeType.ShouldBe(ExpressionType.Equal);
        equality.Left.ShouldBe(key);
    }

    private static Func<T, bool> CompileEqual<T>(T lastKey)
    {
        var key = Expression.Parameter(typeof(T), "key");
        return Expression.Lambda<Func<T, bool>>(KeysetComparison.Equal(key, Expression.Constant(lastKey, typeof(T))), key).Compile();
    }

    private static Func<T, bool> Compile<T>(T lastKey, bool greaterThan)
    {
        var key = Expression.Parameter(typeof(T), "key");
        var comparison = KeysetComparison.After(key, Expression.Constant(lastKey, typeof(T)), greaterThan);
        return Expression.Lambda<Func<T, bool>>(comparison, key).Compile();
    }

    private sealed class Row
    {
        public string Name { get; set; } = string.Empty;

        public string? Nickname { get; set; }

        public string? NicknameField = "nickname";
    }
}
