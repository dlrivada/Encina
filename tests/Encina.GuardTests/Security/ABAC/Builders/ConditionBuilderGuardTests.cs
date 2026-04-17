using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;

using Shouldly;

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

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Function_EmptyFunctionId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Function("", ConditionBuilder.StringValue("x"));

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Function_WhitespaceFunctionId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Function("   ", ConditionBuilder.StringValue("x"));

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Function_NullArgs_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Function("string-equal", null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Function_ValidParams_ReturnsApply()
    {
        var result = ConditionBuilder.Function("string-equal",
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        result.ShouldNotBeNull();
        result.FunctionId.ShouldBe("string-equal");
        result.Arguments.Count.ShouldBe(2);
    }

    #endregion

    #region Attribute — Guards

    [Fact]
    public void Attribute_NullAttributeId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, null!, XACMLDataTypes.String);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Attribute_EmptyAttributeId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, "", XACMLDataTypes.String);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Attribute_WhitespaceAttributeId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, "   ", XACMLDataTypes.String);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Attribute_NullDataType_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, "department", null!);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Attribute_EmptyDataType_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Attribute(
            AttributeCategory.Subject, "department", "");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Attribute_ValidParams_ReturnsDesignator()
    {
        var result = ConditionBuilder.Attribute(
            AttributeCategory.Subject, "department", XACMLDataTypes.String, mustBePresent: true);

        result.Category.ShouldBe(AttributeCategory.Subject);
        result.AttributeId.ShouldBe("department");
        result.DataType.ShouldBe(XACMLDataTypes.String);
        result.MustBePresent.ShouldBeTrue();
    }

    [Fact]
    public void Attribute_DefaultMustBePresent_IsFalse()
    {
        var result = ConditionBuilder.Attribute(
            AttributeCategory.Resource, "id", XACMLDataTypes.String);

        result.MustBePresent.ShouldBeFalse();
    }

    #endregion

    #region Value — Guards

    [Fact]
    public void Value_NullDataType_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Value(null!, "test");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Value_EmptyDataType_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Value("", "test");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Value_NullValue_DoesNotThrow()
    {
        // XACML spec allows null values
        var act = () => ConditionBuilder.Value(XACMLDataTypes.String, null);

        Should.NotThrow(act);
    }

    [Fact]
    public void Value_ValidParams_ReturnsAttributeValue()
    {
        var result = ConditionBuilder.Value(XACMLDataTypes.Integer, 42);

        result.DataType.ShouldBe(XACMLDataTypes.Integer);
        result.Value.ShouldBe(42);
    }

    #endregion

    #region Variable — Guards

    [Fact]
    public void Variable_NullId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Variable(null!);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Variable_EmptyId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Variable("");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Variable_WhitespaceId_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Variable("   ");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Variable_ValidId_ReturnsVariableReference()
    {
        var result = ConditionBuilder.Variable("myVar");

        result.VariableId.ShouldBe("myVar");
    }

    #endregion

    #region Typed Value Factories

    [Fact]
    public void StringValue_ReturnsCorrectType()
    {
        var result = ConditionBuilder.StringValue("hello");

        result.DataType.ShouldBe(XACMLDataTypes.String);
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void IntValue_ReturnsCorrectType()
    {
        var result = ConditionBuilder.IntValue(42);

        result.DataType.ShouldBe(XACMLDataTypes.Integer);
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void DoubleValue_ReturnsCorrectType()
    {
        var result = ConditionBuilder.DoubleValue(3.14);

        result.DataType.ShouldBe(XACMLDataTypes.Double);
        result.Value.ShouldBe(3.14);
    }

    [Fact]
    public void BoolValue_ReturnsCorrectType()
    {
        var result = ConditionBuilder.BoolValue(true);

        result.DataType.ShouldBe(XACMLDataTypes.Boolean);
        result.Value.ShouldBe(true);
    }

    [Fact]
    public void DateTimeValue_ReturnsCorrectType()
    {
        var now = DateTime.UtcNow;
        var result = ConditionBuilder.DateTimeValue(now);

        result.DataType.ShouldBe(XACMLDataTypes.DateTime);
        result.Value.ShouldBe(now);
    }

    [Fact]
    public void DateValue_ReturnsCorrectType()
    {
        var date = new DateOnly(2026, 4, 3);
        var result = ConditionBuilder.DateValue(date);

        result.DataType.ShouldBe(XACMLDataTypes.Date);
        result.Value.ShouldBe(date);
    }

    [Fact]
    public void TimeValue_ReturnsCorrectType()
    {
        var time = new TimeOnly(14, 30);
        var result = ConditionBuilder.TimeValue(time);

        result.DataType.ShouldBe(XACMLDataTypes.Time);
        result.Value.ShouldBe(time);
    }

    #endregion

    #region And — Guards

    [Fact]
    public void And_NullConditions_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.And(null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void And_EmptyConditions_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.And([]);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void And_SingleCondition_ReturnsApplyWithAndFunction()
    {
        var condition = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        var result = ConditionBuilder.And(condition);

        result.FunctionId.ShouldBe(XACMLFunctionIds.And);
        result.Arguments.Count.ShouldBe(1);
    }

    [Fact]
    public void And_MultipleConditions_ReturnsApplyWithAllArgs()
    {
        var c1 = ConditionBuilder.Equal(ConditionBuilder.StringValue("a"), ConditionBuilder.StringValue("b"));
        var c2 = ConditionBuilder.Equal(ConditionBuilder.IntValue(1), ConditionBuilder.IntValue(2));

        var result = ConditionBuilder.And(c1, c2);

        result.FunctionId.ShouldBe(XACMLFunctionIds.And);
        result.Arguments.Count.ShouldBe(2);
    }

    #endregion

    #region Or — Guards

    [Fact]
    public void Or_NullConditions_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Or(null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Or_EmptyConditions_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.Or([]);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Or_ValidConditions_ReturnsApplyWithOrFunction()
    {
        var condition = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        var result = ConditionBuilder.Or(condition);

        result.FunctionId.ShouldBe(XACMLFunctionIds.Or);
    }

    #endregion

    #region Not — Guards

    [Fact]
    public void Not_NullCondition_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Not(null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Not_ValidCondition_ReturnsApplyWithNotFunction()
    {
        var condition = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        var result = ConditionBuilder.Not(condition);

        result.FunctionId.ShouldBe(XACMLFunctionIds.Not);
        result.Arguments.Count.ShouldBe(1);
    }

    #endregion

    #region Equal — Guards

    [Fact]
    public void Equal_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Equal(null!, ConditionBuilder.StringValue("b"));

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Equal_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.Equal(ConditionBuilder.StringValue("a"), null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Equal_StringOperands_InfersStringEqual()
    {
        var result = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        result.FunctionId.ShouldContain("equal");
    }

    [Fact]
    public void Equal_IntegerOperands_InfersIntegerEqual()
    {
        var result = ConditionBuilder.Equal(
            ConditionBuilder.IntValue(1),
            ConditionBuilder.IntValue(2));

        result.FunctionId.ShouldContain("equal");
    }

    #endregion

    #region GreaterThan — Guards

    [Fact]
    public void GreaterThan_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.GreaterThan(null!, ConditionBuilder.IntValue(1));

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GreaterThan_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.GreaterThan(ConditionBuilder.IntValue(1), null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GreaterThan_ValidOperands_ReturnsApply()
    {
        var result = ConditionBuilder.GreaterThan(
            ConditionBuilder.IntValue(10),
            ConditionBuilder.IntValue(5));

        result.FunctionId.ShouldContain("greater-than");
    }

    #endregion

    #region LessThan — Guards

    [Fact]
    public void LessThan_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.LessThan(null!, ConditionBuilder.IntValue(1));

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void LessThan_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.LessThan(ConditionBuilder.IntValue(1), null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region GreaterThanOrEqual — Guards

    [Fact]
    public void GreaterThanOrEqual_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.GreaterThanOrEqual(null!, ConditionBuilder.IntValue(1));

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GreaterThanOrEqual_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.GreaterThanOrEqual(ConditionBuilder.IntValue(1), null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region LessThanOrEqual — Guards

    [Fact]
    public void LessThanOrEqual_NullLeft_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.LessThanOrEqual(null!, ConditionBuilder.IntValue(1));

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void LessThanOrEqual_NullRight_ThrowsArgumentNullException()
    {
        var act = () => ConditionBuilder.LessThanOrEqual(ConditionBuilder.IntValue(1), null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region MapOperatorToFunctionId — Edge Cases

    [Fact]
    public void MapOperatorToFunctionId_Exists_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.Exists, XACMLDataTypes.String);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void MapOperatorToFunctionId_DoesNotExist_ThrowsArgumentException()
    {
        var act = () => ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.DoesNotExist, XACMLDataTypes.String);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void MapOperatorToFunctionId_InvalidOperator_ThrowsArgumentOutOfRangeException()
    {
        var act = () => ConditionBuilder.MapOperatorToFunctionId(
            (ConditionOperator)999, XACMLDataTypes.String);

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void MapOperatorToFunctionId_Equals_ReturnsEqualFunction()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.Equals, XACMLDataTypes.String);

        result.ShouldBe("string-equal");
    }

    [Fact]
    public void MapOperatorToFunctionId_Contains_ReturnsStringContains()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.Contains, XACMLDataTypes.String);

        result.ShouldBe(XACMLFunctionIds.StringContains);
    }

    [Fact]
    public void MapOperatorToFunctionId_StartsWith_ReturnsStringStartsWith()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.StartsWith, XACMLDataTypes.String);

        result.ShouldBe(XACMLFunctionIds.StringStartsWith);
    }

    [Fact]
    public void MapOperatorToFunctionId_EndsWith_ReturnsStringEndsWith()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.EndsWith, XACMLDataTypes.String);

        result.ShouldBe(XACMLFunctionIds.StringEndsWith);
    }

    [Fact]
    public void MapOperatorToFunctionId_RegexMatch_ReturnsRegexpMatch()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.RegexMatch, XACMLDataTypes.String);

        result.ShouldBe(XACMLFunctionIds.StringRegexpMatch);
    }

    [Fact]
    public void MapOperatorToFunctionId_In_ReturnsIsInFunction()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.In, XACMLDataTypes.Integer);

        result.ShouldBe("integer-is-in");
    }

    [Fact]
    public void MapOperatorToFunctionId_GreaterThan_IntegerType()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.GreaterThan, XACMLDataTypes.Integer);

        result.ShouldBe("integer-greater-than");
    }

    [Fact]
    public void MapOperatorToFunctionId_LessThanOrEqual_DoubleType()
    {
        var result = ConditionBuilder.MapOperatorToFunctionId(
            ConditionOperator.LessThanOrEqual, XACMLDataTypes.Double);

        result.ShouldBe("double-less-than-or-equal");
    }

    #endregion

    #region InferDataType — Runtime Type Mapping

    [Fact]
    public void InferDataType_String_ReturnsStringType()
    {
        ConditionBuilder.InferDataType("hello").ShouldBe(XACMLDataTypes.String);
    }

    [Fact]
    public void InferDataType_Int_ReturnsIntegerType()
    {
        ConditionBuilder.InferDataType(42).ShouldBe(XACMLDataTypes.Integer);
    }

    [Fact]
    public void InferDataType_Double_ReturnsDoubleType()
    {
        ConditionBuilder.InferDataType(3.14).ShouldBe(XACMLDataTypes.Double);
    }

    [Fact]
    public void InferDataType_Bool_ReturnsBooleanType()
    {
        ConditionBuilder.InferDataType(true).ShouldBe(XACMLDataTypes.Boolean);
    }

    [Fact]
    public void InferDataType_DateTime_ReturnsDateTimeType()
    {
        ConditionBuilder.InferDataType(DateTime.UtcNow).ShouldBe(XACMLDataTypes.DateTime);
    }

    [Fact]
    public void InferDataType_DateOnly_ReturnsDateType()
    {
        ConditionBuilder.InferDataType(DateOnly.FromDateTime(DateTime.UtcNow)).ShouldBe(XACMLDataTypes.Date);
    }

    [Fact]
    public void InferDataType_TimeOnly_ReturnsTimeType()
    {
        ConditionBuilder.InferDataType(TimeOnly.FromDateTime(DateTime.UtcNow)).ShouldBe(XACMLDataTypes.Time);
    }

    [Fact]
    public void InferDataType_Null_ReturnsStringDefault()
    {
        ConditionBuilder.InferDataType(null).ShouldBe(XACMLDataTypes.String);
    }

    [Fact]
    public void InferDataType_UnknownType_ReturnsStringDefault()
    {
        ConditionBuilder.InferDataType(Guid.NewGuid()).ShouldBe(XACMLDataTypes.String);
    }

    #endregion

    #region InferDataTypeFromExpression — Expression Node Inference

    [Fact]
    public void InferDataTypeFromExpression_AttributeDesignator_ReturnsDataType()
    {
        var designator = ConditionBuilder.Attribute(
            AttributeCategory.Subject, "role", XACMLDataTypes.String);

        ConditionBuilder.InferDataTypeFromExpression(designator)
            .ShouldBe(XACMLDataTypes.String);
    }

    [Fact]
    public void InferDataTypeFromExpression_AttributeValue_ReturnsDataType()
    {
        var value = ConditionBuilder.IntValue(42);

        ConditionBuilder.InferDataTypeFromExpression(value)
            .ShouldBe(XACMLDataTypes.Integer);
    }

    [Fact]
    public void InferDataTypeFromExpression_Apply_ReturnsNull()
    {
        var apply = ConditionBuilder.Equal(
            ConditionBuilder.StringValue("a"),
            ConditionBuilder.StringValue("b"));

        ConditionBuilder.InferDataTypeFromExpression(apply)
            .ShouldBeNull();
    }

    [Fact]
    public void InferDataTypeFromExpression_VariableReference_ReturnsNull()
    {
        var varRef = ConditionBuilder.Variable("myVar");

        ConditionBuilder.InferDataTypeFromExpression(varRef)
            .ShouldBeNull();
    }

    #endregion
}
