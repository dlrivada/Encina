# Implementation Plan: `Encina.Security.ABAC` — Attribute-Based Access Control Engine (Full XACML 3.0)

> **Issue**: [#401](https://github.com/dlrivada/Encina/issues/401)
> **Type**: Feature
> **Complexity**: Very High (11 phases, cross-cutting, ~168-190 files)
> **Estimated Scope**: ~6,000-8,000 lines of production code + ~5,000-6,500 lines of tests + ~3,000-4,000 lines of documentation

---

## Summary

Implement a full XACML 3.0-compliant Attribute-Based Access Control (ABAC) engine for fine-grained authorization based on user attributes, resource properties, and environmental factors. The engine provides the complete XACML architecture (PDP, PAP, PIP, PEP) with hierarchical PolicySets, Obligations, Advice, four effects (Permit, Deny, NotApplicable, Indeterminate), eight combining algorithms, a formal function registry, structured Targets with AnyOf/AllOf/Match, VariableDefinitions, a fluent C#-idiomatic policy DSL, **EEL (Encina Expression Language)** — Roslyn-compiled `[RequireCondition]` expressions, and an `ABACPipelineBehavior` that integrates with Encina's CQRS pipeline.

This is a **provider-independent cross-cutting feature** — it does NOT require 13 database providers. The core package `Encina.Security.ABAC` contains all in-memory infrastructure. Future satellite packages (`Encina.Security.ABAC.Keycloak`, `Encina.Security.ABAC.OpenPolicyAgent`, `Encina.Security.ABAC.Casbin`) can provide external policy backends.

**Affected packages**:

- `Encina.Security.ABAC` (new package — core engine, DSL, behaviors)
- `Encina.Security` (reference only — `ISecurityContext`, base `SecurityAttribute`)
- `Encina.AspNetCore` (optional integration — HTTP environment attributes)

**Provider category**: None — provider-independent (in-memory policy engine with extensible interfaces for future external backends).

**Prerequisites** (all satisfied):

- [x] #394 `Encina.Security` — Core Security Abstractions (CLOSED)
- [x] #356 Policy-Based Authorization Enhancement (CLOSED)

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Security.ABAC</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Security.ABAC` package** | Clean separation, own pipeline behavior, independent versioning, clear XACML architecture | New NuGet package to maintain |
| **B) Extend `Encina.Security` core** | Single package, shared pipeline | Bloats core (~20 files already), ABAC is complex (~60+ files with full XACML), RBAC and ABAC have different concerns |
| **C) Embed in `Encina.AspNetCore`** | Close to HTTP context | Ties ABAC to ASP.NET, can't use in worker services or non-HTTP scenarios |

### Chosen Option: **A — New `Encina.Security.ABAC` package**

### Rationale

- Full XACML 3.0 is a substantial domain (~60+ files for models, evaluation, functions, algorithms) that absolutely warrants its own package
- Follows the established Encina.Security.* satellite pattern (`AntiTampering`, `Audit`, `Encryption`, `PII`, `Sanitization`)
- References `Encina.Security` for `ISecurityContext` (user attributes come from claims)
- Optional `Encina.AspNetCore` reference for HTTP environment attributes (IP, UserAgent)
- Future backends (`Keycloak`, `OPA`, `Casbin`) become natural satellite packages
- Users who don't need ABAC pay nothing — opt-in philosophy

</details>

<details>
<summary><strong>2. XACML Architecture Model — Full XACML 3.0 specification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Simplified XACML (PDP, PAP, PIP as interfaces)** | Simpler implementation, fewer types | Incomplete standard, can't model PolicySets, obligations, or 4-effect logic |
| **B) Single policy engine class** | Simple, one class does everything | Violates SRP, hard to extend, hard to test |
| **C) Full XACML 3.0 specification** | Complete standard compliance, hierarchical PolicySets, Obligations/Advice, 4 effects, 8 combining algorithms, formal function registry | More types and interfaces, requires careful design to keep C#-idiomatic |

### Chosen Option: **C — Full XACML 3.0 specification**

### Rationale

- **Pre-1.0 philosophy**: "Choose the best solution, not the compatible one" — a full XACML 3.0 implementation is the best long-term architecture
- **PolicySet hierarchy**: Enables nesting policies in groups with separate combining algorithms at each level — critical for large-scale systems with department-level + org-level policies
- **4 Effects** (`Permit`, `Deny`, `NotApplicable`, `Indeterminate`): Essential for correct combining algorithm semantics (simplified 2-effect models silently lose information)
- **Obligations + Advice**: Post-decision actions (audit logging, user notification, MFA escalation) are part of the standard and solve real authorization needs
- **8 combining algorithms**: Full XACML set including `DenyUnlessPermit`, `PermitUnlessDeny`, `OnlyOneApplicable`, ordered variants — each solves distinct real-world scenarios
- **Structured Target** with `AnyOf/AllOf/Match`: Precise request matching beyond simple string patterns, enables the PDP to skip irrelevant policies efficiently
- **AttributeDesignator**: Formal attribute resolution paths (`subject.department`, `resource.classification`) with type safety
- **VariableDefinition**: Reusable sub-expressions within policies, avoids condition duplication
- **Function registry**: Extensible comparison functions beyond operators, maps to XACML's function URNs but with C#-idiomatic registration
- **C#-idiomatic API**: While the model follows XACML 3.0 semantics, the API surface is designed for C# developers (no XML, no URNs in user-facing code — fluent DSL + records)

**XACML 3.0 component mapping to Encina types**:

| XACML 3.0 Component | Encina Type | Notes |
|---|---|---|
| `<PolicySet>` | `PolicySet` record | Hierarchical, contains policies + nested policy sets |
| `<Policy>` | `Policy` record | Contains rules + target + obligations |
| `<Rule>` | `Rule` record | Leaf node with effect + condition |
| `<Target>` | `Target` record | Structured matching with AnyOf/AllOf/Match |
| `<Condition>` | `Condition` record | Tree of Apply/Function nodes |
| `<Obligation>` | `Obligation` record | Mandatory post-decision action |
| `<Advice>` | `Advice` record | Optional post-decision recommendation |
| `<AttributeDesignator>` | `AttributeDesignator` record | Formal attribute path with category + type |
| `<Apply>` | `Apply` record | Function application node in condition tree |
| `<VariableDefinition>` | `VariableDefinition` record | Reusable sub-expression |
| PDP | `IPolicyDecisionPoint` | Full 4-effect evaluation + obligations |
| PAP | `IPolicyAdministrationPoint` | PolicySet + Policy CRUD |
| PIP | `IPolicyInformationPoint` | On-demand attribute retrieval |
| PEP | `ABACPipelineBehavior` | Pipeline enforcement + obligation execution |

</details>

<details>
<summary><strong>3. Condition Model — XACML Apply/Function tree with C#-idiomatic sugar</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Simple tree-based conditions with And/Or** | Simple data structure, easy to understand | Cannot express XACML functions, no type safety on comparisons |
| **B) XACML Apply/Function model** | Exact XACML semantics, extensible function registry, type-safe | More complex evaluation engine |
| **C) Expression tree-based (System.Linq.Expressions)** | Full C# expression power | Hard to serialize, hard to inspect, runtime compilation overhead |
| **D) String-based DSL** | Flexible, human-readable | Requires parser, runtime errors, no IntelliSense |

### Chosen Option: **B — XACML Apply/Function model**

### Rationale

- **XACML Condition model**: A `Condition` contains an `Apply` node, which calls a `Function` with arguments (which can be `AttributeDesignator`, literal values, or nested `Apply` nodes)
- This is a tree structure (like Option A) but with **functions as first-class citizens**:
  - `string-equal(subject.department, "Finance")` instead of `Attribute="subject.department", Operator=Equals, Value="Finance"`
  - `and(string-equal(...), integer-greater-than(...))` instead of separate And/Or lists
- **C#-idiomatic layer**: The fluent DSL translates `.When("user.department", Equals, "Finance")` into the Apply/Function tree — users never see the XACML structure unless they want to
- **Function registry** (`IFunctionRegistry`): All XACML 3.0 standard functions pre-registered + extensible for custom functions
- **Built-in function categories**: String functions, numeric comparison, date/time, bag functions, set functions, higher-order functions, logical connectives, type conversion
- **AttributeDesignator**: `Category` + `AttributeId` + `DataType` + `MustBePresent` flag
- **VariableDefinition/VariableReference**: Define once, reference in multiple conditions within a policy

</details>

<details>
<summary><strong>4. Pipeline Behavior Integration — Separate <code>ABACPipelineBehavior</code> with Obligation execution</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Separate `ABACPipelineBehavior<TRequest, TResponse>` with obligation execution** | Own pipeline stage, handles obligations/advice, independent enforcement modes | Additional pipeline step per request |
| **B) Extend existing `SecurityPipelineBehavior`** | Single security evaluation point | Bloats security behavior, mixes RBAC/ABAC, can't handle XACML obligations independently |
| **C) Decorator around `SecurityPipelineBehavior`** | Composable | Unclear ordering, hard to configure independently, obligation execution unclear |

### Chosen Option: **A — Separate `ABACPipelineBehavior` with obligation execution**

### Rationale

- XACML 3.0 requires the PEP to **execute obligations** returned by the PDP — this is a mandatory post-decision step that RBAC doesn't have
- The behavior must: (1) build attribute context, (2) evaluate via PDP, (3) check effect, (4) execute `OnPermit` obligations if permitted, (5) execute `OnDeny` obligations if denied, (6) return advice alongside response metadata
- Separate behavior allows independent enforcement modes (`Block`/`Warn`/`Disabled`)
- Pipeline ordering: Security (RBAC) → ABAC (XACML) → Validation → Handler
- `IObligationHandler` interface enables custom obligation actions (audit, notify, MFA escalation)
- Static per-generic-type attribute caching for `[RequirePolicy]` / `[RequireCondition]`
- If the PEP cannot fulfill a mandatory obligation, it MUST deny access (XACML spec requirement)

</details>

<details>
<summary><strong>5. Attribute Collection Strategy — XACML AttributeDesignator with composite providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Composite provider with formal AttributeDesignator resolution** | XACML-compliant, typed attributes, MustBePresent enforcement | More structured than simple dictionary |
| **B) Single monolithic attribute provider** | Simple, one implementation | Not extensible, can't add custom attributes |
| **C) Claims-only (from ISecurityContext)** | Zero additional calls, already available | No resource attributes, no dynamic environment data |

### Chosen Option: **A — Composite provider with XACML AttributeDesignator**

### Rationale

- `AttributeDesignator` formalizes attribute resolution: `Category` (Subject/Resource/Environment/Action), `AttributeId`, `DataType`, `MustBePresent`
- `MustBePresent = true` causes `Indeterminate` if the attribute can't be resolved (XACML spec)
- `CompositeAttributeProvider` aggregates results from `IEnumerable<IAttributeProvider>`
- Built-in providers:
  - `ClaimsAttributeProvider`: Maps claims to `subject.*` attributes with type inference
  - `EnvironmentAttributeProvider`: Time, day of week, business hours (via `TimeProvider`)
  - `RequestAttributeProvider`: Reflects request object properties as `resource.*` attributes
  - `HttpEnvironmentAttributeProvider` (in AspNetCore): IP address, user agent, request path
- `IPolicyInformationPoint` provides lazy on-demand resolution for missing attributes during evaluation
- `AttributeBag` (XACML concept): Attributes can be multi-valued (e.g., user has multiple roles) — bags support set operations
- Attribute resolution feeds both the XACML PDP evaluation AND `EELGlobals` for Roslyn-compiled expressions

</details>

<details>
<summary><strong>6. Combining Algorithms — Full XACML 3.0 set at Policy and PolicySet level</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Full XACML 3.0 set (8 algorithms) via strategy pattern** | Complete standard, each algorithm solves distinct scenario | 8 strategy classes |
| **B) Three simplified algorithms** | Less code, easier to understand | Missing real-world scenarios (DenyUnlessPermit, OnlyOneApplicable) |
| **C) Configurable per-policy via delegate** | Maximum flexibility | Hard to understand, hard to serialize |

### Chosen Option: **A — Full XACML 3.0 set (8 algorithms)**

### Rationale

- XACML 3.0 defines distinct combining algorithms for rules (within a policy) and policies (within a policy set):

| Algorithm | Behavior | Use Case |
|-----------|----------|----------|
| `DenyOverrides` | Any Deny wins, even over Permit | Default restrictive — safety-critical systems |
| `PermitOverrides` | Any Permit wins, even over Deny | Least restrictive — whitelist scenarios |
| `FirstApplicable` | First applicable rule/policy wins | Ordered policy evaluation — priority lists |
| `OnlyOneApplicable` | Exactly one must apply, otherwise Indeterminate | Mutual exclusion — prevents conflicts |
| `DenyUnlessPermit` | Deny unless explicitly Permit (no NotApplicable/Indeterminate) | Simplified deny-by-default — no edge cases |
| `PermitUnlessDeny` | Permit unless explicitly Deny | Simplified permit-by-default — open systems |
| `OrderedDenyOverrides` | Like DenyOverrides but guarantees evaluation order | When side effects of evaluation matter |
| `OrderedPermitOverrides` | Like PermitOverrides but guarantees evaluation order | When side effects of evaluation matter |

- `ICombiningAlgorithm` strategy interface with two overloads:
  - `CombineRuleResults(IReadOnlyList<RuleEvaluationResult>)` → for policies
  - `CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult>)` → for policy sets
- Registered in DI, resolved by `CombiningAlgorithmId` enum value
- Users can register custom algorithms: `options.AddCombiningAlgorithm<CustomAlgorithm>()`

</details>

<details>
<summary><strong>7. Function Registry — Extensible with XACML 3.0 standard functions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extensible registry with pre-registered XACML functions** | Standard-compliant, extensible, type-safe | Function registry infrastructure |
| **B) Fixed set of operators (Equals, GreaterThan, etc.)** | Simple enum, no registry | Not extensible, can't express XACML bag/set functions |
| **C) Delegate-based (user provides Func<>)** | Maximum flexibility | Not serializable, hard to inspect policies |

### Chosen Option: **A — Extensible function registry**

### Rationale

- `IFunctionRegistry` manages function lookup by ID (C#-friendly names, not XACML URNs)
- `IXACMLFunction` interface: `Evaluate(IReadOnlyList<object?> args) → object?`
- **Pre-registered function categories** (C#-idiomatic names mapping to XACML URNs):

| Category | Functions | XACML Section |
|----------|-----------|---------------|
| **Equality** | `string-equal`, `boolean-equal`, `integer-equal`, `double-equal`, `date-equal`, `dateTime-equal` | A.3.1 |
| **Comparison** | `integer-greater-than`, `double-less-than`, `string-greater-than`, `date-greater-than`, etc. | A.3.2-A.3.5 |
| **Arithmetic** | `integer-add`, `integer-subtract`, `integer-multiply`, `double-add`, etc. | A.3.6 |
| **String** | `string-starts-with`, `string-ends-with`, `string-contains`, `string-concatenate`, `string-normalize-space` | A.3.9 |
| **Bag** | `*-one-and-only`, `*-bag-size`, `*-is-in`, `*-bag` | A.3.10-A.3.12 |
| **Set** | `*-intersection`, `*-union`, `*-subset`, `*-at-least-one-member-of`, `*-set-equals` | A.3.11 |
| **Higher-order** | `any-of`, `all-of`, `any-of-any`, `all-of-any`, `all-of-all`, `map` | A.3.12 |
| **Logical** | `and`, `or`, `not`, `n-of` | A.3.13 |
| **Type conversion** | `string-from-integer`, `integer-from-string`, `boolean-from-string`, etc. | A.3.15 |
| **Regular expression** | `string-regexp-match` | A.3.14 |

- Users register custom functions: `options.AddFunction("geo-distance", new GeoDistanceFunction())`
- The `ConditionEvaluator` resolves functions from the registry during evaluation

</details>

<details>
<summary><strong>8. EEL (Encina Expression Language) for <code>[RequireCondition]</code> — Roslyn scripting over Apply/Function tree</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Lightweight recursive descent parser** | No external dependencies, fast cold start | Custom parser code introduces subtle parsing bugs, limited expression power, must maintain and evolve grammar manually |
| **B) Roslyn scripting** | Full C# expression power, battle-tested Microsoft parser, rich error messages, supports any C# expression, pre-compilation eliminates runtime cost | NuGet dependency (`Microsoft.CodeAnalysis.CSharp.Scripting`), ~100ms cold compilation (amortized at startup), no built-in sandboxing |
| **C) Skip inline expressions, force policy references only** | Simplest approach | Loses convenience of quick inline conditions |

### Chosen Option: **B — Roslyn scripting → branded as EEL (Encina Expression Language)**

### Rationale

- **Full XACML 3.0 commitment**: We chose the full specification to avoid shortcuts — using a battle-tested compiler instead of writing a custom parser follows the same philosophy
- **Roslyn is part of the .NET ecosystem**: `Microsoft.CodeAnalysis.CSharp.Scripting` is maintained by the dotnet/roslyn team at Microsoft — we treat it the same way we treat other Microsoft .NET packages, not as an external dependency
- **Custom parsers introduce complex bugs**: Recursive descent parsers are deceptively simple but accumulate edge cases — operator precedence, Unicode identifiers, escape sequences, nested parentheses, error recovery. Roslyn has solved all of these problems with 10+ years of production usage
- **`CSharpScript.Create()` + `CreateDelegate()` pattern**: Expressions are compiled once at startup and cached as delegates — subsequent evaluations are <1ms (near-native speed), eliminating the ~100ms cold compilation concern
- **Rich error messages for free**: Roslyn provides precise error diagnostics with positions, expected tokens, and suggestions — far better than anything a custom parser could produce
- **Full C# expression support**: Users can write `user.Department == "Finance" && resource.Sensitivity <= user.Clearance` using natural C# syntax — no custom grammar to learn
- **Type safety via `Globals` class**: The scripting API accepts a `Globals` type that defines the variables available in expressions — provides compile-time validation of attribute names
- **EEL branding**: The expression language is branded as **EEL (Encina Expression Language)** — following the industry convention of naming expression languages (SpEL, CEL, JEXL). While EEL is technically C# with a specific context (`EELGlobals`), giving it a name enables cleaner documentation, a distinct identity, and a recognizable API surface

**Implementation approach**:

- `[RequireCondition("user.Department == \"Finance\" && resource.Sensitivity <= user.Clearance")]`
- At startup, each expression is compiled via `CSharpScript.Create<bool>(expression, globalsType: typeof(EELGlobals))` and cached as `ScriptRunner<bool>` delegate
- `EELGlobals` exposes `dynamic user`, `dynamic resource`, `dynamic environment`, `dynamic action` properties that map to XACML attribute categories
- Compilation errors at startup → `ABACErrors.InvalidCondition` with Roslyn diagnostic details
- **Sandboxing**: Not needed — expressions run with the same trust level as the application code (they ARE application code, defined by the developer in attributes)
- **Caching**: `ConcurrentDictionary<string, ScriptRunner<bool>>` — compile once, run many
- **Not** a replacement for the full policy DSL — used only for inline conditions in `[RequireCondition]` attributes

</details>

---

## Implementation Phases

### Phase 1: Core Models & Enums

> **Goal**: Establish the full XACML 3.0 type system in C#-idiomatic sealed records.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Security.ABAC/`

1. **Create project file** `Encina.Security.ABAC.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina.Security`, `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, `Microsoft.CodeAnalysis.CSharp.Scripting` (v5.0.0 — Roslyn scripting for `[RequireCondition]` expression compilation)
   - Enable nullable, implicit usings, XML doc

2. **Enums** (`Model/` folder):
   - `Effect` — `Permit`, `Deny`, `NotApplicable`, `Indeterminate` (XACML 3.0 §7.1 — four effects, not two)
   - `CombiningAlgorithmId` — `DenyOverrides`, `PermitOverrides`, `FirstApplicable`, `OnlyOneApplicable`, `DenyUnlessPermit`, `PermitUnlessDeny`, `OrderedDenyOverrides`, `OrderedPermitOverrides` (XACML 3.0 §C — eight algorithms)
   - `FulfillOn` — `Permit`, `Deny` (when to execute an obligation/advice)
   - `ConditionOperator` — `Equals`, `NotEquals`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `In`, `NotIn`, `Exists`, `DoesNotExist`, `RegexMatch` (syntactic sugar for expression parser — maps to XACML functions)
   - `AttributeCategory` — `Subject`, `Resource`, `Environment`, `Action` (XACML 3.0 §B)
   - `ABACEnforcementMode` — `Block`, `Warn`, `Disabled`

3. **XACML structural records** (`Model/` folder):
   - `PolicySet` — sealed record: `Id (string)`, `Version (string?)`, `Description (string?)`, `Target (Target?)`, `Policies (IReadOnlyList<Policy>)`, `PolicySets (IReadOnlyList<PolicySet>)`, `Algorithm (CombiningAlgorithmId)`, `Obligations (IReadOnlyList<Obligation>)`, `Advice (IReadOnlyList<AdviceExpression>)`, `IsEnabled (bool, default true)`, `Priority (int, default 0)`
   - `Policy` — sealed record: `Id (string)`, `Version (string?)`, `Description (string?)`, `Target (Target?)`, `Rules (IReadOnlyList<Rule>)`, `Algorithm (CombiningAlgorithmId)`, `Obligations (IReadOnlyList<Obligation>)`, `Advice (IReadOnlyList<AdviceExpression>)`, `VariableDefinitions (IReadOnlyList<VariableDefinition>)`, `IsEnabled (bool, default true)`, `Priority (int, default 0)`
   - `Rule` — sealed record: `Id (string)`, `Description (string?)`, `Effect (Effect)`, `Target (Target?)`, `Condition (Apply?)` (null = unconditional), `Obligations (IReadOnlyList<Obligation>)`, `Advice (IReadOnlyList<AdviceExpression>)`

4. **Target model** (`Model/` folder) — XACML 3.0 §7.6:
   - `Target` — sealed record: `AnyOf (IReadOnlyList<AnyOf>)` (all AnyOf elements must match = AND)
   - `AnyOf` — sealed record: `AllOf (IReadOnlyList<AllOf>)` (any AllOf element can match = OR)
   - `AllOf` — sealed record: `Matches (IReadOnlyList<Match>)` (all Match elements must match = AND)
   - `Match` — sealed record: `FunctionId (string)`, `AttributeDesignator (AttributeDesignator)`, `AttributeValue (AttributeValue)`

5. **Attribute model** (`Model/` folder) — XACML 3.0 §7.3:
   - `AttributeDesignator` — sealed record: `Category (AttributeCategory)`, `AttributeId (string)`, `DataType (string)`, `MustBePresent (bool, default false)`
   - `AttributeValue` — sealed record: `DataType (string)`, `Value (object?)`
   - `AttributeBag` — sealed class: `IReadOnlyList<AttributeValue>` wrapper, supports multi-valued attributes + bag functions

6. **Condition / Apply model** (`Model/` folder) — XACML 3.0 §7.7:
   - `Apply` — sealed record: `FunctionId (string)`, `Arguments (IReadOnlyList<IExpression>)` (arguments can be Apply, AttributeDesignator, AttributeValue, VariableReference)
   - `IExpression` — interface: marker for Apply, AttributeDesignator, AttributeValue, VariableReference
   - `VariableDefinition` — sealed record: `VariableId (string)`, `Expression (IExpression)`
   - `VariableReference` — sealed record implementing `IExpression`: `VariableId (string)`

7. **Obligation / Advice model** (`Model/` folder) — XACML 3.0 §7.18:
   - `Obligation` — sealed record: `Id (string)`, `FulfillOn (FulfillOn)`, `AttributeAssignments (IReadOnlyList<AttributeAssignment>)`
   - `AdviceExpression` — sealed record: `Id (string)`, `AppliesTo (FulfillOn)`, `AttributeAssignments (IReadOnlyList<AttributeAssignment>)`
   - `AttributeAssignment` — sealed record: `AttributeId (string)`, `Category (AttributeCategory?)`, `Value (IExpression)`

8. **Decision response records** (`Model/` folder):
   - `PolicyDecision` — sealed record: `Effect (Effect)`, `Status (DecisionStatus?)`, `Obligations (IReadOnlyList<Obligation>)`, `Advice (IReadOnlyList<AdviceExpression>)`, `PolicyId (string?)`, `RuleId (string?)`, `Reason (string?)`, `EvaluationDuration (TimeSpan)`
   - `DecisionStatus` — sealed record: `StatusCode (string)`, `StatusMessage (string?)` (XACML 3.0 §7.10)
   - `PolicyEvaluationContext` — sealed record: `SubjectAttributes (AttributeBag)`, `ResourceAttributes (AttributeBag)`, `EnvironmentAttributes (AttributeBag)`, `ActionAttributes (AttributeBag)`, `RequestType (Type)`, `IncludeAdvice (bool, default true)`
   - `PolicyEvaluationResult` — sealed record: `Effect (Effect)`, `PolicyId (string)`, `Obligations (IReadOnlyList<Obligation>)`, `Advice (IReadOnlyList<AdviceExpression>)` (used by combining algorithms)
   - `RuleEvaluationResult` — sealed record: `Rule (Rule)`, `Effect (Effect)`, `Obligations (IReadOnlyList<Obligation>)`, `Advice (IReadOnlyList<AdviceExpression>)`

9. **`PublicAPI.Unshipped.txt`** — Add all public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Security.ABAC (Issue #401) — Full XACML 3.0 type system.

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a NEW project: src/Encina.Security.ABAC/
- Implementing FULL XACML 3.0 specification (OASIS Standard, March 2013) with C#-idiomatic types
- All domain models are sealed records with XML documentation
- Use LanguageExt for Option<T> and Either<L, R>

TASK:
Create the project file and ALL model types listed in Phase 1 Tasks: enums (Effect with 4 values, CombiningAlgorithmId with 8 values, FulfillOn), XACML structural records (PolicySet, Policy, Rule), Target model (Target, AnyOf, AllOf, Match), Attribute model (AttributeDesignator, AttributeValue, AttributeBag), Condition/Apply model (Apply, IExpression, VariableDefinition, VariableReference), Obligation/Advice model (Obligation, AdviceExpression, AttributeAssignment), and Decision records (PolicyDecision, DecisionStatus, PolicyEvaluationContext, PolicyEvaluationResult, RuleEvaluationResult).

KEY RULES:
- Target net10.0, enable nullable, enable implicit usings
- Dependencies include Microsoft.CodeAnalysis.CSharp.Scripting v5.0.0 (Roslyn scripting for expression compilation)
- All types are sealed records except IExpression (interface) and AttributeBag (sealed class)
- All public types need XML documentation with <summary>, <remarks> referencing XACML 3.0 sections
- Effect has 4 values: Permit, Deny, NotApplicable, Indeterminate (NOT just 2!)
- CombiningAlgorithmId has 8 values (all XACML 3.0 standard algorithms)
- PolicySet is HIERARCHICAL: contains nested PolicySets AND Policies
- Target uses AnyOf/AllOf/Match triple nesting (XACML 3.0 §7.6)
- Apply implements IExpression; its arguments are IExpression[] (recursive tree)
- AttributeDesignator, AttributeValue, VariableReference all implement IExpression
- PolicyDecision carries Obligations AND Advice (post-decision actions)
- PolicyEvaluationContext uses AttributeBag (multi-valued) not simple dictionaries
- Obligations have FulfillOn: Permit or Deny (when to execute)

REFERENCE FILES:
- src/Encina.Security/SecurityErrors.cs (error factory pattern)
- src/Encina.Compliance.GDPR/Model/LawfulBasis.cs (enum pattern)
- src/Encina.Security/Abstractions/ISecurityContext.cs (immutable record pattern)
- XACML 3.0 OASIS Standard §5-7 (policy language, request/response, evaluation model)
```

</details>

---

### Phase 2: Core Interfaces, Attributes & Errors

> **Goal**: Define the public API surface — XACML component interfaces, function registry, attributes, and error codes.

<details>
<summary><strong>Tasks</strong></summary>

1. **XACML component interfaces** (`Abstractions/` folder):
   - `IPolicyDecisionPoint` — full XACML evaluation:
     - `EvaluateAsync(PolicyEvaluationContext context, CancellationToken)` → `ValueTask<PolicyDecision>` (always returns decision with 4-effect semantics + obligations + advice)
   - `IPolicyAdministrationPoint` — policy + policy set CRUD (all `Either<EncinaError, T>`):
     - `GetPolicySetsAsync(CancellationToken)` → `ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>>`
     - `GetPolicySetAsync(string policySetId, CancellationToken)` → `ValueTask<Either<EncinaError, Option<PolicySet>>>`
     - `AddPolicySetAsync(PolicySet policySet, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`
     - `UpdatePolicySetAsync(PolicySet policySet, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`
     - `RemovePolicySetAsync(string policySetId, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`
     - `GetPoliciesAsync(string? policySetId, CancellationToken)` → `ValueTask<Either<EncinaError, IReadOnlyList<Policy>>>`
     - `GetPolicyAsync(string policyId, CancellationToken)` → `ValueTask<Either<EncinaError, Option<Policy>>>`
     - `AddPolicyAsync(Policy policy, string? parentPolicySetId, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`
     - `UpdatePolicyAsync(Policy policy, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`
     - `RemovePolicyAsync(string policyId, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`
   - `IPolicyInformationPoint` — on-demand attribute retrieval:
     - `ResolveAttributeAsync(AttributeDesignator designator, CancellationToken)` → `ValueTask<AttributeBag>` (returns bag, possibly empty)
   - `IAttributeProvider` — attribute collection:
     - `GetSubjectAttributesAsync(string userId, CancellationToken)` → `ValueTask<IReadOnlyDictionary<string, object>>`
     - `GetResourceAttributesAsync<TResource>(TResource resource, CancellationToken)` → `ValueTask<IReadOnlyDictionary<string, object>>`
     - `GetEnvironmentAttributesAsync(CancellationToken)` → `ValueTask<IReadOnlyDictionary<string, object>>`
   - `ICombiningAlgorithm` — XACML combining:
     - `CombineRuleResults(IReadOnlyList<RuleEvaluationResult> results)` → `Effect`
     - `CombinePolicyResults(IReadOnlyList<PolicyEvaluationResult> results)` → `PolicyEvaluationResult`
     - `AlgorithmId { get; }` → `CombiningAlgorithmId`
   - `IObligationHandler` — PEP obligation execution:
     - `CanHandle(string obligationId)` → `bool`
     - `HandleAsync(Obligation obligation, PolicyEvaluationContext context, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`

2. **Function registry interfaces** (`Functions/` folder):
   - `IFunctionRegistry` — function management:
     - `GetFunction(string functionId)` → `IXACMLFunction?`
     - `Register(string functionId, IXACMLFunction function)` → `void`
     - `GetAllFunctionIds()` → `IReadOnlyList<string>`
   - `IXACMLFunction` — individual function:
     - `Evaluate(IReadOnlyList<object?> arguments)` → `object?`
     - `ReturnType { get; }` → `string`

3. **Attributes** (`Attributes/` folder):
   - `RequirePolicyAttribute : SecurityAttribute` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]`
     - Constructor: `RequirePolicyAttribute(string policyName)`
     - Properties: `PolicyName (string)`, `AllMustPass (bool, default true)`
   - `RequireConditionAttribute : SecurityAttribute` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]`
     - Constructor: `RequireConditionAttribute(string expression)`
     - Property: `Expression (string)`

4. **Error codes** (`ABACErrors.cs`):
   - Error code prefix: `abac.`
   - Codes:
     - `abac.access_denied` — Policy evaluation resulted in Deny
     - `abac.indeterminate` — Policy evaluation resulted in Indeterminate (error during evaluation)
     - `abac.policy_not_found` — Referenced policy does not exist
     - `abac.policy_set_not_found` — Referenced policy set does not exist
     - `abac.evaluation_failed` — Policy evaluation threw an exception
     - `abac.attribute_resolution_failed` — Could not resolve required attribute (MustBePresent = true)
     - `abac.invalid_policy` — Policy definition is invalid
     - `abac.invalid_policy_set` — PolicySet definition is invalid
     - `abac.invalid_condition` — Condition expression could not be parsed
     - `abac.duplicate_policy` — Policy with same ID already exists
     - `abac.duplicate_policy_set` — PolicySet with same ID already exists
     - `abac.combining_failed` — Combining algorithm produced Indeterminate
     - `abac.missing_context` — Security context not available
     - `abac.obligation_failed` — Mandatory obligation handler failed (access must be denied per XACML spec)
     - `abac.function_not_found` — Referenced function not in registry
     - `abac.function_error` — Function evaluation threw an exception
     - `abac.variable_not_found` — VariableReference to undefined VariableDefinition

5. **Constants** (`EnvironmentAttributes.cs`, `XACMLDataTypes.cs`, `XACMLFunctionIds.cs`):
   - `EnvironmentAttributes`: `CurrentTime`, `DayOfWeek`, `IsBusinessHours`, `IpAddress`, `UserAgent`, `TenantId`, `Region`, `RequestPath`, `HttpMethod`
   - `XACMLDataTypes`: `String`, `Boolean`, `Integer`, `Double`, `Date`, `DateTime`, `Time`, `AnyURI`, `HexBinary`, `Base64Binary`, `DayTimeDuration`, `YearMonthDuration`
   - `XACMLFunctionIds`: All pre-registered function identifier constants (C#-friendly names)

6. **`PublicAPI.Unshipped.txt`** — Update

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Security.ABAC (Issue #401) — Full XACML 3.0 interfaces.

CONTEXT:
- Phase 1 models are implemented with FULL XACML 3.0 type system (PolicySet, 4 effects, obligations, etc.)
- PAP now manages both PolicySets AND Policies (hierarchical CRUD)
- PDP returns full PolicyDecision with obligations + advice
- ICombiningAlgorithm handles both rule-level AND policy-level combining
- IObligationHandler is NEW: PEP must execute obligations after decision

TASK:
Create all interfaces, attributes, error codes, and constants listed in Phase 2 Tasks.

KEY RULES:
- IPolicyDecisionPoint always returns PolicyDecision (4 possible effects: Permit, Deny, NotApplicable, Indeterminate)
- IPolicyAdministrationPoint manages PolicySets AND Policies separately (hierarchical CRUD)
- ICombiningAlgorithm has BOTH CombineRuleResults AND CombinePolicyResults methods
- IObligationHandler: PEP calls after decision; if mandatory obligation fails → MUST deny access
- IFunctionRegistry is extensible: pre-registered XACML functions + custom user functions
- IXACMLFunction.Evaluate takes list of arguments (recursive — can be results of other functions)
- ABACErrors includes new codes: indeterminate, obligation_failed, function_not_found, variable_not_found, policy_set variants
- XACMLDataTypes maps to XACML 3.0 Appendix B data type URNs
- XACMLFunctionIds maps to XACML 3.0 Appendix A function URNs (C#-friendly names)

REFERENCE FILES:
- src/Encina.Security/SecurityErrors.cs (error factory pattern)
- src/Encina.Security/Abstractions/IPermissionEvaluator.cs (ValueTask interface pattern)
- src/Encina.Security/Attributes/RequirePermissionAttribute.cs (SecurityAttribute inheritance)
- XACML 3.0 OASIS Standard §7, Appendices A-C
```

</details>

---

### Phase 3: Function Registry & Standard Functions

> **Goal**: Implement the XACML 3.0 function registry with all standard functions.

<details>
<summary><strong>Tasks</strong></summary>

1. **Function registry** (`Functions/DefaultFunctionRegistry.cs`):
   - `public sealed class DefaultFunctionRegistry : IFunctionRegistry`
   - `ConcurrentDictionary<string, IXACMLFunction>` storage
   - Constructor pre-registers all standard XACML 3.0 functions

2. **Standard function implementations** (`Functions/Standard/` folder — one class per category):
   - `EqualityFunctions.cs` — `string-equal`, `boolean-equal`, `integer-equal`, `double-equal`, `date-equal`, `dateTime-equal`, `time-equal`
   - `ComparisonFunctions.cs` — `*-greater-than`, `*-less-than`, `*-greater-than-or-equal`, `*-less-than-or-equal` for integer, double, string, date, dateTime, time
   - `ArithmeticFunctions.cs` — `integer-add`, `integer-subtract`, `integer-multiply`, `integer-divide`, `integer-mod`, `integer-abs`, `double-add`, `double-subtract`, `double-multiply`, `double-divide`, `double-abs`, `round`, `floor`
   - `StringFunctions.cs` — `string-concatenate`, `string-starts-with`, `string-ends-with`, `string-contains`, `string-substring`, `string-normalize-space`, `string-normalize-to-lower-case`, `string-length`
   - `LogicalFunctions.cs` — `and`, `or`, `not`, `n-of`
   - `BagFunctions.cs` — `*-one-and-only`, `*-bag-size`, `*-is-in`, `*-bag` (create bag from values)
   - `SetFunctions.cs` — `*-intersection`, `*-union`, `*-subset`, `*-at-least-one-member-of`, `*-set-equals`
   - `HigherOrderFunctions.cs` — `any-of`, `all-of`, `any-of-any`, `all-of-any`, `all-of-all`, `map`
   - `TypeConversionFunctions.cs` — `string-from-integer`, `integer-from-string`, `double-from-string`, `boolean-from-string`, `string-from-boolean`, `string-from-double`, `string-from-dateTime`
   - `RegexFunctions.cs` — `string-regexp-match`

3. **Function registration helper** (`Functions/FunctionRegistrationExtensions.cs`):
   - Extension methods on `IFunctionRegistry` to register function categories:
   - `RegisterEqualityFunctions()`, `RegisterComparisonFunctions()`, etc.
   - `RegisterAllStandardFunctions()` — calls all registration methods

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Security.ABAC (Issue #401) — XACML 3.0 function registry.

CONTEXT:
- Phases 1-2 are implemented (full XACML type system, interfaces)
- The function registry is central to XACML 3.0: conditions use functions (not operators)
- Each function implements IXACMLFunction with Evaluate(IReadOnlyList<object?>) → object?
- Functions must handle type coercion and null arguments gracefully

TASK:
Create the DefaultFunctionRegistry and all standard XACML 3.0 function implementations.

KEY RULES:
- DefaultFunctionRegistry pre-registers all standard functions in constructor
- Each function category in its own file (EqualityFunctions, ComparisonFunctions, etc.)
- Functions follow XACML 3.0 Appendix A semantics exactly
- Type coercion: try Convert.ChangeType for mismatched types, return Indeterminate on failure
- Bag functions operate on AttributeBag (multi-valued attributes)
- Higher-order functions (any-of, all-of, map) take a function ID as first argument
- Logical functions: and/or short-circuit, not inverts boolean, n-of counts true results
- Set functions operate on bags: intersection, union, subset, at-least-one-member-of
- All functions validate argument count and types — throw descriptive errors
- Register all functions with C#-friendly IDs (not XACML URNs): "string-equal" not "urn:oasis:names:tc:xacml:1.0:function:string-equal"
- Thread-safe: ConcurrentDictionary in registry

REFERENCE FILES:
- XACML 3.0 OASIS Standard Appendix A (function definitions)
- src/Encina.Security/SecurityErrors.cs (error pattern for function errors)
```

</details>

---

### Phase 4: Policy DSL, Condition Evaluator & EEL Compiler

> **Goal**: Implement the fluent policy builder, condition evaluator using the function registry, and EEL (Encina Expression Language) — the Roslyn-based expression compiler for `[RequireCondition]`.

<details>
<summary><strong>Tasks</strong></summary>

1. **PolicySet builder** (`DSL/PolicySetBuilder.cs`):
   - `public sealed class PolicySetBuilder`
   - `public static PolicySetBuilder Create(string policySetId)` — static factory
   - Fluent methods:
     - `.WithDescription(string)`, `.WithTarget(Action<TargetBuilder>)`, `.WithPriority(int)`
     - `.AddPolicy(Action<PolicyBuilder> configure)` → adds a child policy
     - `.AddPolicySet(Action<PolicySetBuilder> configure)` → adds a child policy set (nesting)
     - `.Using(CombiningAlgorithmId algorithm)` → combining algorithm
     - `.WithObligation(Action<ObligationBuilder> configure)` → adds obligation
     - `.WithAdvice(Action<AdviceBuilder> configure)` → adds advice
     - `.Build()` → `PolicySet`

2. **Policy builder** (`DSL/PolicyBuilder.cs`):
   - Same as before plus:
     - `.WithTarget(Action<TargetBuilder> configure)` → structured XACML target
     - `.WithObligation(Action<ObligationBuilder> configure)` → obligation
     - `.WithAdvice(Action<AdviceBuilder> configure)` → advice
     - `.DefineVariable(string variableId, Action<ExpressionBuilder> expression)` → variable definition
     - `.ForResourceType<TResource>()` → shortcut that builds a Target matching the type name

3. **Rule builder** (`DSL/RuleBuilder.cs`):
   - `.Permit()`, `.Deny()`, `.WithDescription(string)`
   - `.WithTarget(Action<TargetBuilder> configure)` → rule-level target
   - `.When(Action<ConditionBuilder> condition)` → builds Apply tree
   - `.WithObligation(Action<ObligationBuilder>)`, `.WithAdvice(Action<AdviceBuilder>)`
   - `.AsDefault()` → no target, no condition (matches everything)

4. **Target builder** (`DSL/TargetBuilder.cs`):
   - `.AddAnyOf(Action<AnyOfBuilder> configure)` → adds AnyOf element
   - `AnyOfBuilder.AddAllOf(Action<AllOfBuilder> configure)` → adds AllOf
   - `AllOfBuilder.AddMatch(string functionId, AttributeDesignator designator, AttributeValue value)` → adds Match
   - Shortcut: `.MatchSubject(string attributeId, string functionId, object value)`, `.MatchResource(...)`, `.MatchAction(...)`

5. **Obligation / Advice builders** (`DSL/ObligationBuilder.cs`, `DSL/AdviceBuilder.cs`):
   - `.WithId(string)`, `.OnPermit()` / `.OnDeny()`, `.AssignAttribute(string attrId, IExpression value)`

6. **Condition builder** (`DSL/ConditionBuilder.cs`):
   - C#-idiomatic API that builds `Apply` trees:
   - `.Apply(string functionId, params IExpression[] args)` → nested function application
   - `.Attribute(string category, string attributeId)` → AttributeDesignator
   - `.Value(object value)` → AttributeValue
   - `.Variable(string variableId)` → VariableReference
   - Convenience: `.Equal(AttributeDesignator attr, object value)` → `Apply("string-equal", attr, value)` with type inference
   - Convenience: `.And(params Apply[] conditions)`, `.Or(...)`, `.Not(Apply condition)`

7. **Condition evaluator** (`Evaluation/ConditionEvaluator.cs`):
   - `public sealed class ConditionEvaluator`
   - `Either<EncinaError, object?> Evaluate(IExpression expression, PolicyEvaluationContext context, IReadOnlyDictionary<string, IExpression>? variables = null)`
   - Evaluation dispatch by `IExpression` type:
     - `AttributeValue` → return value directly
     - `AttributeDesignator` → resolve from context's AttributeBag by category; if not found and MustBePresent → Indeterminate
     - `VariableReference` → lookup in variables dict → evaluate recursively
     - `Apply` → evaluate all arguments recursively → call function from `IFunctionRegistry` → return result
   - Dependencies: `IFunctionRegistry`, `IPolicyInformationPoint` (for lazy attribute resolution)

8. **EEL compiler** (`Evaluation/EELCompiler.cs`):
   - `public sealed class EELCompiler` (Encina Expression Language — Roslyn-backed)
   - Compiles `[RequireCondition]` C# expressions into cached `ScriptRunner<bool>` delegates via Roslyn scripting
   - Uses `CSharpScript.Create<bool>(expression, options, globalsType: typeof(EELGlobals))` + `CreateDelegate()`
   - `EELGlobals` (formerly `EELGlobals`) — sealed class with `dynamic` properties for XACML attribute categories:
     - `dynamic user` → subject attributes (maps to `AttributeCategory.Subject`)
     - `dynamic resource` → resource attributes (maps to `AttributeCategory.Resource`)
     - `dynamic environment` → environment attributes (maps to `AttributeCategory.Environment`)
     - `dynamic action` → action attributes (maps to `AttributeCategory.Action`)
   - `ScriptOptions` configured with minimal imports: `System`, `System.Linq`, `System.Collections.Generic`
   - Compilation cache: `ConcurrentDictionary<string, ScriptRunner<bool>>` — compile once at startup, reuse forever
   - `CompileAsync(string expression)` → `Either<EncinaError, ScriptRunner<bool>>` — Left if Roslyn diagnostics contain errors
   - `EvaluateAsync(string expression, EELGlobals globals)` → `ValueTask<Either<EncinaError, bool>>` — resolves from cache, evaluates delegate
   - Error handling: Roslyn `Diagnostic` errors mapped to `ABACErrors.InvalidCondition` with position, message, and severity
   - Thread-safe: `ConcurrentDictionary` for cache, `SemaphoreSlim` for concurrent compilation of the same expression

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Security.ABAC (Issue #401) — DSL, evaluator, and EEL (Encina Expression Language) compiler for XACML 3.0.

CONTEXT:
- Phases 1-3 are implemented (full XACML types, interfaces, function registry with standard functions)
- The DSL must build full XACML structures: PolicySet (hierarchical), Policy, Rule, Target, Obligations, Advice
- The condition evaluator uses the function registry (not direct operator comparison)
- The EEL (Encina Expression Language) compiler compiles [RequireCondition] C# expressions into cached ScriptRunner<bool> delegates

TASK:
Create all builders (PolicySet, Policy, Rule, Target, Obligation, Advice, Condition), the ConditionEvaluator, and EELCompiler.

KEY RULES:
- PolicySetBuilder supports nesting: .AddPolicySet() within a policy set (recursive hierarchy)
- PolicyBuilder.ForResourceType<T>() is a shortcut that builds Target with Match on resource type
- TargetBuilder follows XACML AnyOf/AllOf/Match triple nesting
- ConditionBuilder produces Apply trees, NOT simple operator conditions
- ConditionEvaluator evaluates IExpression recursively:
  - AttributeValue → return value
  - AttributeDesignator → resolve from context, MustBePresent → Indeterminate if missing
  - VariableReference → lookup and evaluate
  - Apply → evaluate args → call IXACMLFunction → return result
- Evaluator returns Either<EncinaError, object?> — Left for Indeterminate cases
- EELCompiler: uses CSharpScript.Create<bool>(expr, globalsType: typeof(EELGlobals)) + CreateDelegate()
- EELGlobals (Encina Expression Language globals): dynamic properties for user (subject), resource, environment, action attribute categories
- ScriptOptions: minimal imports (System, System.Linq, System.Collections.Generic)
- Cache compiled ScriptRunner<bool> delegates in ConcurrentDictionary<string, ScriptRunner<bool>>
- Compilation errors → ABACErrors.InvalidCondition with Roslyn Diagnostic details (position, message)
- Thread-safe: ConcurrentDictionary for cache, SemaphoreSlim for concurrent compilation
- All builders validate on Build(): non-empty IDs, valid effects, at least one rule in Policy

REFERENCE FILES:
- src/Encina.Compliance.DataResidency/DataResidencyFluentPolicyDescriptor.cs (fluent builder)
- Microsoft.CodeAnalysis.CSharp.Scripting API: CSharpScript.Create<T>(), ScriptOptions.Default, ScriptRunner<T>
- XACML 3.0 §7.6 (Target evaluation), §7.7 (Condition evaluation), §7.11-7.14 (Apply, Function)
```

</details>

---

### Phase 5: Default Implementations & Combining Algorithms

> **Goal**: Provide full XACML 3.0 evaluation engine with all 8 combining algorithms.

<details>
<summary><strong>Tasks</strong></summary>

1. **Target evaluator** (`Evaluation/TargetEvaluator.cs`):
   - `internal sealed class TargetEvaluator`
   - `Effect EvaluateTarget(Target? target, PolicyEvaluationContext context)` → `Permit` (match), `NotApplicable` (no match), `Indeterminate` (error)
   - Follows XACML 3.0 §7.6: all AnyOf must match (AND); within each AnyOf, any AllOf can match (OR); within each AllOf, all Matches must match (AND)

2. **In-memory PDP** (`Evaluation/XACMLPolicyDecisionPoint.cs`):
   - `public sealed class XACMLPolicyDecisionPoint : IPolicyDecisionPoint`
   - Full XACML 3.0 evaluation algorithm (§7.12-7.14):
     1. Get root PolicySets from PAP
     2. For each PolicySet: evaluate Target → if applicable, recursively evaluate child PolicySets and Policies
     3. For each Policy: evaluate Target → if applicable, evaluate all Rules
     4. For each Rule: evaluate Target → if applicable, evaluate Condition via ConditionEvaluator
     5. Combine rule results using Policy's combining algorithm
     6. Combine policy results using PolicySet's combining algorithm
     7. Collect Obligations (matching FulfillOn) and Advice
     8. Return PolicyDecision with final Effect, Obligations, and Advice

3. **In-memory PAP** (`InMemory/InMemoryPolicyAdministrationPoint.cs`):
   - `ConcurrentDictionary<string, PolicySet>` for policy sets
   - `ConcurrentDictionary<string, Policy>` for standalone policies
   - Hierarchical CRUD: add policy to specific policy set, or as standalone

4. **All 8 combining algorithms** (`Evaluation/Algorithms/` folder):
   - `DenyOverridesCombiningAlgorithm` — XACML 3.0 §C.2
   - `PermitOverridesCombiningAlgorithm` — XACML 3.0 §C.4
   - `FirstApplicableCombiningAlgorithm` — XACML 3.0 §C.6
   - `OnlyOneApplicableCombiningAlgorithm` — XACML 3.0 §C.8 (Indeterminate if >1 applicable)
   - `DenyUnlessPermitCombiningAlgorithm` — XACML 3.0 §C.10 (simplified: no Indeterminate/NotApplicable)
   - `PermitUnlessDenyCombiningAlgorithm` — XACML 3.0 §C.12 (simplified: always decides)
   - `OrderedDenyOverridesCombiningAlgorithm` — XACML 3.0 §C.3 (ordered evaluation)
   - `OrderedPermitOverridesCombiningAlgorithm` — XACML 3.0 §C.5 (ordered evaluation)

5. **Attribute providers** (`Providers/` folder):
   - `ClaimsAttributeProvider : IAttributeProvider` — maps claims to subject attributes
   - `EnvironmentAttributeProvider : IAttributeProvider` — time-based attributes via TimeProvider
   - `RequestAttributeProvider : IAttributeProvider` — reflection-based request property extraction
   - `CompositeAttributeProvider : IAttributeProvider` — aggregates all providers

6. **Default PIP** (`Providers/DefaultPolicyInformationPoint.cs`):
   - Resolves attributes by category from registered providers
   - Returns AttributeBag (multi-valued)

7. **Default obligation handler** (`Obligations/LoggingObligationHandler.cs`):
   - Built-in handler that logs obligation fulfillment via ILogger
   - Handles obligations with ID prefix `log:` — e.g., `log:access-audit`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Security.ABAC (Issue #401) — Full XACML 3.0 evaluation engine.

CONTEXT:
- Phases 1-4 are implemented (types, interfaces, functions, DSL, evaluator)
- PDP must implement the FULL XACML 3.0 evaluation algorithm (§7.12-7.14)
- 8 combining algorithms (not 3) — each with distinct semantics including Indeterminate handling
- PolicySet evaluation is RECURSIVE (nested PolicySets)
- Obligations are collected from ALL applicable rules/policies/policy sets that match FulfillOn

TASK:
Create TargetEvaluator, XACMLPolicyDecisionPoint, InMemoryPAP, all 8 combining algorithms, attribute providers, PIP, and obligation handler.

KEY RULES:
- TargetEvaluator: null Target = always matches (Permit); evaluation errors → Indeterminate
- PDP evaluation is recursive: PolicySet → child PolicySets/Policies → Rules
- Combining algorithms MUST handle 4 effects correctly:
  - DenyOverrides: if any Deny → Deny; if any Indeterminate with potential Deny → Indeterminate(DP); else if Permit → Permit; else NotApplicable
  - PermitOverrides: mirror of DenyOverrides
  - FirstApplicable: first non-NotApplicable result wins
  - OnlyOneApplicable: exactly ONE applicable → use its result; 0 → NotApplicable; >1 → Indeterminate
  - DenyUnlessPermit: any Permit → Permit; otherwise → Deny (never NotApplicable/Indeterminate)
  - PermitUnlessDeny: any Deny → Deny; otherwise → Permit
  - OrderedDeny/PermitOverrides: same as unordered but GUARANTEE evaluation order
- Obligations collected from rules + policies + policy sets that match the final decision's FulfillOn
- Advice collected same as obligations but non-binding
- InMemoryPAP: hierarchical storage, target matching supports type patterns and wildcards

REFERENCE FILES:
- XACML 3.0 §7.12-7.14 (evaluation), §C (combining algorithms), §7.18 (obligations)
- src/Encina.Security/DefaultPermissionEvaluator.cs (evaluator pattern)
- src/Encina.Compliance.Consent/InMemoryConsentStore.cs (ConcurrentDictionary pattern)
```

</details>

---

### Phase 6: Pipeline Behavior — `ABACPipelineBehavior` with Obligation Execution

> **Goal**: Implement the PEP (Policy Enforcement Point) as a pipeline behavior that evaluates policies and executes obligations.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ABACPipelineBehavior<TRequest, TResponse>`** (`ABACPipelineBehavior.cs`):
   - Implements `IPipelineBehavior<TRequest, TResponse>` where `TRequest : IRequest<TResponse>`
   - Static per-generic-type attribute caching:

     ```
     private static readonly ABACAttributeInfo? CachedAttributeInfo = ResolveAttributeInfo()
     ```

   - `Handle` method flow:
     1. If `EnforcementMode == Disabled` → `nextStep()`
     2. If `CachedAttributeInfo is null` → `nextStep()`
     3. Build `PolicyEvaluationContext` via `AttributeContextBuilder`
     4. Evaluate via `IPolicyDecisionPoint.EvaluateAsync(context)`
     5. Handle 4-effect result:
        - `Permit`: execute `OnPermit` obligations → if any fails, DENY (XACML spec) → `nextStep()`
        - `Deny`: execute `OnDeny` obligations → return `Left(ABACErrors.AccessDenied(...))`
        - `NotApplicable`: if `DenyOnNotApplicable` option → Deny; otherwise → `nextStep()`
        - `Indeterminate`: return `Left(ABACErrors.Indeterminate(...))`
     6. Attach advice to response metadata (via `IRequestContext` or response wrapper)

2. **Obligation executor** (`Obligations/ObligationExecutor.cs`):
   - `internal sealed class ObligationExecutor`
   - Resolves `IObligationHandler` from DI for each obligation
   - Executes all obligations matching the decision's FulfillOn
   - If ANY mandatory obligation handler fails → returns error (PEP must deny)
   - If no handler found for an obligation → configurable behavior (fail/warn)

3. **`ABACAttributeInfo`** (private sealed record):
   - `IReadOnlyList<string> PolicyNames`, `IReadOnlyList<Apply> InlineConditions`, `bool AllMustPass`

4. **Attribute context builder** (`Evaluation/AttributeContextBuilder.cs`):
   - Builds `PolicyEvaluationContext` with `AttributeBag` collections (not simple dictionaries)
   - Resolves attributes from all `IAttributeProvider` implementations
   - Wraps values in `AttributeBag` for XACML bag function compatibility

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Security.ABAC (Issue #401) — PEP with obligation execution.

CONTEXT:
- Phases 1-5 are implemented (full XACML engine, 8 algorithms, function registry)
- The pipeline behavior is the PEP (Policy Enforcement Point) in XACML terms
- XACML REQUIRES the PEP to execute obligations — this is NOT optional
- If an obligation handler fails, access MUST be denied (XACML 3.0 §7.18)

TASK:
Create ABACPipelineBehavior, ObligationExecutor, ABACAttributeInfo, AttributeContextBuilder.

KEY RULES:
- Static per-generic-type attribute caching pattern (same as LawfulBasisValidationPipelineBehavior)
- Handle ALL 4 effects: Permit → execute OnPermit obligations → allow; Deny → execute OnDeny obligations → reject; NotApplicable → configurable; Indeterminate → reject
- ObligationExecutor: resolves IObligationHandler from DI via CanHandle(obligationId)
- If no handler for a mandatory obligation → DENY access (XACML spec)
- Advice is best-effort: failure doesn't affect the decision
- AttributeContextBuilder wraps values in AttributeBag for XACML bag function compatibility
- Error details include: requestType, policyId, effect, obligations attempted/failed, evaluation duration
- Three enforcement modes: Block/Warn/Disabled

REFERENCE FILES:
- src/Encina.Compliance.GDPR/LawfulBasisValidationPipelineBehavior.cs (static caching pattern)
- src/Encina.Security/SecurityPipelineBehavior.cs (attribute evaluation)
- XACML 3.0 §7.18 (obligation enforcement requirements)
```

</details>

---

### Phase 7: Configuration, DI & Registration

> **Goal**: Wire everything together with options, service registration, and health check.

<details>
<summary><strong>Tasks</strong></summary>

1. **Options** (`ABACOptions.cs`):
   - `ABACEnforcementMode EnforcementMode { get; set; }` — default: `Block`
   - `bool DenyOnNotApplicable { get; set; }` — default: `true` (deny if no policy applies)
   - `bool FailOnMissingObligationHandler { get; set; }` — default: `true` (XACML spec)
   - `bool AddHealthCheck { get; set; }` — default: `false`
   - `TimeOnly BusinessHoursStart { get; set; }` — default: `new TimeOnly(9, 0)`
   - `TimeOnly BusinessHoursEnd { get; set; }` — default: `new TimeOnly(17, 0)`
   - `DayOfWeek[] BusinessDays { get; set; }` — default: `[Monday..Friday]`
   - `CombiningAlgorithmId DefaultCombiningAlgorithm { get; set; }` — default: `DenyOverrides`
   - `List<PolicySet> PolicySets { get; }` — programmatic policy sets
   - `List<Policy> Policies { get; }` — programmatic standalone policies
   - **Fluent methods**:
     - `AddAttributeProvider<TProvider>()` where `TProvider : IAttributeProvider`
     - `AddObligationHandler<THandler>()` where `THandler : IObligationHandler`
     - `AddFunction(string functionId, IXACMLFunction function)` — custom function
     - `AddPolicySet(Action<PolicySetBuilder> configure)` — build and register policy set
     - `AddPolicy(Action<PolicyBuilder> configure)` — build and register standalone policy
     - `AddCombiningAlgorithm<TAlgorithm>()` where `TAlgorithm : ICombiningAlgorithm` — custom algorithm

2. **Options validator** (`ABACOptionsValidator.cs`)

3. **Service collection extensions** (`ServiceCollectionExtensions.cs`):
   - `AddEncinaABAC(this IServiceCollection services, Action<ABACOptions>? configure = null)`
   - Registers all core services, all 8 combining algorithms, built-in attribute providers, function registry, pipeline behavior, obligation executor, evaluators, health check

4. **Policy seeder** (`ABACPolicySeedingHostedService.cs`)

5. **Health check** (`Health/ABACHealthCheck.cs`):
   - `const string DefaultName = "encina-abac"`
   - Tags: `["encina", "security", "abac", "xacml", "ready"]`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Security.ABAC (Issue #401) — DI and configuration.

CONTEXT:
- Phases 1-6 are fully implemented (XACML engine, pipeline behavior, obligation execution)
- DI must register ALL components: 8 combining algorithms, function registry with standard functions, evaluators, providers, obligation handler(s), pipeline behavior

TASK:
Create options, DI registration, policy seeder, and health check.

KEY RULES:
- ABACOptions has NEW XACML-specific options: DenyOnNotApplicable, FailOnMissingObligationHandler
- All 8 combining algorithms registered via TryAddEnumerable
- IFunctionRegistry registered as singleton with all standard functions pre-loaded
- IObligationHandler registered via TryAddEnumerable (multiple handlers possible)
- Custom functions from options.AddFunction() registered into IFunctionRegistry at startup
- ABACPolicySeedingHostedService seeds BOTH PolicySets and standalone Policies
- Phase 8 adds EELExpressionPrecompilationService for fail-fast startup validation of all EEL expressions
- Health check: Degraded if no policies/policy sets, Healthy if at least one exists

REFERENCE FILES:
- src/Encina.Security/ServiceCollectionExtensions.cs (DI pattern)
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs (TryAddEnumerable)
```

</details>

---

### Phase 8: EEL Validation Tooling

> **Goal**: Provide pre-runtime expression validation through a Roslyn DiagnosticAnalyzer (compile-time IDE squiggles) and a CLI validator (CI/CD integration). Based on patterns from .NET's `[GeneratedRegex]` analyzer, `[StringSyntax]` attribute, `opa check`, and `terraform validate`.

<details>
<summary><strong>Tasks</strong></summary>

#### 8a. Roslyn DiagnosticAnalyzer (`src/Encina.Security.ABAC.Analyzers/`)

> **Rationale**: The gold standard for developer experience — red squiggles in the IDE as you type `[RequireCondition("...")]`. Ships as a NuGet package with `PrivateAssets="all"` (development dependency only, not in runtime). Follows the pattern of `Microsoft.EntityFrameworkCore.Analyzers`.

1. **Project setup** (`Encina.Security.ABAC.Analyzers.csproj`):
   - `Microsoft.CodeAnalysis.CSharp` analyzer project
   - `PackBuildOutput = false` (analyzer-only package)
   - Targets `netstandard2.0` (Roslyn analyzer requirement)

2. **`RequireConditionAnalyzer.cs`** — `DiagnosticAnalyzer` for `[RequireCondition]` expressions:
   - Registers for `SyntaxKind.Attribute` nodes
   - Extracts string literal from `[RequireCondition("...")]` argument
   - Attempts compilation via `CSharpScript.Create<bool>(expression, options, typeof(EELGlobals))`
   - Reports Roslyn diagnostics as analyzer diagnostics

3. **Diagnostic rules**:

   | Rule ID | Severity | Description |
   |---------|----------|-------------|
   | `EEL001` | Error | EEL expression does not compile as valid C# |
   | `EEL002` | Warning | EEL expression uses unknown top-level identifier (not `user`, `resource`, `environment`, `action`) |
   | `EEL003` | Warning | EEL expression may not return `bool` |
   | `EEL004` | Info | EEL expression is empty or whitespace |
   | `EEL005` | Warning | EEL expression uses potentially expensive operations (e.g., LINQ over unbounded collections) |
   | `EEL006` | Warning | EEL expression contains statements (`;`, `if`, `for`, `while`) — only expressions allowed |

4. **Unit tests** (`tests/Encina.UnitTests/Security/ABAC/Analyzers/`):
   - `RequireConditionAnalyzerTests.cs` — using `Microsoft.CodeAnalysis.Testing` framework
   - Test each diagnostic rule with valid and invalid expressions
   - Verify diagnostic positions, messages, and severities

#### 8b. CLI Validator (`dotnet tool`)

> **Rationale**: CI/CD integration — validate all EEL expressions in a project before deployment. Equivalent to `opa check` or `terraform validate` for EEL expressions.

5. **Project setup** (`tools/Encina.Security.ABAC.Cli/`):
   - `dotnet tool` with command: `dotnet encina-abac validate`
   - Scans compiled assemblies for `[RequireCondition]` attributes
   - Compiles each expression via `EELCompiler`
   - Reports results in console (table) or SARIF format (for CI integration)

6. **CLI commands**:

   | Command | Description |
   |---------|-------------|
   | `dotnet encina-abac validate --project <path>` | Validate all EEL expressions in a project |
   | `dotnet encina-abac validate --assembly <path>` | Validate EEL expressions in a compiled assembly |
   | `dotnet encina-abac eval "<expression>" [--globals <json>]` | Evaluate a single EEL expression interactively |
   | `dotnet encina-abac list --project <path>` | List all EEL expressions found in a project |

7. **Output formats**:
   - Console table (default): expression, status (✅/❌), error message
   - `--format sarif` — SARIF output for CI/CD integration (GitHub Actions, Azure DevOps)
   - `--format json` — machine-readable JSON output
   - Exit code: `0` if all valid, `1` if any errors found

#### 8c. Startup Fail-Fast Enhancement

8. **EEL expression pre-compilation hosted service** (`EELExpressionPrecompilationService.cs`):
   - `IHostedLifecycleService.StartingAsync()` — runs before `StartAsync()`
   - Scans all `[RequireCondition]` attributes via reflection
   - Compiles all expressions concurrently (`Task.WhenAll` with bounded `SemaphoreSlim`)
   - On success: logs structured summary `[EEL] Compiled {N} expressions in {elapsed}ms (0 errors)`
   - On failure: logs each error with class name, expression, and Roslyn diagnostic → throws `InvalidOperationException` with aggregated details
   - Caches compiled delegates in `EELCompiler` for runtime use (zero re-compilation)

#### 8d. Test Helpers (`Encina.Testing` integration)

9. **`EELTestHelper.cs`** — test utility for validating EEL expressions:

   ```csharp
   // Validate all expressions in an assembly compile correctly
   EELTestHelper.ValidateAllExpressions(typeof(GetOrderQuery).Assembly);

   // Evaluate a single expression against test data
   var result = await EELTestHelper.EvaluateAsync(
       "user.Department == \"Finance\"",
       new { user = new { Department = "Finance" }, resource = new { } });

   // Assert expression compiles
   EELTestHelper.AssertCompiles("user.Clearance >= resource.Sensitivity");

   // Assert expression does NOT compile (for negative testing)
   EELTestHelper.AssertDoesNotCompile("invalid !!!");
   ```

#### 8e. IDE Integration & Developer Experience

> **Rationale**: Research found that Roslyn's embedded language APIs (`IEmbeddedLanguage`, `EmbeddedLanguageClassifier`) are **internal and not extensible** by third parties — full syntax highlighting inside EEL strings is not achievable. However, three practical enhancements provide excellent developer experience across IDEs with minimal complexity.

10. **CodeFixProvider** (extend 8a — `src/Encina.Security.ABAC.Analyzers/`):
    - `RequireConditionCodeFixProvider.cs` — companion to the DiagnosticAnalyzer
    - Quick fixes for common EEL errors:

      | Diagnostic | Code Fix | Description |
      |------------|----------|-------------|
      | `EEL001` (compilation error) | Suggest similar property names | Levenshtein distance: "Did you mean `user.Role`?" |
      | `EEL004` (empty expression) | Replace with `true` / `false` | One-click fix for placeholder expressions |
      | `EEL006` (statements detected) | Extract to method suggestion | Guide user to move logic to a method |

    - Uses `CodeAction.Create()` with document edit operations
    - Unit tests: `RequireConditionCodeFixProviderTests.cs` (using `Microsoft.CodeAnalysis.Testing`)

11. **`[StringSyntax("EEL")]` attribute annotation** (`src/Encina.Security.ABAC/`):
    - Add `[StringSyntax("EEL")]` to the `expression` parameter of `RequireConditionAttribute`
    - **Future-proofing**: when/if .NET tooling adds support for custom `[StringSyntax]` identifiers, EEL expressions will automatically get enhanced validation
    - Pattern: same as `[StringSyntax(StringSyntaxAttribute.Regex)]` on `Regex.IsMatch()`
    - Zero runtime cost — attribute is compile-time metadata only
    - No current IDE benefit, but positions EEL for future tooling ecosystem

12. **JetBrains Rider `[LanguageInjection]` support** (`src/Encina.Security.ABAC/`):
    - Add `[LanguageInjection(InjectedLanguage = "csharp")]` to `RequireConditionAttribute` expression parameter
    - **Rider users** get partial C# IntelliSense, syntax highlighting, and error detection inside EEL strings
    - Requires `JetBrains.Annotations` NuGet package (`PrivateAssets="all"`, development-only dependency)
    - Conditional compilation: `#if JETBRAINS_ANNOTATIONS` to avoid hard dependency for non-Rider users
    - Alternative approach: `[StringSyntax("csharp")]` which Rider also recognizes without extra dependency

> **Future consideration**: A strongly-typed `RequireCondition<TRequest>(Expression<Func<EELContext<TRequest>, bool>>)` generic variant could provide full IntelliSense by using expression trees instead of strings. This would complement (not replace) string-based EEL. Deferred to post-1.0 as it couples policy definitions to request types.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Security.ABAC (Issue #401) — EEL Validation Tooling.

CONTEXT:
- Phases 1-7 are fully implemented
- EEL (Encina Expression Language) compiles [RequireCondition] C# expressions via Roslyn at startup
- Phase 8 provides pre-runtime validation tools to catch EEL errors earlier in the development cycle

TASK:
Create five validation and IDE integration components:
1. Roslyn DiagnosticAnalyzer + CodeFixProvider (src/Encina.Security.ABAC.Analyzers/) — compile-time IDE squiggles + quick fixes
2. CLI validator (tools/Encina.Security.ABAC.Cli/) — CI/CD integration
3. Startup fail-fast hosted service (EELExpressionPrecompilationService)
4. Test helpers (EELTestHelper) for Encina.Testing integration
5. IDE integration annotations ([StringSyntax("EEL")] + [LanguageInjection] for Rider)

KEY RULES:
Analyzer + CodeFixProvider:
- Target netstandard2.0 (Roslyn analyzer requirement)
- Register for SyntaxKind.Attribute, extract string from [RequireCondition("...")]
- Attempt CSharpScript.Create<bool>(expr, options, typeof(EELGlobals))
- Report Roslyn diagnostics as EEL001-EEL006
- CodeFixProvider: suggest similar properties (EEL001), offer true/false (EEL004), extract to method (EEL006)
- PrivateAssets="all" — dev dependency only
- Limitation: dynamic properties cannot be type-checked (EEL002 is best-effort)

CLI:
- dotnet tool with verify/eval/list commands
- SARIF output for CI integration
- Assembly scanning via reflection for [RequireCondition] attributes
- Exit code 0/1 for scripting

Startup Service:
- IHostedLifecycleService.StartingAsync() — earliest possible moment
- Concurrent compilation with bounded SemaphoreSlim
- Structured logging of compilation summary
- Fail-fast: InvalidOperationException if any expression fails

Test Helpers:
- ValidateAllExpressions(Assembly) — one-liner test
- EvaluateAsync(expression, anonymousGlobals) — ad-hoc evaluation
- AssertCompiles / AssertDoesNotCompile — assertion helpers

IDE Integration:
- [StringSyntax("EEL")] on RequireConditionAttribute expression parameter — future-proofing
- [LanguageInjection(InjectedLanguage = "csharp")] for JetBrains Rider — partial C# IntelliSense
- JetBrains.Annotations with PrivateAssets="all" + conditional #if JETBRAINS_ANNOTATIONS
- Roslyn embedded language APIs (IEmbeddedLanguage) are INTERNAL — full syntax highlighting NOT possible

REFERENCE FILES:
- .NET [GeneratedRegex] analyzer pattern
- [StringSyntax("Regex")] validation approach
- Microsoft.CodeAnalysis.Testing framework for analyzer unit tests
- src/Encina.Security.ABAC/Evaluation/EELCompiler.cs (reuse compilation logic)
```

</details>

---

### Phase 9: Observability — Diagnostics, Metrics & Logging

> **Goal**: Add OpenTelemetry traces, counters, and structured logging.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ABACDiagnostics.cs`** (`Diagnostics/` folder):
   - `internal static class ABACDiagnostics`
   - `ActivitySource`: `"Encina.Security.ABAC"`, version `"1.0"`
   - `Meter`: `"Encina.Security.ABAC"`, version `"1.0"`
   - **Counters** (Counter<long>):
     - `abac.evaluations.total` — tagged by `request_type`, `outcome` (permit, deny, not_applicable, indeterminate, error)
     - `abac.policy.evaluations` — tagged by `policy_id`, `outcome`
     - `abac.policy_set.evaluations` — tagged by `policy_set_id`, `outcome`
     - `abac.obligation.executions` — tagged by `obligation_id`, `outcome` (success, failed, no_handler)
     - `abac.attribute.resolutions` — tagged by `category`, `outcome`
     - `abac.function.invocations` — tagged by `function_id`, `outcome`
   - **Histogram** (Histogram<double>):
     - `abac.evaluation.duration_ms` — tagged by `request_type`
     - `abac.obligation.duration_ms` — tagged by `obligation_id`
   - **Tag constants**:
     - `TagRequestType`, `TagPolicyId`, `TagPolicySetId`, `TagOutcome`, `TagCategory`, `TagEnforcementMode`, `TagAlgorithm`, `TagObligationId`, `TagFunctionId`, `TagEffect`

2. **`ABACLogMessages.cs`** (`Diagnostics/` folder):
   - **Event ID range: 9000-9099**:
     - 9000-9005: Evaluation lifecycle (started, permit, deny, not_applicable, indeterminate, skipped)
     - 9006: Enforcement disabled / 9007: Enforcement warning
     - 9010-9015: Policy/PolicySet evaluation (started, completed, target mismatch, not found)
     - 9020-9023: Attribute resolution (started, completed, failed, MustBePresent violation)
     - 9030-9033: Condition/function evaluation
     - 9040-9043: Obligation execution (started, completed, failed, no handler)
     - 9050-9052: Advice processing
     - 9060-9062: Policy seeding
     - 9070: Health check
     - 9080-9082: Function registry (registered, custom added, not found)

3. **Integrate observability** into ABACPipelineBehavior, XACMLPolicyDecisionPoint, ObligationExecutor, ConditionEvaluator

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Encina.Security.ABAC (Issue #401) — Observability.

CONTEXT:
- Event IDs 9000-9099 (between Security core 8000 and AntiTampering 9100)
- Full XACML 3.0: need metrics for policy sets, policies, rules, obligations, functions, and 4-effect outcomes

TASK:
Create ABACDiagnostics, ABACLogMessages, integrate into all evaluation components.

KEY RULES:
- Counters track ALL 4 effects (permit/deny/not_applicable/indeterminate) not just 2
- Obligation execution has its own metrics (success/failed/no_handler)
- Function invocations tracked for performance monitoring
- PolicySet evaluations tracked separately from Policy evaluations
- Duration histograms for overall evaluation AND per-obligation
- Log messages use [LoggerMessage] source generator

REFERENCE FILES:
- src/Encina.Security/Diagnostics/SecurityDiagnostics.cs (pattern)
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs (Stopwatch timing)
```

</details>

---

### Phase 10: Testing

> **Goal**: Comprehensive test coverage across all test categories.

<details>
<summary><strong>Tasks</strong></summary>

#### 10a. Unit Tests (`tests/Encina.UnitTests/Security/ABAC/`)

**Models & Enums** (~15 tests):
- `EffectTests.cs`, `PolicySetTests.cs`, `PolicyTests.cs`, `RuleTests.cs`, `TargetTests.cs`, `ObligationTests.cs`, `AttributeDesignatorTests.cs`, `AttributeBagTests.cs`, `ApplyTests.cs`

**Function Registry** (~40 tests):
- `DefaultFunctionRegistryTests.cs` — registration, lookup, custom functions
- `EqualityFunctionsTests.cs`, `ComparisonFunctionsTests.cs`, `ArithmeticFunctionsTests.cs`, `StringFunctionsTests.cs`, `LogicalFunctionsTests.cs`, `BagFunctionsTests.cs`, `SetFunctionsTests.cs`, `HigherOrderFunctionsTests.cs`, `TypeConversionFunctionsTests.cs`, `RegexFunctionsTests.cs`

**DSL Builders** (~25 tests):
- `PolicySetBuilderTests.cs`, `PolicyBuilderTests.cs`, `RuleBuilderTests.cs`, `TargetBuilderTests.cs`, `ConditionBuilderTests.cs`, `ObligationBuilderTests.cs`, `AdviceBuilderTests.cs`

**Evaluation Engine** (~40 tests):
- `ConditionEvaluatorTests.cs` — all function types, type coercion, MustBePresent, VariableReference, nested Apply
- `TargetEvaluatorTests.cs` — AnyOf/AllOf/Match triple nesting, null target, Indeterminate
- `XACMLPolicyDecisionPointTests.cs` — full evaluation algorithm, PolicySet recursion, obligation collection
- `EELCompilerTests.cs` — valid/invalid C# expressions, Roslyn diagnostic error mapping, compilation caching, EELGlobals binding, concurrent compilation thread safety

**EEL Conformance Test Suite** (~80-100 tests):

> **Rationale**: Expression languages require dedicated, comprehensive testing beyond what standard unit tests cover. Based on testing patterns from CEL (Google), SpEL (Spring), NCalc (.NET), and JEXL (Apache). The EEL test suite follows a **data-driven conformance** approach inspired by CEL's `*.textproto` test files.

- `EELConformanceTests.cs` — **Data-driven test suite** using JSON test data files (`tests/Encina.UnitTests/Security/ABAC/EEL/TestData/*.json`):
  - Each JSON file contains test cases: `{ "name": "...", "expression": "...", "globals": {...}, "expected": true/false, "expectError": "..." }`
  - Test runner deserializes, compiles expression, evaluates against globals, asserts result or error

  **Test data files organized by category** (following CEL/SpEL patterns):

  | File | Category | Tests | What It Validates |
  |------|----------|-------|-------------------|
  | `eel-literals.json` | Literals | ~8 | String, integer, double, boolean, null, char literals |
  | `eel-comparison.json` | Comparison operators | ~12 | `==`, `!=`, `<`, `>`, `<=`, `>=` across types (string, int, double, bool, null) |
  | `eel-logical.json` | Logical operators | ~10 | `&&`, `||`, `!`, short-circuit evaluation, De Morgan's laws |
  | `eel-arithmetic.json` | Arithmetic | ~8 | `+`, `-`, `*`, `/`, `%` with int/double, overflow, division by zero |
  | `eel-null-safety.json` | Null handling | ~10 | `?.`, `??`, null comparisons, null propagation, `NullReferenceException` paths |
  | `eel-property-access.json` | Property access | ~10 | `user.Department`, nested `user.Manager.Name`, missing properties, dynamic resolution |
  | `eel-method-calls.json` | Method calls | ~10 | `.Contains()`, `.StartsWith()`, `.EndsWith()`, `.ToLower()`, `.Any()`, `.All()` |
  | `eel-collections.json` | Collection operations | ~8 | `.Contains()`, `.Any(x => ...)`, `.All(x => ...)`, `.Count`, LINQ on arrays/lists |
  | `eel-string-operations.json` | String operations | ~8 | Concatenation, interpolation (NOT supported), escape sequences, Unicode |
  | `eel-complex-expressions.json` | Combined expressions | ~10 | Multi-operator expressions with precedence, nested parentheses, real-world ABAC patterns |

- `EELErrorTests.cs` — **Error message quality tests** (following SpEL `ParserErrorMessagesTests.java` pattern):
  - Every compilation error produces a human-readable message with position
  - Tests for: syntax errors, unknown identifiers, type mismatches, missing parentheses, invalid operators
  - Verifies Roslyn `Diagnostic` → `ABACErrors.InvalidCondition` mapping preserves position and message quality
  - **~15 tests** for common developer mistakes and their error messages

- `EELEdgeCaseTests.cs` — **Boundary and edge case tests** (following CEL/Lox `limit/` patterns):
  - Empty string expression → meaningful error
  - Whitespace-only expression → meaningful error
  - Very long expressions (>10KB) → compilation within timeout
  - Deeply nested parentheses (50+ levels) → no stack overflow
  - Unicode identifiers in property names
  - Expressions returning non-bool (int, string) → meaningful error
  - Semicolons, statements, assignments → rejected with clear error
  - `async`/`await`, `throw`, `new` → rejected appropriately

- `EELCachingTests.cs` — **Compilation caching tests** (following NCalc `MemoryCacheTests.cs` pattern):
  - Same expression compiled twice → same `ScriptRunner<bool>` instance reused
  - Different expressions → different delegates
  - Concurrent compilation of same expression → single compilation, others wait
  - Cache size under load (1000 unique expressions)
  - `SemaphoreSlim` contention under concurrent access

- `EELThreadSafetyTests.cs` — **Concurrent evaluation tests** (following NCalc `MultiThreadTests.cs` pattern):
  - Same compiled expression evaluated concurrently from 100 threads → all produce correct results
  - Different expressions evaluated concurrently → no cross-contamination
  - Concurrent compilation + evaluation → no deadlocks
  - `EELGlobals` instances are independent per evaluation (no shared mutable state)

- `EELDocumentationTests.cs` — **Documentation example tests** (following SpEL `SpelDocumentationTests.java` pattern):
  - Every expression example from docs `10-eel-language-guide.md` through `12-eel-cookbook.md` is verified to compile and evaluate correctly
  - Ensures documentation stays in sync with implementation

**Combining Algorithms** (~30 tests):
- One test class per algorithm (8 classes) — verify XACML spec behavior including Indeterminate handling

**Providers & Pipeline** (~25 tests):
- `ClaimsAttributeProviderTests.cs`, `EnvironmentAttributeProviderTests.cs`, `RequestAttributeProviderTests.cs`, `CompositeAttributeProviderTests.cs`
- `ABACPipelineBehaviorTests.cs` — 4 effects, obligation execution, enforcement modes
- `ObligationExecutorTests.cs` — handler resolution, failure → deny, missing handler behavior

**Infrastructure** (~10 tests):
- `ABACErrorsTests.cs`, `ABACOptionsValidatorTests.cs`, `ServiceCollectionExtensionsTests.cs`, `ABACHealthCheckTests.cs`

**Target**: ~280-320 unit tests (includes ~80-100 EEL conformance tests)

#### 10b. Guard Tests (`tests/Encina.GuardTests/Security/ABAC/`)

**Target**: ~60-80 guard tests

#### 10c. Contract Tests (`tests/Encina.ContractTests/Security/ABAC/`)

- `IPolicyDecisionPointContractTests.cs` — 4-effect semantics, obligation attachment
- `IPolicyAdministrationPointContractTests.cs` — hierarchical CRUD
- `IAttributeProviderContractTests.cs`, `ICombiningAlgorithmContractTests.cs` — all 8 algorithms
- `IObligationHandlerContractTests.cs`, `IXACMLFunctionContractTests.cs`

**Target**: ~25-30 contract tests

#### 10d. Property Tests (`tests/Encina.PropertyTests/Security/ABAC/`)

- `CombiningAlgorithmPropertyTests.cs` — XACML spec invariants for all 8 algorithms:
  - DenyOverrides(all_permit) = Permit, DenyOverrides(any_deny) = Deny
  - PermitUnlessDeny never returns NotApplicable or Indeterminate
  - DenyUnlessPermit never returns NotApplicable or Indeterminate
  - OnlyOneApplicable(>1 applicable) = Indeterminate
- `TargetEvaluatorPropertyTests.cs` — null target always matches
- `ConditionEvaluatorPropertyTests.cs` — boolean algebra (and/or/not) invariants via functions
- `PolicyBuilderPropertyTests.cs` — Build always produces valid structures
- `FunctionRegistryPropertyTests.cs` — equality functions are symmetric, comparison functions are transitive
- `EELPropertyTests.cs` — EEL-specific invariants via FsCheck:
  - `compile(expr) → evaluate(expr, globals1) == evaluate(expr, globals1)` (deterministic evaluation)
  - `eval(a && b) == eval(b && a)` for pure expressions (commutativity)
  - `eval(!(!x)) == eval(x)` (double negation elimination)
  - `eval(a || true) == true`, `eval(a && false) == false` (identity laws)
  - Random valid expression generation → always compiles without crash (no unhandled exceptions)

**Target**: ~35-45 property tests

#### 10e. Load Tests

- `ABACEvaluationLoadTests.cs` — concurrent evaluations with nested PolicySets, obligations

**Target**: 1 load test class

#### 10f. Benchmark Tests

- `ABACEvaluationBenchmarks.cs` — 0/1/10/50 policies, flat vs nested PolicySets
- `ConditionEvaluatorBenchmarks.cs` — simple function vs deep nested Apply tree
- `CombiningAlgorithmBenchmarks.cs` — 8 algorithms with varying result counts
- `FunctionRegistryBenchmarks.cs` — lookup + invocation
- `EELCompilerBenchmarks.cs` — cold compilation, warm cached evaluation, concurrent compilation

**Target**: 5 benchmark classes

#### 10g. Build Verification

- 0 build errors, 0 warnings
- ≥85% line coverage
- All tests pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of Encina.Security.ABAC (Issue #401) — Testing.

CONTEXT:
- Phases 1-9 are fully implemented (full XACML 3.0 engine with obligations, 8 algorithms, function registry)
- More test surface than simplified version: 8 algorithms, 70+ functions, obligations, 4 effects
- EELCompiler tests need Roslyn-specific scenarios (compilation errors, caching, thread safety)

TASK:
Create comprehensive tests across all test categories: Unit, Guard, Contract, Property, Load, Benchmark.
Special emphasis on EEL (Encina Expression Language) conformance testing — expression languages require dedicated test suites beyond standard unit tests.

KEY RULES:
Unit Tests:
- XACML function tests: every standard function category needs its own test class
- Combining algorithm tests: MUST verify all 4-effect handling per XACML spec (not just Permit/Deny)
- ObligationExecutor: test failure → deny access behavior
- Pipeline behavior: test all 4 effects + obligation execution + enforcement modes
- EELCompiler: test valid expressions compile and evaluate, invalid expressions produce ABACErrors.InvalidCondition with Roslyn diagnostics, cached delegates reused, concurrent compilation thread safety, EELGlobals attribute binding

EEL Conformance Test Suite (CRITICAL — ~80-100 tests):
- Data-driven tests using JSON test data files (expression + globals + expected result/error)
- Organized by category: literals, comparison, logical, arithmetic, null-safety, property access, method calls, collections, strings, complex expressions
- Dedicated error message quality tests (every error must be human-readable with position)
- Edge case tests: empty input, deeply nested parens, Unicode, non-bool return, statements rejected
- Caching tests: delegate reuse, SemaphoreSlim contention, concurrent compilation
- Thread safety tests: 100-thread concurrent evaluation
- Documentation tests: every example from EEL docs must compile and evaluate correctly (keeps docs in sync)
- Based on testing patterns from CEL (Google conformance suite), SpEL (Spring), NCalc (.NET), JEXL (Apache)

Contract Tests:
- ICombiningAlgorithm: verify all 8 implementations follow the same contract
- All 4 effects must be properly handled in combining algorithms

Property Tests:
- XACML combining algorithm invariants are the most important property tests
- DenyUnlessPermit and PermitUnlessDeny NEVER return NotApplicable/Indeterminate
- EEL invariants: deterministic evaluation, commutativity of pure logical ops, double negation, identity laws

Benchmark Tests:
- BenchmarkSwitcher (NOT BenchmarkRunner)
- Compare flat policies vs nested PolicySets
- EELCompiler: cold compilation vs warm cached evaluation benchmarks

REFERENCE FILES:
- tests/Encina.UnitTests/Security/ (existing patterns)
- tests/Encina.PropertyTests/ (FsCheck patterns)
- CEL conformance suite pattern: https://github.com/google/cel-spec/tree/master/tests/simple/testdata
- SpEL test organization: ParserErrorMessagesTests, SpelDocumentationTests, SpelReproTests
- NCalc test patterns: MultiThreadTests, MemoryCacheTests, CustomCultureTests
```

</details>

---

### Phase 11: Documentation

> **Goal**: Comprehensive documentation covering XACML 3.0 concepts, Encina's implementation, EEL (Encina Expression Language) reference, and all supporting materials. This is a **documentation-heavy phase** because Full XACML 3.0 is a complex standard that requires extensive explanation, and the EEL (Encina Expression Language) needs its own complete language reference.

<details>
<summary><strong>Tasks</strong></summary>

#### 11a. Architecture Decision Records

1. **`docs/architecture/adr/NNN-xacml-3.0-full-specification.md`** — ADR documenting the choice of Full XACML 3.0 vs simplified XACML
   - Options evaluated: Simplified XACML (2 effects, 3 algorithms), Custom ABAC, Full XACML 3.0
   - Why full XACML 3.0: pre-1.0 best-solution philosophy, PolicySet hierarchy, 4 effects, obligations
   - Trade-offs: complexity vs completeness, learning curve vs specification compliance

2. **`docs/architecture/adr/NNN-roslyn-scripting-for-abac-expressions.md`** — ADR documenting Roslyn scripting vs custom recursive descent parser
   - Options evaluated: Recursive descent parser, Roslyn scripting, System.Linq.Expressions, skip inline expressions
   - Why Roslyn: battle-tested parser, part of .NET ecosystem, CreateDelegate() pattern, rich diagnostics
   - Trade-offs: NuGet dependency vs custom code bugs, cold start vs parsing correctness

3. **`docs/architecture/adr/NNN-eel-encina-expression-language-naming.md`** — ADR documenting the EEL naming decision
   - Why name the expression language: industry convention (SpEL, CEL, JEXL), cleaner documentation, distinct identity
   - Options evaluated: EEL (Encina Expression Language), Acorn, EAEL, Oak, unnamed
   - Why EEL: 3-letter convention, general enough for future use beyond ABAC, immediately descriptive

#### 11b. XACML 3.0 Concepts Guide (`docs/features/security-abac/`)

> **Rationale**: Full XACML 3.0 is a complex standard with many interrelated concepts. Users unfamiliar with XACML need a comprehensive conceptual introduction before they can use the library effectively. Based on documentation patterns from AuthzForce, WSO2, Axiomatics, and the OASIS specification itself.

3. **`docs/features/security-abac/01-introduction.md`** — What is ABAC? What is XACML?
   - What is Attribute-Based Access Control (ABAC) — comparison with RBAC, ACL, ReBAC
   - What is XACML 3.0 — history (OASIS standard, January 2013), purpose, relationship to ABAC
   - When to use XACML vs simpler authorization — decision guide table
   - Encina.Security.ABAC vs other XACML implementations (AuthzForce, OPA, Casbin) — positioning
   - Glossary of XACML terms (from OASIS spec §1.1)

4. **`docs/features/security-abac/02-architecture.md`** — XACML Reference Architecture
   - **PDP** (Policy Decision Point) — the brain: evaluates policies against request attributes
   - **PEP** (Policy Enforcement Point) — the guard: intercepts requests, calls PDP, enforces decisions
   - **PAP** (Policy Administration Point) — the manager: stores, manages, and provides policies
   - **PIP** (Policy Information Point) — the resolver: provides missing attributes on demand
   - **Context Handler** — builds the XACML request context from raw application data
   - Data flow diagram: Request → PEP → Context Handler → PDP ← PAP + PIP → Decision → PEP → Enforcement
   - How Encina maps XACML architecture: `ABACPipelineBehavior` = PEP, `XACMLPolicyDecisionPoint` = PDP, `InMemoryPolicyAdministrationPoint` = PAP, `DefaultPolicyInformationPoint` = PIP
   - **Mermaid diagrams**: Architecture overview, request/response sequence diagram, evaluation flow

5. **`docs/features/security-abac/03-policy-language.md`** — XACML Policy Language Model
   - **PolicySet** — hierarchical container for Policies and nested PolicySets
     - PolicySet nesting: organizational hierarchy (Department → Team → Project)
     - Combining algorithms at PolicySet level
   - **Policy** — container for Rules with a combining algorithm
     - Policy structure: Target + Rules + Obligations + Advice + Variables
   - **Rule** — leaf node with an Effect (Permit/Deny) and optional Condition
     - Rule structure: Target + Condition + Effect + Obligations + Advice
   - **Target** — structured matching with AnyOf/AllOf/Match triple nesting (XACML §7.6)
     - AnyOf = OR groups, AllOf = AND within group, Match = individual comparison
     - How Target matching determines policy applicability
   - **Condition** — boolean expression using Apply/Function tree
     - Nested Apply nodes, AttributeDesignator resolution, VariableReference
   - **Obligations** — mandatory post-decision actions the PEP must execute
     - FulfillOn: Permit or Deny — when the obligation is triggered
     - If PEP fails to fulfill → access MUST be denied (XACML §7.18)
   - **Advice** — optional post-decision recommendations
     - Same structure as Obligations but non-binding
   - **VariableDefinition/VariableReference** — reusable sub-expressions within a Policy
   - Policy hierarchy diagram (PolicySet → Policy → Rule tree) with Mermaid
   - Complete annotated policy example with all elements

6. **`docs/features/security-abac/04-effects-and-decisions.md`** — The 4-Effect Model
   - **Permit** — access allowed
   - **Deny** — access denied
   - **NotApplicable** — no applicable policy found (configurable: deny-by-default or allow)
   - **Indeterminate** — error during evaluation (missing attribute, function error, etc.)
   - Extended Indeterminate values: Indeterminate{D}, Indeterminate{P}, Indeterminate{DP}
   - Decision flow diagram (Target match → Condition → Effect → Combining → Decision)
   - Why 4 effects matter (2-effect models silently lose Indeterminate/NotApplicable information)
   - `DenyOnNotApplicable` option: configuring default behavior for unmatched requests

7. **`docs/features/security-abac/05-combining-algorithms.md`** — The 8 Combining Algorithms
   - Complete reference for each algorithm with:
     - Behavior description
     - Use case (when to choose this algorithm)
     - Truth table (inputs → output for all 4-effect combinations)
     - Code example showing configuration
   - **DenyOverrides** (§C.2) — any Deny wins → safety-critical systems
   - **PermitOverrides** (§C.4) — any Permit wins → whitelist scenarios
   - **FirstApplicable** (§C.6) — first non-NotApplicable wins → priority lists
   - **OnlyOneApplicable** (§C.8) — exactly one must apply → mutual exclusion
   - **DenyUnlessPermit** (§C.10) — simplified deny-by-default (never NotApplicable/Indeterminate)
   - **PermitUnlessDeny** (§C.12) — simplified permit-by-default
   - **OrderedDenyOverrides** (§C.3) — guaranteed evaluation order
   - **OrderedPermitOverrides** (§C.5) — guaranteed evaluation order
   - Comparison matrix table (all 8 algorithms side by side)
   - Algorithm selection decision tree (flowchart: "Do you need deny-by-default? → Yes → DenyOverrides or DenyUnlessPermit...")
   - Custom combining algorithms: `options.AddCombiningAlgorithm<CustomAlgorithm>()`

8. **`docs/features/security-abac/06-attributes.md`** — Attribute System
   - **Attribute categories**: Subject, Resource, Environment, Action (XACML §B.2)
   - **AttributeDesignator**: Category + AttributeId + DataType + MustBePresent
     - MustBePresent = true → Indeterminate if attribute can't be resolved
   - **AttributeValue**: typed literal values
   - **AttributeBag**: multi-valued attributes (XACML bag semantics)
     - Why bags: a user can have multiple roles, a resource multiple tags
     - Bag functions: `one-and-only`, `bag-size`, `is-in`, `bag`
     - Set functions: `intersection`, `union`, `subset`, `at-least-one-member-of`, `set-equals`
   - **Built-in attribute providers**:
     - `ClaimsAttributeProvider` — maps ClaimsPrincipal claims to subject attributes
     - `EnvironmentAttributeProvider` — time, day of week, business hours (via TimeProvider)
     - `RequestAttributeProvider` — reflects request object properties to resource attributes
     - `HttpEnvironmentAttributeProvider` — IP address, user agent, request path (ASP.NET Core)
   - **Custom attribute providers**: implementing `IAttributeProvider`
   - **PIP (Policy Information Point)**: on-demand lazy attribute resolution during evaluation

9. **`docs/features/security-abac/07-obligations-and-advice.md`** — Post-Decision Actions
   - Obligations vs Advice: mandatory vs optional
   - FulfillOn: when obligations/advice trigger (Permit or Deny)
   - Obligation execution flow: PDP decision → PEP collects obligations → PEP executes via `IObligationHandler`
   - XACML §7.18 requirement: if PEP cannot fulfill a mandatory obligation → MUST deny access
   - Built-in `LoggingObligationHandler` (handles `log:*` obligation IDs)
   - Implementing custom `IObligationHandler`: `CanHandle(string obligationId)` + `HandleAsync(...)`
   - Real-world obligation examples:
     - `log:access-audit` — log access decision to audit trail
     - `notify:admin` — send notification to admin on denied access
     - `mfa:escalate` — require MFA for sensitive resources
     - `watermark:document` — add watermark when downloading classified documents
   - `FailOnMissingObligationHandler` option: configuring behavior when no handler is found
   - Advice processing: best-effort, failure doesn't affect decision

#### 11c. XACML Function Library Reference (`docs/features/security-abac/`)

> **Rationale**: The 70+ standard XACML functions are the "vocabulary" of policy conditions. Users need a searchable reference with signatures, descriptions, and examples. Based on documentation patterns from OPA Rego (28 function categories), Kubernetes CEL (extension libraries), and the XACML 3.0 Appendix A.

10. **`docs/features/security-abac/08-function-reference.md`** — Complete XACML Function Library
    - **Equality Functions** (A.3.1) — `string-equal`, `boolean-equal`, `integer-equal`, `double-equal`, `date-equal`, `dateTime-equal`, `time-equal`
      - Each with: signature, parameters, return type, description, C# example
    - **Comparison Functions** (A.3.2-A.3.5) — `*-greater-than`, `*-less-than`, `*-greater-than-or-equal`, `*-less-than-or-equal` for integer, double, string, date, dateTime, time
    - **Arithmetic Functions** (A.3.6) — `integer-add`, `integer-subtract`, `integer-multiply`, `integer-divide`, `integer-mod`, `integer-abs`, `double-add`, `double-subtract`, `double-multiply`, `double-divide`, `double-abs`, `round`, `floor`
    - **String Functions** (A.3.9) — `string-concatenate`, `string-starts-with`, `string-ends-with`, `string-contains`, `string-substring`, `string-normalize-space`, `string-normalize-to-lower-case`, `string-length`
    - **Logical Functions** (A.3.13) — `and`, `or`, `not`, `n-of`
      - Short-circuit behavior documented
    - **Bag Functions** (A.3.10-A.3.12) — `*-one-and-only`, `*-bag-size`, `*-is-in`, `*-bag`
      - Explanation of bag semantics and multi-valued attributes
    - **Set Functions** (A.3.11) — `*-intersection`, `*-union`, `*-subset`, `*-at-least-one-member-of`, `*-set-equals`
      - Venn diagram illustrations for each set operation
    - **Higher-Order Functions** (A.3.12) — `any-of`, `all-of`, `any-of-any`, `all-of-any`, `all-of-all`, `map`
      - How higher-order functions take a function ID as first argument
    - **Type Conversion Functions** (A.3.15) — `string-from-integer`, `integer-from-string`, `double-from-string`, `boolean-from-string`, etc.
    - **Regular Expression Functions** (A.3.14) — `string-regexp-match`
    - **Custom functions**: how to register with `options.AddFunction("geo-distance", new GeoDistanceFunction())`
    - **Quick reference table**: all functions in a single table (Function Name | Category | Parameters | Return Type)

11. **`docs/features/security-abac/09-data-types.md`** — XACML Data Types
    - Standard XACML 3.0 data types (Appendix B): `string`, `boolean`, `integer`, `double`, `date`, `dateTime`, `time`, `anyURI`, `hexBinary`, `base64Binary`, `dayTimeDuration`, `yearMonthDuration`
    - C# type mapping table (XACML type → .NET type)
    - Type coercion rules: how the evaluator handles type mismatches
    - Custom data types: extending the type system

#### 11d. EEL (Encina Expression Language) Reference (`docs/features/security-abac/`)

> **Rationale**: EEL (Encina Expression Language) uses Roslyn-compiled C# expressions as a policy DSL. While it's "real C#", it runs in a special context (`EELGlobals`) that needs complete documentation. Users need to understand what C# subset is available, what variables exist, how types work, and how errors are reported. Named **EEL** following the convention of SpEL, CEL, JEXL. Based on documentation patterns from Spring EL, Symfony ExpressionLanguage, OGNL, JEXL, and NCalc.

12. **`docs/features/security-abac/10-eel-language-guide.md`** — EEL (Encina Expression Language) Guide
    - **What is EEL**: Encina Expression Language — C# expressions compiled via Roslyn for ABAC policy conditions, named following the convention of SpEL, CEL, JEXL
    - **How it works**: expressions are compiled at startup via `CSharpScript.Create<bool>()`, cached as `ScriptRunner<bool>` delegates, evaluated at <1ms per request
    - **Why Roslyn as backend**: battle-tested parser, full C# power, no custom grammar bugs, rich error messages
    - **Available context variables** (`EELGlobals`):
      - `user` (dynamic) — subject attributes mapped from claims
        - `user.Department`, `user.Role`, `user.Email`, `user.Clearance`, etc.
      - `resource` (dynamic) — resource attributes from request properties
        - `resource.OwnerId`, `resource.Classification`, `resource.TenantId`, etc.
      - `environment` (dynamic) — environment attributes
        - `environment.CurrentTime`, `environment.DayOfWeek`, `environment.IsBusinessHours`, `environment.IpAddress`, etc.
      - `action` (dynamic) — action attributes
        - `action.Name`, `action.Type`, etc.
    - **Supported expressions**: any C# expression that returns `bool`
      - Comparison: `==`, `!=`, `<`, `>`, `<=`, `>=`
      - Logical: `&&`, `||`, `!`
      - String methods: `user.Email.EndsWith("@company.com")`, `resource.Name.Contains("confidential")`
      - LINQ: `user.Roles.Contains("Admin")`, `user.Groups.Any(g => g == "Finance")`
      - Ternary: not recommended (expression must return bool)
      - Null-safe: `user.Department?.ToLower() == "finance"`
    - **Unsupported constructs**: statements (`if`, `for`, `while`), assignments, `void` expressions, I/O, `async/await`
    - **Type coercion**: dynamic properties are resolved at runtime — type mismatches produce `ABACErrors.InvalidCondition`

13. **`docs/features/security-abac/11-eel-syntax-reference.md`** — EEL Syntax Reference
    - **Literals**: strings (`"Finance"`), integers (`42`), doubles (`3.14`), booleans (`true`/`false`), `null`
    - **Operators** (with precedence table):
      - Arithmetic: `+`, `-`, `*`, `/`, `%`
      - Comparison: `==`, `!=`, `<`, `>`, `<=`, `>=`
      - Logical: `&&`, `||`, `!`
      - Null-conditional: `?.`, `??`
      - Parentheses: `()`
    - **Property access**: `user.Department`, `resource.Classification`
    - **Method calls**: `user.Email.EndsWith("@company.com")`
    - **String interpolation**: NOT supported (use `string.Concat()` or `+`)
    - **Collection operations**: `user.Roles.Contains("Admin")`, `user.Groups.Any(...)`, `user.Tags.Count`
    - **Complete operator precedence table** (C# standard)

14. **`docs/features/security-abac/12-eel-cookbook.md`** — EEL Cookbook / Recipes
    - **Role-based**: `user.Roles.Contains("Admin")`, `user.Role == "Manager" || user.Role == "Director"`
    - **Department-based**: `user.Department == "Finance" && resource.Classification <= 3`
    - **Time-based**: `environment.IsBusinessHours`, `environment.DayOfWeek != DayOfWeek.Sunday`
    - **Owner-based**: `resource.OwnerId == user.Id`
    - **IP-based**: `environment.IpAddress.StartsWith("10.0.")`
    - **Clearance-based**: `user.Clearance >= resource.RequiredClearance`
    - **Multi-tenant**: `resource.TenantId == user.TenantId`
    - **Complex combinations**: `(user.Department == "HR" || user.Roles.Contains("Admin")) && environment.IsBusinessHours && resource.Classification < 4`
    - **Collection checks**: `user.Groups.Any(g => g == "SecurityTeam")`, `user.Permissions.Count > 0`
    - **Null-safe patterns**: `user.Manager?.Department == "Executive"`
    - Each recipe includes: expression, explanation, equivalent XACML Apply/Function tree

15. **`docs/features/security-abac/13-eel-errors.md`** — EEL Error Reference
    - **Compilation errors** (detected at startup):
      - Syntax errors: missing parenthesis, unknown operator → Roslyn Diagnostic with position
      - Type errors: method not found, wrong argument types
      - Undefined variable: `admin.Role` (only `user`, `resource`, `environment`, `action` are available)
    - **Runtime errors** (detected during evaluation):
      - `NullReferenceException` → `ABACErrors.EvaluationFailed` with details
      - Type mismatch on dynamic property → `RuntimeBinderException`
      - Division by zero, overflow
    - **Error code mapping**: Roslyn `DiagnosticSeverity` → `ABACErrors` error codes
    - **Troubleshooting guide**: common errors and their solutions
    - **Debug mode**: enabling detailed expression evaluation logging (EventId 9030-9033)

16. **`docs/features/security-abac/14-eel-performance.md`** — EEL Performance Guide
    - **Compilation cost**: ~100ms per expression (first time only)
    - **Cached evaluation**: <1ms via `ScriptRunner<bool>` delegate (near-native speed)
    - **Startup impact**: N expressions × ~100ms cold compilation, amortized via concurrent pre-compilation
    - **Memory**: compiled delegates are cached in `ConcurrentDictionary<string, ScriptRunner<bool>>` for the application lifetime
    - **Best practices**:
      - Prefer `[RequirePolicy]` over `[RequireCondition]` for complex logic (policies are pre-built, no compilation)
      - Keep expressions simple — complex logic belongs in named policies
      - Avoid method calls on large collections (LINQ over thousands of items)
    - **Benchmark results reference**: link to `EELCompilerBenchmarks.cs` results

#### 11e. Fluent Policy DSL Guide (`docs/features/security-abac/`)

17. **`docs/features/security-abac/15-policy-dsl.md`** — Fluent Policy Builder DSL
    - **PolicySetBuilder**: `PolicySetBuilder.Create("healthcare").AddPolicy(p => ...).Build()`
    - **PolicyBuilder**: `.AddRule(r => ...)`, `.WithTarget(...)`, `.WithObligation(...)`, `.DefineVariable(...)`
    - **RuleBuilder**: `.Permit()`, `.Deny()`, `.When(...)`, `.WithTarget(...)`, `.AsDefault()`
    - **TargetBuilder**: `.AddAnyOf(...)`, `.MatchSubject(...)`, `.MatchResource(...)`, `.MatchAction(...)`
    - **ConditionBuilder**: `.Apply(...)`, `.Attribute(...)`, `.Value(...)`, `.Equal(...)`, `.And(...)`, `.Or(...)`
    - **ObligationBuilder / AdviceBuilder**: `.WithId(...)`, `.OnPermit()`, `.OnDeny()`, `.AssignAttribute(...)`
    - **ForResourceType<T>()**: type-based target shortcut
    - Complete example: building a healthcare HIPAA policy from scratch using the fluent DSL
    - DSL vs `[RequireCondition]` vs `[RequirePolicy]`: when to use which approach

#### 11f. Getting Started & Tutorials (`docs/features/security-abac/`)

18. **`docs/features/security-abac/16-getting-started.md`** — Quick Start Guide
    - Install: `dotnet add package Encina.Security.ABAC`
    - Minimal setup: `services.AddEncinaABAC()`
    - Your first policy: create, register, evaluate (complete working example in <20 lines)
    - Your first `[RequirePolicy]`: annotating a command handler
    - Your first `[RequireCondition]`: inline expression
    - Enforcement modes: Block, Warn, Disabled

19. **`docs/features/security-abac/17-tutorials.md`** — Step-by-Step Tutorials
    - **Tutorial 1: Role-Based Access** — implementing RBAC using XACML policies
    - **Tutorial 2: Resource Owner Access** — users can only access their own data
    - **Tutorial 3: Time-Based Restrictions** — business hours access control
    - **Tutorial 4: Multi-Tenant Isolation** — tenant-scoped authorization
    - **Tutorial 5: Obligation-Driven Audit** — logging all access decisions with obligations
    - **Tutorial 6: Hierarchical Policies** — department → team → project policy hierarchy using nested PolicySets
    - **Tutorial 7: Custom Attribute Provider** — implementing an LDAP attribute provider
    - **Tutorial 8: Custom Function** — implementing a geo-distance function for location-based access
    - Each tutorial includes: scenario description, complete code, expected behavior, testing guide

#### 11g. Advanced Topics (`docs/features/security-abac/`)

20. **`docs/features/security-abac/18-advanced-topics.md`** — Advanced Configuration & Patterns
    - **Policy conflict detection**: identifying overlapping or contradictory policies
    - **Policy testing patterns**: unit testing policies with mock attribute contexts
    - **Custom combining algorithms**: implementing `ICombiningAlgorithm`
    - **Dynamic policy loading**: adding/removing policies at runtime via PAP
    - **Multi-PDP patterns**: separate PDP instances for different domains
    - **Caching strategies**: decision caching, attribute caching
    - **Thread safety**: concurrent evaluation guarantees
    - **Integration with Encina.Security RBAC**: combining RBAC and ABAC in the same pipeline

21. **`docs/features/security-abac/19-security-considerations.md`** — Security Guide
    - **Expression injection prevention**: `[RequireCondition]` expressions are developer-defined (compile-time), never user-input
    - **Deny-by-default**: why `DenyOnNotApplicable = true` is the safe default
    - **Obligation failure = deny**: XACML §7.18 requirement
    - **Indeterminate = deny**: errors should never open access
    - **Roslyn sandboxing**: expressions run with application trust (not sandboxed — intentional for developer-defined policies)
    - **Policy validation**: detecting invalid or dangerous policies at registration time
    - **Audit trail**: using obligations for comprehensive access logging

#### 11h. Reference Materials (`docs/features/security-abac/`)

22. **`docs/features/security-abac/20-configuration-reference.md`** — ABACOptions Reference
    - All `ABACOptions` properties with types, defaults, descriptions, and examples
    - `EnforcementMode` (Block/Warn/Disabled), `DenyOnNotApplicable`, `FailOnMissingObligationHandler`
    - `BusinessHoursStart/End`, `BusinessDays`, `DefaultCombiningAlgorithm`
    - Fluent methods: `AddAttributeProvider<T>()`, `AddObligationHandler<T>()`, `AddFunction()`, `AddPolicySet()`, `AddPolicy()`, `AddCombiningAlgorithm<T>()`

23. **`docs/features/security-abac/21-error-reference.md`** — Error Code Reference
    - All `abac.*` error codes with descriptions, causes, and resolution guidance:
      - `abac.access_denied`, `abac.indeterminate`, `abac.policy_not_found`, `abac.evaluation_failed`
      - `abac.attribute_resolution_failed`, `abac.invalid_policy`, `abac.invalid_condition`
      - `abac.obligation_failed`, `abac.function_not_found`, `abac.variable_not_found`, etc.
    - Common error scenarios and troubleshooting

24. **`docs/features/security-abac/22-observability-reference.md`** — Metrics, Traces & Logs Reference
    - All OpenTelemetry counters: `abac.evaluations.total`, `abac.policy.evaluations`, `abac.obligation.executions`, etc.
    - All histograms: `abac.evaluation.duration_ms`, `abac.obligation.duration_ms`
    - All EventId log messages (9000-9099) with levels, templates, and when they fire
    - Dashboard setup guide: Grafana/Prometheus dashboard template for ABAC monitoring

25. **`docs/features/security-abac/23-quick-reference.md`** — Quick Reference / Cheat Sheet
    - **One-page summary**: all operators, functions, data types, algorithms, error codes
    - **Expression examples**: 10 most common `[RequireCondition]` patterns
    - **DSL examples**: PolicySet → Policy → Rule pattern in 5 lines
    - **Configuration**: minimal vs full ABACOptions setup
    - **Attribute category shortcuts**: user.* → Subject, resource.* → Resource, environment.* → Environment, action.* → Action
    - Designed to be printable / pinnable

#### 11i. Conformance & Specification Mapping

26. **`docs/features/security-abac/24-xacml-conformance.md`** — XACML 3.0 Conformance
    - **Feature support matrix**: all XACML 3.0 specification sections with support status (✅ Supported, ❌ Not Supported, 🔮 Planned)
    - Mandatory features from XACML 3.0 Core conformance requirements
    - XACML Profiles implementation status:
      - Core (✅), RBAC Profile (🔮), Multiple Decision Profile (🔮), JSON Profile (🔮), REST Profile (🔮)
    - XACML 3.0 element → Encina type mapping table (comprehensive)
    - XACML URN → C#-friendly name mapping table for all functions and data types
    - Known deviations from the specification (if any) with rationale

#### 11j. Package & Project Files

27. **`src/Encina.Security.ABAC/README.md`** — Package README (NuGet)
    - Package overview (2-3 paragraphs)
    - Installation
    - Minimal quick start (5-line setup)
    - Key features list (bullet points)
    - Link to full documentation

28. **`CHANGELOG.md`** — add under Unreleased:
    ```
    ### Added
    - Encina.Security.ABAC — Full XACML 3.0-compliant Attribute-Based Access Control engine with hierarchical PolicySets, 4-effect evaluation (Permit/Deny/NotApplicable/Indeterminate), 8 combining algorithms, Obligations and Advice, XACML function registry with 70+ standard functions, fluent C#-idiomatic policy DSL, [RequirePolicy] and [RequireCondition] attributes with EEL (Encina Expression Language, Roslyn-compiled), EEL Roslyn DiagnosticAnalyzer for compile-time validation, ABACPipelineBehavior with obligation execution, built-in attribute providers, and OpenTelemetry observability (Fixes #401)
    ```

29. **`ROADMAP.md`** — update v0.13.0 milestone

30. **`docs/INVENTORY.md`** — update with new package

31. **`PublicAPI.Unshipped.txt`** — final review and verification

32. **XML doc comments** — verify all public APIs have `<summary>`, `<remarks>` with XACML 3.0 section references, and `<example>` where appropriate

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of Encina.Security.ABAC (Issue #401) — Comprehensive Documentation.

CONTEXT:
- Phases 1-10 are fully implemented and tested (full XACML 3.0 engine, all tests passing)
- This phase creates EXTENSIVE documentation because:
  1. Full XACML 3.0 is a complex standard unfamiliar to most developers — needs conceptual guides, architecture diagrams, evaluation flow explanations
  2. The EEL (Encina Expression Language) needs its own complete language reference (variables, operators, cookbook, errors, performance)
  3. 70+ XACML functions need individual reference entries
  4. 8 combining algorithms need truth tables and decision guides
  5. Obligations/Advice need real-world examples

TASK:
Create all documentation listed in Phase 11 Tasks. This is the largest documentation effort in the Encina project — 26+ documents organized into:
- 3 ADRs (XACML 3.0 choice, Roslyn choice, EEL naming)
- 7 XACML concept guides (architecture, policy language, effects, algorithms, attributes, obligations, functions)
- 2 reference documents (data types, function library)
- 5 EEL (Encina Expression Language) documents (guide, syntax reference, cookbook, errors, performance)
- 1 DSL guide
- 2 getting started documents (quick start, tutorials with 8 scenarios)
- 3 advanced documents (advanced topics, security, conformance)
- 4 reference materials (configuration, errors, observability, cheat sheet)
- 4 project files (README, CHANGELOG, ROADMAP, INVENTORY)

KEY RULES:
XACML Documentation:
- Each concept guide must be self-contained but link to related guides
- Architecture diagrams use Mermaid (flowcharts, sequence diagrams)
- Combining algorithm truth tables show ALL 4 effects (not just Permit/Deny)
- Function reference: every function with signature, parameters, return type, description, C# example
- Reference XACML 3.0 OASIS Standard sections (§5-7, §C, Appendix A-B)

Roslyn Expression Documentation:
- Complete operator precedence table
- EELGlobals context variables with types and examples
- Cookbook with 15+ real-world expression patterns
- Error reference mapping Roslyn Diagnostics to ABACErrors
- Performance guide with compilation vs evaluation costs

General:
- All documentation in English
- Code examples use C# 14 syntax
- Mermaid diagrams for all architecture and flow visualizations
- Cross-references between documents (relative links)
- Table of contents in each document >50 lines

REFERENCE FILES:
- XACML 3.0 OASIS Standard (http://docs.oasis-open.org/xacml/3.0/xacml-3.0-core-spec-os-en.html)
- AuthzForce documentation (https://authzforce-ce-fiware.readthedocs.io/)
- WSO2 XACML docs (https://is.docs.wso2.com/)
- Spring Expression Language docs (https://docs.spring.io/spring-framework/reference/core/expressions.html)
- OPA Rego documentation (https://www.openpolicyagent.org/docs/latest/policy-reference/)
- docs/architecture/adr/ (ADR format)
- docs/features/ (existing feature documentation pattern)
```

</details>

---

## Research

### XACML 3.0 Standard References

| Standard/Specification | Relevance | Key Concepts Adopted |
|------------------------|-----------|---------------------|
| **XACML 3.0 (OASIS Standard, Jan 2013)** | **Core specification — fully implemented** | PDP, PAP, PIP, PEP, PolicySet, Policy, Rule, Target, Condition, Obligation, Advice, 4 effects, 8 combining algorithms, function library |
| XACML 3.0 Core (§5 Syntax) | Policy language types | PolicySet, Policy, Rule, Target, Condition, Apply, AttributeDesignator |
| XACML 3.0 Core (§7 Evaluation) | Evaluation semantics | §7.6 Target, §7.7 Condition, §7.12-7.14 PolicySet/Policy/Rule evaluation |
| XACML 3.0 Core (§C Algorithms) | Combining algorithms | 8 standard algorithms with Indeterminate handling |
| XACML 3.0 Core (Appendix A) | Standard functions | 70+ functions across 10 categories |
| XACML 3.0 Core (Appendix B) | Data types | 12 standard data types |
| XACML 3.0 Core (§7.18) | Obligations/Advice | Mandatory obligation execution by PEP |
| NIST SP 800-162 | ABAC definition | Attribute categories: Subject, Resource, Environment, Action |
| OWASP Authorization Patterns | Security best practices | Deny-by-default, least privilege |
| Keycloak ABAC | Industry implementation | Policy-based authorization model |
| Open Policy Agent (OPA) | Policy engine reference | Declarative policy evaluation |
| Casbin | Authorization library | Model-based access control |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in ABAC |
|-----------|----------|---------------|
| `ISecurityContext` | `Encina.Security` | User attributes (claims, roles, permissions) → subject attributes |
| `ISecurityContextAccessor` | `Encina.Security` | Request-scoped security context access |
| `SecurityAttribute` | `Encina.Security` | Base class for `[RequirePolicy]`, `[RequireCondition]` |
| `SecurityPipelineBehavior` | `Encina.Security` | Reference pattern for attribute discovery and evaluation |
| `IPipelineBehavior<,>` | `Encina` core | Pipeline behavior registration |
| `EncinaErrors.Create()` | `Encina` core | Error factory pattern |
| `TimeProvider` | .NET 10 BCL | Testable time-dependent logic (business hours, env attributes) |
| `IOptions<T>` pattern | `Microsoft.Extensions.Options` | Configuration injection |
| `TryAdd*` DI pattern | All Encina packages | Overridable service registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security` (core) | 8000-8004 | Security authorization |
| `Encina.Security.AntiTampering` | 9100-9199 | Request integrity |
| `Encina.Security.PII` | 8000-8099 | PII masking (overlaps with core — known issue) |
| **`Encina.Security.ABAC`** | **9000-9099** | **New — XACML evaluation, policy management, obligations, functions** |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core models & enums (Phase 1) | ~15 | XACML records, enums, 4-effect model |
| Interfaces, attributes & errors (Phase 2) | ~12 | XACML component interfaces, function interfaces, constants |
| Function registry & standard functions (Phase 3) | ~12 | Registry + 10 function category files + helper |
| DSL builders, evaluator & Roslyn compiler (Phase 4) | ~11 | PolicySet/Policy/Rule/Target/Obligation builders, evaluator, EELCompiler, EELGlobals |
| Evaluation engine & algorithms (Phase 5) | ~14 | PDP, PAP, PIP, target evaluator, 8 algorithms, providers |
| Pipeline behavior & obligations (Phase 6) | ~4 | Behavior, obligation executor, context builder, attribute info |
| Configuration & DI (Phase 7) | ~5 | Options, validator, extensions, seeder, health check |
| EEL Validation Tooling (Phase 8) | ~10-13 | Roslyn analyzer + CodeFixProvider (4-5 files), CLI tool (2-3 files), startup service, test helpers, IDE annotations |
| Observability (Phase 9) | ~2 | Diagnostics, log messages |
| Tests (Phase 10) | ~55-65 | Unit (~320 incl. ~100 EEL conformance), Guard (~70), Contract (~30), Property (~45), Load, Benchmark, EEL test data files |
| Documentation (Phase 11) | ~30 | 2 ADRs, 7 XACML concept guides, 2 reference docs, 5 EEL docs, DSL guide, 2 getting started docs, 3 advanced docs, 4 reference materials, 4 project files |
| **Total** | **~168-190** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Encina.Security.ABAC for Issue #401 — Full XACML 3.0 Attribute-Based Access Control Engine.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS + Event Sourcing
- Pre-1.0: no backward compatibility needed, best solution always
- Railway Oriented Programming: Either<EncinaError, T> for fallible operations
- Provider-independent: this package has NO database provider implementations
- Integrates with existing Encina.Security for ISecurityContext and SecurityAttribute

XACML 3.0 COMPLIANCE:
This implementation follows the FULL XACML 3.0 OASIS Standard with C#-idiomatic types:
- PolicySet hierarchy (nested PolicySets containing Policies)
- 4 Effects: Permit, Deny, NotApplicable, Indeterminate
- 8 Combining Algorithms: DenyOverrides, PermitOverrides, FirstApplicable, OnlyOneApplicable, DenyUnlessPermit, PermitUnlessDeny, OrderedDenyOverrides, OrderedPermitOverrides
- Obligations (mandatory) and Advice (optional) post-decision actions
- Structured Target with AnyOf/AllOf/Match triple nesting
- Apply/Function condition model with extensible function registry (70+ standard functions)
- AttributeDesignator with Category, AttributeId, DataType, MustBePresent
- AttributeBag for multi-valued attributes (XACML bag/set functions)
- VariableDefinition/VariableReference for reusable sub-expressions

IMPLEMENTATION OVERVIEW:
New package: src/Encina.Security.ABAC/
References: Encina.Security (for ISecurityContext, SecurityAttribute)

Phase 1: Full XACML 3.0 type system (PolicySet, Policy, Rule, Target, Condition/Apply, Obligation, Advice, AttributeDesignator, 4-effect Effect enum, 8-algorithm CombiningAlgorithmId enum)
Phase 2: XACML component interfaces (PDP, PAP, PIP + IAttributeProvider + ICombiningAlgorithm + IObligationHandler + IFunctionRegistry + IXACMLFunction) + attributes + errors + constants
Phase 3: Function registry with ALL XACML 3.0 standard functions (10 categories, 70+ functions)
Phase 4: Fluent DSL (PolicySet/Policy/Rule/Target/Obligation/Advice/Condition builders) + ConditionEvaluator (function-based) + EELCompiler
Phase 5: Full XACML evaluation engine (TargetEvaluator, XACMLPolicyDecisionPoint with recursive PolicySet evaluation, InMemoryPAP, 8 combining algorithms, attribute providers)
Phase 6: ABACPipelineBehavior (4-effect handling + obligation execution + enforcement modes)
Phase 7: ABACOptions, DI registration (8 algorithms + function registry + obligation handlers), policy seeder, health check
Phase 8: EEL Validation Tooling (Roslyn DiagnosticAnalyzer + CodeFixProvider with EEL001-EEL006 rules, CLI validator `dotnet encina-abac validate`, startup fail-fast service, test helpers, IDE integration: [StringSyntax("EEL")] + [LanguageInjection] for Rider)
Phase 9: Observability (ActivitySource, Meter with 4-effect + obligation metrics, [LoggerMessage] event IDs 9000-9099)
Phase 10: Testing (Unit ~320 incl. ~100 EEL conformance tests, Guard ~70, Contract ~30, Property ~45, Load, Benchmark)
Phase 11: Comprehensive Documentation (~26+ documents: 2 ADRs, 7 XACML concept guides, 2 reference docs, 5 EEL docs, DSL guide, 2 getting started docs, 3 advanced docs, 4 reference materials, conformance mapping, 4 project files)

KEY PATTERNS:
- PDP returns PolicyDecision with 4 effects + obligations + advice (FULL XACML response)
- Obligations: PEP MUST execute; if mandatory obligation fails → DENY (XACML §7.18)
- Combining algorithms handle ALL 4 effects correctly (DenyUnless*/PermitUnless* simplify to 2)
- ConditionEvaluator evaluates Apply trees via IFunctionRegistry (not direct operators)
- EEL (Encina Expression Language): EELCompiler compiles [RequireCondition] C# expressions via CSharpScript.Create<bool>() + CreateDelegate() — cached at startup, <1ms per evaluation
- EEL Validation: DiagnosticAnalyzer + CodeFixProvider (EEL001-EEL006) for compile-time IDE squiggles and quick fixes, CLI validator for CI/CD, startup fail-fast via IHostedLifecycleService, IDE integration ([StringSyntax("EEL")], [LanguageInjection] for Rider)
- PolicySet evaluation is RECURSIVE (nested PolicySets)
- AttributeBag supports multi-valued attributes for bag/set functions
- DI: TryAdd* for all registrations, TryAddEnumerable for providers/algorithms/handlers

REFERENCE FILES:
- XACML 3.0 OASIS Standard (§5-7, §C, Appendices A-B)
- src/Encina.Security/ (pipeline behavior, options, DI, diagnostics)
- src/Encina.Compliance.GDPR/LawfulBasisValidationPipelineBehavior.cs (static caching)
- src/Encina.Compliance.DataResidency/DataResidencyFluentPolicyDescriptor.cs (DSL)
```

</details>

---

## Issue Strategy: Single Issue #401

**Decision**: Maintain a single issue #401 for the entire ABAC implementation.

**Rationale**:

- **Domain coherence**: XACML 3.0 is a tightly-coupled specification where every component depends on shared concepts (4 effects, AttributeDesignator, AttributeBag, Apply/Function tree). Splitting into sub-issues would force repeating extensive XACML context in each one.
- **Phase dependencies**: Each phase builds directly on the previous — Phase 5 (PDP) cannot work without Phase 3 (functions) or Phase 4 (evaluator). There are no truly independent sub-tasks.
- **Context continuity**: The 11 phases already serve as natural sub-tasks within the issue. Each phase is a self-contained commit with a clear boundary.
- **Single package**: Unlike features that span 13 database providers (where splitting by provider makes sense), ABAC is a single package (`Encina.Security.ABAC`) with no provider variants.

**Alternative considered**: Splitting into ~3 issues (Core Models + Evaluation Engine, Pipeline + DI, Testing + Documentation). Rejected because the XACML context needed in each sub-issue would be nearly identical and maintaining cross-issue dependencies adds overhead without benefit.

---

## Next Steps

1. **Review and approve this plan**
2. Publish as comment on Issue #401
3. Begin Phase 1 implementation in a new session
4. Each phase should be a self-contained commit
5. Final commit references `Fixes #401`
