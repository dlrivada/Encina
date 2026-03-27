---
title: "ADR-016: Roslyn Scripting for Expression Language"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-016: Roslyn Scripting for Expression Language

**Status:** Accepted
**Date:** 2026-02-20
**Deciders:** David Lozano Rivada
**Technical Story:** [#401 - Attribute-Based Access Control (ABAC)](https://github.com/dlrivada/Encina/issues/401)

## Context

The XACML 3.0 condition model uses `Apply` expression trees — nested function invocations with formal attribute designators. While powerful, these trees are verbose for simple conditions:

```csharp
// XACML expression tree for: user.department == "Finance" && resource.amount > 10000
var condition = new Apply
{
    FunctionId = XACMLFunctionIds.And,
    Arguments = [
        new Apply
        {
            FunctionId = XACMLFunctionIds.StringEqual,
            Arguments = [
                new AttributeDesignator { Category = AttributeCategory.Subject,
                    AttributeId = "department", DataType = XACMLDataTypes.String },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "Finance" }
            ]
        },
        new Apply
        {
            FunctionId = XACMLFunctionIds.IntegerGreaterThan,
            Arguments = [
                new AttributeDesignator { Category = AttributeCategory.Resource,
                    AttributeId = "amount", DataType = XACMLDataTypes.Integer },
                new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 10000 }
            ]
        }
    ]
};
```

Developers need a concise inline syntax for simple-to-moderate conditions while retaining the full XACML model for complex policies.

### Expression Engine Options

| Engine | Language | Compilation | Type Safety | .NET Native |
|--------|----------|------------|-------------|-------------|
| **Roslyn Scripting** | C# | AOT-compatible compilation | Full C# type system | ✅ Native |
| **NCalc** | Custom expression | Interpreted | Limited | ❌ Custom |
| **DynamicExpresso** | C#-subset | JIT compilation | Partial | 🟡 Partial |
| **Flee** | Custom expression | IL emission | Limited | ❌ Custom |
| **Spring EL (SpEL)** | Custom Java-like | Interpreted | Reflection-based | ❌ Java-origin |
| **Jint (JavaScript)** | JavaScript | Interpreted | Dynamic | ❌ Non-C# |

## Decision

Use **Microsoft.CodeAnalysis.CSharp.Scripting** (Roslyn Scripting API) as the compilation engine for the Encina Expression Language (EEL), enabling developers to write policy conditions as native C# expressions.

### Key Design Decisions

#### 1. C# as the Expression Language

EEL expressions are valid C# boolean expressions:

```csharp
// Instead of learning a custom DSL:
[RequireCondition("user.department == \"Finance\" && resource.amount <= 50000")]
public sealed record ApproveTransaction : IRequest<ApprovalResult>;
```

Developers use their existing C# knowledge — no new language to learn.

#### 2. Globals-Based Context

Expressions access attributes through four strongly-typed global variables:

```csharp
public class EELGlobals
{
    public dynamic user { get; set; }        // Subject attributes
    public dynamic resource { get; set; }    // Resource attributes
    public dynamic environment { get; set; } // Environment attributes
    public dynamic action { get; set; }      // Action attributes
}
```

These map directly to XACML attribute categories (§5.4.2.2).

#### 3. Compilation and Caching

```
Expression String → Roslyn Compile → ScriptRunner<bool> → Cache
                                                            ↓
Subsequent Calls → Cache Hit → ScriptRunner<bool>.RunAsync(globals)
```

- First evaluation: ~50-100ms (compilation)
- Subsequent evaluations: ~0.1-0.5ms (cached runner execution)
- Cache key: expression string (deterministic)
- Thread-safe: `ConcurrentDictionary` with compilation lock

#### 4. Startup Validation (Optional)

```csharp
services.AddEncinaABAC(options =>
{
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies = [typeof(Program).Assembly];
});
```

A hosted service scans for `[RequireCondition]` attributes and pre-compiles all expressions, failing fast on syntax errors.

#### 5. Error Mapping

Roslyn diagnostics are mapped to `EncinaError` with the `abac.invalid_condition` error code:

| Roslyn Diagnostic | EncinaError Detail |
|-------------------|--------------------|
| CS1002: `;` expected | Syntax error in expression |
| CS0103: name does not exist | Undefined variable reference |
| CS0029: cannot implicitly convert | Expression does not return boolean |
| Runtime `RuntimeBinderException` | Property does not exist on dynamic object |

## Consequences

### Positive

- **Zero learning curve**: Developers write C# expressions they already know
- **Full type system**: Roslyn provides complete C# type checking at compile time
- **IDE support**: `[StringSyntax("csharp")]` enables syntax highlighting in IDEs
- **Performance**: Compiled delegates execute at near-native speed after first compilation
- **Diagnostics**: Roslyn provides precise error messages with line/column positions
- **Extensibility**: Any valid C# expression works — LINQ, string methods, math operations

### Negative

- **Package size**: `Microsoft.CodeAnalysis.CSharp.Scripting` adds ~15MB to the package
- **Cold start**: First compilation takes 50-100ms per expression
- **Dynamic typing**: `ExpandoObject` globals lose compile-time property checking
- **Security**: Arbitrary C# expressions could theoretically call dangerous APIs (mitigated by sandboxing the globals context)

### Neutral

- **Dual model**: Developers can choose between XACML expression trees (for complex policies) and EEL (for simple conditions)
- **Optional**: EEL is only loaded when `[RequireCondition]` is used — no overhead for XACML-only users

## Alternatives Considered

### 1. Custom Expression Parser

Building a custom parser for a domain-specific expression language.

**Rejected because**: Building a parser, type checker, and evaluator from scratch would be months of work with inevitable edge cases. Roslyn is battle-tested with millions of users.

### 2. NCalc

Using NCalc for mathematical and logical expression evaluation.

**Rejected because**: NCalc uses a custom syntax (`[variable]` for references), lacks C# type system integration, and has limited string/date operations.

### 3. DynamicExpresso

Using DynamicExpresso for C#-subset expression evaluation.

**Rejected because**: DynamicExpresso compiles via Reflection.Emit (not Roslyn), has incomplete C# support, and lacks the diagnostic quality of Roslyn error messages.

### 4. JavaScript (Jint)

Embedding a JavaScript engine for expression evaluation.

**Rejected because**: JavaScript introduces a foreign language into the C# codebase, requires type marshaling, and developers would need to context-switch between languages.

## Related Decisions

- [ADR-015: XACML 3.0 as ABAC Foundation](015-xacml-3.0-abac-foundation.md) — The standard that EEL simplifies
- [ADR-017: EEL Naming and Design](017-eel-naming-design.md) — Expression language naming rationale

## References

- [Roslyn Scripting API Documentation](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md)
- [Microsoft.CodeAnalysis.CSharp.Scripting NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting)
- [Spring Expression Language (SpEL) — Inspiration](https://docs.spring.io/spring-framework/reference/core/expressions.html)
