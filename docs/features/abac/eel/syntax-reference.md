# EEL Syntax Reference

## Overview

EEL (Encina Expression Language) expressions are valid C# expressions constrained to evaluate to a `bool` return type. They are compiled by Roslyn's `CSharpScript.Create<bool>()` API with the following imports available:

- `System`
- `System.Linq`
- `System.Collections.Generic`

Expressions have access to four `dynamic` top-level variables: `user`, `resource`, `environment`, and `action`. Each is backed by an `ExpandoObject` at runtime.

---

## Literals

### Boolean

```csharp
true
false
```

### Integer

```csharp
0
42
-1
1_000_000   // digit separator
```

### Double / Decimal

```csharp
3.14
-0.5
1_000.50
```

### String

```csharp
"hello"
"Finance"
""          // empty string
```

> **Important**: Inside `[RequireCondition("...")]`, double quotes within the expression must be escaped. See [Escaped Strings](#escaped-strings) below.

### Null

```csharp
null
```

---

## Variables

EEL provides four top-level context variables corresponding to XACML 3.0 attribute categories. All are `dynamic` objects.

| Variable | XACML Category | Description |
|----------|---------------|-------------|
| `user` | Subject | The entity requesting access |
| `resource` | Resource | The resource being accessed |
| `environment` | Environment | Current environmental conditions |
| `action` | Action | The action being performed |

### Accessing Properties

```csharp
user.role
user.department
resource.classification
resource.amount
environment.currentTime
environment.isBusinessHours
action.name
action.httpMethod
```

### Nested Properties

If a context variable contains nested objects, use dot notation:

```csharp
user.address.country
resource.metadata.createdBy
```

> **Warning**: Accessing a property that does not exist on the `ExpandoObject` at runtime throws `Microsoft.CSharp.RuntimeBinder.RuntimeBinderException`. The expression will return an evaluation error, not `false`.

---

## Operators

### Comparison Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `==` | Equal to | `user.role == "Admin"` |
| `!=` | Not equal to | `user.department != "Contractors"` |
| `<` | Less than | `resource.amount < 1000` |
| `>` | Greater than | `user.clearanceLevel > 3` |
| `<=` | Less than or equal | `resource.amount <= 50000` |
| `>=` | Greater than or equal | `user.clearanceLevel >= resource.requiredLevel` |

### Logical Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `&&` | Logical AND (short-circuit) | `user.isActive && user.isVerified` |
| `\|\|` | Logical OR (short-circuit) | `user.role == "Admin" \|\| user.role == "Super"` |
| `!` | Logical NOT | `!resource.isArchived` |

### Arithmetic Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `+` | Addition | `(int)user.score + 10 > 50` |
| `-` | Subtraction | `(double)resource.amount - (double)user.discount >= 0` |
| `*` | Multiplication | `(double)resource.price * 1.1 <= (double)user.budget` |
| `/` | Division | `(int)resource.count / 2 > 0` |
| `%` | Modulo | `(int)resource.id % 2 == 0` |

> **Note**: Because context variables are `dynamic`, arithmetic operations may require explicit type casts. See [Type Casting](#type-casting).

---

## Operator Precedence

EEL follows standard C# operator precedence. Operators listed at a higher precedence level bind more tightly.

| Precedence | Operator(s) | Description | Associativity |
|------------|-------------|-------------|---------------|
| 1 (highest) | `()` | Grouping / parentheses | N/A |
| 2 | `.` | Member access | Left to right |
| 3 | `!` | Logical NOT (unary) | Right to left |
| 4 | `(type)` | Type cast | Right to left |
| 5 | `*` `/` `%` | Multiplicative | Left to right |
| 6 | `+` `-` | Additive | Left to right |
| 7 | `<` `>` `<=` `>=` | Relational | Left to right |
| 8 | `==` `!=` | Equality | Left to right |
| 9 | `&&` | Logical AND | Left to right |
| 10 (lowest) | `\|\|` | Logical OR | Left to right |

### Examples

```csharp
// Without parentheses: && binds tighter than ||
user.role == "Admin" || user.role == "Manager" && user.isActive
// Equivalent to: user.role == "Admin" || (user.role == "Manager" && user.isActive)

// Use parentheses for clarity
(user.role == "Admin" || user.role == "Manager") && user.isActive
```

---

## String Methods

EEL supports all standard `System.String` instance methods since expressions are compiled as C#.

### Commonly Used Methods

| Method | Description | Example |
|--------|-------------|---------|
| `Contains(string)` | Checks if string contains substring | `user.email.Contains("@company.com")` |
| `StartsWith(string)` | Checks if string starts with prefix | `resource.path.StartsWith("/api/admin")` |
| `EndsWith(string)` | Checks if string ends with suffix | `user.email.EndsWith("@company.com")` |
| `ToLower()` | Converts to lowercase | `user.role.ToLower() == "admin"` |
| `ToUpper()` | Converts to uppercase | `action.httpMethod.ToUpper() == "GET"` |
| `Trim()` | Removes leading/trailing whitespace | `user.name.Trim() != ""` |
| `Length` | Gets string length (property) | `user.password.Length >= 8` |
| `Replace(old, new)` | Replaces occurrences | `resource.path.Replace("/v2/", "/") == resource.canonicalPath` |
| `Substring(start)` | Extracts substring | `user.code.Substring(0, 2) == "US"` |

### String Comparison with Case Insensitivity

```csharp
// Option 1: Normalize case
user.role.ToLower() == "admin"

// Option 2: Use StringComparison
user.role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
```

### String Null/Empty Checks

```csharp
user.name != null && user.name != ""
!string.IsNullOrEmpty((string)user.name)
!string.IsNullOrWhiteSpace((string)user.name)
```

> **Note**: `string.IsNullOrEmpty` and `string.IsNullOrWhiteSpace` require casting the `dynamic` value to `string` because the static method cannot resolve the argument type at compile time.

---

## Collection Methods

When context properties contain collections (e.g., `List<string>`, `string[]`), you can use LINQ methods since `System.Linq` is imported.

### Contains

```csharp
// Check if a list contains a value
((IEnumerable<object>)user.roles).Contains("Admin")
```

### Any

```csharp
// Check if any element matches a condition
((IEnumerable<object>)user.permissions).Cast<string>().Any(p => p == "write")
```

### Count

```csharp
// Check collection size
((IEnumerable<object>)user.roles).Count() >= 2
```

> **Note**: Because context variables are `dynamic`, LINQ extension methods may require explicit casts to `IEnumerable<object>` or specific types. The C# compiler cannot resolve extension methods on `dynamic` types.

---

## Null Safety

### Null Checks

```csharp
// Explicit null check before property access
user.manager != null

// Null comparison for optional attributes
resource.expiresAt == null
resource.expiresAt != null
```

### Null-Conditional Operator

```csharp
// Safe navigation (returns null if user.manager is null)
user.manager?.department == "Engineering"
```

> **Caution**: The null-conditional operator (`?.`) on `dynamic` types has limited support. It works in many cases, but behavior may vary depending on the runtime binder. Prefer explicit null checks for reliability.

### Null Coalescing

```csharp
// Default value when null
(user.nickname ?? "Unknown") != "Unknown"
```

### Common Null Patterns

```csharp
// Optional attribute with default
resource.expiresAt == null || resource.expiresAt > environment.currentTime

// Required attribute must be present
user.department != null && user.department == "Finance"
```

---

## Type Casting

Because all context variables are `dynamic`, explicit type casts are sometimes necessary for arithmetic operations, static method calls, or disambiguating comparisons.

### Supported Cast Syntax

```csharp
(int)user.clearanceLevel
(double)resource.amount
(string)user.role
(bool)user.isActive
(long)resource.fileSize
(decimal)resource.price
```

### When Casts Are Required

| Scenario | Cast Needed? | Example |
|----------|-------------|---------|
| Simple equality | No | `user.role == "Admin"` |
| Arithmetic operations | Recommended | `(int)user.score + 10 > 50` |
| Static method calls | Yes | `string.IsNullOrEmpty((string)user.name)` |
| LINQ on collections | Yes | `((IEnumerable<object>)user.roles).Count()` |
| Comparison with typed literal | Usually no | `resource.amount > 1000` |

### Cast Failure

If a cast is invalid at runtime (e.g., casting a string to int), the expression evaluation fails with an `InvalidCastException`, which is caught by `EELCompiler` and returned as `Left(EncinaError)`.

---

## Parentheses and Grouping

Use parentheses to override default operator precedence or improve readability.

```csharp
// Without parentheses: user.isAdmin OR (user.isModerator AND resource.isPublic)
user.isAdmin || user.isModerator && resource.isPublic

// With parentheses: (user.isAdmin OR user.isModerator) AND resource.isPublic
(user.isAdmin || user.isModerator) && resource.isPublic
```

### Nested Grouping

```csharp
((user.role == "Admin" || user.role == "SuperAdmin") && user.isActive) ||
(user.role == "Auditor" && action.name == "read")
```

### Negation with Grouping

```csharp
!(user.role == "Guest" || user.role == "Anonymous")
!(resource.isArchived && resource.isDeleted)
```

---

## Escaped Strings

EEL expressions are written inside C# attribute string literals. This requires escaping double quotes within the expression.

### Escaping Rules

The `[RequireCondition]` attribute accepts a regular C# string literal. Inside this literal:

| Character | Escape Sequence | Example |
|-----------|----------------|---------|
| `"` (double quote) | `\"` | `"user.role == \"Admin\""` |
| `\` (backslash) | `\\` | `"resource.path == \"C:\\\\data\""` |

### Correct Examples

```csharp
// String comparison with escaped quotes
[RequireCondition("user.role == \"Admin\"")]

// String method with escaped quotes
[RequireCondition("user.email.EndsWith(\"@company.com\")")]

// Multiple string values
[RequireCondition("user.role == \"Admin\" || user.role == \"Manager\"")]

// Nested escaped quotes in string arguments
[RequireCondition("resource.tags.Contains(\"high-priority\")")]
```

### Common Mistakes

```csharp
// WRONG: unescaped quotes cause C# compilation error
[RequireCondition("user.role == "Admin"")]

// WRONG: single quotes are not string delimiters in C#
// (single quotes are char literals in C#)
[RequireCondition("user.role == 'Admin'")]

// CORRECT: escaped double quotes
[RequireCondition("user.role == \"Admin\"")]
```

> **Note**: C# raw string literals (`"""..."""`) are not usable inside attribute arguments as of C# 14. You must use escaped regular string literals.

### Readability Tips

For complex expressions with many escaped quotes, consider breaking conditions into multiple `[RequireCondition]` attributes:

```csharp
// Hard to read
[RequireCondition("user.department == \"Finance\" && user.role == \"Manager\" && resource.type == \"Invoice\"")]

// Easier to read: multiple attributes (AND semantics)
[RequireCondition("user.department == \"Finance\"")]
[RequireCondition("user.role == \"Manager\"")]
[RequireCondition("resource.type == \"Invoice\"")]
```

---

## Quick Reference Card

### Comparison

```
==  !=  <  >  <=  >=
```

### Logical

```
&&  ||  !
```

### Arithmetic

```
+  -  *  /  %
```

### Null

```
== null    != null    ?.    ??
```

### String

```
.Contains()  .StartsWith()  .EndsWith()  .ToLower()  .ToUpper()  .Trim()  .Length
```

### Casting

```
(int)  (double)  (string)  (bool)  (long)  (decimal)
```

### Example Templates

```csharp
// Role check
"user.role == \"RoleName\""

// Attribute comparison
"user.level >= resource.requiredLevel"

// Time-based
"environment.isBusinessHours == true"

// Multi-condition
"user.dept == \"X\" && resource.amount <= 1000"

// Null-safe optional
"resource.expiresAt == null || resource.expiresAt > environment.currentTime"

// String matching
"user.email.EndsWith(\"@company.com\")"
```
