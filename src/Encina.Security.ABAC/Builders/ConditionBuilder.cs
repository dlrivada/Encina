namespace Encina.Security.ABAC.Builders;

/// <summary>
/// Static factory methods for building XACML 3.0 expression trees (<see cref="Apply"/>,
/// <see cref="AttributeDesignator"/>, <see cref="AttributeValue"/>, <see cref="VariableReference"/>)
/// in a C#-idiomatic way.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 uses function application (not operators) for all comparisons and logic.
/// This builder provides convenience methods that map C#-friendly syntax to XACML
/// <see cref="Apply"/> expression trees referencing functions from <see cref="XACMLFunctionIds"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Build a condition: subject.department == "Finance" AND resource.amount > 10000
/// var condition = ConditionBuilder.And(
///     ConditionBuilder.Equal(
///         ConditionBuilder.Attribute(AttributeCategory.Subject, "department", XACMLDataTypes.String),
///         ConditionBuilder.StringValue("Finance")),
///     ConditionBuilder.GreaterThan(
///         ConditionBuilder.Attribute(AttributeCategory.Resource, "amount", XACMLDataTypes.Integer),
///         ConditionBuilder.IntValue(10000)));
/// </code>
/// </example>
public static class ConditionBuilder
{
    // ── Core Expression Factories ──

    /// <summary>
    /// Creates an <see cref="Apply"/> node representing a function call with the specified arguments.
    /// </summary>
    /// <param name="functionId">The XACML function identifier (e.g., <see cref="XACMLFunctionIds.StringEqual"/>).</param>
    /// <param name="args">The function arguments as expression trees.</param>
    /// <returns>A new <see cref="Apply"/> expression node.</returns>
    public static Apply Function(string functionId, params IExpression[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionId);
        ArgumentNullException.ThrowIfNull(args);

        return new Apply
        {
            FunctionId = functionId,
            Arguments = args
        };
    }

    /// <summary>
    /// Creates an <see cref="AttributeDesignator"/> referencing an attribute in the evaluation context.
    /// </summary>
    /// <param name="category">The attribute category (Subject, Resource, Action, Environment).</param>
    /// <param name="attributeId">The attribute identifier within the category.</param>
    /// <param name="dataType">The expected XACML data type (e.g., <see cref="XACMLDataTypes.String"/>).</param>
    /// <param name="mustBePresent">
    /// If <c>true</c>, a missing attribute causes an Indeterminate result.
    /// If <c>false</c> (default), a missing attribute produces an empty bag.
    /// </param>
    /// <returns>A new <see cref="AttributeDesignator"/> expression node.</returns>
    public static AttributeDesignator Attribute(
        AttributeCategory category,
        string attributeId,
        string dataType,
        bool mustBePresent = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);

        return new AttributeDesignator
        {
            Category = category,
            AttributeId = attributeId,
            DataType = dataType,
            MustBePresent = mustBePresent
        };
    }

    /// <summary>
    /// Creates an <see cref="AttributeValue"/> literal with the specified data type and value.
    /// </summary>
    /// <param name="dataType">The XACML data type identifier.</param>
    /// <param name="value">The literal value (may be <c>null</c> per XACML spec).</param>
    /// <returns>A new <see cref="AttributeValue"/> expression node.</returns>
    public static AttributeValue Value(string dataType, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);

        return new AttributeValue
        {
            DataType = dataType,
            Value = value
        };
    }

    /// <summary>
    /// Creates a <see cref="VariableReference"/> to a named variable defined in the same policy.
    /// </summary>
    /// <param name="variableId">The variable identifier matching a <see cref="VariableDefinition.VariableId"/>.</param>
    /// <returns>A new <see cref="VariableReference"/> expression node.</returns>
    public static VariableReference Variable(string variableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        return new VariableReference
        {
            VariableId = variableId
        };
    }

    // ── Typed Value Convenience Factories ──

    /// <summary>Creates a string-typed <see cref="AttributeValue"/>.</summary>
    public static AttributeValue StringValue(string val) =>
        new() { DataType = XACMLDataTypes.String, Value = val };

    /// <summary>Creates an integer-typed <see cref="AttributeValue"/>.</summary>
    public static AttributeValue IntValue(int val) =>
        new() { DataType = XACMLDataTypes.Integer, Value = val };

    /// <summary>Creates a double-typed <see cref="AttributeValue"/>.</summary>
    public static AttributeValue DoubleValue(double val) =>
        new() { DataType = XACMLDataTypes.Double, Value = val };

    /// <summary>Creates a boolean-typed <see cref="AttributeValue"/>.</summary>
    public static AttributeValue BoolValue(bool val) =>
        new() { DataType = XACMLDataTypes.Boolean, Value = val };

    /// <summary>Creates a dateTime-typed <see cref="AttributeValue"/>.</summary>
    public static AttributeValue DateTimeValue(DateTime val) =>
        new() { DataType = XACMLDataTypes.DateTime, Value = val };

    /// <summary>Creates a date-typed <see cref="AttributeValue"/>.</summary>
    public static AttributeValue DateValue(DateOnly val) =>
        new() { DataType = XACMLDataTypes.Date, Value = val };

    /// <summary>Creates a time-typed <see cref="AttributeValue"/>.</summary>
    public static AttributeValue TimeValue(TimeOnly val) =>
        new() { DataType = XACMLDataTypes.Time, Value = val };

    // ── Logical Connectives ──

    /// <summary>
    /// Creates an <see cref="Apply"/> node for the XACML logical AND function (short-circuit).
    /// </summary>
    /// <param name="conditions">One or more boolean-returning conditions to AND together.</param>
    /// <returns>An <see cref="Apply"/> node with <see cref="XACMLFunctionIds.And"/>.</returns>
    public static Apply And(params Apply[] conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        if (conditions.Length == 0)
            throw new ArgumentException("At least one condition is required.", nameof(conditions));

        return new Apply
        {
            FunctionId = XACMLFunctionIds.And,
            Arguments = conditions
        };
    }

    /// <summary>
    /// Creates an <see cref="Apply"/> node for the XACML logical OR function (short-circuit).
    /// </summary>
    /// <param name="conditions">One or more boolean-returning conditions to OR together.</param>
    /// <returns>An <see cref="Apply"/> node with <see cref="XACMLFunctionIds.Or"/>.</returns>
    public static Apply Or(params Apply[] conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        if (conditions.Length == 0)
            throw new ArgumentException("At least one condition is required.", nameof(conditions));

        return new Apply
        {
            FunctionId = XACMLFunctionIds.Or,
            Arguments = conditions
        };
    }

    /// <summary>
    /// Creates an <see cref="Apply"/> node for the XACML logical NOT function.
    /// </summary>
    /// <param name="condition">The boolean-returning condition to negate.</param>
    /// <returns>An <see cref="Apply"/> node with <see cref="XACMLFunctionIds.Not"/>.</returns>
    public static Apply Not(Apply condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return new Apply
        {
            FunctionId = XACMLFunctionIds.Not,
            Arguments = [condition]
        };
    }

    // ── Comparison Sugar ──

    /// <summary>
    /// Creates an equality comparison, inferring the XACML function from argument data types.
    /// </summary>
    /// <param name="left">Left operand expression.</param>
    /// <param name="right">Right operand expression.</param>
    /// <returns>An <see cref="Apply"/> node with the appropriate equality function.</returns>
    public static Apply Equal(IExpression left, IExpression right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var dataType = InferDataTypeFromExpression(left)
            ?? InferDataTypeFromExpression(right)
            ?? XACMLDataTypes.String;

        var functionId = ResolveComparisonFunctionId("equal", dataType);
        return new Apply { FunctionId = functionId, Arguments = [left, right] };
    }

    /// <summary>
    /// Creates a greater-than comparison, inferring the XACML function from argument data types.
    /// </summary>
    public static Apply GreaterThan(IExpression left, IExpression right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var dataType = InferDataTypeFromExpression(left)
            ?? InferDataTypeFromExpression(right)
            ?? XACMLDataTypes.Integer;

        var functionId = ResolveComparisonFunctionId("greater-than", dataType);
        return new Apply { FunctionId = functionId, Arguments = [left, right] };
    }

    /// <summary>
    /// Creates a less-than comparison, inferring the XACML function from argument data types.
    /// </summary>
    public static Apply LessThan(IExpression left, IExpression right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var dataType = InferDataTypeFromExpression(left)
            ?? InferDataTypeFromExpression(right)
            ?? XACMLDataTypes.Integer;

        var functionId = ResolveComparisonFunctionId("less-than", dataType);
        return new Apply { FunctionId = functionId, Arguments = [left, right] };
    }

    /// <summary>
    /// Creates a greater-than-or-equal comparison, inferring the XACML function from argument data types.
    /// </summary>
    public static Apply GreaterThanOrEqual(IExpression left, IExpression right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var dataType = InferDataTypeFromExpression(left)
            ?? InferDataTypeFromExpression(right)
            ?? XACMLDataTypes.Integer;

        var functionId = ResolveComparisonFunctionId("greater-than-or-equal", dataType);
        return new Apply { FunctionId = functionId, Arguments = [left, right] };
    }

    /// <summary>
    /// Creates a less-than-or-equal comparison, inferring the XACML function from argument data types.
    /// </summary>
    public static Apply LessThanOrEqual(IExpression left, IExpression right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var dataType = InferDataTypeFromExpression(left)
            ?? InferDataTypeFromExpression(right)
            ?? XACMLDataTypes.Integer;

        var functionId = ResolveComparisonFunctionId("less-than-or-equal", dataType);
        return new Apply { FunctionId = functionId, Arguments = [left, right] };
    }

    // ── Operator-to-FunctionId Mapping ──

    /// <summary>
    /// Maps a <see cref="ConditionOperator"/> and data type to the corresponding XACML function identifier.
    /// </summary>
    /// <param name="op">The condition operator.</param>
    /// <param name="dataType">The XACML data type URI.</param>
    /// <returns>The XACML function identifier string.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the operator is not supported for the given data type, or when
    /// <see cref="ConditionOperator.Exists"/>/<see cref="ConditionOperator.DoesNotExist"/>
    /// are used (these are not valid in Match context).
    /// </exception>
    internal static string MapOperatorToFunctionId(ConditionOperator op, string dataType)
    {
        var shortType = SimplifyDataType(dataType);

        return op switch
        {
            ConditionOperator.Equals => $"{shortType}-equal",
            ConditionOperator.NotEquals => $"{shortType}-equal", // Caller wraps in NOT
            ConditionOperator.GreaterThan => $"{shortType}-greater-than",
            ConditionOperator.GreaterThanOrEqual => $"{shortType}-greater-than-or-equal",
            ConditionOperator.LessThan => $"{shortType}-less-than",
            ConditionOperator.LessThanOrEqual => $"{shortType}-less-than-or-equal",
            ConditionOperator.Contains => XACMLFunctionIds.StringContains,
            ConditionOperator.NotContains => XACMLFunctionIds.StringContains, // Caller wraps in NOT
            ConditionOperator.StartsWith => XACMLFunctionIds.StringStartsWith,
            ConditionOperator.EndsWith => XACMLFunctionIds.StringEndsWith,
            ConditionOperator.In => $"{shortType}-is-in",
            ConditionOperator.NotIn => $"{shortType}-is-in", // Caller wraps in NOT
            ConditionOperator.RegexMatch => XACMLFunctionIds.StringRegexpMatch,
            ConditionOperator.Exists => throw new ArgumentException(
                "ConditionOperator.Exists is not valid in Match context. Use in condition expressions instead.",
                nameof(op)),
            ConditionOperator.DoesNotExist => throw new ArgumentException(
                "ConditionOperator.DoesNotExist is not valid in Match context. Use in condition expressions instead.",
                nameof(op)),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, $"Unsupported operator: {op}")
        };
    }

    // ── Internal Helpers ──

    /// <summary>
    /// Infers the XACML data type from an expression node.
    /// Returns <c>null</c> if the type cannot be determined.
    /// </summary>
    internal static string? InferDataTypeFromExpression(IExpression expression) =>
        expression switch
        {
            AttributeDesignator d => d.DataType,
            AttributeValue v => v.DataType,
            _ => null
        };

    /// <summary>
    /// Infers the XACML data type URI from a runtime .NET type.
    /// </summary>
    internal static string InferDataType(object? value) =>
        value switch
        {
            string => XACMLDataTypes.String,
            int => XACMLDataTypes.Integer,
            double => XACMLDataTypes.Double,
            bool => XACMLDataTypes.Boolean,
            DateTime => XACMLDataTypes.DateTime,
            DateOnly => XACMLDataTypes.Date,
            TimeOnly => XACMLDataTypes.Time,
            _ => XACMLDataTypes.String
        };

    /// <summary>
    /// Simplifies a full XACML data type URI to its short name for function ID construction.
    /// </summary>
    private static string SimplifyDataType(string dataType) =>
        dataType switch
        {
            XACMLDataTypes.String => "string",
            XACMLDataTypes.Integer => "integer",
            XACMLDataTypes.Double => "double",
            XACMLDataTypes.Boolean => "boolean",
            XACMLDataTypes.Date => "date",
            XACMLDataTypes.DateTime => "dateTime",
            XACMLDataTypes.Time => "time",
            XACMLDataTypes.AnyURI => "anyURI",
            _ => "string"
        };

    /// <summary>
    /// Resolves a comparison function ID by combining the operation suffix with the data type.
    /// </summary>
    private static string ResolveComparisonFunctionId(string operation, string dataType)
    {
        var shortType = SimplifyDataType(dataType);
        return $"{shortType}-{operation}";
    }
}
