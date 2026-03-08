# EEL Expression Cookbook

## Overview

This cookbook provides practical, copy-paste-ready EEL (Encina Expression Language) patterns
for common ABAC authorization scenarios. Each recipe includes the expression, a complete
globals setup using `ExpandoObject`, and the expected boolean result.

EEL expressions are standard C# boolean expressions evaluated by Roslyn. They have access
to four global `dynamic` variables matching the XACML 3.0 attribute categories:

| Variable      | Type      | XACML Category | Typical Attributes                          |
|---------------|-----------|----------------|---------------------------------------------|
| `user`        | `dynamic` | Subject        | department, role, roles, email, clearance    |
| `resource`    | `dynamic` | Resource       | amount, classification, ownerId, path        |
| `environment` | `dynamic` | Environment    | hour, country, ipAddress, dayOfWeek          |
| `action`      | `dynamic` | Action         | name (read/write/delete), httpMethod          |

All globals are populated as `ExpandoObject` instances and cast to
`IDictionary<string, object?>` when assigning properties.

### Helper for Recipes

Every recipe below uses this helper to keep snippets concise:

```csharp
using System.Dynamic;

static ExpandoObject Expando(Action<IDictionary<string, object?>> configure)
{
    var obj = new ExpandoObject();
    configure((IDictionary<string, object?>)obj);
    return obj;
}
```

---

## 1. Department-Based Access

**Scenario:** Only Finance department members can view financial reports.

```csharp
// Expression
"user.department == \"Finance\""

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["department"] = "Finance"),
    resource = new ExpandoObject(),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true
```

---

## 2. Role-Based Conditions (Collection Contains)

**Scenario:** Only users whose `roles` list contains `"admin"` can manage settings.

```csharp
// Expression
"((IEnumerable<object>)user.roles).Contains(\"admin\")"

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["roles"] = new List<object> { "editor", "admin" }),
    resource = new ExpandoObject(),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true
```

> **Tip:** Because `user` is `dynamic`, cast collection properties to
> `IEnumerable<object>` before calling LINQ methods.

---

## 3. Multi-Role Check (OR Logic)

**Scenario:** Managers or admins can approve purchase orders.

```csharp
// Expression
"user.role == \"admin\" || user.role == \"manager\""

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["role"] = "manager"),
    resource = new ExpandoObject(),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true (matches "manager")
```

---

## 4. Amount Thresholds

**Scenario:** Standard users can only approve expenses up to $50,000.

```csharp
// Expression
"resource.amount <= 50000"

// Globals
var globals = new EELGlobals
{
    user = new ExpandoObject(),
    resource = Expando(r => r["amount"] = 35000),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true (35000 <= 50000)
```

For a denied case, set `r["amount"] = 75000` and the result becomes `false`.

---

## 5. Time-Based Access (Business Hours)

**Scenario:** Non-admin users may only access the system during business hours (09:00 - 17:00).

```csharp
// Expression
"environment.hour >= 9 && environment.hour < 17"

// Globals
var globals = new EELGlobals
{
    user = new ExpandoObject(),
    resource = new ExpandoObject(),
    environment = Expando(e => e["hour"] = 14),
    action = new ExpandoObject()
};

// Result: true (14 is within 9..16)
```

> **Tip:** Populate `environment.hour` from `DateTime.UtcNow.Hour` (or the user's
> local hour) in your `IAttributeProvider` implementation.

---

## 6. Ownership Check

**Scenario:** Users can only edit resources they own.

```csharp
// Expression
"user.id == resource.ownerId"

// Globals
var userId = "usr-42";
var globals = new EELGlobals
{
    user = Expando(u => u["id"] = userId),
    resource = Expando(r => r["ownerId"] = userId),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true (same string reference value)
```

---

## 7. Email Domain Restriction

**Scenario:** Only users with a `@company.com` email may access internal APIs.

```csharp
// Expression
"((string)user.email).EndsWith(\"@company.com\")"

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["email"] = "alice@company.com"),
    resource = new ExpandoObject(),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true
```

> **Note:** Cast to `(string)` before calling `EndsWith` to avoid runtime binder
> ambiguity on the `dynamic` target.

---

## 8. Clearance Level Comparison

**Scenario:** The user's clearance level must meet or exceed the resource's required clearance.

```csharp
// Expression
"user.clearanceLevel >= resource.requiredClearance"

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["clearanceLevel"] = 4),
    resource = Expando(r => r["requiredClearance"] = 3),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true (4 >= 3)
```

---

## 9. Geo-Restriction

**Scenario:** Access is allowed only from the US or Canada.

```csharp
// Expression
"environment.country == \"US\" || environment.country == \"CA\""

// Globals
var globals = new EELGlobals
{
    user = new ExpandoObject(),
    resource = new ExpandoObject(),
    environment = Expando(e => e["country"] = "US"),
    action = new ExpandoObject()
};

// Result: true
```

---

## 10. Resource Classification Guard

**Scenario:** Top-secret resources require clearance level 5 or higher; all other
classifications are accessible to any authenticated user.

```csharp
// Expression
"resource.classification != \"top-secret\" || user.clearanceLevel >= 5"

// Globals (user with clearance 3 accessing a "confidential" resource)
var globals = new EELGlobals
{
    user = Expando(u => u["clearanceLevel"] = 3),
    resource = Expando(r => r["classification"] = "confidential"),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true (classification != "top-secret" short-circuits)
```

```csharp
// Globals (user with clearance 3 accessing a "top-secret" resource)
var globals = new EELGlobals
{
    user = Expando(u => u["clearanceLevel"] = 3),
    resource = Expando(r => r["classification"] = "top-secret"),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: false (classification matches AND clearance < 5)
```

---

## 11. Combined Conditions (Department + Amount + Time)

**Scenario:** Finance department staff can approve amounts up to $100,000, but only during
business hours.

```csharp
// Expression
"user.department == \"Finance\" && resource.amount <= 100000 && environment.hour >= 9 && environment.hour < 17"

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["department"] = "Finance"),
    resource = Expando(r => r["amount"] = 80000),
    environment = Expando(e => e["hour"] = 10),
    action = new ExpandoObject()
};

// Result: true
```

---

## 12. Null-Safe Patterns

**Scenario:** Only evaluate the email domain when the email attribute is present.

```csharp
// Expression
"user.email != null && ((string)user.email).Contains(\"@\")"

// Globals (email present)
var globals = new EELGlobals
{
    user = Expando(u => u["email"] = "bob@company.com"),
    resource = new ExpandoObject(),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true
```

```csharp
// Globals (email absent)
var globals = new EELGlobals
{
    user = Expando(u => u["email"] = null),
    resource = new ExpandoObject(),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: false (short-circuits on null check)
```

> **Warning:** Accessing a property that was never added to the `ExpandoObject` throws
> a `RuntimeBinderException`. Always add the property (even as `null`) or guard with a
> null check at the globals-population level.

---

## 13. Negation Patterns

**Scenario:** Blocked users must be denied access regardless of other conditions.

```csharp
// Expression
"!((bool)user.isBlocked)"

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["isBlocked"] = false),
    resource = new ExpandoObject(),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true (user is NOT blocked)
```

> **Note:** Cast to `(bool)` before negation. Without the cast, the `!` operator on
> a `dynamic` value can produce unexpected results or a runtime binder error.

---

## 14. String Matching (Path Prefix)

**Scenario:** Only admins can access paths under `/api/admin`.

```csharp
// Expression
"((string)resource.path).StartsWith(\"/api/admin\")"

// Globals
var globals = new EELGlobals
{
    user = new ExpandoObject(),
    resource = Expando(r => r["path"] = "/api/admin/users"),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true
```

Combine with a role check for a complete policy:

```csharp
// Expression
"user.role == \"admin\" && ((string)resource.path).StartsWith(\"/api/admin\")"
```

---

## 15. Numeric Ranges

**Scenario:** Age-restricted content requires users between 18 and 65.

```csharp
// Expression
"user.age >= 18 && user.age <= 65"

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["age"] = 30),
    resource = new ExpandoObject(),
    environment = new ExpandoObject(),
    action = new ExpandoObject()
};

// Result: true (30 is within 18..65)
```

---

## 16. Cross-Category Expression (All Four Variables)

**Scenario:** A comprehensive policy checking subject, resource, environment, and action
together: a Finance user reading an internal document during business hours from the US.

```csharp
// Expression
"user.department == \"Finance\" && action.name == \"read\" && resource.classification == \"internal\" && environment.hour >= 9 && environment.hour < 17 && environment.country == \"US\""

// Globals
var globals = new EELGlobals
{
    user = Expando(u => u["department"] = "Finance"),
    resource = Expando(r => r["classification"] = "internal"),
    environment = Expando(e =>
    {
        e["hour"] = 14;
        e["country"] = "US";
    }),
    action = Expando(a => a["name"] = "read")
};

// Result: true
```

---

## 17. Day-of-Week Restriction

**Scenario:** Batch operations are only allowed on weekdays.

```csharp
// Expression
"environment.dayOfWeek >= 1 && environment.dayOfWeek <= 5"

// Globals (Wednesday = 3)
var globals = new EELGlobals
{
    user = new ExpandoObject(),
    resource = new ExpandoObject(),
    environment = Expando(e => e["dayOfWeek"] = (int)DayOfWeek.Wednesday),
    action = new ExpandoObject()
};

// Result: true (3 is within 1..5 = Monday..Friday)
```

---

## Quick Reference: Operator Cheat Sheet

| Operator            | EEL Example                                         | Notes                          |
|---------------------|-----------------------------------------------------|--------------------------------|
| Equality            | `user.role == "admin"`                              | String or numeric              |
| Inequality          | `resource.status != "archived"`                     |                                |
| Greater / Less      | `user.clearanceLevel >= 3`                          | Numeric comparison             |
| Logical AND         | `expr1 && expr2`                                    | Short-circuit evaluation       |
| Logical OR          | `expr1 \|\| expr2`                                  | Short-circuit evaluation       |
| Negation            | `!((bool)user.isBlocked)`                           | Cast bool before `!`           |
| String method       | `((string)user.email).EndsWith("@co.com")`          | Cast to `string` first         |
| Collection contains | `((IEnumerable<object>)user.roles).Contains("x")`   | Cast collection first          |
| Null check          | `user.email != null`                                | Check before method calls      |
| Grouping            | `(a \|\| b) && c`                                   | Use parentheses for clarity    |

---

## Tips for Writing Effective Expressions

1. **Keep expressions simple.** Prefer multiple simple policies over one complex expression.
2. **Cast before calling methods.** Dynamic dispatch does not resolve extension methods
   (`Contains`, `StartsWith`, etc.) without explicit casts.
3. **Guard against null.** Use `!= null &&` before property access on optional attributes.
4. **Populate all four globals.** Even unused categories must be initialized as empty
   `ExpandoObject` instances to avoid `NullReferenceException`.
5. **Use numeric types consistently.** If `clearanceLevel` is stored as `int`, compare with
   an `int` literal (`>= 3`), not a string (`>= "3"`).
6. **Precompile expressions at startup.** Call `EELCompiler.CompileAsync` during application
   initialization to pay the Roslyn compilation cost once. See [performance.md](performance.md).
7. **Test expressions independently.** Use unit tests with `EELCompiler.EvaluateAsync` to
   verify each expression before deploying policies.
