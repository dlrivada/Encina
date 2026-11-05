using System.Linq.Expressions;
using Encina.EntityFrameworkCore.Extensions;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Extensions;

/// <summary>
/// Unit tests for <see cref="KeysetComparison"/>: <see cref="string"/> and <see cref="Guid"/> keys, which have no
/// comparison operators, compare through <see cref="string.Compare(string, string)"/> and
/// <see cref="Guid.CompareTo(Guid)"/> against zero; other keys keep the operators (#3201).
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
    public void After_GuidKey_ComparesGuidCompareToWithZero(bool greaterThan, ExpressionType expectedNode)
    {
        // Arrange
        var key = Expression.Parameter(typeof(Guid), "key");

        // Act
        var comparison = (BinaryExpression)KeysetComparison.After(key, Expression.Constant(SecondGuid), greaterThan);

        // Assert
        comparison.NodeType.ShouldBe(expectedNode);
        var call = comparison.Left.ShouldBeAssignableTo<MethodCallExpression>()!;
        call.Method.ShouldBe(typeof(Guid).GetMethod(nameof(Guid.CompareTo), [typeof(Guid)]));
        call.Object.ShouldBe(key);
        comparison.Right.ShouldBeAssignableTo<ConstantExpression>()!.Value.ShouldBe(0);
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

    private static Func<T, bool> Compile<T>(T lastKey, bool greaterThan)
    {
        var key = Expression.Parameter(typeof(T), "key");
        var comparison = KeysetComparison.After(key, Expression.Constant(lastKey, typeof(T)), greaterThan);
        return Expression.Lambda<Func<T, bool>>(comparison, key).Compile();
    }
}
