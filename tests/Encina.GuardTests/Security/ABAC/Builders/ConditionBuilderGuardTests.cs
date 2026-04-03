using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Builders;

/// <summary>
/// Guard clause tests for <see cref="ConditionBuilder"/>.
/// Covers null/empty parameter validation for all factory methods,
/// logical connectives, comparison operators, and data type inference.
/// </summary>
public class ConditionBuilderGuardTests
{
    #region Function — Guards

    [Fact]
    public void Function_NullFunctionId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Function(null!, ConditionBuilder.StringValue("x"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Function_EmptyFunctionId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Function("", ConditionBuilder.StringValue("x"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Function_WhitespaceFunctionId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Function("   ", ConditionBuilder.StringValue("x"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Function_NullArgs_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Function("string-equal", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Function_ValidParams_ReturnsApply()
    {
        var result = ConditionBuilder.Function("string-equal",
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        result.Should().NotBeNull();
        result.FunctionId.Should().Be("string-equal");
        result.Arguments.Should().HaveCount(2);
    }

    #endregion

    #region Attribute — Guards

    [Fact]
    public void Attribute_NullAttributeId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, null!, XACMLDataTypes.String);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_EmptyAttributeId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, "", XACMLDataTypes.String);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_WhitespaceAttributeId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, "   ", XACMLDataTypes.String);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_NullDataType_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, "department", null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_EmptyDataType_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, "department", "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_ValidParams_ReturnsDesignator()
    {
        var result = ConditionBuilder.Attribute(
            AttributeCategory.Subject, "department", XACMLDataTypes.String, mustBePresent: true);

        result.Category.Should().Be(AttributeCategory.Subject);
        result.AttributeId.Should().Be("department");
        result.DataType.Should().Be(XACMLDataTypes.String);
        result.MustBePresent.Should().BeTrue();
    }

    [Fact]
    public void Attribute_DefaultMustBePresent_IsFalse()
    {
        var result = ConditionBuilder.Attribute(
            AttributeCategory.Resource, "id", XACMLDataTypes.String);

        result.MustBePresent.Should().BeFalse();
    }

    #endregion

    #region Value — Guards

    [Fact]
    public void Value_NullDataType_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Value(null!, "test");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Value_EmptyDataType_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Value("", "test");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Value_NullValue_DoesNotThrow()
    {
        // XACML spec allows null values
        var act = () => ConditionBuilder.Value(XACMLDataTypes.String, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Value_ValidParams_ReturnsAttributeValue()
    {
        var result = ConditionBuilder.Value(XACMLDataTypes.Integer, 42);

        result.DataType.Should().Be(XACMLDataTypes.Integer);
        result.Value.Should().Be(42);
    }

    #endregion

    #region Variable — Guards

    [Fact]
    public void Variable_NullId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Variable(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Variable_EmptyId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Variable("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Variable_WhitespaceId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Variable("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Variable_ValidId_ReturnsVariableReference()
    {
        var result = ConditionBuilder.Variable("myVar");

        result.VariableId.Should().Be("myVar");
    }

    #endregion

    #region Typed Value Factories

    [Fact]
    public void StringValue_ReturnsCorrectType()
    {
        var result = ConditionBuilder.StringValue("hello");

        result.DataType.Should().Be(XACMLDataTypes.String);
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void IntValue_ReturnsCorrectType()
    {
        var result = ConditionBuilder.IntValue(42);

        result.DataType.Should().Be(XACMLDataTypes.Integer);
        result.Value.Should().Be(42);
    }

    [Fact]
    public void DoubleValue_ReturnsCorrectType()
    {
        var result = ConditionBuilder.DoubleValue(3.14);

        result.DataType.Should().Be(XACMLDataTypes.Double);
        result.Value.Should().Be(3.14);
    }

    [Fact]
    public void BoolValue_ReturnsCorrectType()
    {
        var result = ConditionBuilder.BoolValue(true);

        result.DataType.Should().Be(XACMLDataTypes.Boolean);
        result.Value.Should().Be(true);
    }

    [Fact]
    public void DateTimeValue_ReturnsCorrectType()
    {
        var now = DateTime.UtcNow;
        var result = ConditionBuilder.DateTimeValue(now);

        result.DataType.Should().Be(XACMLDataTypes.DateTime);
        result.Value.Should().Be(now);
    }

    [Fact]
    public void DateValue_ReturnsCorrectType()
    {
        var date = new DateOnly(2026, 4, 3);
        var result = ConditionBuilder.DateValue(date);

        result.DataType.Should().Be(XACMLDataTypes.Date);
        result.Value.Should().Be(date);
    }

    [Fact]
    public void TimeValue_ReturnsCorrectType()
    {
        var time = new TimeOnly(14, 30);
        var result = ConditionBuilder.TimeValue(time);

        result.DataType.Should().Be(XACMLDataTypes.Time);
        result.Value.Should().Be(time);
    }

    #endregion

    #region And — Guards

    [Fact]
    public void And_NullConditions_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.And(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void And_EmptyConditions_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.And([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void And_SingleCondition_ReturnsApplyWithAndFunction()
    {
        var condition = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        var result = ConditionBuilder.And(condition);

        result.FunctionId.Should().Be(XACMLFunctionIds.And);
        result.Arguments.Should().HaveCount(1);
    }

    [Fact]
    public void And_MultipleConditions_ReturnsApplyWithAllArgs()
    {
        var c1 = ConditionBuilder.Equal(ConditionBuilder.StringValue("a"), ConditionBuilder.StringValue("b"));
        var c2 = ConditionBuilder.Equal(ConditionBuilder.IntValue(1), ConditionBuilder.IntValue(2));

        var result = ConditionBuilder.And(c1, c2);

        result.FunctionId.Should().Be(XACMLFunctionIds.And);
        result.Arguments.Should().HaveCount(2);
    }

    #endregion

    #region Or — Guards

    [Fact]
    public void Or_NullConditions_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Or(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Or_EmptyConditions_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Or([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Or_ValidConditions_ReturnsApplyWithOrFunction()
    {
        var condition = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        var result = ConditionBuilder.Or(condition);

        result.FunctionId.Should().Be(XACMLFunctionIds.Or);
    }

    #endregion

    #region Not — Guards

    [Fact]
    public void Not_NullCondition_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Not(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Not_ValidCondition_ReturnsApplyWithNotFunction()
    {
        var condition = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        var result = ConditionBuilder.Not(condition);

        result.FunctionId.Should().Be(XACMLFunctionIds.Not);
        result.Arguments.Should().HaveCount(1);
    }

    #endregion

    #region Equal — Guards

    [Fact]
    public void Equal_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Equal(null!, ConditionBuilder.StringValue("b"));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equal_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Equal(ConditionBuilder.StringValue("a"), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equal_StringOperands_InfersStringEqual()
    {
        var result = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        result.FunctionId.Should().Contain("equal");
    }

    [Fact]
    public void Equal_IntegerOperands_InfersIntegerEqual()
    {
        var result = ConditionBuilder.Equal(
            ConditionBuilder.IntValue(1),
            ConditionBuilder.IntValue(2));

        result.FunctionId.Should().Contain("equal");
    }

    #endregion

    #region GreaterThan — Guards

    [Fact]
    public void GreaterThan_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.GreaterThan(null!, ConditionBuilder.IntValue(1));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GreaterThan_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.GreaterThan(ConditionBuilder.IntValue(1), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GreaterThan_ValidOperands_ReturnsApply()
    {
        var result = ConditionBuilder.GreaterThan(
            ConditionBuilder.IntValue(10),
            ConditionBuilder.IntValue(5));

        result.FunctionId.Should().Contain("greater-than");
    }

    #endregion

    #region LessThan — Guards

    [Fact]
    public void LessThan_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.LessThan(null!, ConditionBuilder.IntValue(1));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LessThan_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.LessThan(ConditionBuilder.IntValue(1), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region GreaterThanOrEqual — Guards

    [Fact]
    public void GreaterThanOrEqual_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.GreaterThanOrEqual(null!, ConditionBuilder.IntValue(1));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GreaterThanOrEqual_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.GreaterThanOrEqual(ConditionBuilder.IntValue(1), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region LessThanOrEqual — Guards

    [Fact]
    public void LessThanOrEqual_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.LessThanOrEqual(null!, ConditionBuilder.IntValue(1));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LessThanOrEqual_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.LessThanOrEqual(ConditionBuilder.IntValue(1), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region MapOperatorToFunctionId — Edge Cases

    [Fact]
    public void MapOperatorToFunctionId_Exists_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.Exists, XACMLDataTypes.String);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapOperatorToFunctionId_DoesNotExist_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.DoesNotExist, XACMLDataTypes.String);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapOperatorToFunctionId_InvalidOperator_ThrowsArgumentOutOfRangeException()
    {
        var act = () => ConditionBuilder.MapOperatorToFunctionId(
            (ConditionOperator)999, XACMLDataTypes.String);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MapOperatorToFunctionId_Equals_ReturnsEqualFunction()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.Equals, XACMLDataTypes.String);

        result.Should().Be("string-equal");
    }

    [Fact]
    public void MapOperatorToFunctionId_Contains_ReturnsStringContains()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.Contains, XACMLDataTypes.String);

        result.Should().Be(XACMLFunctionIds.StringContains);
    }

    [Fact]
    public void MapOperatorToFunctionId_StartsWith_ReturnsStringStartsWith()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.StartsWith, XACMLDataTypes.String);

        result.Should().Be(XACMLFunctionIds.StringStartsWith);
    }

    [Fact]
    public void MapOperatorToFunctionId_EndsWith_ReturnsStringEndsWith()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.EndsWith, XACMLDataTypes.String);

        result.Should().Be(XACMLFunctionIds.StringEndsWith);
    }

    [Fact]
    public void MapOperatorToFunctionId_RegexMatch_ReturnsRegexpMatch()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.RegexMatch, XACMLDataTypes.String);

        result.Should().Be(XACMLFunctionIds.StringRegexpMatch);
    }

    [Fact]
    public void MapOperatorToFunctionId_In_ReturnsIsInFunction()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.In, XACMLDataTypes.Integer);

        result.Should().Be("integer-is-in");
    }

    [Fact]
    public void MapOperatorToFunctionId_GreaterThan_IntegerType()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.GreaterThan, XACMLDataTypes.Integer);

        result.Should().Be("integer-greater-than");
    }

    [Fact]
    public void MapOperatorToFunctionId_LessThanOrEqual_DoubleType()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.LessThanOrEqual, XACMLDataTypes.Double);

        result.Should().Be("double-less-than-or-equal");
    }

    #endregion

    #region InferDataType — Runtime Type Mapping

    [Fact]
    public void InferDataType_String_ReturnsStringType()
    {
        ConditionBuilder.InferDataType("hello").Should().Be(XACMLDataTypes.String);
    }

    [Fact]
    public void InferDataType_Int_ReturnsIntegerType()
    {
        ConditionBuilder.InferDataType(42).Should().Be(XACMLDataTypes.Integer);
    }

    [Fact]
    public void InferDataType_Double_ReturnsDoubleType()
    {
        ConditionBuilder.InferDataType(3.14).Should().Be(XACMLDataTypes.Double);
    }

    [Fact]
    public void InferDataType_Bool_ReturnsBooleanType()
    {
        ConditionBuilder.InferDataType(true).Should().Be(XACMLDataTypes.Boolean);
    }

    [Fact]
    public void InferDataType_DateTime_ReturnsDateTimeType()
    {
        ConditionBuilder.InferDataType(DateTime.UtcNow).Should().Be(XACMLDataTypes.DateTime);
    }

    [Fact]
    public void InferDataType_DateOnly_ReturnsDateType()
    {
        ConditionBuilder.InferDataType(DateOnly.FromDateTime(DateTime.UtcNow)).Should().Be(XACMLDataTypes.Date);
    }

    [Fact]
    public void InferDataType_TimeOnly_ReturnsTimeType()
    {
        ConditionBuilder.InferDataType(TimeOnly.FromDateTime(DateTime.UtcNow)).Should().Be(XACMLDataTypes.Time);
    }

    [Fact]
    public void InferDataType_Null_ReturnsStringDefault()
    {
        ConditionBuilder.InferDataType(null).Should().Be(XACMLDataTypes.String);
    }

    [Fact]
    public void InferDataType_UnknownType_ReturnsStringDefault()
    {
        ConditionBuilder.InferDataType(Guid.NewGuid()).Should().Be(XACMLDataTypes.String);
    }

    #endregion

    #region InferDataTypeFromExpression — Expression Node Inference

    [Fact]
    public void InferDataTypeFromExpression_AttributeDesignator_ReturnsDataType()
    {
        var designator = ConditionBuilder.Attribute(
            AttributeCategory.Subject, "role", XACMLDataTypes.String);

        ConditionBuilder.InferDataTypeFromExpression(designator)
            .Should().Be(XACMLDataTypes.String);
    }

    [Fact]
    public void InferDataTypeFromExpression_AttributeValue_ReturnsDataType()
    {
        var value = ConditionBuilder.IntValue(42);

        ConditionBuilder.InferDataTypeFromExpression(value)
            .Should().Be(XACMLDataTypes.Integer);
    }

    [Fact]
    public void InferDataTypeFromExpression_Apply_ReturnsNull()
    {
        var apply = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        ConditionBuilder.InferDataTypeFromExpression(apply)
            .Should().BeNull();
    }

    [Fact]
    public void InferDataTypeFromExpression_VariableReference_ReturnsNull()
    {
        var varRef = ConditionBuilder.Variable("myVar");

        ConditionBuilder.InferDataTypeFromExpression(varRef)
            .Should().BeNull();
    }

    #endregion
}
