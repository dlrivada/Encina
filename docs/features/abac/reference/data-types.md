---
title: "XACML Data Types"
layout: default
parent: "Features"
---

# XACML Data Types

## Overview

The XACML 3.0 data type system is grounded in XML Schema (XSD) primitive types. Every attribute value, function argument, and function return value in a policy has an associated data type. The data type determines how values are compared, converted, and validated during policy evaluation.

Encina maps each XACML data type to a native C# type, enabling natural interop between the policy engine and .NET application code. The `XACMLDataTypes` static class provides string constants for all 12 supported type identifiers. These constants are used in `AttributeValue.DataType`, `AttributeDesignator.DataType`, and `IXACMLFunction.ReturnType`.

## Supported Types

Encina supports the 12 data types defined in the XACML 3.0 specification (Appendix B):

| Constant | XACML URI | C# Type | Example Value |
|----------|-----------|---------|---------------|
| `XACMLDataTypes.String` | `http://www.w3.org/2001/XMLSchema#string` | `string` | `"hello"` |
| `XACMLDataTypes.Boolean` | `http://www.w3.org/2001/XMLSchema#boolean` | `bool` | `true` |
| `XACMLDataTypes.Integer` | `http://www.w3.org/2001/XMLSchema#integer` | `int` / `long` | `42` |
| `XACMLDataTypes.Double` | `http://www.w3.org/2001/XMLSchema#double` | `double` | `3.14` |
| `XACMLDataTypes.Date` | `http://www.w3.org/2001/XMLSchema#date` | `DateOnly` | `2026-03-08` |
| `XACMLDataTypes.DateTime` | `http://www.w3.org/2001/XMLSchema#dateTime` | `DateTime` | `2026-03-08T12:00:00Z` |
| `XACMLDataTypes.Time` | `http://www.w3.org/2001/XMLSchema#time` | `TimeOnly` | `12:00:00` |
| `XACMLDataTypes.AnyURI` | `http://www.w3.org/2001/XMLSchema#anyURI` | `Uri` | `https://example.com` |
| `XACMLDataTypes.HexBinary` | `http://www.w3.org/2001/XMLSchema#hexBinary` | `byte[]` | `0x48656C6C6F` |
| `XACMLDataTypes.Base64Binary` | `http://www.w3.org/2001/XMLSchema#base64Binary` | `byte[]` | `SGVsbG8=` |
| `XACMLDataTypes.DayTimeDuration` | `http://www.w3.org/2001/XMLSchema#dayTimeDuration` | `TimeSpan` | `P1DT2H` (1 day, 2 hours) |
| `XACMLDataTypes.YearMonthDuration` | `http://www.w3.org/2001/XMLSchema#yearMonthDuration` | `TimeSpan` | `P1Y2M` (1 year, 2 months) |

### Type Notes

- **Integer**: Both `int` and `long` are accepted. Functions internally use `Convert.ToInt64()` for safe coercion.
- **Date / DateTime / Time**: Encina uses `DateOnly`, `DateTime`, and `TimeOnly` respectively. All DateTime values should be UTC.
- **HexBinary / Base64Binary**: Both map to `byte[]`. The distinction is in serialization format, not runtime representation.
- **DayTimeDuration / YearMonthDuration**: Both map to `TimeSpan`. XACML distinguishes them semantically (calendar vs. clock durations), but .NET represents both as `TimeSpan`.

## Type Usage in AttributeValue

`AttributeValue` is the literal value expression in XACML. The `DataType` property declares the type; the `Value` property holds the boxed C# value.

```csharp
// String literal
var stringVal = new AttributeValue
{
    DataType = XACMLDataTypes.String,
    Value = "Finance"
};

// Integer literal
var intVal = new AttributeValue
{
    DataType = XACMLDataTypes.Integer,
    Value = 10000
};

// Boolean literal
var boolVal = new AttributeValue
{
    DataType = XACMLDataTypes.Boolean,
    Value = true
};

// DateTime literal
var dateTimeVal = new AttributeValue
{
    DataType = XACMLDataTypes.DateTime,
    Value = new DateTime(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc)
};

// Date literal
var dateVal = new AttributeValue
{
    DataType = XACMLDataTypes.Date,
    Value = new DateOnly(2026, 3, 8)
};

// Time literal
var timeVal = new AttributeValue
{
    DataType = XACMLDataTypes.Time,
    Value = new TimeOnly(14, 30, 0)
};

// URI literal
var uriVal = new AttributeValue
{
    DataType = XACMLDataTypes.AnyURI,
    Value = new Uri("https://api.example.com/resources")
};

// Duration literal
var durationVal = new AttributeValue
{
    DataType = XACMLDataTypes.DayTimeDuration,
    Value = TimeSpan.FromHours(26) // P1DT2H
};
```

A `null` `Value` represents an absent or undefined attribute value, which is semantically distinct from an empty string or zero.

## Type Usage in AttributeDesignator

`AttributeDesignator` declares the expected type of an attribute resolved from the request context. The `DataType` property tells the evaluation engine what type to expect from the `IAttributeProvider`.

```csharp
// Expect a string attribute for the subject's department
var designator = new AttributeDesignator
{
    Category = AttributeCategory.Subject,
    AttributeId = "department",
    DataType = XACMLDataTypes.String,
    MustBePresent = true
};

// Expect an integer attribute for the resource's classification level
var classificationDesignator = new AttributeDesignator
{
    Category = AttributeCategory.Resource,
    AttributeId = "classification-level",
    DataType = XACMLDataTypes.Integer,
    MustBePresent = true
};

// Expect a dateTime attribute for the current time (environment)
var timeDesignator = new AttributeDesignator
{
    Category = AttributeCategory.Environment,
    AttributeId = "current-dateTime",
    DataType = XACMLDataTypes.DateTime,
    MustBePresent = false
};
```

When `MustBePresent` is `true` and the attribute cannot be resolved, the evaluation result is `Indeterminate`. When `false`, a missing attribute produces an empty `AttributeBag` and evaluation continues.

## Type Conversion Functions

XACML provides a set of type conversion functions to transform values between types. These are registered in the `IFunctionRegistry` and can be used in `Apply` expression trees.

| Function ID | Input Type | Output Type | Description |
|-------------|-----------|-------------|-------------|
| `string-from-integer` | `int` | `string` | Integer to decimal string |
| `integer-from-string` | `string` | `int` | Parse decimal string to integer |
| `double-from-string` | `string` | `double` | Parse string to double |
| `boolean-from-string` | `string` | `bool` | Parse `"true"` / `"false"` |
| `string-from-boolean` | `bool` | `string` | Boolean to `"true"` / `"false"` |
| `string-from-double` | `double` | `string` | Double to string |
| `string-from-dateTime` | `DateTime` | `string` | DateTime to ISO 8601 string |

### Conversion Example

```csharp
// Convert an integer attribute to string for concatenation
var convertedExpr = new Apply
{
    FunctionId = XACMLFunctionIds.StringFromInteger,
    Arguments =
    [
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "classification-level",
            DataType = XACMLDataTypes.Integer
        }
    ]
};

// Use the converted value in a string comparison
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.StringEqual,
    Arguments =
    [
        convertedExpr,
        new AttributeValue { DataType = XACMLDataTypes.String, Value = "5" }
    ]
};
```

Conversion functions throw `InvalidOperationException` when the input cannot be parsed (e.g., `"abc"` passed to `integer-from-string`). The evaluation engine treats this as an `Indeterminate` result.

## Type Safety

The ABAC engine enforces type safety at multiple levels:

### 1. Function Argument Validation

Each `IXACMLFunction` implementation validates argument count and types at evaluation time. If a function receives arguments of the wrong type or an incorrect number of arguments, it throws `InvalidOperationException`, which the `ConditionEvaluator` converts to an `Indeterminate` decision.

```csharp
// string-equal expects exactly 2 string arguments.
// Passing an integer will cause a type validation error.
```

### 2. AttributeDesignator Type Declaration

The `DataType` on `AttributeDesignator` declares what type the `IAttributeProvider` is expected to return. A mismatch between the declared type and the actual resolved value type can cause function evaluation errors. Providers should ensure returned values match the expected type.

### 3. Bag Type Consistency

Bag functions are type-specific. `string-one-and-only` operates on a bag of strings; passing a bag of integers results in an error. Each type has its own family of bag functions (e.g., `string-bag-size`, `integer-bag-size`, `double-bag-size`).

### 4. Return Type Metadata

Every `IXACMLFunction` declares its `ReturnType` as an `XACMLDataTypes` constant. This metadata enables the engine to validate expression tree consistency before evaluation and provide clearer error messages.

### Type Mismatch Behavior

When a type mismatch occurs during evaluation:

1. The function throws `InvalidOperationException` with a descriptive message.
2. The `ConditionEvaluator` catches the exception.
3. The rule evaluation result becomes `Indeterminate`.
4. The combining algorithm determines the final policy decision based on the `Indeterminate` result.

This fail-safe behavior ensures that type errors never silently produce incorrect authorization decisions.

## XACMLDataTypes Constants

The `XACMLDataTypes` static class provides all 12 type identifiers as `const string` fields:

```csharp
public static class XACMLDataTypes
{
    public const string String           = "http://www.w3.org/2001/XMLSchema#string";
    public const string Boolean          = "http://www.w3.org/2001/XMLSchema#boolean";
    public const string Integer          = "http://www.w3.org/2001/XMLSchema#integer";
    public const string Double           = "http://www.w3.org/2001/XMLSchema#double";
    public const string Date             = "http://www.w3.org/2001/XMLSchema#date";
    public const string DateTime         = "http://www.w3.org/2001/XMLSchema#dateTime";
    public const string Time             = "http://www.w3.org/2001/XMLSchema#time";
    public const string AnyURI           = "http://www.w3.org/2001/XMLSchema#anyURI";
    public const string HexBinary        = "http://www.w3.org/2001/XMLSchema#hexBinary";
    public const string Base64Binary     = "http://www.w3.org/2001/XMLSchema#base64Binary";
    public const string DayTimeDuration  = "http://www.w3.org/2001/XMLSchema#dayTimeDuration";
    public const string YearMonthDuration = "http://www.w3.org/2001/XMLSchema#yearMonthDuration";
}
```

Always use these constants instead of raw strings to benefit from compile-time checking and refactoring support.

## See Also

- [Functions](../xacml/functions.md) -- Function overview, categories, and usage in conditions
- [Function Library](function-library.md) -- Exhaustive reference of all ~70 standard functions with signatures
