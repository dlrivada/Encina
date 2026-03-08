# EEL Error Reference

## Overview

EEL (Encina Expression Language) errors fall into two categories:

| Phase         | When It Happens                          | Error Code               | Recoverable? |
|---------------|------------------------------------------|--------------------------|:------------:|
| **Compilation** | First time an expression is compiled     | `abac.invalid_condition` | Yes (fix expression) |
| **Runtime**     | Each time a compiled expression runs     | `abac.invalid_condition` | Depends on cause |

Both phases return `Either<EncinaError, T>` (Left on failure), following the Railway
Oriented Programming pattern used throughout Encina. No exceptions escape the
`EELCompiler` public API -- they are always captured and wrapped in an `EncinaError`.

---

## Compilation Errors

Compilation errors occur when Roslyn cannot parse or type-check the expression. The
`EELCompiler.CompileAsync` method collects all Roslyn diagnostics with severity `Error`,
formats them with line/column positions, and returns a single `EncinaError` via
`ABACErrors.InvalidCondition(expression, errorMessages)`.

### Error Code: `abac.invalid_condition`

**Triggered by:** Any expression that produces one or more Roslyn `DiagnosticSeverity.Error`
entries during `Script.Compile()`.

**Error structure:**

```
Code:    abac.invalid_condition
Message: Condition expression is invalid: (0,15): ; expected
Details:
  stage:      abac
  expression: user.age >>> 25
  reason:     (0,15): ; expected
```

**How to fix:**

1. Read the `reason` field -- it contains Roslyn's diagnostic message with position.
2. The `(line, column)` offset is relative to the expression string (always line 0 for
   single-line expressions).
3. Correct the C# syntax error and recompile.

### Common Roslyn Diagnostic Codes

| Roslyn Code | Meaning                        | Example Expression           | Fix                            |
|-------------|--------------------------------|------------------------------|--------------------------------|
| CS1525      | Invalid expression term        | `user.age >>>`               | Fix the operator               |
| CS0103      | Name does not exist            | `undefinedVar == true`       | Use `user`, `resource`, `environment`, or `action` |
| CS0131      | Left side cannot be assigned   | `user.age = 25`              | Use `==` for comparison        |
| CS0029      | Cannot convert type            | `"hello"` (non-boolean)      | Add a boolean comparison       |
| CS1503      | Argument type mismatch         | Wrong cast in method call    | Fix the cast type              |
| CS0201      | Only assignment/call/etc.      | `user.age;`                  | Make it a boolean expression   |

---

## Runtime Errors

Runtime errors occur when a compiled expression executes against an `EELGlobals` instance.
The `EELCompiler.EvaluateAsync` method wraps any exception thrown during `runner.Invoke`
into `ABACErrors.InvalidCondition(expression, $"Evaluation failed: {ex.Message}")`.

### Error Code: `abac.invalid_condition` (Runtime Variant)

**Triggered by:** An exception during evaluation of a successfully compiled expression.

**Error structure:**

```
Code:    abac.invalid_condition
Message: Condition expression is invalid: Evaluation failed: 'System.Dynamic.ExpandoObject'
         does not contain a definition for 'nonExistent'
Details:
  stage:      abac
  expression: user.nonExistent == "x"
  reason:     Evaluation failed: 'System.Dynamic.ExpandoObject' does not contain a
              definition for 'nonExistent'
```

### Common Runtime Exception Types

| Exception                          | Cause                                          | Example                                      |
|------------------------------------|-------------------------------------------------|----------------------------------------------|
| `RuntimeBinderException`           | Property not found on `ExpandoObject`           | `user.nonExistent == "x"` when key is absent |
| `RuntimeBinderException`           | Operator not applicable to dynamic operand      | `!user.isBlocked` without `(bool)` cast      |
| `NullReferenceException`           | Global variable is `null`                       | Accessing `user.name` when `user` is `null`  |
| `InvalidCastException`             | Explicit cast to incompatible type              | `(int)user.name` when value is a string      |
| `RuntimeBinderException`           | Extension method not resolved on dynamic        | `user.roles.Contains("x")` without cast      |

---

## Other ABAC Error Codes

While `abac.invalid_condition` is the primary EEL-related error, the following ABAC
errors may appear in the broader policy evaluation pipeline:

| Error Code                        | Trigger                                                    |
|-----------------------------------|------------------------------------------------------------|
| `abac.evaluation_failed`          | Policy evaluation threw an unhandled exception             |
| `abac.access_denied`              | Policy evaluation produced a Deny decision                 |
| `abac.indeterminate`              | Evaluation could not reach Permit or Deny                  |
| `abac.policy_not_found`           | Referenced policy ID does not exist in the store           |
| `abac.policy_set_not_found`       | Referenced policy set ID does not exist in the store       |
| `abac.attribute_resolution_failed`| Required attribute (MustBePresent) could not be resolved   |
| `abac.invalid_policy`             | Policy definition is structurally invalid                  |
| `abac.invalid_policy_set`         | Policy set definition is structurally invalid              |
| `abac.duplicate_policy`           | A policy with the same ID already exists                   |
| `abac.duplicate_policy_set`       | A policy set with the same ID already exists               |
| `abac.combining_failed`           | Combining algorithm produced Indeterminate                 |
| `abac.missing_context`            | ABAC security context not available (middleware missing)   |
| `abac.obligation_failed`          | Mandatory obligation handler failed (access denied per XACML) |
| `abac.function_not_found`         | Referenced function not in the function registry           |
| `abac.function_error`             | Custom function threw an exception during evaluation       |
| `abac.variable_not_found`         | VariableReference targets an undefined VariableDefinition  |

---

## Common Errors Table

The following table consolidates the most frequently encountered mistakes when writing
EEL expressions:

| Expression                                    | Error Type   | Cause                                          | Fix                                                |
|-----------------------------------------------|--------------|-------------------------------------------------|----------------------------------------------------|
| `user.age >>>`                                | Compilation  | CS1525 -- invalid expression term               | Fix the operator (e.g., `user.age > 3`)           |
| `"hello"`                                     | Compilation  | CS0029 -- string does not implicitly convert to bool | Add a comparison: `user.name == "hello"`      |
| `undefinedVar == true`                        | Compilation  | CS0103 -- name does not exist in context         | Use `user`, `resource`, `environment`, or `action` |
| `user.age = 25`                               | Compilation  | CS0131 -- left-hand side cannot be assigned      | Use `==` for comparison                           |
| `user.nonExistent == "x"`                     | Runtime      | `RuntimeBinderException` -- property missing     | Add the attribute to the `ExpandoObject`           |
| `user.roles.Contains("admin")`                | Runtime      | `RuntimeBinderException` -- extension method     | Cast: `((IEnumerable<object>)user.roles).Contains("admin")` |
| `!user.isBlocked`                             | Runtime      | `RuntimeBinderException` -- operator on dynamic  | Cast: `!((bool)user.isBlocked)`                    |
| `(int)user.name`                              | Runtime      | `InvalidCastException` -- type mismatch          | Use the correct type or validate before casting    |
| `user.email.EndsWith("@co.com")`              | Runtime      | `RuntimeBinderException` -- method resolution    | Cast: `((string)user.email).EndsWith("@co.com")`  |
| `user.name == "alice"` (user is `null`)       | Runtime      | `NullReferenceException`                         | Initialize all four globals as `ExpandoObject`     |

---

## Debugging Tips

1. **Read the `reason` field.** Every `abac.invalid_condition` error includes the Roslyn
   diagnostic or exception message in `Details["reason"]`.

2. **Check the `expression` field.** The full expression string is preserved in
   `Details["expression"]` for logging and diagnostics.

3. **Validate at startup.** Use `EELCompiler.CompileAsync` during application startup
   (e.g., in a hosted service) to detect compilation errors before the first request.
   See [performance.md](performance.md) for the precompilation pattern.

4. **Unit-test your expressions.** Create a test that calls `EvaluateAsync` with known
   globals and asserts the expected boolean result. See [cookbook.md](cookbook.md) for
   ready-made test patterns.

5. **Log structured errors.** The `EncinaError.Details` dictionary contains `stage`,
   `expression`, and `reason` keys that integrate naturally with structured logging
   (Serilog, OpenTelemetry).
