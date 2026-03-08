# EEL (Encina Expression Language) Guide

## Overview

EEL (Encina Expression Language) is a lightweight expression system built on top of Roslyn's C# scripting API. It allows you to write inline ABAC (Attribute-Based Access Control) conditions as standard C# boolean expressions that are compiled, cached, and evaluated at runtime.

EEL expressions are attached to MediatR request types via the `[RequireCondition]` attribute. At evaluation time, the expression receives four `dynamic` context variables representing the XACML 3.0 attribute categories: `user`, `resource`, `environment`, and `action`.

```csharp
[RequireCondition("user.department == \"Finance\" && resource.amount <= 10000")]
public sealed record ApproveExpenseCommand(Guid ExpenseId) : ICommand;
```

### When to Use EEL vs XACML Expression Trees

| Scenario | Recommendation |
|----------|----------------|
| Simple attribute comparisons on a single request | EEL (`[RequireCondition]`) |
| Conditions known at compile time | EEL |
| Quick prototyping or low-ceremony policies | EEL |
| Complex, reusable, composable policies | XACML expression trees |
| Policies loaded from external storage at runtime | XACML expression trees |
| Policies that reference custom XACML functions | XACML expression trees |
| Need for XACML combining algorithms | XACML expression trees |

Both approaches can coexist in the same application. EEL conditions are evaluated **before** the full XACML PDP pipeline, providing a fast short-circuit path for simple cases.

---

## Getting Started

### 1. Decorate Your Request

Apply `[RequireCondition]` to any MediatR request (command, query, or notification):

```csharp
using Encina.Security.ABAC;

// Single condition
[RequireCondition("user.role == \"Admin\"")]
public sealed record DeleteUserCommand(Guid UserId) : ICommand;

// Multiple conditions (ALL must be true - logical AND)
[RequireCondition("user.department == \"Engineering\"")]
[RequireCondition("resource.isPublished == true")]
public sealed record EditArticleCommand(Guid ArticleId, string Content) : ICommand;
```

When multiple `[RequireCondition]` attributes are applied, every expression must evaluate to `true` for access to be granted.

### 2. Register ABAC Services

```csharp
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;

    // Optional: validate all expressions at startup
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies.Add(typeof(DeleteUserCommand).Assembly);
});
```

### 3. Populate Context Attributes

The ABAC pipeline behavior populates `EELGlobals` from the current security context. Your attribute resolvers provide the `user`, `resource`, `environment`, and `action` values as `ExpandoObject` instances. See the ABAC attribute resolution documentation for details.

---

## Context Variables

EEL expressions have access to four top-level variables, each a `dynamic` object (backed by `ExpandoObject`). These correspond to the four XACML 3.0 attribute categories defined in `EELGlobals`:

### `user` (Subject Attributes)

Describes the entity requesting access (user, service principal, or system account).

```csharp
// Common properties
user.id              // Guid or string identifier
user.role            // "Admin", "Manager", "Viewer"
user.department      // "Finance", "Engineering", "HR"
user.clearanceLevel  // int: 1, 2, 3, 4, 5
user.email           // "alice@example.com"
user.isActive        // bool
```

### `resource` (Resource Attributes)

Describes the resource being accessed.

```csharp
// Common properties
resource.id              // Guid or string identifier
resource.type            // "Document", "Order", "Account"
resource.classification  // "Public", "Internal", "Confidential", "Secret"
resource.owner           // string or Guid
resource.amount          // decimal or int
resource.isPublished     // bool
```

### `environment` (Environment Attributes)

Describes the current environmental conditions at the time of the request.

```csharp
// Common properties
environment.currentTime     // DateTime
environment.ipAddress       // string
environment.isBusinessHours // bool
environment.region          // "US-East", "EU-West"
environment.riskScore       // int or double
```

### `action` (Action Attributes)

Describes the action being performed on the resource.

```csharp
// Common properties
action.name        // "read", "write", "delete", "approve"
action.httpMethod  // "GET", "POST", "PUT", "DELETE"
action.isReadOnly  // bool
```

All properties are dynamically resolved. The property names and types depend entirely on what your attribute resolvers populate into the `ExpandoObject` instances.

---

## How It Works

The compilation and evaluation flow has four stages:

### 1. Expression Discovery

At startup (if enabled), `EELExpressionDiscovery` scans configured assemblies for types decorated with `[RequireCondition]`. It collects all `(Type, Expression)` tuples across all exported types, handling `ReflectionTypeLoadException` gracefully by skipping types that fail to load.

### 2. Roslyn Compilation

`EELCompiler` compiles each expression string into a Roslyn `Script<bool>` using `CSharpScript.Create<bool>()`. The script options include:
- References: `System`, `System.Linq`, `System.Dynamic`, `Microsoft.CSharp`
- Imports: `System`, `System.Linq`, `System.Collections.Generic`
- Globals type: `EELGlobals`

The compiled script's diagnostics are inspected. If any errors are found, the compiler returns `Left(EncinaError)` with the diagnostic messages. On success, it creates a `ScriptRunner<bool>` delegate.

### 3. Caching

The `ScriptRunner<bool>` delegate is stored in a `ConcurrentDictionary<string, ScriptRunner<bool>>` keyed by the expression string. Compilation is protected by a `SemaphoreSlim` with double-check locking to prevent duplicate compilations under concurrent access.

### 4. Evaluation

When a request arrives, the pipeline behavior calls `EvaluateAsync(expression, globals)`:

1. **Cache hit** (fast path): The cached `ScriptRunner<bool>` is retrieved and invoked directly.
2. **Cache miss**: The expression is compiled, cached, and then invoked.
3. The runner executes via `runner.Invoke(globals, cancellationToken)`.
4. Runtime exceptions during evaluation are caught and returned as `Left(EncinaError)`.

```
[RequireCondition("user.role == \"Admin\"")]
        |
        v
  EELCompiler.CompileAsync("user.role == \"Admin\"")
        |
        v
  CSharpScript.Create<bool>(expression, options, typeof(EELGlobals))
        |
        v
  script.Compile() --> diagnostics check
        |
        v
  script.CreateDelegate() --> ScriptRunner<bool>
        |
        v
  _cache.TryAdd(expression, runner)
        |
        v
  runner.Invoke(globals) --> bool result
```

---

## Expression Examples

### Basic Comparisons

```csharp
[RequireCondition("user.role == \"Admin\"")]
[RequireCondition("resource.amount > 1000")]
[RequireCondition("user.clearanceLevel >= resource.requiredClearance")]
[RequireCondition("user.department != \"Contractors\"")]
```

### String Methods

```csharp
[RequireCondition("user.email.EndsWith(\"@company.com\")")]
[RequireCondition("resource.name.Contains(\"draft\")")]
[RequireCondition("user.role.ToLower() == \"admin\"")]
```

### Logical Operators

```csharp
[RequireCondition("user.role == \"Admin\" || user.role == \"SuperAdmin\"")]
[RequireCondition("user.isActive && user.isVerified")]
[RequireCondition("!(resource.isArchived)")]
```

### Null Checks

```csharp
[RequireCondition("user.manager != null")]
[RequireCondition("resource.expiresAt == null || resource.expiresAt > environment.currentTime")]
```

### Complex Conditions

```csharp
[RequireCondition("user.clearanceLevel >= 3 && (resource.classification == \"Confidential\" || resource.classification == \"Internal\")")]
[RequireCondition("user.department == \"Finance\" && resource.amount <= 50000 && environment.isBusinessHours")]
```

### Type Casting

Because the context variables are `dynamic`, explicit casts may be needed for arithmetic or comparisons with typed values:

```csharp
[RequireCondition("(int)user.clearanceLevel >= (int)resource.requiredLevel")]
[RequireCondition("(double)resource.amount * 1.1 <= (double)user.spendingLimit")]
```

---

## Startup Validation

EEL supports fail-fast startup validation through the `EELExpressionPrecompilationService` hosted service. When enabled, it scans assemblies for `[RequireCondition]` decorations and compiles every expression before the application starts accepting requests.

### Configuration

```csharp
services.AddEncinaABAC(options =>
{
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies.Add(typeof(MyCommand).Assembly);
    options.ExpressionScanAssemblies.Add(typeof(AnotherCommand).Assembly);
});
```

### Behavior

| Scenario | Result |
|----------|--------|
| All expressions compile successfully | Application starts normally; expressions are pre-cached |
| One or more expressions fail to compile | `InvalidOperationException` is thrown; application fails to start |
| `ValidateExpressionsAtStartup = false` | No scanning occurs; expressions are compiled on first use |
| `ExpressionScanAssemblies` is empty | Debug log emitted; no scanning occurs |

### Precompilation Details

- Expressions are compiled concurrently with bounded parallelism (`Environment.ProcessorCount`).
- Each failure is logged individually at `Error` level with the request type, expression, and error message.
- The thrown `InvalidOperationException` includes a summary of all failing expressions.
- Successfully compiled expressions are cached and ready for immediate use at request time.

---

## EEL vs XACML Expression Trees

| Aspect | EEL | XACML Expression Trees |
|--------|-----|----------------------|
| **Syntax** | C# boolean expressions | Programmatic `Match`, `Apply`, `Condition` objects |
| **Definition location** | Inline on request type via attribute | Separate `Policy`/`PolicySet` objects |
| **Compilation** | Roslyn at startup or first use | No compilation needed (in-memory objects) |
| **Runtime cost** | First call: ~ms (compilation); subsequent: ~us | Consistent evaluation cost |
| **Composability** | Limited (AND via multiple attributes) | Full (combining algorithms, policy sets) |
| **Custom functions** | Standard C# methods only | `IXACMLFunction` registry |
| **External storage** | No (compiled from source code) | Yes (serializable policy definitions) |
| **IDE support** | Syntax highlighting via `[StringSyntax("csharp")]` | Standard C# IntelliSense |
| **Best for** | Simple, per-request inline conditions | Complex, reusable enterprise policies |

---

## Programmatic Usage

You can use `EELCompiler` directly for scenarios outside the pipeline behavior:

```csharp
using Encina.Security.ABAC.EEL;
using System.Dynamic;

// Create and configure the compiler
using var compiler = new EELCompiler();

// Build the globals
var globals = new EELGlobals
{
    user = CreateExpando(new { department = "Finance", role = "Manager" }),
    resource = CreateExpando(new { amount = 5000, classification = "Internal" }),
    environment = CreateExpando(new { isBusinessHours = true }),
    action = CreateExpando(new { name = "approve" })
};

// Compile and evaluate
var result = await compiler.EvaluateAsync(
    "user.department == \"Finance\" && resource.amount <= 10000",
    globals);

result.Match(
    Left: error => Console.WriteLine($"Error: {error.Message}"),
    Right: value => Console.WriteLine($"Access granted: {value}"));

// Helper method
static ExpandoObject CreateExpando(object source)
{
    var expando = new ExpandoObject();
    var dict = (IDictionary<string, object?>)expando;
    foreach (var prop in source.GetType().GetProperties())
    {
        dict[prop.Name] = prop.GetValue(source);
    }
    return expando;
}
```

### Compile-Only (Pre-validation)

```csharp
using var compiler = new EELCompiler();

var compileResult = await compiler.CompileAsync("user.role == \"Admin\"");

compileResult.Match(
    Left: error => Console.WriteLine($"Compilation failed: {error.Message}"),
    Right: runner => Console.WriteLine("Expression compiled successfully"));
```

The `CompileAsync` method returns `Either<EncinaError, ScriptRunner<bool>>`, following the Railway Oriented Programming pattern used throughout Encina.

---

## IDE Support

The `[RequireCondition]` attribute annotates its `expression` parameter with two attributes for IDE integration:

| Attribute | IDE | Effect |
|-----------|-----|--------|
| `[StringSyntax("csharp")]` | Visual Studio 2022+, VS Code | C# syntax highlighting inside the string literal |
| `[LanguageInjection("csharp")]` | JetBrains Rider | C# syntax highlighting, basic IntelliSense |

This means expressions like `"user.role == \"Admin\""` receive color coding, brace matching, and basic error detection directly in the IDE.

> **Note**: IDE support is best-effort. Because context variables are `dynamic`, the IDE cannot verify property names or types at design time. Use `ValidateExpressionsAtStartup` for compile-time safety.

---

## Limitations

### Dynamic Typing

All four context variables (`user`, `resource`, `environment`, `action`) are `dynamic`. This means:

- **No compile-time property verification**: `user.nonExistentProperty` compiles successfully but throws `RuntimeBinderException` at evaluation time.
- **No IntelliSense for properties**: IDEs cannot suggest available attributes.
- **Type mismatches are runtime errors**: Comparing `user.role == 42` when `role` is a string will fail at evaluation, not compilation.

Mitigation: Enable `ValidateExpressionsAtStartup` and write unit tests for your expressions using `EELTestHelper`.

### Roslyn Package Size

EEL depends on `Microsoft.CodeAnalysis.CSharp.Scripting`, which adds approximately 15-20 MB to deployment size. If your application only uses XACML expression trees and does not need EEL, you can avoid this dependency by not referencing the EEL types.

### No Async Expressions

EEL expressions are synchronous C# boolean expressions. You cannot `await` inside an expression. Any data needed for the condition must be pre-resolved into the `EELGlobals` before evaluation.

### Expression Scope

EEL expressions have access to the following imports: `System`, `System.Linq`, and `System.Collections.Generic`. You cannot import additional namespaces or reference arbitrary types. The expressions are constrained to the standard BCL types and LINQ methods.

### String Escaping in Attributes

Because EEL expressions live inside C# attribute string literals, you must escape double quotes:

```csharp
// Correct: escaped double quotes
[RequireCondition("user.role == \"Admin\"")]

// Alternative: use single-character comparison tricks or constants
// (not recommended for readability)
```

See the [EEL Syntax Reference](syntax-reference.md) for comprehensive escaping rules.
