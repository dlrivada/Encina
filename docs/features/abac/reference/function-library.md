# XACML Function Library Reference

Complete reference for all XACML 3.0 standard functions implemented in `Encina.Security.ABAC`. These functions are registered automatically via `DefaultFunctionRegistry` and are available for use in `Apply` expression trees, `Match` elements, and the EEL (Encina Expression Language) compiler.

All function identifiers are defined as constants in the `XACMLFunctionIds` static class. They follow the XACML 3.0 Appendix A naming convention: `{type}-{operation}`.

---

## Table of Contents

1. [Equality Functions](#1-equality-functions) (7 functions)
2. [Comparison Functions](#2-comparison-functions) (24 functions)
3. [Arithmetic Functions](#3-arithmetic-functions) (13 functions)
4. [String Functions](#4-string-functions) (8 functions)
5. [Logical Functions](#5-logical-functions) (4 functions)
6. [Bag Functions](#6-bag-functions) (32 functions)
7. [Set Functions](#7-set-functions) (15 functions)
8. [Higher-Order Functions](#8-higher-order-functions) (6 functions)
9. [Type Conversion Functions](#9-type-conversion-functions) (7 functions)
10. [Regular Expression Functions](#10-regular-expression-functions) (1 function)

**Total: 117 functions**

---

## Quick Reference Table

| Function ID | Category | Description |
|---|---|---|
| `string-equal` | Equality | String equality (ordinal) |
| `boolean-equal` | Equality | Boolean equality |
| `integer-equal` | Equality | Integer equality |
| `double-equal` | Equality | Double equality |
| `date-equal` | Equality | Date equality |
| `dateTime-equal` | Equality | DateTime equality |
| `time-equal` | Equality | Time equality |
| `integer-greater-than` | Comparison | Integer > |
| `integer-less-than` | Comparison | Integer < |
| `integer-greater-than-or-equal` | Comparison | Integer >= |
| `integer-less-than-or-equal` | Comparison | Integer <= |
| `double-greater-than` | Comparison | Double > |
| `double-less-than` | Comparison | Double < |
| `double-greater-than-or-equal` | Comparison | Double >= |
| `double-less-than-or-equal` | Comparison | Double <= |
| `string-greater-than` | Comparison | String > (lexicographic ordinal) |
| `string-less-than` | Comparison | String < (lexicographic ordinal) |
| `string-greater-than-or-equal` | Comparison | String >= (lexicographic ordinal) |
| `string-less-than-or-equal` | Comparison | String <= (lexicographic ordinal) |
| `date-greater-than` | Comparison | Date > |
| `date-less-than` | Comparison | Date < |
| `date-greater-than-or-equal` | Comparison | Date >= |
| `date-less-than-or-equal` | Comparison | Date <= |
| `dateTime-greater-than` | Comparison | DateTime > |
| `dateTime-less-than` | Comparison | DateTime < |
| `dateTime-greater-than-or-equal` | Comparison | DateTime >= |
| `dateTime-less-than-or-equal` | Comparison | DateTime <= |
| `time-greater-than` | Comparison | Time > |
| `time-less-than` | Comparison | Time < |
| `time-greater-than-or-equal` | Comparison | Time >= |
| `time-less-than-or-equal` | Comparison | Time <= |
| `integer-add` | Arithmetic | Integer addition (checked) |
| `integer-subtract` | Arithmetic | Integer subtraction (checked) |
| `integer-multiply` | Arithmetic | Integer multiplication (checked) |
| `integer-divide` | Arithmetic | Integer division |
| `integer-mod` | Arithmetic | Integer modulus |
| `integer-abs` | Arithmetic | Integer absolute value |
| `double-add` | Arithmetic | Double addition |
| `double-subtract` | Arithmetic | Double subtraction |
| `double-multiply` | Arithmetic | Double multiplication |
| `double-divide` | Arithmetic | Double division |
| `double-abs` | Arithmetic | Double absolute value |
| `round` | Arithmetic | Round double to nearest integer |
| `floor` | Arithmetic | Floor double to lower integer |
| `string-concatenate` | String | Concatenate 2+ strings |
| `string-starts-with` | String | Check prefix |
| `string-ends-with` | String | Check suffix |
| `string-contains` | String | Check substring containment |
| `string-substring` | String | Extract substring by index |
| `string-normalize-space` | String | Normalize whitespace |
| `string-normalize-to-lower-case` | String | Convert to lowercase |
| `string-length` | String | Get string length |
| `and` | Logical | Logical AND (short-circuit) |
| `or` | Logical | Logical OR (short-circuit) |
| `not` | Logical | Logical NOT |
| `n-of` | Logical | At least N of M are true |
| `string-one-and-only` | Bag | Extract single string from bag |
| `string-bag-size` | Bag | Count of string bag |
| `string-is-in` | Bag | Membership test in string bag |
| `string-bag` | Bag | Create string bag |
| `boolean-one-and-only` | Bag | Extract single boolean from bag |
| `boolean-bag-size` | Bag | Count of boolean bag |
| `boolean-is-in` | Bag | Membership test in boolean bag |
| `boolean-bag` | Bag | Create boolean bag |
| `integer-one-and-only` | Bag | Extract single integer from bag |
| `integer-bag-size` | Bag | Count of integer bag |
| `integer-is-in` | Bag | Membership test in integer bag |
| `integer-bag` | Bag | Create integer bag |
| `double-one-and-only` | Bag | Extract single double from bag |
| `double-bag-size` | Bag | Count of double bag |
| `double-is-in` | Bag | Membership test in double bag |
| `double-bag` | Bag | Create double bag |
| `date-one-and-only` | Bag | Extract single date from bag |
| `date-bag-size` | Bag | Count of date bag |
| `date-is-in` | Bag | Membership test in date bag |
| `date-bag` | Bag | Create date bag |
| `dateTime-one-and-only` | Bag | Extract single dateTime from bag |
| `dateTime-bag-size` | Bag | Count of dateTime bag |
| `dateTime-is-in` | Bag | Membership test in dateTime bag |
| `dateTime-bag` | Bag | Create dateTime bag |
| `time-one-and-only` | Bag | Extract single time from bag |
| `time-bag-size` | Bag | Count of time bag |
| `time-is-in` | Bag | Membership test in time bag |
| `time-bag` | Bag | Create time bag |
| `anyURI-one-and-only` | Bag | Extract single anyURI from bag |
| `anyURI-bag-size` | Bag | Count of anyURI bag |
| `anyURI-is-in` | Bag | Membership test in anyURI bag |
| `anyURI-bag` | Bag | Create anyURI bag |
| `string-intersection` | Set | Intersection of two string bags |
| `string-union` | Set | Union of two string bags |
| `string-subset` | Set | Subset test for string bags |
| `string-at-least-one-member-of` | Set | Overlap test for string bags |
| `string-set-equals` | Set | Set equality for string bags |
| `integer-intersection` | Set | Intersection of two integer bags |
| `integer-union` | Set | Union of two integer bags |
| `integer-subset` | Set | Subset test for integer bags |
| `integer-at-least-one-member-of` | Set | Overlap test for integer bags |
| `integer-set-equals` | Set | Set equality for integer bags |
| `double-intersection` | Set | Intersection of two double bags |
| `double-union` | Set | Union of two double bags |
| `double-subset` | Set | Subset test for double bags |
| `double-at-least-one-member-of` | Set | Overlap test for double bags |
| `double-set-equals` | Set | Set equality for double bags |
| `any-of` | Higher-Order | Apply function to any bag element |
| `all-of` | Higher-Order | Apply function to all bag elements |
| `any-of-any` | Higher-Order | Any pair from two bags |
| `all-of-any` | Higher-Order | All-of-any across two bags |
| `all-of-all` | Higher-Order | All pairs from two bags |
| `map` | Higher-Order | Transform bag with function |
| `string-from-integer` | Type Conversion | Integer to string |
| `integer-from-string` | Type Conversion | String to integer |
| `double-from-string` | Type Conversion | String to double |
| `boolean-from-string` | Type Conversion | String to boolean |
| `string-from-boolean` | Type Conversion | Boolean to string |
| `string-from-double` | Type Conversion | Double to string |
| `string-from-dateTime` | Type Conversion | DateTime to ISO 8601 string |
| `string-regexp-match` | Regular Expression | Regex pattern matching |

---

## 1. Equality Functions

Equality functions compare two values of the same type and return a boolean result. All comparisons are exact-match; no type coercion is performed between different data types.

**Source**: `Functions/Standard/EqualityFunctions.cs`
**XACML Reference**: Appendix A.3.1

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `string-equal` | `StringEqual` | `(string, string)` | `bool` | Ordinal string equality (`StringComparison.Ordinal`) |
| `boolean-equal` | `BooleanEqual` | `(bool, bool)` | `bool` | Boolean equality |
| `integer-equal` | `IntegerEqual` | `(int, int)` | `bool` | Integer equality |
| `double-equal` | `DoubleEqual` | `(double, double)` | `bool` | Double equality (uses `Double.Equals`) |
| `date-equal` | `DateEqual` | `(DateOnly, DateOnly)` | `bool` | Date equality |
| `dateTime-equal` | `DateTimeEqual` | `(DateTime, DateTime)` | `bool` | DateTime equality |
| `time-equal` | `TimeEqual` | `(TimeOnly, TimeOnly)` | `bool` | Time equality |

### `string-equal`
- **Constant**: `XACMLFunctionIds.StringEqual`
- **Parameters**: `(string, string)`
- **Returns**: `bool`
- **Description**: Returns true if both strings are equal using ordinal (case-sensitive, culture-independent) comparison.
- **XACML Reference**: A.3.1

### `boolean-equal`
- **Constant**: `XACMLFunctionIds.BooleanEqual`
- **Parameters**: `(bool, bool)`
- **Returns**: `bool`
- **Description**: Returns true if both boolean values are identical.
- **XACML Reference**: A.3.1

### `integer-equal`
- **Constant**: `XACMLFunctionIds.IntegerEqual`
- **Parameters**: `(int, int)`
- **Returns**: `bool`
- **Description**: Returns true if both integer values are equal.
- **XACML Reference**: A.3.1

### `double-equal`
- **Constant**: `XACMLFunctionIds.DoubleEqual`
- **Parameters**: `(double, double)`
- **Returns**: `bool`
- **Description**: Returns true if both double values are equal. Uses `Double.Equals()` for comparison, which handles NaN correctly (NaN != NaN).
- **XACML Reference**: A.3.1

### `date-equal`
- **Constant**: `XACMLFunctionIds.DateEqual`
- **Parameters**: `(DateOnly, DateOnly)`
- **Returns**: `bool`
- **Description**: Returns true if both date values represent the same calendar date.
- **XACML Reference**: A.3.1

### `dateTime-equal`
- **Constant**: `XACMLFunctionIds.DateTimeEqual`
- **Parameters**: `(DateTime, DateTime)`
- **Returns**: `bool`
- **Description**: Returns true if both dateTime values represent the same point in time.
- **XACML Reference**: A.3.1

### `time-equal`
- **Constant**: `XACMLFunctionIds.TimeEqual`
- **Parameters**: `(TimeOnly, TimeOnly)`
- **Returns**: `bool`
- **Description**: Returns true if both time values are identical.
- **XACML Reference**: A.3.1

**C# Apply example:**

```csharp
// Condition: subject.department == "Finance"
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.StringEqual,
    Arguments =
    [
        new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "department",
            DataType = XACMLDataTypes.String
        },
        new AttributeValue { DataType = XACMLDataTypes.String, Value = "Finance" }
    ]
};
```

---

## 2. Comparison Functions

Comparison functions perform ordering comparisons (greater-than, less-than, etc.) on typed values. String comparisons use lexicographic ordinal ordering. Date, DateTime, and Time comparisons use chronological ordering.

**Source**: `Functions/Standard/ComparisonFunctions.cs`
**XACML Reference**: Appendix A.3.2

### Integer Comparisons

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `integer-greater-than` | `IntegerGreaterThan` | `(int, int)` | `bool` | Returns true if first > second |
| `integer-less-than` | `IntegerLessThan` | `(int, int)` | `bool` | Returns true if first < second |
| `integer-greater-than-or-equal` | `IntegerGreaterThanOrEqual` | `(int, int)` | `bool` | Returns true if first >= second |
| `integer-less-than-or-equal` | `IntegerLessThanOrEqual` | `(int, int)` | `bool` | Returns true if first <= second |

### `integer-greater-than`
- **Constant**: `XACMLFunctionIds.IntegerGreaterThan`
- **Parameters**: `(int, int)`
- **Returns**: `bool`
- **Description**: Returns true if the first integer argument is strictly greater than the second.
- **XACML Reference**: A.3.2

### `integer-less-than`
- **Constant**: `XACMLFunctionIds.IntegerLessThan`
- **Parameters**: `(int, int)`
- **Returns**: `bool`
- **Description**: Returns true if the first integer argument is strictly less than the second.
- **XACML Reference**: A.3.2

### `integer-greater-than-or-equal`
- **Constant**: `XACMLFunctionIds.IntegerGreaterThanOrEqual`
- **Parameters**: `(int, int)`
- **Returns**: `bool`
- **Description**: Returns true if the first integer argument is greater than or equal to the second.
- **XACML Reference**: A.3.2

### `integer-less-than-or-equal`
- **Constant**: `XACMLFunctionIds.IntegerLessThanOrEqual`
- **Parameters**: `(int, int)`
- **Returns**: `bool`
- **Description**: Returns true if the first integer argument is less than or equal to the second.
- **XACML Reference**: A.3.2

### Double Comparisons

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `double-greater-than` | `DoubleGreaterThan` | `(double, double)` | `bool` | Returns true if first > second |
| `double-less-than` | `DoubleLessThan` | `(double, double)` | `bool` | Returns true if first < second |
| `double-greater-than-or-equal` | `DoubleGreaterThanOrEqual` | `(double, double)` | `bool` | Returns true if first >= second |
| `double-less-than-or-equal` | `DoubleLessThanOrEqual` | `(double, double)` | `bool` | Returns true if first <= second |

### `double-greater-than`
- **Constant**: `XACMLFunctionIds.DoubleGreaterThan`
- **Parameters**: `(double, double)`
- **Returns**: `bool`
- **Description**: Returns true if the first double argument is strictly greater than the second.
- **XACML Reference**: A.3.2

### `double-less-than`
- **Constant**: `XACMLFunctionIds.DoubleLessThan`
- **Parameters**: `(double, double)`
- **Returns**: `bool`
- **Description**: Returns true if the first double argument is strictly less than the second.
- **XACML Reference**: A.3.2

### `double-greater-than-or-equal`
- **Constant**: `XACMLFunctionIds.DoubleGreaterThanOrEqual`
- **Parameters**: `(double, double)`
- **Returns**: `bool`
- **Description**: Returns true if the first double argument is greater than or equal to the second.
- **XACML Reference**: A.3.2

### `double-less-than-or-equal`
- **Constant**: `XACMLFunctionIds.DoubleLessThanOrEqual`
- **Parameters**: `(double, double)`
- **Returns**: `bool`
- **Description**: Returns true if the first double argument is less than or equal to the second.
- **XACML Reference**: A.3.2

### String Comparisons

String comparisons use `StringComparison.Ordinal` for lexicographic ordering. This is culture-independent and case-sensitive.

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `string-greater-than` | `StringGreaterThan` | `(string, string)` | `bool` | Lexicographic: first > second |
| `string-less-than` | `StringLessThan` | `(string, string)` | `bool` | Lexicographic: first < second |
| `string-greater-than-or-equal` | `StringGreaterThanOrEqual` | `(string, string)` | `bool` | Lexicographic: first >= second |
| `string-less-than-or-equal` | `StringLessThanOrEqual` | `(string, string)` | `bool` | Lexicographic: first <= second |

### `string-greater-than`
- **Constant**: `XACMLFunctionIds.StringGreaterThan`
- **Parameters**: `(string, string)`
- **Returns**: `bool`
- **Description**: Returns true if the first string is lexicographically greater than the second using ordinal comparison.
- **XACML Reference**: A.3.2

### `string-less-than`
- **Constant**: `XACMLFunctionIds.StringLessThan`
- **Parameters**: `(string, string)`
- **Returns**: `bool`
- **Description**: Returns true if the first string is lexicographically less than the second using ordinal comparison.
- **XACML Reference**: A.3.2

### `string-greater-than-or-equal`
- **Constant**: `XACMLFunctionIds.StringGreaterThanOrEqual`
- **Parameters**: `(string, string)`
- **Returns**: `bool`
- **Description**: Returns true if the first string is lexicographically greater than or equal to the second using ordinal comparison.
- **XACML Reference**: A.3.2

### `string-less-than-or-equal`
- **Constant**: `XACMLFunctionIds.StringLessThanOrEqual`
- **Parameters**: `(string, string)`
- **Returns**: `bool`
- **Description**: Returns true if the first string is lexicographically less than or equal to the second using ordinal comparison.
- **XACML Reference**: A.3.2

### Date Comparisons

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `date-greater-than` | `DateGreaterThan` | `(DateOnly, DateOnly)` | `bool` | Chronological: first > second |
| `date-less-than` | `DateLessThan` | `(DateOnly, DateOnly)` | `bool` | Chronological: first < second |
| `date-greater-than-or-equal` | `DateGreaterThanOrEqual` | `(DateOnly, DateOnly)` | `bool` | Chronological: first >= second |
| `date-less-than-or-equal` | `DateLessThanOrEqual` | `(DateOnly, DateOnly)` | `bool` | Chronological: first <= second |

### `date-greater-than`
- **Constant**: `XACMLFunctionIds.DateGreaterThan`
- **Parameters**: `(DateOnly, DateOnly)`
- **Returns**: `bool`
- **Description**: Returns true if the first date is chronologically after the second.
- **XACML Reference**: A.3.2

### `date-less-than`
- **Constant**: `XACMLFunctionIds.DateLessThan`
- **Parameters**: `(DateOnly, DateOnly)`
- **Returns**: `bool`
- **Description**: Returns true if the first date is chronologically before the second.
- **XACML Reference**: A.3.2

### `date-greater-than-or-equal`
- **Constant**: `XACMLFunctionIds.DateGreaterThanOrEqual`
- **Parameters**: `(DateOnly, DateOnly)`
- **Returns**: `bool`
- **Description**: Returns true if the first date is on or after the second.
- **XACML Reference**: A.3.2

### `date-less-than-or-equal`
- **Constant**: `XACMLFunctionIds.DateLessThanOrEqual`
- **Parameters**: `(DateOnly, DateOnly)`
- **Returns**: `bool`
- **Description**: Returns true if the first date is on or before the second.
- **XACML Reference**: A.3.2

### DateTime Comparisons

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `dateTime-greater-than` | `DateTimeGreaterThan` | `(DateTime, DateTime)` | `bool` | Chronological: first > second |
| `dateTime-less-than` | `DateTimeLessThan` | `(DateTime, DateTime)` | `bool` | Chronological: first < second |
| `dateTime-greater-than-or-equal` | `DateTimeGreaterThanOrEqual` | `(DateTime, DateTime)` | `bool` | Chronological: first >= second |
| `dateTime-less-than-or-equal` | `DateTimeLessThanOrEqual` | `(DateTime, DateTime)` | `bool` | Chronological: first <= second |

### `dateTime-greater-than`
- **Constant**: `XACMLFunctionIds.DateTimeGreaterThan`
- **Parameters**: `(DateTime, DateTime)`
- **Returns**: `bool`
- **Description**: Returns true if the first dateTime is chronologically after the second.
- **XACML Reference**: A.3.2

### `dateTime-less-than`
- **Constant**: `XACMLFunctionIds.DateTimeLessThan`
- **Parameters**: `(DateTime, DateTime)`
- **Returns**: `bool`
- **Description**: Returns true if the first dateTime is chronologically before the second.
- **XACML Reference**: A.3.2

### `dateTime-greater-than-or-equal`
- **Constant**: `XACMLFunctionIds.DateTimeGreaterThanOrEqual`
- **Parameters**: `(DateTime, DateTime)`
- **Returns**: `bool`
- **Description**: Returns true if the first dateTime is on or after the second.
- **XACML Reference**: A.3.2

### `dateTime-less-than-or-equal`
- **Constant**: `XACMLFunctionIds.DateTimeLessThanOrEqual`
- **Parameters**: `(DateTime, DateTime)`
- **Returns**: `bool`
- **Description**: Returns true if the first dateTime is on or before the second.
- **XACML Reference**: A.3.2

### Time Comparisons

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `time-greater-than` | `TimeGreaterThan` | `(TimeOnly, TimeOnly)` | `bool` | Chronological: first > second |
| `time-less-than` | `TimeLessThan` | `(TimeOnly, TimeOnly)` | `bool` | Chronological: first < second |
| `time-greater-than-or-equal` | `TimeGreaterThanOrEqual` | `(TimeOnly, TimeOnly)` | `bool` | Chronological: first >= second |
| `time-less-than-or-equal` | `TimeLessThanOrEqual` | `(TimeOnly, TimeOnly)` | `bool` | Chronological: first <= second |

### `time-greater-than`
- **Constant**: `XACMLFunctionIds.TimeGreaterThan`
- **Parameters**: `(TimeOnly, TimeOnly)`
- **Returns**: `bool`
- **Description**: Returns true if the first time is chronologically after the second.
- **XACML Reference**: A.3.2

### `time-less-than`
- **Constant**: `XACMLFunctionIds.TimeLessThan`
- **Parameters**: `(TimeOnly, TimeOnly)`
- **Returns**: `bool`
- **Description**: Returns true if the first time is chronologically before the second.
- **XACML Reference**: A.3.2

### `time-greater-than-or-equal`
- **Constant**: `XACMLFunctionIds.TimeGreaterThanOrEqual`
- **Parameters**: `(TimeOnly, TimeOnly)`
- **Returns**: `bool`
- **Description**: Returns true if the first time is at or after the second.
- **XACML Reference**: A.3.2

### `time-less-than-or-equal`
- **Constant**: `XACMLFunctionIds.TimeLessThanOrEqual`
- **Parameters**: `(TimeOnly, TimeOnly)`
- **Returns**: `bool`
- **Description**: Returns true if the first time is at or before the second.
- **XACML Reference**: A.3.2

**C# Apply example:**

```csharp
// Condition: resource.amount > 10000
var condition = new Apply
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

---

## 3. Arithmetic Functions

Arithmetic functions perform mathematical operations on integer and double values. Integer operations use `checked` arithmetic and will throw `OverflowException` on overflow. Division by zero throws `InvalidOperationException`.

**Source**: `Functions/Standard/ArithmeticFunctions.cs`
**XACML Reference**: Appendix A.3.3

### Integer Arithmetic

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `integer-add` | `IntegerAdd` | `(int, int)` | `int` | Checked addition |
| `integer-subtract` | `IntegerSubtract` | `(int, int)` | `int` | Checked subtraction |
| `integer-multiply` | `IntegerMultiply` | `(int, int)` | `int` | Checked multiplication |
| `integer-divide` | `IntegerDivide` | `(int, int)` | `int` | Integer division (truncating) |
| `integer-mod` | `IntegerMod` | `(int, int)` | `int` | Integer modulus |
| `integer-abs` | `IntegerAbs` | `(int)` | `int` | Absolute value |

### `integer-add`
- **Constant**: `XACMLFunctionIds.IntegerAdd`
- **Parameters**: `(int, int)`
- **Returns**: `int`
- **Description**: Returns the sum of two integers. Uses `checked` arithmetic; throws `OverflowException` on overflow.
- **XACML Reference**: A.3.3

### `integer-subtract`
- **Constant**: `XACMLFunctionIds.IntegerSubtract`
- **Parameters**: `(int, int)`
- **Returns**: `int`
- **Description**: Returns the difference of two integers (first - second). Uses `checked` arithmetic.
- **XACML Reference**: A.3.3

### `integer-multiply`
- **Constant**: `XACMLFunctionIds.IntegerMultiply`
- **Parameters**: `(int, int)`
- **Returns**: `int`
- **Description**: Returns the product of two integers. Uses `checked` arithmetic.
- **XACML Reference**: A.3.3

### `integer-divide`
- **Constant**: `XACMLFunctionIds.IntegerDivide`
- **Parameters**: `(int, int)`
- **Returns**: `int`
- **Description**: Returns the integer quotient of two integers (truncating toward zero). Throws `InvalidOperationException` if divisor is zero.
- **XACML Reference**: A.3.3

### `integer-mod`
- **Constant**: `XACMLFunctionIds.IntegerMod`
- **Parameters**: `(int, int)`
- **Returns**: `int`
- **Description**: Returns the remainder of integer division (first % second). Throws `InvalidOperationException` if divisor is zero.
- **XACML Reference**: A.3.3

### `integer-abs`
- **Constant**: `XACMLFunctionIds.IntegerAbs`
- **Parameters**: `(int)`
- **Returns**: `int`
- **Description**: Returns the absolute value of an integer.
- **XACML Reference**: A.3.3

### Double Arithmetic

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `double-add` | `DoubleAdd` | `(double, double)` | `double` | Addition |
| `double-subtract` | `DoubleSubtract` | `(double, double)` | `double` | Subtraction |
| `double-multiply` | `DoubleMultiply` | `(double, double)` | `double` | Multiplication |
| `double-divide` | `DoubleDivide` | `(double, double)` | `double` | Division |
| `double-abs` | `DoubleAbs` | `(double)` | `double` | Absolute value |

### `double-add`
- **Constant**: `XACMLFunctionIds.DoubleAdd`
- **Parameters**: `(double, double)`
- **Returns**: `double`
- **Description**: Returns the sum of two doubles.
- **XACML Reference**: A.3.3

### `double-subtract`
- **Constant**: `XACMLFunctionIds.DoubleSubtract`
- **Parameters**: `(double, double)`
- **Returns**: `double`
- **Description**: Returns the difference of two doubles (first - second).
- **XACML Reference**: A.3.3

### `double-multiply`
- **Constant**: `XACMLFunctionIds.DoubleMultiply`
- **Parameters**: `(double, double)`
- **Returns**: `double`
- **Description**: Returns the product of two doubles.
- **XACML Reference**: A.3.3

### `double-divide`
- **Constant**: `XACMLFunctionIds.DoubleDivide`
- **Parameters**: `(double, double)`
- **Returns**: `double`
- **Description**: Returns the quotient of two doubles. Throws `InvalidOperationException` if divisor is exactly 0.0.
- **XACML Reference**: A.3.3

### `double-abs`
- **Constant**: `XACMLFunctionIds.DoubleAbs`
- **Parameters**: `(double)`
- **Returns**: `double`
- **Description**: Returns the absolute value of a double.
- **XACML Reference**: A.3.3

### Rounding Functions

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `round` | `Round` | `(double)` | `double` | Round to nearest integer (AwayFromZero) |
| `floor` | `Floor` | `(double)` | `double` | Floor to nearest lower integer |

### `round`
- **Constant**: `XACMLFunctionIds.Round`
- **Parameters**: `(double)`
- **Returns**: `double`
- **Description**: Rounds a double to the nearest integer value. Uses `MidpointRounding.AwayFromZero` (0.5 rounds up). Returns the result as a double.
- **XACML Reference**: A.3.3

### `floor`
- **Constant**: `XACMLFunctionIds.Floor`
- **Parameters**: `(double)`
- **Returns**: `double`
- **Description**: Returns the largest integer value less than or equal to the argument. Returns the result as a double.
- **XACML Reference**: A.3.3

**C# Apply example:**

```csharp
// Condition: integer-add(resource.base-price, resource.tax) > 500
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.IntegerGreaterThan,
    Arguments =
    [
        new Apply
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
        },
        new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 500 }
    ]
};
```

---

## 4. String Functions

String manipulation functions for concatenation, searching, substring extraction, normalization, and length. All string comparisons use ordinal (culture-independent) semantics.

**Source**: `Functions/Standard/StringFunctions.cs`
**XACML Reference**: Appendix A.3.4

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `string-concatenate` | `StringConcatenate` | `(string, string, ...)` | `string` | Concatenate 2+ strings |
| `string-starts-with` | `StringStartsWith` | `(string, string)` | `bool` | Check if second string starts with first |
| `string-ends-with` | `StringEndsWith` | `(string, string)` | `bool` | Check if second string ends with first |
| `string-contains` | `StringContains` | `(string, string)` | `bool` | Check if second string contains first |
| `string-substring` | `StringSubstring` | `(string, int, int)` | `string` | Extract substring by index range |
| `string-normalize-space` | `StringNormalizeSpace` | `(string)` | `string` | Trim and collapse whitespace |
| `string-normalize-to-lower-case` | `StringNormalizeToLowerCase` | `(string)` | `string` | Convert to invariant lowercase |
| `string-length` | `StringLength` | `(string)` | `int` | Get character count |

### `string-concatenate`
- **Constant**: `XACMLFunctionIds.StringConcatenate`
- **Parameters**: `(string, string, ...)` -- variadic, minimum 2
- **Returns**: `string`
- **Description**: Concatenates two or more string arguments into a single string. Accepts a variable number of arguments (minimum 2).
- **XACML Reference**: A.3.4

### `string-starts-with`
- **Constant**: `XACMLFunctionIds.StringStartsWith`
- **Parameters**: `(string substring, string fullString)`
- **Returns**: `bool`
- **Description**: Returns true if `fullString` starts with `substring`. Note the XACML parameter order: substring first, full string second. Uses ordinal comparison.
- **XACML Reference**: A.3.4

### `string-ends-with`
- **Constant**: `XACMLFunctionIds.StringEndsWith`
- **Parameters**: `(string substring, string fullString)`
- **Returns**: `bool`
- **Description**: Returns true if `fullString` ends with `substring`. Note the XACML parameter order: substring first, full string second. Uses ordinal comparison.
- **XACML Reference**: A.3.4

### `string-contains`
- **Constant**: `XACMLFunctionIds.StringContains`
- **Parameters**: `(string substring, string fullString)`
- **Returns**: `bool`
- **Description**: Returns true if `fullString` contains `substring`. Note the XACML parameter order: substring first, full string second. Uses ordinal comparison.
- **XACML Reference**: A.3.4

### `string-substring`
- **Constant**: `XACMLFunctionIds.StringSubstring`
- **Parameters**: `(string source, int beginIndex, int endIndex)`
- **Returns**: `string`
- **Description**: Extracts a substring from `source` starting at `beginIndex` (0-based, inclusive) up to `endIndex` (exclusive). If `endIndex` is `-1`, extracts to the end of the string. Throws `InvalidOperationException` if indices are out of range.
- **XACML Reference**: A.3.4

### `string-normalize-space`
- **Constant**: `XACMLFunctionIds.StringNormalizeSpace`
- **Parameters**: `(string)`
- **Returns**: `string`
- **Description**: Trims leading and trailing whitespace, then collapses all internal whitespace sequences to a single space character. Uses regex `\s+` for whitespace detection.
- **XACML Reference**: A.3.4

### `string-normalize-to-lower-case`
- **Constant**: `XACMLFunctionIds.StringNormalizeToLowerCase`
- **Parameters**: `(string)`
- **Returns**: `string`
- **Description**: Converts the entire string to lowercase using invariant culture rules (`ToLowerInvariant()`).
- **XACML Reference**: A.3.4

### `string-length`
- **Constant**: `XACMLFunctionIds.StringLength`
- **Parameters**: `(string)`
- **Returns**: `int`
- **Description**: Returns the number of characters (UTF-16 code units) in the string.
- **XACML Reference**: A.3.4

**C# Apply example:**

```csharp
// Condition: string-starts-with("admin", subject.role)
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.StringStartsWith,
    Arguments =
    [
        new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" },
        new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "role",
            DataType = XACMLDataTypes.String
        }
    ]
};
```

---

## 5. Logical Functions

Boolean logic functions with short-circuit evaluation. These are the primary combining operators for building complex conditions from simpler boolean expressions.

**Source**: `Functions/Standard/LogicalFunctions.cs`
**XACML Reference**: Appendix A.3.5

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `and` | `And` | `(bool, bool, ...)` | `bool` | Short-circuit AND (min 1 arg) |
| `or` | `Or` | `(bool, bool, ...)` | `bool` | Short-circuit OR (min 1 arg) |
| `not` | `Not` | `(bool)` | `bool` | Boolean negation |
| `n-of` | `NOf` | `(int, bool, bool, ...)` | `bool` | At least N of M are true |

### `and`
- **Constant**: `XACMLFunctionIds.And`
- **Parameters**: `(bool, bool, ...)` -- variadic, minimum 1
- **Returns**: `bool`
- **Description**: Returns true if ALL boolean arguments are true. Uses short-circuit evaluation: stops at the first `false` argument. Accepts a variable number of boolean arguments (minimum 1).
- **XACML Reference**: A.3.5

### `or`
- **Constant**: `XACMLFunctionIds.Or`
- **Parameters**: `(bool, bool, ...)` -- variadic, minimum 1
- **Returns**: `bool`
- **Description**: Returns true if ANY boolean argument is true. Uses short-circuit evaluation: stops at the first `true` argument. Accepts a variable number of boolean arguments (minimum 1).
- **XACML Reference**: A.3.5

### `not`
- **Constant**: `XACMLFunctionIds.Not`
- **Parameters**: `(bool)`
- **Returns**: `bool`
- **Description**: Returns the logical negation of the boolean argument. `true` becomes `false`, `false` becomes `true`.
- **XACML Reference**: A.3.5

### `n-of`
- **Constant**: `XACMLFunctionIds.NOf`
- **Parameters**: `(int N, bool, bool, ...)` -- first arg is integer threshold, rest are booleans
- **Returns**: `bool`
- **Description**: Returns true if at least `N` of the remaining boolean arguments are true. The first argument is the integer threshold `N`, and the remaining arguments are boolean values. Uses short-circuit evaluation: stops as soon as `N` true values are found.
- **XACML Reference**: A.3.5

**C# Apply example:**

```csharp
// Condition: subject.department == "Finance" AND resource.amount > 10000
var condition = new Apply
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
                    AttributeId = "department",
                    DataType = XACMLDataTypes.String
                },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "Finance" }
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
                new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 10000 }
            ]
        }
    ]
};
```

---

## 6. Bag Functions

Bag functions operate on XACML attribute bags (multi-valued attribute collections). They provide operations to extract single values, measure bag size, test membership, and construct bags. Functions are provided for all 8 standard data types: string, boolean, integer, double, date, dateTime, time, and anyURI.

**Source**: `Functions/Standard/BagFunctions.cs`
**XACML Reference**: Appendix A.3.10

For each type `T`, four functions are registered:

| Pattern | Parameters | Returns | Description |
|---|---|---|---|
| `T-one-and-only` | `(Bag<T>)` | `T` | Extract single value; error if bag size is not exactly 1 |
| `T-bag-size` | `(Bag<T>)` | `int` | Return the number of values in the bag |
| `T-is-in` | `(T, Bag<T>)` | `bool` | Return true if the value exists in the bag |
| `T-bag` | `(T, T, ...)` | `Bag<T>` | Create a new bag from the given values |

### String Bag Functions

### `string-one-and-only`
- **Constant**: `XACMLFunctionIds.StringOneAndOnly`
- **Parameters**: `(Bag<string>)`
- **Returns**: `string`
- **Description**: Extracts the single value from a string bag. Throws an error if the bag does not contain exactly one value.
- **XACML Reference**: A.3.10

### `string-bag-size`
- **Constant**: `XACMLFunctionIds.StringBagSize`
- **Parameters**: `(Bag<string>)`
- **Returns**: `int`
- **Description**: Returns the number of values in a string bag.
- **XACML Reference**: A.3.10

### `string-is-in`
- **Constant**: `XACMLFunctionIds.StringIsIn`
- **Parameters**: `(string, Bag<string>)`
- **Returns**: `bool`
- **Description**: Returns true if the string value exists in the bag. Uses value equality for comparison.
- **XACML Reference**: A.3.10

### `string-bag`
- **Constant**: `XACMLFunctionIds.StringBag`
- **Parameters**: `(string, string, ...)` -- variadic, 0 or more
- **Returns**: `Bag<string>`
- **Description**: Creates a new string bag containing the given values.
- **XACML Reference**: A.3.10

### Boolean Bag Functions

### `boolean-one-and-only`
- **Constant**: `XACMLFunctionIds.BooleanOneAndOnly`
- **Parameters**: `(Bag<bool>)`
- **Returns**: `bool`
- **Description**: Extracts the single value from a boolean bag. Throws an error if the bag does not contain exactly one value.
- **XACML Reference**: A.3.10

### `boolean-bag-size`
- **Constant**: `XACMLFunctionIds.BooleanBagSize`
- **Parameters**: `(Bag<bool>)`
- **Returns**: `int`
- **Description**: Returns the number of values in a boolean bag.
- **XACML Reference**: A.3.10

### `boolean-is-in`
- **Constant**: `XACMLFunctionIds.BooleanIsIn`
- **Parameters**: `(bool, Bag<bool>)`
- **Returns**: `bool`
- **Description**: Returns true if the boolean value exists in the bag.
- **XACML Reference**: A.3.10

### `boolean-bag`
- **Constant**: `XACMLFunctionIds.BooleanBag`
- **Parameters**: `(bool, bool, ...)` -- variadic
- **Returns**: `Bag<bool>`
- **Description**: Creates a new boolean bag containing the given values.
- **XACML Reference**: A.3.10

### Integer Bag Functions

### `integer-one-and-only`
- **Constant**: `XACMLFunctionIds.IntegerOneAndOnly`
- **Parameters**: `(Bag<int>)`
- **Returns**: `int`
- **Description**: Extracts the single value from an integer bag. Throws an error if the bag does not contain exactly one value.
- **XACML Reference**: A.3.10

### `integer-bag-size`
- **Constant**: `XACMLFunctionIds.IntegerBagSize`
- **Parameters**: `(Bag<int>)`
- **Returns**: `int`
- **Description**: Returns the number of values in an integer bag.
- **XACML Reference**: A.3.10

### `integer-is-in`
- **Constant**: `XACMLFunctionIds.IntegerIsIn`
- **Parameters**: `(int, Bag<int>)`
- **Returns**: `bool`
- **Description**: Returns true if the integer value exists in the bag.
- **XACML Reference**: A.3.10

### `integer-bag`
- **Constant**: `XACMLFunctionIds.IntegerBag`
- **Parameters**: `(int, int, ...)` -- variadic
- **Returns**: `Bag<int>`
- **Description**: Creates a new integer bag containing the given values.
- **XACML Reference**: A.3.10

### Double Bag Functions

### `double-one-and-only`
- **Constant**: `XACMLFunctionIds.DoubleOneAndOnly`
- **Parameters**: `(Bag<double>)`
- **Returns**: `double`
- **Description**: Extracts the single value from a double bag. Throws an error if the bag does not contain exactly one value.
- **XACML Reference**: A.3.10

### `double-bag-size`
- **Constant**: `XACMLFunctionIds.DoubleBagSize`
- **Parameters**: `(Bag<double>)`
- **Returns**: `int`
- **Description**: Returns the number of values in a double bag.
- **XACML Reference**: A.3.10

### `double-is-in`
- **Constant**: `XACMLFunctionIds.DoubleIsIn`
- **Parameters**: `(double, Bag<double>)`
- **Returns**: `bool`
- **Description**: Returns true if the double value exists in the bag.
- **XACML Reference**: A.3.10

### `double-bag`
- **Constant**: `XACMLFunctionIds.DoubleBag`
- **Parameters**: `(double, double, ...)` -- variadic
- **Returns**: `Bag<double>`
- **Description**: Creates a new double bag containing the given values.
- **XACML Reference**: A.3.10

### Date Bag Functions

### `date-one-and-only`
- **Constant**: `XACMLFunctionIds.DateOneAndOnly`
- **Parameters**: `(Bag<DateOnly>)`
- **Returns**: `DateOnly`
- **Description**: Extracts the single value from a date bag. Throws an error if the bag does not contain exactly one value.
- **XACML Reference**: A.3.10

### `date-bag-size`
- **Constant**: `XACMLFunctionIds.DateBagSize`
- **Parameters**: `(Bag<DateOnly>)`
- **Returns**: `int`
- **Description**: Returns the number of values in a date bag.
- **XACML Reference**: A.3.10

### `date-is-in`
- **Constant**: `XACMLFunctionIds.DateIsIn`
- **Parameters**: `(DateOnly, Bag<DateOnly>)`
- **Returns**: `bool`
- **Description**: Returns true if the date value exists in the bag.
- **XACML Reference**: A.3.10

### `date-bag`
- **Constant**: `XACMLFunctionIds.DateBag`
- **Parameters**: `(DateOnly, DateOnly, ...)` -- variadic
- **Returns**: `Bag<DateOnly>`
- **Description**: Creates a new date bag containing the given values.
- **XACML Reference**: A.3.10

### DateTime Bag Functions

### `dateTime-one-and-only`
- **Constant**: `XACMLFunctionIds.DateTimeOneAndOnly`
- **Parameters**: `(Bag<DateTime>)`
- **Returns**: `DateTime`
- **Description**: Extracts the single value from a dateTime bag. Throws an error if the bag does not contain exactly one value.
- **XACML Reference**: A.3.10

### `dateTime-bag-size`
- **Constant**: `XACMLFunctionIds.DateTimeBagSize`
- **Parameters**: `(Bag<DateTime>)`
- **Returns**: `int`
- **Description**: Returns the number of values in a dateTime bag.
- **XACML Reference**: A.3.10

### `dateTime-is-in`
- **Constant**: `XACMLFunctionIds.DateTimeIsIn`
- **Parameters**: `(DateTime, Bag<DateTime>)`
- **Returns**: `bool`
- **Description**: Returns true if the dateTime value exists in the bag.
- **XACML Reference**: A.3.10

### `dateTime-bag`
- **Constant**: `XACMLFunctionIds.DateTimeBag`
- **Parameters**: `(DateTime, DateTime, ...)` -- variadic
- **Returns**: `Bag<DateTime>`
- **Description**: Creates a new dateTime bag containing the given values.
- **XACML Reference**: A.3.10

### Time Bag Functions

### `time-one-and-only`
- **Constant**: `XACMLFunctionIds.TimeOneAndOnly`
- **Parameters**: `(Bag<TimeOnly>)`
- **Returns**: `TimeOnly`
- **Description**: Extracts the single value from a time bag. Throws an error if the bag does not contain exactly one value.
- **XACML Reference**: A.3.10

### `time-bag-size`
- **Constant**: `XACMLFunctionIds.TimeBagSize`
- **Parameters**: `(Bag<TimeOnly>)`
- **Returns**: `int`
- **Description**: Returns the number of values in a time bag.
- **XACML Reference**: A.3.10

### `time-is-in`
- **Constant**: `XACMLFunctionIds.TimeIsIn`
- **Parameters**: `(TimeOnly, Bag<TimeOnly>)`
- **Returns**: `bool`
- **Description**: Returns true if the time value exists in the bag.
- **XACML Reference**: A.3.10

### `time-bag`
- **Constant**: `XACMLFunctionIds.TimeBag`
- **Parameters**: `(TimeOnly, TimeOnly, ...)` -- variadic
- **Returns**: `Bag<TimeOnly>`
- **Description**: Creates a new time bag containing the given values.
- **XACML Reference**: A.3.10

### AnyURI Bag Functions

### `anyURI-one-and-only`
- **Constant**: `XACMLFunctionIds.AnyURIOneAndOnly`
- **Parameters**: `(Bag<anyURI>)`
- **Returns**: `string` (URI)
- **Description**: Extracts the single value from an anyURI bag. Throws an error if the bag does not contain exactly one value.
- **XACML Reference**: A.3.10

### `anyURI-bag-size`
- **Constant**: `XACMLFunctionIds.AnyURIBagSize`
- **Parameters**: `(Bag<anyURI>)`
- **Returns**: `int`
- **Description**: Returns the number of values in an anyURI bag.
- **XACML Reference**: A.3.10

### `anyURI-is-in`
- **Constant**: `XACMLFunctionIds.AnyURIIsIn`
- **Parameters**: `(string, Bag<anyURI>)`
- **Returns**: `bool`
- **Description**: Returns true if the anyURI value exists in the bag.
- **XACML Reference**: A.3.10

### `anyURI-bag`
- **Constant**: `XACMLFunctionIds.AnyURIBag`
- **Parameters**: `(string, string, ...)` -- variadic (URI values)
- **Returns**: `Bag<anyURI>`
- **Description**: Creates a new anyURI bag containing the given values.
- **XACML Reference**: A.3.10

**C# Apply example:**

```csharp
// Condition: string-is-in("admin", subject.roles)
// Checks if "admin" is one of the subject's roles (multi-valued attribute)
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.StringIsIn,
    Arguments =
    [
        new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" },
        new AttributeDesignator
        {
            Category = AttributeCategory.Subject,
            AttributeId = "roles",
            DataType = XACMLDataTypes.String
        }
    ]
};
```

---

## 7. Set Functions

Set functions perform set-theoretic operations on attribute bags. They are available for string, integer, and double types. Each type provides five operations: intersection, union, subset test, overlap test, and set equality.

**Source**: `Functions/Standard/SetFunctions.cs`
**XACML Reference**: Appendix A.3.11

For each type `T`, five functions are registered:

| Pattern | Parameters | Returns | Description |
|---|---|---|---|
| `T-intersection` | `(Bag<T>, Bag<T>)` | `Bag<T>` | Values present in both bags (deduplicated) |
| `T-union` | `(Bag<T>, Bag<T>)` | `Bag<T>` | All unique values from both bags |
| `T-subset` | `(Bag<T>, Bag<T>)` | `bool` | True if first bag is a subset of second |
| `T-at-least-one-member-of` | `(Bag<T>, Bag<T>)` | `bool` | True if bags share at least one common value |
| `T-set-equals` | `(Bag<T>, Bag<T>)` | `bool` | True if bags contain the same set of values |

### String Set Functions

### `string-intersection`
- **Constant**: `XACMLFunctionIds.StringIntersection`
- **Parameters**: `(Bag<string>, Bag<string>)`
- **Returns**: `Bag<string>`
- **Description**: Returns a new bag containing values that exist in both input bags. The result is deduplicated. Returns an empty bag if there are no common values.
- **XACML Reference**: A.3.11

### `string-union`
- **Constant**: `XACMLFunctionIds.StringUnion`
- **Parameters**: `(Bag<string>, Bag<string>)`
- **Returns**: `Bag<string>`
- **Description**: Returns a new bag containing all unique values from both input bags. Duplicate values are included only once.
- **XACML Reference**: A.3.11

### `string-subset`
- **Constant**: `XACMLFunctionIds.StringSubset`
- **Parameters**: `(Bag<string>, Bag<string>)`
- **Returns**: `bool`
- **Description**: Returns true if every value in the first bag also exists in the second bag. An empty first bag is always a subset.
- **XACML Reference**: A.3.11

### `string-at-least-one-member-of`
- **Constant**: `XACMLFunctionIds.StringAtLeastOneMemberOf`
- **Parameters**: `(Bag<string>, Bag<string>)`
- **Returns**: `bool`
- **Description**: Returns true if the two bags share at least one common value.
- **XACML Reference**: A.3.11

### `string-set-equals`
- **Constant**: `XACMLFunctionIds.StringSetEquals`
- **Parameters**: `(Bag<string>, Bag<string>)`
- **Returns**: `bool`
- **Description**: Returns true if both bags contain the same set of values (ignoring duplicates and ordering). Equivalent to: first is subset of second AND second is subset of first.
- **XACML Reference**: A.3.11

### Integer Set Functions

### `integer-intersection`
- **Constant**: `XACMLFunctionIds.IntegerIntersection`
- **Parameters**: `(Bag<int>, Bag<int>)`
- **Returns**: `Bag<int>`
- **Description**: Returns a new bag containing integer values that exist in both input bags. The result is deduplicated.
- **XACML Reference**: A.3.11

### `integer-union`
- **Constant**: `XACMLFunctionIds.IntegerUnion`
- **Parameters**: `(Bag<int>, Bag<int>)`
- **Returns**: `Bag<int>`
- **Description**: Returns a new bag containing all unique integer values from both input bags.
- **XACML Reference**: A.3.11

### `integer-subset`
- **Constant**: `XACMLFunctionIds.IntegerSubset`
- **Parameters**: `(Bag<int>, Bag<int>)`
- **Returns**: `bool`
- **Description**: Returns true if every value in the first integer bag also exists in the second.
- **XACML Reference**: A.3.11

### `integer-at-least-one-member-of`
- **Constant**: `XACMLFunctionIds.IntegerAtLeastOneMemberOf`
- **Parameters**: `(Bag<int>, Bag<int>)`
- **Returns**: `bool`
- **Description**: Returns true if the two integer bags share at least one common value.
- **XACML Reference**: A.3.11

### `integer-set-equals`
- **Constant**: `XACMLFunctionIds.IntegerSetEquals`
- **Parameters**: `(Bag<int>, Bag<int>)`
- **Returns**: `bool`
- **Description**: Returns true if both integer bags contain the same set of values.
- **XACML Reference**: A.3.11

### Double Set Functions

### `double-intersection`
- **Constant**: `XACMLFunctionIds.DoubleIntersection`
- **Parameters**: `(Bag<double>, Bag<double>)`
- **Returns**: `Bag<double>`
- **Description**: Returns a new bag containing double values that exist in both input bags. The result is deduplicated.
- **XACML Reference**: A.3.11

### `double-union`
- **Constant**: `XACMLFunctionIds.DoubleUnion`
- **Parameters**: `(Bag<double>, Bag<double>)`
- **Returns**: `Bag<double>`
- **Description**: Returns a new bag containing all unique double values from both input bags.
- **XACML Reference**: A.3.11

### `double-subset`
- **Constant**: `XACMLFunctionIds.DoubleSubset`
- **Parameters**: `(Bag<double>, Bag<double>)`
- **Returns**: `bool`
- **Description**: Returns true if every value in the first double bag also exists in the second.
- **XACML Reference**: A.3.11

### `double-at-least-one-member-of`
- **Constant**: `XACMLFunctionIds.DoubleAtLeastOneMemberOf`
- **Parameters**: `(Bag<double>, Bag<double>)`
- **Returns**: `bool`
- **Description**: Returns true if the two double bags share at least one common value.
- **XACML Reference**: A.3.11

### `double-set-equals`
- **Constant**: `XACMLFunctionIds.DoubleSetEquals`
- **Parameters**: `(Bag<double>, Bag<double>)`
- **Returns**: `bool`
- **Description**: Returns true if both double bags contain the same set of values.
- **XACML Reference**: A.3.11

**C# Apply example:**

```csharp
// Condition: string-at-least-one-member-of(subject.roles, resource.required-roles)
// Checks if any of the subject's roles matches a required role for the resource
var condition = new Apply
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
            AttributeId = "required-roles",
            DataType = XACMLDataTypes.String
        }
    ]
};
```

---

## 8. Higher-Order Functions

Higher-order functions take a function identifier as their first argument and apply it to elements of one or two bags. They enable powerful composition of simpler functions over multi-valued attributes.

**Source**: `Functions/Standard/HigherOrderFunctions.cs`
**XACML Reference**: Appendix A.3.12

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `any-of` | `AnyOfFunc` | `(string, T, Bag<T>)` | `bool` | True if fn(value, x) is true for ANY x in bag |
| `all-of` | `AllOfFunc` | `(string, T, Bag<T>)` | `bool` | True if fn(value, x) is true for ALL x in bag |
| `any-of-any` | `AnyOfAny` | `(string, Bag<T>, Bag<T>)` | `bool` | True if fn(x, y) is true for ANY pair (x, y) |
| `all-of-any` | `AllOfAny` | `(string, Bag<T>, Bag<T>)` | `bool` | True if for ALL x there EXISTS y: fn(x, y) |
| `all-of-all` | `AllOfAll` | `(string, Bag<T>, Bag<T>)` | `bool` | True if fn(x, y) is true for ALL pairs (x, y) |
| `map` | `Map` | `(string, Bag<T>)` | `Bag<U>` | Apply fn to each element, return result bag |

### `any-of`
- **Constant**: `XACMLFunctionIds.AnyOfFunc`
- **Parameters**: `(string functionId, T value, Bag<T> bag)` -- minimum 3 arguments
- **Returns**: `bool`
- **Description**: Applies the function identified by `functionId` to `value` and each element `x` in `bag`. Returns true if the function returns true for at least one element. Short-circuits on first true result.
- **XACML Reference**: A.3.12

### `all-of`
- **Constant**: `XACMLFunctionIds.AllOfFunc`
- **Parameters**: `(string functionId, T value, Bag<T> bag)` -- minimum 3 arguments
- **Returns**: `bool`
- **Description**: Applies the function identified by `functionId` to `value` and each element `x` in `bag`. Returns true only if the function returns true for every element. Short-circuits on first false result.
- **XACML Reference**: A.3.12

### `any-of-any`
- **Constant**: `XACMLFunctionIds.AnyOfAny`
- **Parameters**: `(string functionId, Bag<T> bag1, Bag<T> bag2)`
- **Returns**: `bool`
- **Description**: Applies the function identified by `functionId` to every pair `(x, y)` where `x` comes from `bag1` and `y` from `bag2`. Returns true if the function returns true for at least one pair. Short-circuits on first true result.
- **XACML Reference**: A.3.12

### `all-of-any`
- **Constant**: `XACMLFunctionIds.AllOfAny`
- **Parameters**: `(string functionId, Bag<T> bag1, Bag<T> bag2)`
- **Returns**: `bool`
- **Description**: Returns true if for EVERY element `x` in `bag1`, there exists at least one element `y` in `bag2` such that `fn(x, y)` returns true. Formally: for all x in bag1, exists y in bag2 such that fn(x, y) = true.
- **XACML Reference**: A.3.12

### `all-of-all`
- **Constant**: `XACMLFunctionIds.AllOfAll`
- **Parameters**: `(string functionId, Bag<T> bag1, Bag<T> bag2)`
- **Returns**: `bool`
- **Description**: Returns true if `fn(x, y)` returns true for ALL pairs `(x, y)` where `x` comes from `bag1` and `y` from `bag2`. The Cartesian product must be fully satisfied. Short-circuits on first false result.
- **XACML Reference**: A.3.12

### `map`
- **Constant**: `XACMLFunctionIds.Map`
- **Parameters**: `(string functionId, Bag<T> bag)`
- **Returns**: `Bag<U>` (type depends on the mapped function's return type)
- **Description**: Applies the function identified by `functionId` to each element in `bag` and returns a new bag containing the results. The return type of the resulting bag matches the mapped function's return type.
- **XACML Reference**: A.3.12

**C# Apply example:**

```csharp
// Condition: any-of-any(string-equal, subject.roles, resource.allowed-roles)
// True if any subject role matches any allowed role for the resource
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.AnyOfAny,
    Arguments =
    [
        new AttributeValue { DataType = XACMLDataTypes.String, Value = XACMLFunctionIds.StringEqual },
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

---

## 9. Type Conversion Functions

Type conversion functions transform values between different XACML data types. They are used to bridge type mismatches in policy conditions. Parse functions throw `InvalidOperationException` if the input string cannot be parsed into the target type.

**Source**: `Functions/Standard/TypeConversionFunctions.cs`
**XACML Reference**: Appendix A.3.13

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `string-from-integer` | `StringFromInteger` | `(int)` | `string` | Integer to string (InvariantCulture) |
| `integer-from-string` | `IntegerFromString` | `(string)` | `int` | Parse string as integer |
| `double-from-string` | `DoubleFromString` | `(string)` | `double` | Parse string as double |
| `boolean-from-string` | `BooleanFromString` | `(string)` | `bool` | Parse string as boolean |
| `string-from-boolean` | `StringFromBoolean` | `(bool)` | `string` | Boolean to "true"/"false" |
| `string-from-double` | `StringFromDouble` | `(double)` | `string` | Double to string (InvariantCulture) |
| `string-from-dateTime` | `StringFromDateTime` | `(DateTime)` | `string` | DateTime to ISO 8601 round-trip string |

### `string-from-integer`
- **Constant**: `XACMLFunctionIds.StringFromInteger`
- **Parameters**: `(int)`
- **Returns**: `string`
- **Description**: Converts an integer to its string representation using `CultureInfo.InvariantCulture` formatting.
- **XACML Reference**: A.3.13

### `integer-from-string`
- **Constant**: `XACMLFunctionIds.IntegerFromString`
- **Parameters**: `(string)`
- **Returns**: `int`
- **Description**: Parses a string into an integer using `NumberStyles.Integer` and `CultureInfo.InvariantCulture`. Throws `InvalidOperationException` if the string cannot be parsed.
- **XACML Reference**: A.3.13

### `double-from-string`
- **Constant**: `XACMLFunctionIds.DoubleFromString`
- **Parameters**: `(string)`
- **Returns**: `double`
- **Description**: Parses a string into a double using `NumberStyles.Float | NumberStyles.AllowThousands` and `CultureInfo.InvariantCulture`. Throws `InvalidOperationException` if the string cannot be parsed.
- **XACML Reference**: A.3.13

### `boolean-from-string`
- **Constant**: `XACMLFunctionIds.BooleanFromString`
- **Parameters**: `(string)`
- **Returns**: `bool`
- **Description**: Parses a string into a boolean. Accepts "true" and "false" (case-insensitive via `bool.TryParse`). Throws `InvalidOperationException` if the string cannot be parsed.
- **XACML Reference**: A.3.13

### `string-from-boolean`
- **Constant**: `XACMLFunctionIds.StringFromBoolean`
- **Parameters**: `(bool)`
- **Returns**: `string`
- **Description**: Converts a boolean to its lowercase string representation: `true` becomes `"true"`, `false` becomes `"false"`.
- **XACML Reference**: A.3.13

### `string-from-double`
- **Constant**: `XACMLFunctionIds.StringFromDouble`
- **Parameters**: `(double)`
- **Returns**: `string`
- **Description**: Converts a double to its string representation using `CultureInfo.InvariantCulture` formatting.
- **XACML Reference**: A.3.13

### `string-from-dateTime`
- **Constant**: `XACMLFunctionIds.StringFromDateTime`
- **Parameters**: `(DateTime)`
- **Returns**: `string`
- **Description**: Converts a DateTime to its ISO 8601 round-trip string representation using the `"o"` format specifier and `CultureInfo.InvariantCulture`.
- **XACML Reference**: A.3.13

**C# Apply example:**

```csharp
// Condition: string-equal(string-from-integer(resource.priority), "1")
// Converts a numeric priority to string for comparison
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.StringEqual,
    Arguments =
    [
        new Apply
        {
            FunctionId = XACMLFunctionIds.StringFromInteger,
            Arguments =
            [
                new AttributeDesignator
                {
                    Category = AttributeCategory.Resource,
                    AttributeId = "priority",
                    DataType = XACMLDataTypes.Integer
                }
            ]
        },
        new AttributeValue { DataType = XACMLDataTypes.String, Value = "1" }
    ]
};
```

---

## 10. Regular Expression Functions

Regular expression matching using .NET `System.Text.RegularExpressions.Regex`. Includes a 5-second timeout to prevent ReDoS (Regular Expression Denial of Service) attacks.

**Source**: `Functions/Standard/RegexFunctions.cs`
**XACML Reference**: Appendix A.3.14

| Function ID | Constant | Parameters | Returns | Description |
|---|---|---|---|---|
| `string-regexp-match` | `StringRegexpMatch` | `(string, string)` | `bool` | Regex pattern match with timeout |

### `string-regexp-match`
- **Constant**: `XACMLFunctionIds.StringRegexpMatch`
- **Parameters**: `(string pattern, string input)`
- **Returns**: `bool`
- **Description**: Returns true if `input` matches the regular expression `pattern`. The pattern uses .NET regex syntax. A 5-second timeout is enforced to prevent ReDoS attacks. Throws `InvalidOperationException` if the regex times out or the pattern is invalid.
- **XACML Reference**: A.3.14
- **Security**: Includes `RegexTimeout = TimeSpan.FromSeconds(5)` to mitigate catastrophic backtracking.

**C# Apply example:**

```csharp
// Condition: string-regexp-match("^[A-Z]{2}-\\d{4}$", resource.document-id)
// Validates that the document ID matches the format "XX-0000"
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.StringRegexpMatch,
    Arguments =
    [
        new AttributeValue { DataType = XACMLDataTypes.String, Value = @"^[A-Z]{2}-\d{4}$" },
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "document-id",
            DataType = XACMLDataTypes.String
        }
    ]
};
```

---

## Appendix: Data Types

All functions operate on values whose types correspond to the XACML 3.0 / XML Schema data types defined in `XACMLDataTypes`:

| Constant | XACML Data Type URI | .NET Type |
|---|---|---|
| `XACMLDataTypes.String` | `http://www.w3.org/2001/XMLSchema#string` | `string` |
| `XACMLDataTypes.Boolean` | `http://www.w3.org/2001/XMLSchema#boolean` | `bool` |
| `XACMLDataTypes.Integer` | `http://www.w3.org/2001/XMLSchema#integer` | `int` |
| `XACMLDataTypes.Double` | `http://www.w3.org/2001/XMLSchema#double` | `double` |
| `XACMLDataTypes.Date` | `http://www.w3.org/2001/XMLSchema#date` | `DateOnly` |
| `XACMLDataTypes.DateTime` | `http://www.w3.org/2001/XMLSchema#dateTime` | `DateTime` |
| `XACMLDataTypes.Time` | `http://www.w3.org/2001/XMLSchema#time` | `TimeOnly` |
| `XACMLDataTypes.AnyURI` | `http://www.w3.org/2001/XMLSchema#anyURI` | `string` |

---

## Appendix: Error Handling

All functions validate their arguments before execution:

- **Argument count**: Each function validates that the correct number of arguments is provided via `FunctionHelpers.ValidateArgCount()` or `FunctionHelpers.ValidateMinArgCount()`. Throws `InvalidOperationException` on mismatch.
- **Type coercion**: Arguments are coerced to the expected type via helpers like `CoerceToInt()`, `CoerceToBool()`, `CoerceToDouble()`, `CoerceToDate()`, `CoerceToDateTime()`, `CoerceToTime()`, `CoerceToString()`, and `CoerceToBag()`. Throws `InvalidOperationException` on type mismatch.
- **Domain errors**: Division by zero in `integer-divide`, `integer-mod`, and `double-divide` throws `InvalidOperationException`. Integer overflow in checked arithmetic throws `OverflowException`.
- **Regex errors**: Invalid patterns in `string-regexp-match` throw `InvalidOperationException`. Regex timeouts (>5 seconds) throw `InvalidOperationException`.
- **Bag errors**: `*-one-and-only` functions throw if the bag does not contain exactly one element.

---

## Appendix: Source Files

| File | Category | Functions |
|---|---|---|
| `XACMLFunctionIds.cs` | Constants | All function ID string constants |
| `Functions/Standard/EqualityFunctions.cs` | Equality | 7 functions |
| `Functions/Standard/ComparisonFunctions.cs` | Comparison | 24 functions |
| `Functions/Standard/ArithmeticFunctions.cs` | Arithmetic | 13 functions |
| `Functions/Standard/StringFunctions.cs` | String | 8 functions |
| `Functions/Standard/LogicalFunctions.cs` | Logical | 4 functions |
| `Functions/Standard/BagFunctions.cs` | Bag | 32 functions |
| `Functions/Standard/SetFunctions.cs` | Set | 15 functions |
| `Functions/Standard/HigherOrderFunctions.cs` | Higher-Order | 6 functions |
| `Functions/Standard/TypeConversionFunctions.cs` | Type Conversion | 7 functions |
| `Functions/Standard/RegexFunctions.cs` | Regular Expression | 1 function |
| `Functions/DefaultFunctionRegistry.cs` | Registry | Registration orchestration |
| `Functions/FunctionHelpers.cs` | Utilities | Argument validation and type coercion |
| `Functions/IXACMLFunction.cs` | Interface | `IXACMLFunction` contract |
| `Functions/IFunctionRegistry.cs` | Interface | `IFunctionRegistry` contract |
| `Functions/DelegateFunction.cs` | Implementation | Delegate-based function wrapper |
