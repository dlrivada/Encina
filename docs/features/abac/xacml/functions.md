# XACML Functions

## Overview

Functions are the computational building blocks of the XACML 3.0 policy language. Every condition in an ABAC policy is ultimately a tree of function applications (`Apply` nodes) that evaluates to a boolean result. Functions also drive target matching, where a `Match` element compares an attribute from the request context against a literal value using a specified function.

Encina's ABAC engine ships with all standard XACML 3.0 functions pre-registered in the `DefaultFunctionRegistry`. Custom functions can be added at startup via `ABACOptions.AddFunction()`, enabling domain-specific logic such as geospatial checks or risk scoring.

Key types involved:

| Type | Role |
|------|------|
| `IFunctionRegistry` | Abstraction for looking up and registering functions |
| `DefaultFunctionRegistry` | Default implementation with ~70 standard functions |
| `IXACMLFunction` | Contract every function must implement |
| `DelegateFunction` | Internal adapter wrapping a `Func<>` as `IXACMLFunction` |
| `XACMLFunctionIds` | Static constants for all standard function identifiers |
| `Apply` | Expression tree node that invokes a function with arguments |
| `Match` | Target matching element that compares an attribute against a literal |

## Function Categories

The standard XACML 3.0 functions are organized into ten categories. This section provides a summary table and representative code examples for each. For the exhaustive listing of every function, see the [Function Library](../reference/function-library.md).

### Equality Functions

Test whether two values of the same type are equal. Used heavily in `Match` elements and `Apply` conditions.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `string-equal` | `(string, string) -> bool` | Ordinal string equality |
| `boolean-equal` | `(bool, bool) -> bool` | Boolean equality |
| `integer-equal` | `(int, int) -> bool` | Integer equality |
| `double-equal` | `(double, double) -> bool` | Double equality |
| `date-equal` | `(DateOnly, DateOnly) -> bool` | Date equality |
| `dateTime-equal` | `(DateTime, DateTime) -> bool` | DateTime equality |
| `time-equal` | `(TimeOnly, TimeOnly) -> bool` | Time equality |

```csharp
// Condition: subject.role == "admin"
var apply = new Apply
{
    FunctionId = XACMLFunctionIds.StringEqual,
    Arguments =
    [
        new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "role",
            DataType = XACMLDataTypes.String
        },
        new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" }
    ]
};
```

### Comparison Functions

Ordered comparisons for numeric, string (lexicographic), date, dateTime, and time types.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `integer-greater-than` | `(int, int) -> bool` | Integer `>` |
| `integer-less-than-or-equal` | `(int, int) -> bool` | Integer `<=` |
| `double-greater-than` | `(double, double) -> bool` | Double `>` |
| `string-greater-than` | `(string, string) -> bool` | Lexicographic `>` |
| `date-less-than` | `(DateOnly, DateOnly) -> bool` | Date `<` |
| `dateTime-greater-than-or-equal` | `(DateTime, DateTime) -> bool` | DateTime `>=` |

```csharp
// Condition: resource.amount > 10000
var apply = new Apply
{
    FunctionId = XACMLFunctionIds.IntegerGreaterThan,
    Arguments =
    [
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "amount",
            DataType = XACMLDataTypes.Integer
        },
        new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 10000 }
    ]
};
```

### Arithmetic Functions

Numeric computation for integer and double types, including rounding.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `integer-add` | `(int, int) -> int` | Addition |
| `integer-subtract` | `(int, int) -> int` | Subtraction |
| `integer-multiply` | `(int, int) -> int` | Multiplication |
| `integer-divide` | `(int, int) -> int` | Integer division |
| `integer-mod` | `(int, int) -> int` | Modulus |
| `integer-abs` | `(int) -> int` | Absolute value |
| `double-add` | `(double, double) -> double` | Double addition |
| `round` | `(double) -> double` | Round to nearest integer |
| `floor` | `(double) -> double` | Floor to nearest lower integer |

```csharp
// Nested: integer-add(resource.base-price, resource.tax) used inside a comparison
var addExpr = new Apply
{
    FunctionId = XACMLFunctionIds.IntegerAdd,
    Arguments =
    [
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "base-price",
            DataType = XACMLDataTypes.Integer
        },
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "tax",
            DataType = XACMLDataTypes.Integer
        }
    ]
};
```

### String Functions

String manipulation and inspection operations.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `string-concatenate` | `(string, string, ...) -> string` | Concatenation |
| `string-starts-with` | `(string, string) -> bool` | Prefix check |
| `string-ends-with` | `(string, string) -> bool` | Suffix check |
| `string-contains` | `(string, string) -> bool` | Substring check |
| `string-substring` | `(string, int, int) -> string` | Extract by position |
| `string-normalize-space` | `(string) -> string` | Whitespace normalization |
| `string-normalize-to-lower-case` | `(string) -> string` | Convert to lowercase |
| `string-length` | `(string) -> int` | Character count |

```csharp
// Condition: string-starts-with(resource.path, "/api/admin")
var apply = new Apply
{
    FunctionId = XACMLFunctionIds.StringStartsWith,
    Arguments =
    [
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "path",
            DataType = XACMLDataTypes.String
        },
        new AttributeValue { DataType = XACMLDataTypes.String, Value = "/api/admin" }
    ]
};
```

### Logical Functions

Boolean connectives for composing conditions.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `and` | `(bool, bool, ...) -> bool` | Short-circuit AND |
| `or` | `(bool, bool, ...) -> bool` | Short-circuit OR |
| `not` | `(bool) -> bool` | Boolean inversion |
| `n-of` | `(int, bool, bool, ...) -> bool` | At least N arguments are true |

```csharp
// Condition: and(string-equal(subject.role, "manager"), integer-greater-than(resource.amount, 5000))
var apply = new Apply
{
    FunctionId = XACMLFunctionIds.And,
    Arguments =
    [
        new Apply
        {
            FunctionId = XACMLFunctionIds.StringEqual,
            Arguments =
            [
                new AttributeDesignator
                {
                    Category = AttributeCategory.Subject,
                    AttributeId = "role",
                    DataType = XACMLDataTypes.String
                },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "manager" }
            ]
        },
        new Apply
        {
            FunctionId = XACMLFunctionIds.IntegerGreaterThan,
            Arguments =
            [
                new AttributeDesignator
                {
                    Category = AttributeCategory.Resource,
                    AttributeId = "amount",
                    DataType = XACMLDataTypes.Integer
                },
                new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 5000 }
            ]
        }
    ]
};
```

### Bag Functions

Operations on attribute bags (multi-valued attribute results). Each supported data type has its own set of bag functions.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `string-one-and-only` | `(bag<string>) -> string` | Extract single value (error if size != 1) |
| `string-bag-size` | `(bag<string>) -> int` | Count of values in bag |
| `string-is-in` | `(string, bag<string>) -> bool` | Membership test |
| `string-bag` | `(string, ...) -> bag<string>` | Create bag from values |
| `integer-one-and-only` | `(bag<int>) -> int` | Extract single integer value |
| `integer-is-in` | `(int, bag<int>) -> bool` | Integer membership test |

Bag functions exist for all supported types: `string`, `boolean`, `integer`, `double`, `date`, `dateTime`, `time`, and `anyURI`.

```csharp
// Condition: string-is-in("Finance", subject.departments)
var apply = new Apply
{
    FunctionId = XACMLFunctionIds.StringIsIn,
    Arguments =
    [
        new AttributeValue { DataType = XACMLDataTypes.String, Value = "Finance" },
        new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "departments",
            DataType = XACMLDataTypes.String
        }
    ]
};
```

### Set Functions

Set-theoretic operations on attribute bags. Available for `string`, `integer`, and `double` types.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `string-intersection` | `(bag, bag) -> bag` | Common elements |
| `string-union` | `(bag, bag) -> bag` | All elements from both |
| `string-subset` | `(bag, bag) -> bool` | First is subset of second |
| `string-at-least-one-member-of` | `(bag, bag) -> bool` | Bags share at least one element |
| `string-set-equals` | `(bag, bag) -> bool` | Bags contain same values |

```csharp
// Condition: string-at-least-one-member-of(subject.roles, resource.allowed-roles)
var apply = new Apply
{
    FunctionId = XACMLFunctionIds.StringAtLeastOneMemberOf,
    Arguments =
    [
        new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "roles",
            DataType = XACMLDataTypes.String
        },
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "allowed-roles",
            DataType = XACMLDataTypes.String
        }
    ]
};
```

### Higher-Order Functions

Functions that accept another function as an argument and apply it across bag elements.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `any-of` | `(func, value, bag) -> bool` | True if func returns true for any bag element |
| `all-of` | `(func, value, bag) -> bool` | True if func returns true for all bag elements |
| `any-of-any` | `(func, bag, bag) -> bool` | True for any pair from two bags |
| `all-of-any` | `(func, bag, bag) -> bool` | For any element in bag1, true for all in bag2 |
| `all-of-all` | `(func, bag, bag) -> bool` | True for all pairs from two bags |
| `map` | `(func, bag) -> bag` | Apply func to each element, return new bag |

### Type Conversion Functions

Convert values between XACML data types. See [Data Types](../reference/data-types.md) for the full type system.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `string-from-integer` | `(int) -> string` | Integer to string |
| `integer-from-string` | `(string) -> int` | Parse integer |
| `double-from-string` | `(string) -> double` | Parse double |
| `boolean-from-string` | `(string) -> bool` | Parse boolean |
| `string-from-boolean` | `(bool) -> string` | Boolean to string |
| `string-from-double` | `(double) -> string` | Double to string |
| `string-from-dateTime` | `(DateTime) -> string` | DateTime to ISO 8601 string |

### Regular Expression Functions

Pattern matching against string values using .NET regular expressions.

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `string-regexp-match` | `(string, string) -> bool` | Test if value matches regex pattern |

```csharp
// Condition: string-regexp-match(resource.email, "^.*@company\\.com$")
var apply = new Apply
{
    FunctionId = XACMLFunctionIds.StringRegexpMatch,
    Arguments =
    [
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "email",
            DataType = XACMLDataTypes.String
        },
        new AttributeValue { DataType = XACMLDataTypes.String, Value = @"^.*@company\.com$" }
    ]
};
```

## Function Registry

The `IFunctionRegistry` is the central lookup mechanism for all functions available during policy evaluation.

### IFunctionRegistry Interface

```csharp
public interface IFunctionRegistry
{
    IXACMLFunction? GetFunction(string functionId);
    void Register(string functionId, IXACMLFunction xacmlFunction);
    IReadOnlyList<string> GetAllFunctionIds();
}
```

- `GetFunction` returns `null` when no function is registered for the given ID. The evaluation engine treats this as an Indeterminate result.
- `Register` replaces any existing function with the same ID, allowing standard functions to be overridden.
- `GetAllFunctionIds` returns a sorted list of all registered function identifiers.

### DefaultFunctionRegistry

`DefaultFunctionRegistry` is the production implementation. Its constructor automatically registers all standard XACML 3.0 functions by invoking the static `Register` method on each function category class (`EqualityFunctions`, `ComparisonFunctions`, etc.).

```csharp
var registry = new DefaultFunctionRegistry();

// Retrieve a standard function
IXACMLFunction? fn = registry.GetFunction(XACMLFunctionIds.StringEqual);
object? result = fn!.Evaluate(["admin", "admin"]); // true

// List all available functions
IReadOnlyList<string> allIds = registry.GetAllFunctionIds();
```

The registry is registered as a singleton in the DI container by `AddEncinaABAC()` and is injected into the `ConditionEvaluator` and `TargetEvaluator`.

## Using Functions in Conditions

Conditions are built as recursive `Apply` expression trees. Each `Apply` node references a `FunctionId` and contains an ordered list of `IExpression` arguments. Arguments can be:

- `AttributeValue` -- a literal value
- `AttributeDesignator` -- a reference to a context attribute
- `VariableReference` -- a reference to a named variable
- Another `Apply` -- nested function application

The `ConditionEvaluator` walks the tree bottom-up, evaluating leaf nodes first and feeding their results as arguments to parent nodes. The root `Apply` must return a boolean.

## Using Functions in Match

A `Match` element is a simplified function application used in policy targets. It always compares a single `AttributeDesignator` against a single `AttributeValue` using a two-argument boolean function.

```csharp
var match = new Match
{
    FunctionId = XACMLFunctionIds.StringEqual,
    AttributeDesignator = new AttributeDesignator
    {
        Category = AttributeCategory.Subject,
        AttributeId = "department",
        DataType = XACMLDataTypes.String
    },
    AttributeValue = new AttributeValue
    {
        DataType = XACMLDataTypes.String,
        Value = "Finance"
    }
};
```

The `TargetEvaluator` resolves the attribute from the request context, then invokes the function with the resolved value and the literal value. If the function returns `true`, the match succeeds.

## Custom Functions

Register domain-specific functions via `ABACOptions.AddFunction()` during service configuration. Custom functions must implement the `IXACMLFunction` interface.

### Implementing IXACMLFunction

```csharp
public sealed class GeoDistanceFunction : IXACMLFunction
{
    public string ReturnType => XACMLDataTypes.Double;

    public object? Evaluate(IReadOnlyList<object?> arguments)
    {
        if (arguments.Count != 4)
            throw new InvalidOperationException(
                "geo-distance requires 4 arguments: lat1, lon1, lat2, lon2.");

        double lat1 = Convert.ToDouble(arguments[0]);
        double lon1 = Convert.ToDouble(arguments[1]);
        double lat2 = Convert.ToDouble(arguments[2]);
        double lon2 = Convert.ToDouble(arguments[3]);

        return HaversineDistance(lat1, lon1, lat2, lon2);
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula implementation
        const double R = 6371.0; // Earth radius in km
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
```

### Registration

```csharp
services.AddEncinaABAC(options =>
{
    options.AddFunction("custom:geo-distance", new GeoDistanceFunction())
           .AddFunction("custom:risk-score", new RiskScoreFunction());
});
```

Custom functions are added to the `CustomFunctions` list on `ABACOptions` and registered into the `IFunctionRegistry` singleton during DI setup. They are available immediately for policy condition evaluation.

## Function Naming Convention

Standard XACML function identifiers follow the pattern `{type}-{operation}`:

| Pattern | Examples |
|---------|----------|
| `{type}-equal` | `string-equal`, `integer-equal`, `date-equal` |
| `{type}-greater-than` | `integer-greater-than`, `double-greater-than` |
| `{type}-one-and-only` | `string-one-and-only`, `integer-one-and-only` |
| `{type}-is-in` | `string-is-in`, `boolean-is-in` |
| `{type}-bag` | `string-bag`, `date-bag` |
| `{type}-intersection` | `string-intersection`, `integer-intersection` |
| `{type}-from-{source}` | `integer-from-string`, `string-from-boolean` |

Standalone functions that are not type-prefixed: `and`, `or`, `not`, `n-of`, `round`, `floor`, `any-of`, `all-of`, `any-of-any`, `all-of-any`, `all-of-all`, `map`, `string-regexp-match`.

All standard identifiers are available as `const string` fields on `XACMLFunctionIds`. For custom functions, use a namespace prefix (e.g., `custom:geo-distance`) to avoid collisions with standard identifiers.

## See Also

- [Function Library](../reference/function-library.md) -- Exhaustive reference of all ~70 standard functions
- [Data Types](../reference/data-types.md) -- XACML data type system and C# mappings
- [Policy Language](policy-language.md) -- How functions fit into policies, rules, and conditions
