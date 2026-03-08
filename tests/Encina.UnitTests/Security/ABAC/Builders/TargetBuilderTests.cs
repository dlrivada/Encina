using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.Builders;

/// <summary>
/// Unit tests for <see cref="TargetBuilder"/>, <see cref="AnyOfBuilder"/>, and <see cref="AllOfBuilder"/>.
/// Verifies XACML 3.0 §7.6: Target → AnyOf (AND) → AllOf (OR) → Match structure.
/// </summary>
public sealed class TargetBuilderTests
{
    #region TargetBuilder

    [Fact]
    public void Build_Empty_ReturnsTargetWithNoAnyOfElements()
    {
        var target = new TargetBuilder().Build();

        target.AnyOfElements.Should().BeEmpty(
            "Empty target matches all requests per XACML §7.6");
    }

    [Fact]
    public void AnyOf_SingleElement_AddsToList()
    {
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "role",
                        ConditionOperator.Equals, "admin")))
            .Build();

        target.AnyOfElements.Should().HaveCount(1);
    }

    [Fact]
    public void AnyOf_MultipleElements_AllAdded()
    {
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "department",
                        ConditionOperator.Equals, "Finance")))
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Resource, "type",
                        ConditionOperator.Equals, "Report")))
            .Build();

        target.AnyOfElements.Should().HaveCount(2,
            "Multiple AnyOf elements are combined with AND logic");
    }

    [Fact]
    public void AnyOf_NullConfigure_ThrowsArgumentNullException()
    {
        var act = () => new TargetBuilder().AnyOf(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region AnyOfBuilder

    [Fact]
    public void AllOf_SingleElement_CreatesAnyOfWithOneAllOf()
    {
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "role",
                        ConditionOperator.Equals, "admin")))
            .Build();

        target.AnyOfElements[0].AllOfElements.Should().HaveCount(1);
    }

    [Fact]
    public void AllOf_MultipleElements_AllAdded()
    {
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "role",
                        ConditionOperator.Equals, "admin"))
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "role",
                        ConditionOperator.Equals, "manager")))
            .Build();

        target.AnyOfElements[0].AllOfElements.Should().HaveCount(2,
            "Multiple AllOf elements within AnyOf are combined with OR logic");
    }

    #endregion

    #region AllOfBuilder — MatchAttribute

    [Fact]
    public void MatchAttribute_CreatesMatch()
    {
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "department",
                        ConditionOperator.Equals, "Engineering")))
            .Build();

        var match = target.AnyOfElements[0].AllOfElements[0].Matches[0];
        match.AttributeDesignator.Category.Should().Be(AttributeCategory.Subject);
        match.AttributeDesignator.AttributeId.Should().Be("department");
    }

    [Fact]
    public void MatchAttribute_MultipleMatches_AllAdded()
    {
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "department",
                        ConditionOperator.Equals, "Engineering")
                    .MatchAttribute(AttributeCategory.Action, "name",
                        ConditionOperator.Equals, "read")))
            .Build();

        target.AnyOfElements[0].AllOfElements[0].Matches.Should().HaveCount(2,
            "Multiple matches within AllOf are combined with AND logic");
    }

    [Fact]
    public void MatchAttribute_NullAttributeId_ThrowsArgumentException()
    {
        var act = () => new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, null!,
                        ConditionOperator.Equals, "admin")));

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AllOfBuilder — Match (explicit function)

    [Fact]
    public void Match_WithExplicitFunctionId_SetsFunction()
    {
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .Match(
                        AttributeCategory.Resource,
                        "classification",
                        XACMLDataTypes.String,
                        ConditionOperator.Equals,
                        "Secret")))
            .Build();

        var match = target.AnyOfElements[0].AllOfElements[0].Matches[0];
        match.FunctionId.Should().NotBeNullOrEmpty();
        match.AttributeValue.Value.Should().Be("Secret");
    }

    [Fact]
    public void Match_WithDesignatorAndValue_SetsAllFields()
    {
        var designator = new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "clearance",
            DataType = XACMLDataTypes.Integer
        };
        var value = new AttributeValue
        {
            DataType = XACMLDataTypes.Integer,
            Value = 5
        };

        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .Match("integer-greater-than-or-equal", designator, value)))
            .Build();

        var match = target.AnyOfElements[0].AllOfElements[0].Matches[0];
        match.FunctionId.Should().Be("integer-greater-than-or-equal");
        match.AttributeDesignator.Should().BeSameAs(designator);
        match.AttributeValue.Should().BeSameAs(value);
    }

    #endregion

    #region Complex Target Structures

    [Fact]
    public void ComplexTarget_SubjectAndResourceAndAction()
    {
        // Subject=Finance AND Resource=Report AND Action=read
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "department",
                        ConditionOperator.Equals, "Finance")))
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Resource, "type",
                        ConditionOperator.Equals, "Report")))
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Action, "name",
                        ConditionOperator.Equals, "read")))
            .Build();

        target.AnyOfElements.Should().HaveCount(3);
    }

    [Fact]
    public void ComplexTarget_OrLogicWithMultipleAllOfInAnyOf()
    {
        // Subject is Finance OR Engineering
        var target = new TargetBuilder()
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "department",
                        ConditionOperator.Equals, "Finance"))
                .AllOf(all => all
                    .MatchAttribute(AttributeCategory.Subject, "department",
                        ConditionOperator.Equals, "Engineering")))
            .Build();

        target.AnyOfElements.Should().HaveCount(1);
        target.AnyOfElements[0].AllOfElements.Should().HaveCount(2);
    }

    #endregion
}
