---
title: "ADR-015: XACML 3.0 as ABAC Foundation"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-015: XACML 3.0 as ABAC Foundation

**Status:** Accepted
**Date:** 2026-02-20
**Deciders:** David Lozano Rivada
**Technical Story:** [#401 - Attribute-Based Access Control (ABAC)](https://github.com/dlrivada/Encina/issues/401)

## Context

Encina's security module (`Encina.Security`) provides Role-Based Access Control (RBAC) through `[Authorize]` and policy-based authorization. While RBAC is sufficient for many applications, it cannot express fine-grained, context-aware authorization rules such as:

- "Finance department users can approve transactions up to $50,000 during business hours"
- "Doctors can access patient records only in their assigned ward"
- "EU residents' data can only be processed by services in the EU region"

These rules require Attribute-Based Access Control (ABAC) — a model where access decisions depend on attributes of the subject, resource, action, and environment.

### Industry Standards

Several ABAC standards and implementations exist:

| Standard/Tool | Approach | Limitations |
|---------------|----------|-------------|
| **XACML 3.0** (OASIS) | Full formal specification with PolicySet/Policy/Rule hierarchy, 4 effects, combining algorithms | XML-heavy, complex for simple cases |
| **OPA/Rego** (CNCF) | General-purpose policy language with JSON-oriented evaluation | Non-standard, no formal obligation/advice model |
| **Cedar** (AWS) | Simplified ABAC with Permit/Forbid effects | Limited to 2 effects, AWS-specific origin |
| **Casbin** | ACL/RBAC/ABAC model with configuration files | No obligation/advice, simpler evaluation model |
| **Spring Security ACL** | Java-specific ACL with SpEL expressions | Tightly coupled to Spring, no formal standard |

### Requirements

1. **Formal specification**: A well-defined standard with clear semantics for all edge cases
2. **Four-effect model**: Permit, Deny, NotApplicable, and Indeterminate — enabling nuanced decisions
3. **Obligation/Advice support**: Post-decision actions (mandatory obligations, optional advice)
4. **Combining algorithms**: Multiple strategies for resolving conflicting policy decisions
5. **Extensible functions**: Pluggable function registry for custom business logic
6. **C#-idiomatic API**: No XML configuration — fluent builders and attributes
7. **Pipeline integration**: Seamless integration with Encina's CQRS pipeline

## Decision

Adopt **XACML 3.0** (eXtensible Access Control Markup Language) as the formal foundation for Encina's ABAC engine, implementing the standard's evaluation model, combining algorithms, and function library in a C#-idiomatic way without exposing XML or URN-based identifiers.

### Key Design Decisions

#### 1. XACML Evaluation Model Without XML

We implement the XACML 3.0 evaluation algorithm (§7) using C# records and expression trees instead of XML policies. The standard's semantics are preserved:

- **PolicySet → Policy → Rule** hierarchy
- **Target** matching with AnyOf/AllOf/Match structure
- **Four effects**: Permit, Deny, NotApplicable, Indeterminate
- **Combining algorithms**: All 8 standard algorithms (§C)
- **Obligations and Advice**: Per XACML §7.18

#### 2. XACML Architecture Mapping

| XACML Component | Encina Implementation |
|-----------------|----------------------|
| PEP (Policy Enforcement Point) | `ABACPipelineBehavior<TRequest, TResponse>` |
| PDP (Policy Decision Point) | `XACMLPolicyDecisionPoint` (implements `IPolicyDecisionPoint`) |
| PAP (Policy Administration Point) | `InMemoryPolicyAdministrationPoint` (implements `IPolicyAdministrationPoint`) |
| PIP (Policy Information Point) | `IAttributeProvider` + `DefaultPolicyInformationPoint` |
| Context Handler | `AttributeContextBuilder` |

#### 3. Fluent Builder DSL

Instead of XACML XML, policies are defined using C# fluent builders:

```csharp
var policy = new PolicyBuilder("finance-approval")
    .WithTarget(t => t.AddAnyOf(any => any
        .AddAllOf(all => all
            .AddMatch(XACMLFunctionIds.StringEqual,
                new AttributeDesignator
                {
                    Category = AttributeCategory.Subject,
                    AttributeId = "department",
                    DataType = XACMLDataTypes.String
                },
                new AttributeValue
                {
                    DataType = XACMLDataTypes.String,
                    Value = "Finance"
                }))))
    .AddRule(new RuleBuilder("allow-read", Effect.Permit)
        .WithCondition(new Apply
        {
            FunctionId = XACMLFunctionIds.IntegerLessThanOrEqual,
            Arguments = [
                new AttributeDesignator
                {
                    Category = AttributeCategory.Resource,
                    AttributeId = "amount",
                    DataType = XACMLDataTypes.Integer
                },
                new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 50000 }
            ]
        })
        .Build())
    .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .Build();
```

#### 4. Inline Expression Language (EEL)

For simpler conditions, the Encina Expression Language (EEL) compiles C# expressions at startup using Roslyn (see ADR-016):

```csharp
[RequireCondition("user.department == \"Finance\" && resource.amount <= 50000")]
public sealed record ApproveTransaction : IRequest<ApprovalResult>;
```

### Component Mapping to XACML 3.0

| XACML 3.0 Section | Encina Type | Notes |
|--------------------|-------------|-------|
| §5.1 PolicySet | `PolicySet` record | Recursive nesting supported |
| §5.2 Policy | `Policy` record | Contains Rules and VariableDefinitions |
| §5.3 Rule | `Rule` record | Leaf authorization unit |
| §5.4 Target | `Target` → `AnyOf` → `AllOf` → `Match` | Full structured matching |
| §5.5 Condition | `Apply` expression tree | Recursive function application |
| §5.6 Obligation | `Obligation` record | Mandatory post-decision action |
| §5.7 Advice | `AdviceExpression` record | Optional recommendation |
| §7.3-§7.6 Evaluation | `XACMLPolicyDecisionPoint` | Full four-effect evaluation |
| §7.18 Obligations | `ObligationExecutor` | Failure overrides to Deny |
| §C Combining | 8 algorithm implementations | All standard algorithms |
| §A.3 Functions | 70+ function implementations | Full type-specific library |

## Consequences

### Positive

- **Formal semantics**: Every edge case in policy evaluation is defined by the OASIS standard
- **Industry standard**: XACML 3.0 is widely understood in the authorization community
- **Four-effect model**: NotApplicable and Indeterminate provide nuanced decision making
- **Obligation/Advice**: Post-decision actions enable audit logging, notifications, data masking
- **Combining algorithms**: 8 standard algorithms cover all conflict resolution strategies
- **Extensibility**: Custom functions, obligation handlers, and attribute providers via DI
- **Interoperability**: Policies can be translated to/from XACML XML for tool compatibility

### Negative

- **Complexity**: XACML 3.0 is a comprehensive standard — the full implementation is substantial
- **Learning curve**: Developers unfamiliar with XACML need documentation and examples
- **Performance**: Full expression tree evaluation is slower than simple role checks

### Neutral

- **No XML**: While XACML is XML-based, Encina uses C# records — no XML parsing overhead
- **Separate package**: `Encina.Security.ABAC` is fully optional — RBAC users are unaffected
- **In-memory default**: The default PAP is in-memory; persistent backends are future extensions

## Alternatives Considered

### 1. Custom ABAC Model (No Standard)

Building a custom ABAC model without following any standard.

**Rejected because**: Every edge case (conflicting policies, missing attributes, evaluation errors) would need custom design. XACML 3.0 has solved these problems with formal semantics refined over 20+ years.

### 2. OPA/Rego Integration

Integrating with Open Policy Agent and using Rego as the policy language.

**Rejected because**: OPA requires a separate sidecar process, uses a non-standard language (Rego), and lacks formal obligation/advice support. It would add infrastructure complexity and prevent in-process evaluation.

### 3. Cedar (AWS Verified Permissions)

Using Cedar as the policy language and evaluation model.

**Rejected because**: Cedar only supports Permit and Forbid (no NotApplicable or Indeterminate), lacks obligation/advice, and has limited combining algorithm support. It's also closely tied to the AWS ecosystem.

### 4. XACML 2.0

Using the older XACML 2.0 specification.

**Rejected because**: XACML 3.0 adds critical features including delegation, administration profiles, and improved combining algorithms. The 3.0 specification also clarifies many ambiguities in 2.0.

## Related Decisions

- [ADR-001: Railway Oriented Programming](001-railway-oriented-programming.md) — All ABAC operations return `Either<EncinaError, T>`
- [ADR-016: Roslyn for Expression Language](016-roslyn-expression-language.md) — EEL compilation engine
- [ADR-017: EEL Naming and Design](017-eel-naming-design.md) — Expression language identity

## References

- [XACML 3.0 Core Specification (OASIS)](https://docs.oasis-open.org/xacml/3.0/xacml-3.0-core-spec-os-en.html)
- [XACML 3.0 Multiple Decision Profile](https://docs.oasis-open.org/xacml/3.0/multiple/v1.0/xacml-3.0-multiple-v1.0.html)
- [NIST SP 800-162: Guide to ABAC Definition and Considerations](https://csrc.nist.gov/publications/detail/sp/800-162/final)
- [AuthzForce CE (Java XACML 3.0 Implementation)](https://authzforce-ce-fiware.readthedocs.io/)
