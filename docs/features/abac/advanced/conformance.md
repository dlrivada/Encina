# XACML 3.0 Conformance Guide

Encina.Security.ABAC implements the OASIS XACML 3.0 standard (eXtensible Access Control Markup Language) as a native C# library integrated into the Encina CQRS pipeline. This guide documents the conformance level, what is implemented, what differs from the XML-based specification, and what is planned for future versions.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Supported XACML Features](#2-supported-xacml-features)
3. [XACML Sections Coverage](#3-xacml-sections-coverage)
4. [Not Yet Implemented](#4-not-yet-implemented)
5. [Differences from XACML XML](#5-differences-from-xacml-xml)
6. [Testing Conformance](#6-testing-conformance)
7. [References](#7-references)

---

## 1. Overview

The XACML 3.0 specification (OASIS Standard, January 2013) defines a language for access control policies, a processing model for evaluating authorization requests, and an architecture consisting of Policy Enforcement Point (PEP), Policy Decision Point (PDP), Policy Administration Point (PAP), and Policy Information Point (PIP).

Encina implements the **semantic model** of XACML 3.0 -- the policy language, evaluation algorithms, combining algorithms, functions, and data-flow model -- without requiring XML serialization. Policies are defined using C# records and a fluent builder API, making them type-safe, refactorable, and testable with standard .NET tooling.

### Specification Basis

| Standard | Version | Date | OASIS ID |
|----------|---------|------|----------|
| XACML 3.0 Core | 3.0 | January 2013 | xacml-3.0-core-spec-os-en |
| XACML 3.0 Functions | Appendix A.3 | January 2013 | (same document) |
| XACML 3.0 Combining Algorithms | Appendix C | January 2013 | (same document) |

### Conformance Goal

Encina targets **semantic conformance** with the XACML 3.0 Core specification. This means the evaluation behavior, combining algorithm semantics, function results, and error handling match the specification. The wire format (XML) and transport protocol (SAML/REST) are not implemented in favor of native C# integration.

---

## 2. Supported XACML Features

### Policy Language

- **PolicySet**: Hierarchical containers that group policies and nested policy sets with a combining algorithm.
- **Policy**: Named authorization policies containing rules, a target, obligations, and advice.
- **Rule**: Individual authorization rules with an effect (Permit/Deny), optional target, and optional condition.
- **Target**: AnyOf/AllOf/Match structure for determining policy/rule applicability.
- **Condition**: Boolean expression trees using `Apply` nodes with function references.
- **VariableDefinition / VariableReference**: Named sub-expressions within a policy scope.

### Effects and Decision Model

- **Four-effect model**: Permit, Deny, NotApplicable, Indeterminate per XACML 3.0 section 7.1.
- **Indeterminate propagation**: Evaluation errors produce Indeterminate, which combining algorithms handle according to their defined semantics.

### Attributes

- **Four attribute categories**: Subject, Resource, Action, Environment per XACML 3.0 section 5.
- **AttributeDesignator**: Formal attribute references with category, ID, data type, and `MustBePresent`.
- **AttributeValue**: Typed literal values for use in Match elements and function arguments.
- **AttributeBag**: Multi-valued attribute containers per XACML 3.0 bag semantics.

### Obligations and Advice

- **Obligation expressions**: Mandatory post-decision actions with `FulfillOn` (Permit or Deny).
- **Advice expressions**: Optional post-decision recommendations with `AppliesTo`.
- **Attribute assignments**: Key-value pairs passed to obligation/advice handlers.
- **Section 7.18 enforcement**: Obligation failure overrides Permit to Deny.

### Combining Algorithms

All eight standard XACML 3.0 combining algorithms (Appendix C) are implemented:

| Algorithm | Encina Type | XACML ID |
|-----------|-------------|----------|
| Deny Overrides | `DenyOverridesAlgorithm` | `deny-overrides` |
| Permit Overrides | `PermitOverridesAlgorithm` | `permit-overrides` |
| First Applicable | `FirstApplicableAlgorithm` | `first-applicable` |
| Only One Applicable | `OnlyOneApplicableAlgorithm` | `only-one-applicable` |
| Deny Unless Permit | `DenyUnlessPermitAlgorithm` | `deny-unless-permit` |
| Permit Unless Deny | `PermitUnlessDenyAlgorithm` | `permit-unless-deny` |
| Ordered Deny Overrides | `OrderedDenyOverridesAlgorithm` | `ordered-deny-overrides` |
| Ordered Permit Overrides | `OrderedPermitOverridesAlgorithm` | `ordered-permit-overrides` |

### Standard Functions

Over 70 standard functions from XACML 3.0 Appendix A.3 are implemented across nine categories:

| Category | Count | Examples |
|----------|-------|---------|
| Equality | 10+ | `string-equal`, `integer-equal`, `boolean-equal`, `date-equal` |
| Comparison | 8+ | `integer-greater-than`, `string-greater-than`, `date-less-than` |
| Arithmetic | 6+ | `integer-add`, `integer-subtract`, `double-multiply` |
| String | 8+ | `string-concatenate`, `string-starts-with`, `string-contains` |
| Logical | 4 | `and`, `or`, `not`, `n-of` |
| Bag | 10+ | `*-one-and-only`, `*-bag-size`, `*-is-in`, `*-bag` |
| Set | 8+ | `*-at-least-one-member-of`, `*-subset`, `*-intersection`, `*-union` |
| Type Conversion | 6+ | `string-from-integer`, `integer-from-string`, `boolean-from-string` |
| Regex | 2 | `string-regexp-match`, `anyURI-regexp-match` |
| Higher-Order | 3 | `any-of`, `all-of`, `any-of-any` |

### Data-Flow Model

| Component | XACML Role | Encina Implementation |
|-----------|-----------|----------------------|
| PEP | Policy Enforcement Point | `ABACPipelineBehavior<TRequest, TResponse>` |
| PDP | Policy Decision Point | `XACMLPolicyDecisionPoint` |
| PAP | Policy Administration Point | `InMemoryPolicyAdministrationPoint` (default) |
| PIP | Policy Information Point | `IAttributeProvider` + `IPolicyInformationPoint` |
| Context Handler | Attribute transformation | `AttributeContextBuilder` |

---

## 3. XACML Sections Coverage

The following table maps XACML 3.0 specification sections to Encina's implementation status.

| XACML Section | Feature | Encina Status | Notes |
|---------------|---------|---------------|-------|
| section 4 | Data-flow model (PEP/PDP/PAP/PIP) | Full | All four components implemented |
| section 5.1 | PolicySet | Full | Hierarchical nesting, combining algorithms, obligations, advice |
| section 5.2 | Policy | Full | Rules, target, combining algorithm, obligations, advice |
| section 5.3 | Rule | Full | Effect (Permit/Deny), target, condition |
| section 5.4 | Target (AnyOf/AllOf/Match) | Full | Complete three-level matching structure |
| section 5.5 | Condition (Apply expression trees) | Full | Recursive Apply nodes, function references, variable refs |
| section 5.6 | ObligationExpression | Full | FulfillOn filtering, attribute assignments |
| section 5.7 | AdviceExpression | Full | AppliesTo filtering, best-effort execution |
| section 5.25 | VariableDefinition | Full | Policy-scoped named expressions |
| section 5.26 | VariableReference | Full | References to variable definitions in conditions |
| section 5.29 | AttributeDesignator | Full | Category, ID, DataType, MustBePresent |
| section 5.30 | AttributeSelector (XPath) | Not implemented | XPath selection not supported; use AttributeDesignator |
| section 5.31 | AttributeValue | Full | Typed literal values |
| section 7.1 | Four-effect model | Full | Permit, Deny, NotApplicable, Indeterminate |
| section 7.3-7.6 | Evaluation procedures | Full | Target matching, condition evaluation, rule/policy evaluation |
| section 7.7-7.8 | Target evaluation | Full | AnyOf/AllOf/Match semantics with Indeterminate propagation |
| section 7.9 | Condition evaluation | Full | Apply tree evaluation with error handling |
| section 7.11 | Rule evaluation | Full | Target + Condition + Effect combination |
| section 7.12 | Policy evaluation | Full | Rule combining with algorithm |
| section 7.13 | PolicySet evaluation | Full | Recursive policy/policy-set combining |
| section 7.14 | Whole PDP evaluation | Full | Root-level DenyOverrides combining |
| section 7.18 | Obligation enforcement | Full | Failure causes Deny override |
| section 7.19 | Attribute retrieval | Full | PIP integration for on-demand resolution |
| section 10 | Hierarchical resources | Partial | Supported via custom attribute providers, no built-in hierarchy |
| Appendix A.3 | Standard functions | Full | 70+ functions across 10 categories |
| Appendix B | Data types | Full | String, boolean, integer, double, date, dateTime, time, anyURI |
| Appendix C | Combining algorithms | Full | All 8 standard algorithms |
| section 6 | Administration model | Basic | In-memory PAP; no XML policy import/export |

---

## 4. Not Yet Implemented

The following XACML 3.0 features are not yet available in Encina and are planned for future versions.

### XACML XML Serialization / Deserialization

Encina policies are defined in C# using records and the fluent builder API. There is no support for reading or writing XACML XML policy documents. This is intentional for the C#-native developer experience but limits interoperability with external XACML engines.

**Planned**: A separate `Encina.Security.ABAC.Xml` package for import/export of XACML 3.0 XML policies.

### Delegation Profiles (XACML 3.0 Administrative Policy)

The XACML administrative and delegation profiles allow policies that grant other entities the ability to create policies. This advanced governance feature is not implemented.

**Planned**: Post-1.0 based on demand.

### Multiple Decision Profile (Batch Evaluation)

The XACML Multiple Decision Profile allows a single request to contain multiple resource/action combinations, returning a decision for each. Currently, each MediatR request produces a single PDP evaluation.

**Planned**: Batch evaluation API for bulk authorization checks.

### XACML REST Profile

The XACML REST Profile (OASIS Committee Specification) defines a RESTful API for PDP access. Encina integrates the PDP directly into the application pipeline rather than exposing it as a standalone service.

**Planned**: Optional `Encina.Security.ABAC.Api` package for exposing PDP as a REST endpoint.

### External PAP Backends

The built-in `InMemoryPolicyAdministrationPoint` is suitable for development and testing. Production deployments should use a persistent backend.

**Planned backends**:

- Database-backed PAP (EF Core / Dapper implementations)
- Keycloak integration (read policies from Keycloak authorization services)
- OPA integration (import policies from Open Policy Agent)

### AttributeSelector (XPath)

XACML 3.0 section 5.30 defines `AttributeSelector` for XPath-based attribute selection from XML content in the request. Since Encina uses C# objects rather than XML documents, XPath selection is not applicable.

**Status**: No plans to implement. Use `AttributeDesignator` with custom `IAttributeProvider` implementations instead.

---

## 5. Differences from XACML XML

While Encina follows XACML 3.0 semantics, the representation differs from the XML-based standard.

### Policy Representation

| Aspect | XACML XML | Encina C# |
|--------|-----------|-----------|
| Policy format | XML elements and attributes | C# records (`Policy`, `PolicySet`, `Rule`) |
| Policy construction | XML documents | Fluent builders (`PolicyBuilder`, `RuleBuilder`) |
| Type safety | Schema validation (XSD) | Compile-time type checking |
| Refactoring | Search and replace in XML | IDE refactoring support |
| Version control | XML diff | Standard C# diff |

### Function Identifiers

XACML uses URN-prefixed function identifiers. Encina uses simplified string identifiers without the URN prefix:

| XACML XML Function ID | Encina Function ID |
|-----------------------|-------------------|
| `urn:oasis:names:tc:xacml:1.0:function:string-equal` | `string-equal` |
| `urn:oasis:names:tc:xacml:1.0:function:integer-greater-than` | `integer-greater-than` |
| `urn:oasis:names:tc:xacml:1.0:function:and` | `and` |
| `urn:oasis:names:tc:xacml:3.0:function:string-starts-with` | `string-starts-with` |

The URN prefix is omitted because Encina operates within a single runtime where function lookup is by string key in the `IFunctionRegistry`. Custom functions use a `custom:` prefix (e.g., `custom:geo-within`).

### Data Type Identifiers

XACML uses full XML Schema URIs for data types. Encina supports both the full URI and short-form identifiers:

| XACML Data Type URI | Encina Short Form |
|--------------------|-------------------|
| `http://www.w3.org/2001/XMLSchema#string` | `string` |
| `http://www.w3.org/2001/XMLSchema#boolean` | `boolean` |
| `http://www.w3.org/2001/XMLSchema#integer` | `integer` |
| `http://www.w3.org/2001/XMLSchema#double` | `double` |
| `http://www.w3.org/2001/XMLSchema#dateTime` | `dateTime` |
| `http://www.w3.org/2001/XMLSchema#anyURI` | `anyURI` |

### Combining Algorithm Identifiers

| XACML Algorithm URN | Encina Enum Value |
|---------------------|-------------------|
| `urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides` | `CombiningAlgorithmId.DenyOverrides` |
| `urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:permit-overrides` | `CombiningAlgorithmId.PermitOverrides` |
| `urn:oasis:names:tc:xacml:1.0:rule-combining-algorithm:first-applicable` | `CombiningAlgorithmId.FirstApplicable` |
| `urn:oasis:names:tc:xacml:1.0:rule-combining-algorithm:only-one-applicable` | `CombiningAlgorithmId.OnlyOneApplicable` |

### Attribute Categories

| XACML Category URI | Encina Enum |
|-------------------|-------------|
| `urn:oasis:names:tc:xacml:1.0:subject-category:access-subject` | `AttributeCategory.Subject` |
| `urn:oasis:names:tc:xacml:3.0:attribute-category:resource` | `AttributeCategory.Resource` |
| `urn:oasis:names:tc:xacml:3.0:attribute-category:action` | `AttributeCategory.Action` |
| `urn:oasis:names:tc:xacml:3.0:attribute-category:environment` | `AttributeCategory.Environment` |

### EEL as a Condition Alternative

In addition to the standard XACML `Apply` expression trees, Encina offers the Encina Expression Language (EEL) for inline conditions via `[RequireCondition]` attributes. EEL expressions are C# boolean expressions compiled by Roslyn, providing a developer-friendly alternative to constructing `Apply` trees:

```csharp
// XACML-style Apply tree
var condition = new Apply
{
    FunctionId = "integer-greater-than-or-equal",
    Arguments = [
        new AttributeDesignator { Category = AttributeCategory.Subject, AttributeId = "clearanceLevel", DataType = "integer" },
        new AttributeValue { DataType = "integer", Value = 3 }
    ]
};

// Equivalent EEL expression
[RequireCondition("user.clearanceLevel >= 3")]
public sealed record GetClassifiedDocumentQuery(Guid Id) : IQuery<DocumentDto>;
```

Both approaches produce equivalent authorization behavior. The `Apply` tree is evaluated by the PDP's `ConditionEvaluator`; the EEL expression is compiled and evaluated by `EELCompiler`.

---

## 6. Testing Conformance

Encina verifies XACML 3.0 conformance through a comprehensive test suite organized by specification area.

### Test Coverage Summary

| Test Category | Test Count | XACML Sections Covered |
|---------------|-----------|----------------------|
| Combining Algorithm Tests | ~160 | Appendix C (all 8 algorithms) |
| Function Tests | ~200 | Appendix A.3 (70+ functions, 10 categories) |
| Target Evaluation Tests | ~60 | Sections 7.7-7.8 (AnyOf/AllOf/Match) |
| Condition Evaluation Tests | ~45 | Section 7.9 (Apply trees, variables) |
| Rule Evaluation Tests | ~30 | Section 7.11 (effect + target + condition) |
| Policy/PolicySet Evaluation Tests | ~50 | Sections 7.12-7.14 (recursive evaluation) |
| Obligation Enforcement Tests | ~35 | Section 7.18 (failure override) |
| PEP Integration Tests | ~40 | Section 4 (data-flow, enforcement modes) |
| EEL Compiler Tests | ~50 | (Encina-specific) |
| Builder API Tests | ~60 | (Encina-specific) |
| Attribute Resolution Tests | ~25 | Section 7.19 (PIP, MustBePresent) |
| Health Check & Diagnostics | ~9 | (Encina-specific) |
| **Total** | **~764** | |

### Conformance Test Patterns

Each combining algorithm is tested against the truth table defined in XACML 3.0 Appendix C. For example, the `DenyOverrides` algorithm is verified for all possible combinations of rule effects:

```csharp
// Test: DenyOverrides with mixed Permit and Deny rules returns Deny
[Fact]
public void CombineRuleResults_WithMixedPermitAndDeny_ReturnsDeny()
{
    var algorithm = new DenyOverridesAlgorithm();
    var results = new List<RuleEvaluationResult>
    {
        new() { Effect = Effect.Permit, RuleId = "rule-1" },
        new() { Effect = Effect.Deny, RuleId = "rule-2" },
        new() { Effect = Effect.Permit, RuleId = "rule-3" }
    };

    var combined = algorithm.CombineRuleResults(results);

    Assert.Equal(Effect.Deny, combined);
}
```

Standard functions are tested against expected inputs and outputs per XACML 3.0 Appendix A.3:

```csharp
// Test: string-starts-with returns true when prefix matches
[Fact]
public void StringStartsWith_WithMatchingPrefix_ReturnsTrue()
{
    var function = _registry.GetFunction("string-starts-with");
    var result = function.Evaluate(["cla", "classified-document"]);

    Assert.Equal(true, result);
}
```

Obligation enforcement is tested to verify that handler failure produces Deny even when the PDP returned Permit:

```csharp
// Test: Permit with failing obligation handler results in Deny
[Fact]
public async Task ExecuteObligationsAsync_HandlerFails_ReturnsDeny()
{
    var failingHandler = new FailingObligationHandler("audit-log");
    var executor = new ObligationExecutor([failingHandler], _logger);
    var obligations = new List<Obligation>
    {
        new() { Id = "audit-log", FulfillOn = FulfillOn.Permit }
    };

    var result = await executor.ExecuteObligationsAsync(obligations, _context, CancellationToken.None);

    Assert.True(result.IsLeft); // Error = Deny
}
```

### Running Conformance Tests

```bash
# Run all ABAC unit tests
dotnet test Encina.slnx --filter "FullyQualifiedName~Security.ABAC" --configuration Release

# Run only combining algorithm tests
dotnet test Encina.slnx --filter "FullyQualifiedName~CombiningAlgorithm" --configuration Release

# Run only function tests
dotnet test Encina.slnx --filter "FullyQualifiedName~Functions" --configuration Release
```

---

## 7. References

### OASIS XACML 3.0 Specification

- **Core Specification**: [XACML 3.0 Core (OASIS Standard)](https://docs.oasis-open.org/xacml/3.0/xacml-3.0-core-spec-os-en.html)
  - Section 4: Data-flow model
  - Section 5: Policy language (PolicySet, Policy, Rule, Target, Condition)
  - Section 7: Evaluation procedures and obligation enforcement
  - Section 10: Hierarchical resources
  - Appendix A.3: Standard functions
  - Appendix B: Standard data types
  - Appendix C: Combining algorithms

### XACML Profiles (Not Yet Implemented)

- **REST Profile**: [XACML REST Profile v1.1](https://docs.oasis-open.org/xacml/xacml-rest/v1.1/xacml-rest-v1.1.html)
- **Multiple Decision Profile**: [XACML 3.0 Multiple Decision Profile](https://docs.oasis-open.org/xacml/3.0/xacml-3.0-multiple-v1-spec-cd-03-en.html)
- **Administrative and Delegation Profile**: [XACML 3.0 Administration](https://docs.oasis-open.org/xacml/3.0/xacml-3.0-administration-v1-spec-en.html)

### Encina ABAC Documentation

- [Architecture](../xacml/architecture.md) -- Component architecture and request flow
- [Policy Language](../xacml/policy-language.md) -- PolicySet, Policy, Rule, Target, Condition
- [Effects](../xacml/effects.md) -- Permit, Deny, NotApplicable, Indeterminate
- [Attributes](../xacml/attributes.md) -- Attribute categories, designators, bags
- [Functions](../xacml/functions.md) -- Standard and custom function reference
- [Security Guide](security.md) -- Security considerations and hardening

---

## See Also

- [Security Guide](security.md) -- Defense in depth, common pitfalls, audit trail
- [Architecture](../xacml/architecture.md) -- Full architecture overview with sequence diagrams
