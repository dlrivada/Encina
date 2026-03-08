# EEL Performance Guide

## Overview

EEL (Encina Expression Language) uses Roslyn's C# Scripting API to compile boolean
expressions into IL delegates at runtime. This design trades a one-time compilation cost
for near-native evaluation speed on subsequent calls.

The performance model has two distinct phases:

| Phase          | Cost             | Frequency           | Cached? |
|----------------|------------------|---------------------|:-------:|
| **Compilation** | ~50-100 ms       | Once per expression | Yes     |
| **Evaluation**  | ~0.1-0.5 ms      | Every policy check  | N/A     |

Understanding this two-phase model is key to optimizing ABAC performance.

---

## Compilation Cost

Each unique expression string is compiled exactly once. Roslyn parses the C# expression,
performs type-checking against the `EELGlobals` type, and emits an IL delegate via
`CSharpScript.Create<bool>(...).CreateDelegate()`.

**Typical compilation time:** 50-100 ms per expression (varies by expression complexity
and system load).

**What happens during compilation:**

1. Roslyn parses the expression into a syntax tree.
2. Semantic analysis resolves the `EELGlobals` properties (`user`, `resource`,
   `environment`, `action`) as `dynamic`.
3. The script is compiled to IL and a `ScriptRunner<bool>` delegate is produced.
4. The delegate is stored in the `ConcurrentDictionary<string, ScriptRunner<bool>>` cache.

**First-request latency:** If an expression is encountered for the first time during a
live request, that request pays the full ~50-100 ms compilation overhead. This is why
startup precompilation is strongly recommended (see below).

---

## Evaluation Cost

Once compiled, the `ScriptRunner<bool>` delegate executes against an `EELGlobals`
instance. Because the globals use `dynamic` (backed by `ExpandoObject`), there is a small
DLR (Dynamic Language Runtime) dispatch overhead per property access.

**Typical evaluation time:** 0.1-0.5 ms per evaluation.

**Cost breakdown per evaluation:**

| Step                           | Approximate Cost |
|--------------------------------|------------------|
| Cache lookup (dictionary)      | < 0.001 ms       |
| DLR dispatch per property      | ~0.01-0.05 ms    |
| Boolean logic                  | negligible        |
| **Total (simple expression)**  | ~0.1 ms           |
| **Total (complex, 5+ props)**  | ~0.3-0.5 ms       |

> Expressions that access fewer dynamic properties evaluate faster. An expression like
> `user.role == "admin"` (one property) is measurably faster than a cross-category
> expression accessing six properties.

---

## Caching Strategy

`EELCompiler` uses a `ConcurrentDictionary<string, ScriptRunner<bool>>` keyed by the
exact expression string. The cache is internal to each `EELCompiler` instance.

### Cache Behavior

```
Request 1: "user.role == \"admin\""
  -> Cache MISS -> Compile (~80 ms) -> Cache delegate -> Evaluate (~0.1 ms)

Request 2: "user.role == \"admin\""
  -> Cache HIT -> Evaluate (~0.1 ms)

Request 3: "resource.amount > 10000"
  -> Cache MISS -> Compile (~70 ms) -> Cache delegate -> Evaluate (~0.2 ms)
```

### Thread Safety During Compilation

The `EELCompiler` uses a double-check locking pattern to prevent duplicate compilations:

1. **Fast path:** `ConcurrentDictionary.TryGetValue` -- lock-free read.
2. **Slow path:** `SemaphoreSlim` acquisition, second `TryGetValue` check, then compile.

This ensures that even under high concurrency, each expression is compiled exactly once.
Concurrent requests for the same not-yet-compiled expression will queue on the semaphore;
the first thread compiles and caches, subsequent threads find the cache hit.

### Cache Lifetime

The cache lives for the lifetime of the `EELCompiler` instance. Since `EELCompiler`
is typically registered as a singleton in the DI container, the cache persists for the
application lifetime.

```csharp
// Recommended: singleton registration
services.AddSingleton<EELCompiler>();
```

---

## Startup Precompilation

The most impactful optimization is to compile all known expressions at application
startup, paying the Roslyn cost during initialization rather than on the first live
request.

### Pattern: Precompile in a Hosted Service

```csharp
public class EELWarmupService(
    EELCompiler compiler,
    IPolicyStore policyStore,
    ILogger<EELWarmupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var policies = await policyStore.GetAllAsync(cancellationToken);
        var expressions = policies
            .SelectMany(p => p.Rules)
            .Where(r => r.Condition is not null)
            .Select(r => r.Condition!)
            .Distinct();

        foreach (var expression in expressions)
        {
            var result = await compiler.CompileAsync(expression, cancellationToken);
            result.Match(
                Left: error => logger.LogWarning(
                    "Failed to precompile expression: {Expression}. Error: {Error}",
                    expression, error.Message),
                Right: _ => logger.LogDebug(
                    "Precompiled expression: {Expression}", expression));
        }

        logger.LogInformation(
            "EEL warmup complete. {Count} expressions precompiled.",
            expressions.Count());
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

Register it:

```csharp
services.AddHostedService<EELWarmupService>();
```

This approach also doubles as startup validation -- any malformed expressions will be
logged before the application starts handling traffic.

---

## Memory Overhead

EEL introduces memory overhead from two sources: Roslyn metadata and cached delegates.

### Roslyn Metadata References

The `EELCompiler` constructor configures `ScriptOptions` with references to:

- `System.Runtime` (via `typeof(object).Assembly`)
- `System.Linq` (via `typeof(Enumerable).Assembly`)
- `System.Dynamic.Runtime` (via `typeof(ExpandoObject).Assembly`)
- `Microsoft.CSharp` (via `typeof(CSharpArgumentInfo).Assembly`)

These metadata references are loaded once per `EELCompiler` instance and shared across
all compilations. Approximate overhead: **5-15 MB** depending on runtime version.

### Cached Delegates

Each `ScriptRunner<bool>` delegate is a lightweight object. Memory per cached expression
is small (a few KB). Even with hundreds of cached expressions, delegate memory is
negligible compared to the Roslyn metadata baseline.

| Cached Expressions | Approximate Additional Memory |
|--------------------|-------------------------------|
| 10                 | ~50 KB                        |
| 100                | ~500 KB                       |
| 1,000              | ~5 MB                         |

---

## Package Size Impact

The `Microsoft.CodeAnalysis.CSharp.Scripting` NuGet package and its transitive
dependencies add approximately **15-20 MB** to the published application size. This is
the trade-off for using Roslyn as the expression engine.

| Dependency                                  | Approximate Size |
|---------------------------------------------|------------------|
| Microsoft.CodeAnalysis.CSharp.Scripting     | ~1 MB            |
| Microsoft.CodeAnalysis.CSharp               | ~7 MB            |
| Microsoft.CodeAnalysis.Common               | ~5 MB            |
| Other transitive (Immutable, Reflection)    | ~2-5 MB          |
| **Total**                                   | **~15-20 MB**    |

For applications where package size is critical (e.g., serverless functions), consider
whether the full Roslyn-based EEL engine is appropriate or if a lighter expression
evaluator would be more suitable for the deployment target.

---

## Thread Safety

The `EELCompiler` is fully thread-safe:

| Component              | Mechanism                  | Guarantee                          |
|------------------------|----------------------------|------------------------------------|
| Expression cache       | `ConcurrentDictionary`     | Lock-free concurrent reads         |
| Compilation guard      | `SemaphoreSlim(1, 1)`     | At most one compilation at a time  |
| `ScriptRunner<bool>`   | Roslyn delegate            | Immutable after creation, safe to share |
| `EELGlobals`           | New instance per call      | No shared mutable state            |

**Critical rule:** Always create a fresh `EELGlobals` instance per evaluation. The globals
carry request-specific attribute values and must not be shared across concurrent requests.

---

## Comparison: EEL vs XACML Expression Trees

| Aspect                  | EEL (Roslyn Scripting)      | XACML Expression Trees         |
|-------------------------|-----------------------------|---------------------------------|
| **Expression syntax**   | C# (familiar to developers) | XML/custom DSL (XACML spec)    |
| **Compilation time**    | ~50-100 ms (first time)     | ~1-5 ms (tree building)        |
| **Evaluation time**     | ~0.1-0.5 ms (IL delegate)   | ~0.5-2 ms (tree walking)       |
| **Caching**             | Compiled delegate (fast)    | Parsed tree (moderate)         |
| **Package size**        | +15-20 MB (Roslyn)          | Minimal                        |
| **Expressiveness**      | Full C# (LINQ, methods)     | Limited to XACML functions     |
| **Type safety**         | Dynamic (runtime errors)    | Schema-validated (static)      |
| **Startup cost**        | Higher (Roslyn init)        | Lower                          |
| **Steady-state cost**   | Lower (IL delegates)        | Higher (interpretation)        |

**When EEL excels:**
- Long-running applications where startup cost is amortized over many evaluations.
- Teams familiar with C# who prefer writing `user.role == "admin"` over XML predicates.
- Scenarios requiring complex expressions (LINQ, string methods, collection operations).

**When expression trees may be preferable:**
- Serverless / short-lived processes where startup cost dominates.
- Environments with strict package size constraints.
- Scenarios requiring static schema validation before deployment.

---

## Optimization Tips

### 1. Register EELCompiler as Singleton

```csharp
services.AddSingleton<EELCompiler>();
```

This ensures the compilation cache persists for the application lifetime.

### 2. Precompile at Startup

Use a hosted service to compile all known expressions during application initialization.
This eliminates first-request latency. See the [Startup Precompilation](#startup-precompilation)
section above.

### 3. Keep Expressions Simple

Simpler expressions compile faster and evaluate faster:

```csharp
// Prefer: two simple policies
"user.department == \"Finance\""
"resource.amount <= 50000"

// Over: one complex expression
"user.department == \"Finance\" && resource.amount <= 50000 && environment.hour >= 9"
```

Multiple simple expressions also improve policy readability and reusability.

### 4. Minimize Dynamic Property Accesses

Each `dynamic` property access incurs DLR dispatch overhead. Expressions that touch fewer
properties are faster:

| Expression                                                     | Property Accesses | Relative Cost |
|----------------------------------------------------------------|:-----------------:|:-------------:|
| `user.role == "admin"`                                         | 1                 | Baseline      |
| `user.role == "admin" && resource.amount > 1000`               | 2                 | ~1.5x         |
| `user.dept == "Fin" && resource.amt > 1000 && env.hour >= 9`   | 3                 | ~2x           |

### 5. Avoid Unnecessary Casts in Hot Paths

While casts are required for method calls on `dynamic` targets, prefer direct operator
comparisons when possible:

```csharp
// Faster: direct comparison, no cast needed
"user.department == \"Finance\""

// Slower: cast + method call
"((string)user.department).Equals(\"Finance\")"
```

### 6. Profile with BenchmarkDotNet

For critical authorization paths, measure actual performance with benchmarks:

```csharp
[Benchmark]
public async Task<bool> EvaluateSimple()
{
    var result = await _compiler.EvaluateAsync(
        "user.role == \"admin\"", _globals);
    return result.Match(_ => false, v => v);
}
```

Use `--filter "*EEL*"` with `BenchmarkSwitcher` per the project's
[BenchmarkDotNet guidelines](../../../../CLAUDE.md).
