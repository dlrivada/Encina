# ADR-017: EEL — Encina Expression Language Naming and Design

**Status:** Accepted
**Date:** 2026-02-20
**Deciders:** David Lozano Rivada
**Technical Story:** [#401 - Attribute-Based Access Control (ABAC)](https://github.com/dlrivada/Encina/issues/401)

## Context

The ABAC module provides two ways to define policy conditions:

1. **XACML expression trees** — Formal `Apply` nodes with function IDs and attribute designators
2. **Inline C# expressions** — Compiled by Roslyn at startup or first use

The inline expression system needs a clear identity to distinguish it from the XACML model in documentation, error messages, diagnostics, and developer communication.

### Naming Considerations

| Candidate | Precedent | Issue |
|-----------|-----------|-------|
| CEL (C# Expression Language) | Google CEL (Common Expression Language) | Name collision with Google's project |
| ECEL (Encina C# Expression Language) | None | Awkward pronunciation |
| EQL (Encina Query Language) | Elastic EQL | Not a query language — misleading |
| EEL (Encina Expression Language) | Spring SpEL pattern | Clear, pronounceable, follows convention |
| EPL (Encina Policy Language) | Esper EPL | Broader than just expressions |

## Decision

Name the inline expression system **EEL** (Encina Expression Language), following the convention established by Spring Expression Language (SpEL), Unified Expression Language (UEL), and Object Graph Navigation Language (OGNL).

### Design Principles

#### 1. EEL is C# — Not a New Language

EEL is not a custom DSL. It is **standard C# constrained to boolean expressions** with a predefined set of global variables. Any valid C# boolean expression is a valid EEL expression.

```csharp
// These are all valid EEL expressions:
"user.department == \"Finance\""
"resource.amount > 10000 && environment.isBusinessHours"
"user.roles.Contains(\"admin\") || user.clearanceLevel >= 5"
"!user.isBlocked && user.email.EndsWith(\"@company.com\")"
```

#### 2. Four Context Variables

EEL provides four global variables mapping to XACML attribute categories:

| Variable | XACML Category | Type | Description |
|----------|---------------|------|-------------|
| `user` | Subject (§5.4.2.2) | `dynamic` | Subject/user attributes |
| `resource` | Resource (§5.4.2.2) | `dynamic` | Resource being accessed |
| `environment` | Environment (§5.4.2.2) | `dynamic` | Environmental context |
| `action` | Action (§5.4.2.2) | `dynamic` | Action being performed |

**Why `user` instead of `subject`?** Developer ergonomics. While XACML uses "subject", every developer understands "user" — and 95% of subject attributes are user attributes.

#### 3. Integration Points

EEL expressions are used via the `[RequireCondition]` attribute:

```csharp
[RequireCondition("user.department == \"Finance\"")]
public sealed record GetFinancialReport : IRequest<Report>;
```

Or programmatically via the `EELCompiler`:

```csharp
var compiler = new EELCompiler();
var result = await compiler.EvaluateAsync(
    "user.clearanceLevel >= resource.requiredClearance",
    globals);
// result: Either<EncinaError, bool>
```

#### 4. Naming Convention in Code

| Concept | Naming |
|---------|--------|
| Namespace | `Encina.Security.ABAC.EEL` |
| Compiler | `EELCompiler` |
| Globals | `EELGlobals` |
| Discovery | `EELExpressionDiscovery` |
| Precompilation | `EELExpressionPrecompilationService` |
| Error code | `abac.invalid_condition` |
| Attribute | `[RequireCondition("...")]` (not `[EELCondition]`) |

The `[RequireCondition]` attribute is intentionally not prefixed with "EEL" because it is the primary developer-facing API and should be clean.

## Consequences

### Positive

- **Clear identity**: "EEL" is short, pronounceable, and instantly recognizable
- **Convention-following**: Follows the SpEL/UEL/OGNL naming pattern
- **Not a new language**: EEL is C# — developers have zero learning curve
- **Searchable**: "Encina EEL" produces no naming conflicts in search results

### Negative

- **Potential confusion**: Some may initially think EEL is a custom language (it's just C#)
- **Name length**: "EEL" is very short — some may not realize it's an acronym

### Neutral

- **Dual system**: Developers can use EEL for simple conditions and XACML expression trees for complex policies — both coexist cleanly

## Related Decisions

- [ADR-015: XACML 3.0 as ABAC Foundation](015-xacml-3.0-abac-foundation.md) — The standard model EEL simplifies
- [ADR-016: Roslyn for Expression Language](016-roslyn-expression-language.md) — The compilation engine behind EEL

## References

- [Spring Expression Language (SpEL)](https://docs.spring.io/spring-framework/reference/core/expressions.html)
- [Jakarta Expression Language (EL)](https://jakarta.ee/specifications/expression-language/)
- [Object Graph Navigation Language (OGNL)](https://commons.apache.org/proper/commons-ognl/)
