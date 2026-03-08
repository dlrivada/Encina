# Fluent DSL Guide for Building ABAC Policies

## 1. Overview

### Why a Fluent DSL Instead of XML

XACML 3.0 is traditionally defined in XML, but XML-based policy authoring introduces several
problems for .NET developers: verbose syntax, no compile-time validation, difficult refactoring,
and zero IntelliSense support. The Encina ABAC fluent DSL replaces XML with a strongly-typed C#
builder API that produces the same XACML 3.0 model objects.

### Design Philosophy

- **Compile-time safety** -- Invalid policies are caught by the compiler, not at runtime.
- **Zero XML** -- Policies are pure C# objects. No parsing, no schema validation, no XSLT.
- **Immutable output** -- Every `Build()` call produces an immutable record. Builders are
  single-use; calling `Build()` again on the same builder yields a separate snapshot.
- **XACML 3.0 fidelity** -- The DSL is a thin layer over the XACML 3.0 model.
  Every concept (PolicySet, Policy, Rule, Target, Condition, Obligation, Advice) has a
  direct 1:1 mapping. No shortcuts that hide XACML semantics.
- **Two construction styles** -- Each builder accepts either a pre-built model object
  or a `Action<TBuilder>` delegate for inline construction. Mix and match freely.

### Import Statements

```csharp
using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
```

---

## 2. PolicySetBuilder

`PolicySetBuilder` constructs a `PolicySet` -- the top-level authorization container that groups
policies and nested policy sets under a single combining algorithm.

### Constructor

```csharp
var builder = new PolicySetBuilder("organization-policies");
```

The `id` parameter is required and must not be null or whitespace.

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `WithVersion` | `WithVersion(string version)` | Sets a version identifier (e.g. `"1.0"`, `"2.1.3"`). |
| `WithDescription` | `WithDescription(string description)` | Sets a human-readable description. |
| `WithTarget` | `WithTarget(Target target)` | Sets applicability target from a pre-built instance. |
| `WithTarget` | `WithTarget(Action<TargetBuilder> configure)` | Sets applicability target via inline builder. |
| `AddPolicy` | `AddPolicy(Policy policy)` | Adds a pre-built `Policy`. |
| `AddPolicy` | `AddPolicy(string policyId, Action<PolicyBuilder> configure)` | Adds a policy via inline builder. |
| `AddPolicySet` | `AddPolicySet(PolicySet policySet)` | Adds a pre-built nested `PolicySet`. |
| `AddPolicySet` | `AddPolicySet(string policySetId, Action<PolicySetBuilder> configure)` | Adds a nested policy set via inline builder. |
| `WithAlgorithm` | `WithAlgorithm(CombiningAlgorithmId algorithm)` | Sets the combining algorithm. Default: `DenyOverrides`. |
| `AddObligation` | `AddObligation(Obligation obligation)` | Adds a pre-built obligation. |
| `AddObligation` | `AddObligation(string obligationId, Action<ObligationBuilder> configure)` | Adds an obligation via inline builder. |
| `AddAdvice` | `AddAdvice(AdviceExpression advice)` | Adds a pre-built advice expression. |
| `AddAdvice` | `AddAdvice(string adviceId, Action<AdviceBuilder> configure)` | Adds an advice expression via inline builder. |
| `WithPriority` | `WithPriority(int priority)` | Sets evaluation priority (lower = higher priority). |
| `Disabled` | `Disabled()` | Marks the policy set as disabled (produces `NotApplicable`). |
| `Build` | `Build()` | Builds the immutable `PolicySet`. Throws if no policies or nested sets were added. |

### Example

```csharp
var policySet = new PolicySetBuilder("organization-policies")
    .WithVersion("1.0")
    .WithDescription("Top-level organizational access policies")
    .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .WithTarget(t => t
        .AnyOf(any => any
            .AllOf(all => all
                .MatchAttribute(
                    AttributeCategory.Resource,
                    "organization",
                    ConditionOperator.Equals,
                    "Contoso"))))
    .AddPolicy("finance-policy", policy => policy
        .AddRule("allow-read", Effect.Permit, rule => rule
            .WithCondition(ConditionBuilder.Equal(
                ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
                ConditionBuilder.StringValue("read")))))
    .AddPolicySet("department-policies", nested => nested
        .WithDescription("Department-level policies")
        .AddPolicy("hr-policy", policy => policy
            .AddRule("deny-external", Effect.Deny, _ => { })))
    .AddObligation("audit-log", ob => ob
        .OnPermit()
        .WithAttribute("event", "policy-set-permit"))
    .Build();
```

---

## 3. PolicyBuilder

`PolicyBuilder` constructs a `Policy` -- a collection of rules combined under a common target
and combining algorithm.

### Constructor

```csharp
var builder = new PolicyBuilder("finance-access-policy");
```

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `WithVersion` | `WithVersion(string version)` | Sets a version identifier. |
| `WithDescription` | `WithDescription(string description)` | Sets a human-readable description. |
| `WithTarget` | `WithTarget(Target target)` | Sets applicability target from a pre-built instance. |
| `WithTarget` | `WithTarget(Action<TargetBuilder> configure)` | Sets applicability target via inline builder. |
| `ForResourceType<T>` | `ForResourceType<T>()` | Shortcut: creates a target matching `resource.resourceType == typeof(T).Name`. |
| `AddRule` | `AddRule(Rule rule)` | Adds a pre-built `Rule`. |
| `AddRule` | `AddRule(string ruleId, Effect effect, Action<RuleBuilder> configure)` | Adds a rule via inline builder. |
| `WithAlgorithm` | `WithAlgorithm(CombiningAlgorithmId algorithm)` | Sets the combining algorithm. Default: `DenyOverrides`. |
| `AddObligation` | `AddObligation(Obligation obligation)` | Adds a pre-built obligation. |
| `AddObligation` | `AddObligation(string obligationId, Action<ObligationBuilder> configure)` | Adds an obligation via inline builder. |
| `AddAdvice` | `AddAdvice(AdviceExpression advice)` | Adds a pre-built advice expression. |
| `AddAdvice` | `AddAdvice(string adviceId, Action<AdviceBuilder> configure)` | Adds an advice expression via inline builder. |
| `DefineVariable` | `DefineVariable(string variableId, IExpression expression)` | Defines a reusable sub-expression variable scoped to this policy. |
| `WithPriority` | `WithPriority(int priority)` | Sets evaluation priority (lower = higher priority). |
| `Disabled` | `Disabled()` | Marks the policy as disabled (produces `NotApplicable`). |
| `Build` | `Build()` | Builds the immutable `Policy`. Throws if no rules were added. |

### ForResourceType\<T\> Shortcut

This convenience method creates a target that matches a specific resource type by CLR type name:

```csharp
var policy = new PolicyBuilder("report-policy")
    .ForResourceType<FinancialReport>()
    .AddRule("allow-read", Effect.Permit, rule => rule
        .WithCondition(ConditionBuilder.Equal(
            ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
            ConditionBuilder.StringValue("read"))))
    .Build();
```

This is equivalent to manually building a target with
`resource.resourceType == "FinancialReport"`.

### DefineVariable Example

Variables let you compute a sub-expression once and reference it across multiple rules:

```csharp
var policy = new PolicyBuilder("variable-demo")
    .DefineVariable("is-business-hours",
        ConditionBuilder.And(
            ConditionBuilder.GreaterThanOrEqual(
                ConditionBuilder.Attribute(AttributeCategory.Environment, "currentHour", XACMLDataTypes.Integer),
                ConditionBuilder.IntValue(9)),
            ConditionBuilder.LessThan(
                ConditionBuilder.Attribute(AttributeCategory.Environment, "currentHour", XACMLDataTypes.Integer),
                ConditionBuilder.IntValue(17))))
    .AddRule("allow-during-hours", Effect.Permit, rule => rule
        .WithCondition(ConditionBuilder.Function(
            XACMLFunctionIds.BooleanEqual,
            ConditionBuilder.Variable("is-business-hours"),
            ConditionBuilder.BoolValue(true))))
    .Build();
```

---

## 4. RuleBuilder

`RuleBuilder` constructs a `Rule` -- the most granular policy element that specifies an
`Effect` (Permit or Deny) to return when its target matches and its condition evaluates to true.

### Constructor

```csharp
var builder = new RuleBuilder("allow-finance-read", Effect.Permit);
```

The `effect` parameter must be `Effect.Permit` or `Effect.Deny`. Values like `NotApplicable`
and `Indeterminate` are computed by the evaluation engine and will throw `ArgumentOutOfRangeException`.

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `WithDescription` | `WithDescription(string description)` | Sets a human-readable description. |
| `WithTarget` | `WithTarget(Target target)` | Sets applicability target from a pre-built instance. |
| `WithTarget` | `WithTarget(Action<TargetBuilder> configure)` | Sets applicability target via inline builder. |
| `WithCondition` | `WithCondition(Apply condition)` | Sets the boolean condition expression tree. |
| `AddObligation` | `AddObligation(Obligation obligation)` | Adds a pre-built obligation. |
| `AddObligation` | `AddObligation(string obligationId, Action<ObligationBuilder> configure)` | Adds an obligation via inline builder. |
| `AddAdvice` | `AddAdvice(AdviceExpression advice)` | Adds a pre-built advice expression. |
| `AddAdvice` | `AddAdvice(string adviceId, Action<AdviceBuilder> configure)` | Adds an advice expression via inline builder. |
| `Build` | `Build()` | Builds the immutable `Rule`. |

### Example

```csharp
var rule = new RuleBuilder("allow-finance-read", Effect.Permit)
    .WithDescription("Allow Finance department to read financial reports")
    .WithTarget(t => t
        .AnyOf(any => any
            .AllOf(all => all
                .MatchAttribute(
                    AttributeCategory.Subject,
                    "department",
                    ConditionOperator.Equals,
                    "Finance"))))
    .WithCondition(ConditionBuilder.Equal(
        ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
        ConditionBuilder.StringValue("read")))
    .AddObligation("log-access", ob => ob
        .OnPermit()
        .WithAttribute("action", "Financial report read by Finance user"))
    .Build();
```

---

## 5. TargetBuilder

### Triple-Nesting Structure

The XACML 3.0 Target uses a three-level nesting:

```
Target
  AnyOf[]          -- all AnyOf elements must match (AND)
    AllOf[]        -- any AllOf element can match (OR)
      Match[]      -- all Match elements must match (AND)
```

This translates to: `(AllOf1 OR AllOf2) AND (AllOf3 OR AllOf4)`.

### Builders

- `TargetBuilder` -- entry point; calls `.AnyOf(...)` to add groups.
- `AnyOfBuilder` -- calls `.AllOf(...)` to add conjunctive clauses.
- `AllOfBuilder` -- calls `.Match(...)` or `.MatchAttribute(...)` to add individual comparisons.

### AllOfBuilder.Match Overloads

**Explicit function ID:**
```csharp
.Match(
    "string-equal",                    // XACML function ID
    new AttributeDesignator { ... },   // attribute designator
    new AttributeValue { ... })        // literal value
```

**Category + data type + operator (auto-resolved function ID):**
```csharp
.Match(
    AttributeCategory.Subject,         // category
    "department",                       // attribute ID
    XACMLDataTypes.String,              // data type
    ConditionOperator.Equals,           // operator
    "Finance")                          // literal value
```

**MatchAttribute (auto-inferred data type):**
```csharp
.MatchAttribute(
    AttributeCategory.Subject,         // category
    "department",                       // attribute ID
    ConditionOperator.Equals,           // operator
    "Finance")                          // value -- data type inferred from runtime type
```

`MatchAttribute` is the recommended shorthand for most use cases. It infers the XACML data type
from the .NET runtime type of the value: `string` maps to `XACMLDataTypes.String`, `int` to
`XACMLDataTypes.Integer`, `bool` to `XACMLDataTypes.Boolean`, and so on.

### Full Target Example

```csharp
// Target: (subject is in Finance OR Engineering) AND (resource is Confidential)
var target = new TargetBuilder()
    .AnyOf(any => any
        .AllOf(all => all
            .MatchAttribute(AttributeCategory.Subject, "department", ConditionOperator.Equals, "Finance"))
        .AllOf(all => all
            .MatchAttribute(AttributeCategory.Subject, "department", ConditionOperator.Equals, "Engineering")))
    .AnyOf(any => any
        .AllOf(all => all
            .MatchAttribute(AttributeCategory.Resource, "classification", ConditionOperator.Equals, "Confidential")))
    .Build();
```

### Empty Target

An empty target (no `AnyOf` calls) is valid per XACML 3.0 section 7.6 and means the target
matches all requests unconditionally.

```csharp
var universalTarget = new TargetBuilder().Build();
```

---

## 6. ConditionBuilder

`ConditionBuilder` is a static factory class that constructs `Apply` expression trees -- the
XACML equivalent of function calls. Every condition evaluates to a boolean result.

### Core Expression Factories

| Method | Signature | Returns |
|--------|-----------|---------|
| `Function` | `Function(string functionId, params IExpression[] args)` | `Apply` with the given function and arguments. |
| `Attribute` | `Attribute(AttributeCategory category, string attributeId, string dataType, bool mustBePresent = false)` | `AttributeDesignator` referencing a context attribute. |
| `Value` | `Value(string dataType, object? value)` | `AttributeValue` literal with explicit data type. |
| `Variable` | `Variable(string variableId)` | `VariableReference` to a policy-scoped variable. |

### Typed Value Convenience Factories

| Method | Equivalent |
|--------|------------|
| `StringValue(string val)` | `Value(XACMLDataTypes.String, val)` |
| `IntValue(int val)` | `Value(XACMLDataTypes.Integer, val)` |
| `DoubleValue(double val)` | `Value(XACMLDataTypes.Double, val)` |
| `BoolValue(bool val)` | `Value(XACMLDataTypes.Boolean, val)` |
| `DateTimeValue(DateTime val)` | `Value(XACMLDataTypes.DateTime, val)` |
| `DateValue(DateOnly val)` | `Value(XACMLDataTypes.Date, val)` |
| `TimeValue(TimeOnly val)` | `Value(XACMLDataTypes.Time, val)` |

### Logical Connectives

| Method | XACML Function | Notes |
|--------|----------------|-------|
| `And(params Apply[] conditions)` | `and` | Short-circuit AND. Requires at least one condition. |
| `Or(params Apply[] conditions)` | `or` | Short-circuit OR. Requires at least one condition. |
| `Not(Apply condition)` | `not` | Logical negation. |

### Comparison Sugar

These methods auto-infer the XACML function ID from the operand data types:

| Method | Operation |
|--------|-----------|
| `Equal(IExpression left, IExpression right)` | `*-equal` |
| `GreaterThan(IExpression left, IExpression right)` | `*-greater-than` |
| `LessThan(IExpression left, IExpression right)` | `*-less-than` |
| `GreaterThanOrEqual(IExpression left, IExpression right)` | `*-greater-than-or-equal` |
| `LessThanOrEqual(IExpression left, IExpression right)` | `*-less-than-or-equal` |

### Composing Complex Conditions

```csharp
// subject.department == "Finance" AND resource.amount > 10000
var condition = ConditionBuilder.And(
    ConditionBuilder.Equal(
        ConditionBuilder.Attribute(AttributeCategory.Subject, "department", XACMLDataTypes.String),
        ConditionBuilder.StringValue("Finance")),
    ConditionBuilder.GreaterThan(
        ConditionBuilder.Attribute(AttributeCategory.Resource, "amount", XACMLDataTypes.Integer),
        ConditionBuilder.IntValue(10000)));

// NOT (environment.dayOfWeek == "Saturday" OR environment.dayOfWeek == "Sunday")
var weekdayOnly = ConditionBuilder.Not(
    ConditionBuilder.Or(
        ConditionBuilder.Equal(
            ConditionBuilder.Attribute(AttributeCategory.Environment, "dayOfWeek", XACMLDataTypes.String),
            ConditionBuilder.StringValue("Saturday")),
        ConditionBuilder.Equal(
            ConditionBuilder.Attribute(AttributeCategory.Environment, "dayOfWeek", XACMLDataTypes.String),
            ConditionBuilder.StringValue("Sunday"))));
```

---

## 7. ObligationBuilder

Obligations are mandatory post-decision actions. If the PEP cannot fulfill an obligation,
it must deny access (XACML 3.0 section 7.18).

### Constructor

```csharp
var builder = new ObligationBuilder("log-access");
```

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `OnPermit` | `OnPermit()` | Triggers on Permit decisions. This is the default. |
| `OnDeny` | `OnDeny()` | Triggers on Deny decisions. |
| `WithAttribute` | `WithAttribute(string attributeId, IExpression value)` | Adds an attribute assignment with an expression value. |
| `WithAttribute` | `WithAttribute(string attributeId, object? value)` | Adds an attribute assignment with a literal (auto-wrapped as string `AttributeValue`). |
| `WithAttribute` | `WithAttribute(string attributeId, AttributeCategory category, IExpression value)` | Adds a category-scoped attribute assignment. |
| `Build` | `Build()` | Builds the immutable `Obligation`. |

### Example

```csharp
var obligation = new ObligationBuilder("log-access")
    .OnPermit()
    .WithAttribute("reason", "Audit trail for financial access")
    .WithAttribute("timestamp", ConditionBuilder.DateTimeValue(DateTime.UtcNow))
    .WithAttribute("subject",
        AttributeCategory.Subject,
        ConditionBuilder.Attribute(AttributeCategory.Subject, "userId", XACMLDataTypes.String))
    .Build();
```

---

## 8. AdviceBuilder

Advice expressions are optional post-decision recommendations. Unlike obligations, the PEP
may choose to ignore advice without affecting the authorization decision.

### Constructor

```csharp
var builder = new AdviceBuilder("notify-user");
```

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `OnPermit` | `OnPermit()` | Applies on Permit decisions. This is the default. |
| `OnDeny` | `OnDeny()` | Applies on Deny decisions. |
| `WithAttribute` | `WithAttribute(string attributeId, IExpression value)` | Adds an attribute assignment with an expression value. |
| `WithAttribute` | `WithAttribute(string attributeId, object? value)` | Adds an attribute assignment with a literal (auto-wrapped as string `AttributeValue`). |
| `WithAttribute` | `WithAttribute(string attributeId, AttributeCategory category, IExpression value)` | Adds a category-scoped attribute assignment. |
| `Build` | `Build()` | Builds the immutable `AdviceExpression`. |

### Example

```csharp
var advice = new AdviceBuilder("notify-user")
    .OnDeny()
    .WithAttribute("message", "Contact your manager to request access.")
    .WithAttribute("reason", ConditionBuilder.StringValue("Insufficient clearance"))
    .Build();
```

---

## 9. Complete Examples

### 9a. Finance Approval Workflow

A policy set that restricts access to financial reports based on department, transaction
amount, and business hours.

```csharp
var financeApprovalPolicySet = new PolicySetBuilder("finance-approval")
    .WithVersion("2.0")
    .WithDescription("Finance department approval workflow with amount thresholds")
    .WithAlgorithm(CombiningAlgorithmId.OrderedDenyOverrides)
    .WithTarget(t => t
        .AnyOf(any => any
            .AllOf(all => all
                .MatchAttribute(
                    AttributeCategory.Resource,
                    "resourceType",
                    ConditionOperator.Equals,
                    "FinancialReport"))))
    .AddPolicy("high-value-approval", policy => policy
        .WithDescription("Transactions over 50,000 require VP approval during business hours")
        .WithAlgorithm(CombiningAlgorithmId.DenyUnlessPermit)
        .DefineVariable("is-business-hours",
            ConditionBuilder.And(
                ConditionBuilder.GreaterThanOrEqual(
                    ConditionBuilder.Attribute(AttributeCategory.Environment, "currentHour", XACMLDataTypes.Integer),
                    ConditionBuilder.IntValue(9)),
                ConditionBuilder.LessThan(
                    ConditionBuilder.Attribute(AttributeCategory.Environment, "currentHour", XACMLDataTypes.Integer),
                    ConditionBuilder.IntValue(17))))
        .AddRule("allow-vp-high-value", Effect.Permit, rule => rule
            .WithDescription("VP can approve high-value transactions during business hours")
            .WithCondition(ConditionBuilder.And(
                ConditionBuilder.Equal(
                    ConditionBuilder.Attribute(AttributeCategory.Subject, "role", XACMLDataTypes.String),
                    ConditionBuilder.StringValue("VP")),
                ConditionBuilder.GreaterThan(
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "amount", XACMLDataTypes.Integer),
                    ConditionBuilder.IntValue(50000)),
                ConditionBuilder.Function(
                    XACMLFunctionIds.BooleanEqual,
                    ConditionBuilder.Variable("is-business-hours"),
                    ConditionBuilder.BoolValue(true))))
            .AddObligation("dual-sign-off", ob => ob
                .OnPermit()
                .WithAttribute("reason", "High-value transaction requires dual sign-off")
                .WithAttribute("threshold", "50000")))
        .AddRule("deny-after-hours", Effect.Deny, rule => rule
            .WithDescription("Deny all high-value approvals outside business hours")
            .WithCondition(ConditionBuilder.And(
                ConditionBuilder.GreaterThan(
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "amount", XACMLDataTypes.Integer),
                    ConditionBuilder.IntValue(50000)),
                ConditionBuilder.Not(
                    ConditionBuilder.Function(
                        XACMLFunctionIds.BooleanEqual,
                        ConditionBuilder.Variable("is-business-hours"),
                        ConditionBuilder.BoolValue(true)))))
            .AddAdvice("schedule-retry", adv => adv
                .OnDeny()
                .WithAttribute("message", "Please retry during business hours (09:00-17:00 UTC)"))))
    .AddPolicy("standard-approval", policy => policy
        .WithDescription("Standard read access for Finance department members")
        .WithAlgorithm(CombiningAlgorithmId.FirstApplicable)
        .AddRule("allow-finance-read", Effect.Permit, rule => rule
            .WithTarget(t => t
                .AnyOf(any => any
                    .AllOf(all => all
                        .MatchAttribute(AttributeCategory.Subject, "department", ConditionOperator.Equals, "Finance"))))
            .WithCondition(ConditionBuilder.Equal(
                ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
                ConditionBuilder.StringValue("read"))))
        .AddRule("deny-default", Effect.Deny, _ => { }))
    .AddObligation("audit-trail", ob => ob
        .OnPermit()
        .WithAttribute("event", "finance-access-granted"))
    .Build();
```

### 9b. Healthcare Data Access

A policy that governs doctor access to patient records, requiring ward assignment and
patient consent verification.

```csharp
var healthcarePolicySet = new PolicySetBuilder("healthcare-data-access")
    .WithVersion("1.2")
    .WithDescription("HIPAA-compliant patient data access control")
    .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .AddPolicy("doctor-patient-access", policy => policy
        .WithDescription("Doctors can access records for patients in their assigned ward")
        .ForResourceType<PatientRecord>()
        .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
        .AddRule("allow-assigned-ward", Effect.Permit, rule => rule
            .WithDescription("Doctor must be assigned to the patient ward")
            .WithCondition(ConditionBuilder.And(
                ConditionBuilder.Equal(
                    ConditionBuilder.Attribute(AttributeCategory.Subject, "role", XACMLDataTypes.String),
                    ConditionBuilder.StringValue("Doctor")),
                ConditionBuilder.Equal(
                    ConditionBuilder.Attribute(AttributeCategory.Subject, "assignedWard", XACMLDataTypes.String),
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "ward", XACMLDataTypes.String)),
                ConditionBuilder.Equal(
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "patientConsent", XACMLDataTypes.Boolean),
                    ConditionBuilder.BoolValue(true))))
            .AddObligation("hipaa-audit", ob => ob
                .OnPermit()
                .WithAttribute("eventType", "patient-record-access")
                .WithAttribute("doctor",
                    AttributeCategory.Subject,
                    ConditionBuilder.Attribute(AttributeCategory.Subject, "userId", XACMLDataTypes.String))
                .WithAttribute("patient",
                    AttributeCategory.Resource,
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "patientId", XACMLDataTypes.String))))
        .AddRule("deny-no-consent", Effect.Deny, rule => rule
            .WithDescription("Deny access when patient consent is not granted")
            .WithCondition(ConditionBuilder.Equal(
                ConditionBuilder.Attribute(AttributeCategory.Resource, "patientConsent", XACMLDataTypes.Boolean),
                ConditionBuilder.BoolValue(false)))
            .AddAdvice("request-consent", adv => adv
                .OnDeny()
                .WithAttribute("message", "Patient consent is required before accessing this record.")
                .WithAttribute("patientId",
                    AttributeCategory.Resource,
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "patientId", XACMLDataTypes.String))))
        .AddRule("deny-default", Effect.Deny, rule => rule
            .WithDescription("Default deny for all other access attempts")))
    .AddPolicy("emergency-override", policy => policy
        .WithDescription("Emergency override for life-threatening situations")
        .WithPriority(0)
        .WithAlgorithm(CombiningAlgorithmId.PermitOverrides)
        .AddRule("allow-emergency", Effect.Permit, rule => rule
            .WithCondition(ConditionBuilder.Equal(
                ConditionBuilder.Attribute(AttributeCategory.Environment, "emergencyFlag", XACMLDataTypes.Boolean),
                ConditionBuilder.BoolValue(true)))
            .AddObligation("emergency-audit", ob => ob
                .OnPermit()
                .WithAttribute("severity", "CRITICAL")
                .WithAttribute("reason", "Emergency override invoked -- post-access review required"))))
    .Build();
```

### 9c. Multi-Tenant Resource Isolation

A policy set that enforces strict tenant isolation, ensuring users can only access
resources belonging to their own tenant.

```csharp
var multiTenantPolicySet = new PolicySetBuilder("multi-tenant-isolation")
    .WithVersion("1.0")
    .WithDescription("Enforces strict resource isolation across tenants")
    .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .AddPolicy("tenant-boundary", policy => policy
        .WithDescription("Resources are only accessible by users within the same tenant")
        .WithAlgorithm(CombiningAlgorithmId.DenyUnlessPermit)
        .AddRule("allow-same-tenant", Effect.Permit, rule => rule
            .WithDescription("Permit access when subject tenant matches resource tenant")
            .WithCondition(ConditionBuilder.Equal(
                ConditionBuilder.Attribute(AttributeCategory.Subject, "tenantId", XACMLDataTypes.String),
                ConditionBuilder.Attribute(AttributeCategory.Resource, "tenantId", XACMLDataTypes.String))))
        .AddRule("deny-cross-tenant", Effect.Deny, rule => rule
            .WithDescription("Explicitly deny cross-tenant access attempts")
            .WithCondition(ConditionBuilder.Not(
                ConditionBuilder.Equal(
                    ConditionBuilder.Attribute(AttributeCategory.Subject, "tenantId", XACMLDataTypes.String),
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "tenantId", XACMLDataTypes.String))))
            .AddObligation("security-alert", ob => ob
                .OnDeny()
                .WithAttribute("severity", "HIGH")
                .WithAttribute("event", "cross-tenant-access-attempt")
                .WithAttribute("subjectTenant",
                    AttributeCategory.Subject,
                    ConditionBuilder.Attribute(AttributeCategory.Subject, "tenantId", XACMLDataTypes.String))
                .WithAttribute("resourceTenant",
                    AttributeCategory.Resource,
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "tenantId", XACMLDataTypes.String)))))
    .AddPolicy("tenant-admin-override", policy => policy
        .WithDescription("Platform admins can access any tenant for support purposes")
        .WithAlgorithm(CombiningAlgorithmId.FirstApplicable)
        .AddRule("allow-platform-admin", Effect.Permit, rule => rule
            .WithCondition(ConditionBuilder.Equal(
                ConditionBuilder.Attribute(AttributeCategory.Subject, "role", XACMLDataTypes.String),
                ConditionBuilder.StringValue("PlatformAdmin")))
            .AddObligation("admin-audit", ob => ob
                .OnPermit()
                .WithAttribute("reason", "Platform admin cross-tenant access")
                .WithAttribute("targetTenant",
                    AttributeCategory.Resource,
                    ConditionBuilder.Attribute(AttributeCategory.Resource, "tenantId", XACMLDataTypes.String))))
        .AddRule("not-applicable", Effect.Deny, _ => { }))
    .Build();
```

---

## 10. Seeding Policies at Startup

Use `ABACOptions.SeedPolicySets` and `ABACOptions.SeedPolicies` to load policies into the
Policy Administration Point (PAP) at application startup. The `ABACPolicySeedingHostedService`
processes these lists during host initialization.

### Seeding Policy Sets

```csharp
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;
    options.DefaultNotApplicableEffect = Effect.Deny;
    options.FailOnMissingObligationHandler = true;

    // Seed a full policy set hierarchy
    options.SeedPolicySets.Add(
        new PolicySetBuilder("global-access-control")
            .WithVersion("1.0")
            .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
            .AddPolicy("read-access", policy => policy
                .AddRule("allow-authenticated", Effect.Permit, rule => rule
                    .WithCondition(ConditionBuilder.Equal(
                        ConditionBuilder.Attribute(AttributeCategory.Subject, "isAuthenticated", XACMLDataTypes.Boolean),
                        ConditionBuilder.BoolValue(true)))))
            .Build());
});
```

### Seeding Standalone Policies

Standalone policies (not grouped into a policy set) can also be seeded:

```csharp
services.AddEncinaABAC(options =>
{
    // Seed a standalone policy
    options.SeedPolicies.Add(
        new PolicyBuilder("maintenance-mode")
            .WithDescription("Deny all access during maintenance windows")
            .Disabled() // Start disabled; enable via PAP at runtime
            .AddRule("deny-all", Effect.Deny, _ => { })
            .Build());
});
```

### Organizing Seeds with Helper Methods

For large applications, extract policy definitions into static factory methods:

```csharp
public static class PolicyDefinitions
{
    public static PolicySet FinanceApprovalPolicies() =>
        new PolicySetBuilder("finance-approval")
            .WithVersion("2.0")
            .WithAlgorithm(CombiningAlgorithmId.OrderedDenyOverrides)
            // ... rules and policies
            .Build();

    public static PolicySet HealthcareAccessPolicies() =>
        new PolicySetBuilder("healthcare-data-access")
            .WithVersion("1.2")
            // ... rules and policies
            .Build();

    public static Policy MaintenanceModePolicy() =>
        new PolicyBuilder("maintenance-mode")
            .Disabled()
            .AddRule("deny-all", Effect.Deny, _ => { })
            .Build();
}

// In Program.cs or Startup
services.AddEncinaABAC(options =>
{
    options.SeedPolicySets.Add(PolicyDefinitions.FinanceApprovalPolicies());
    options.SeedPolicySets.Add(PolicyDefinitions.HealthcareAccessPolicies());
    options.SeedPolicies.Add(PolicyDefinitions.MaintenanceModePolicy());
});
```

Duplicate policy IDs are logged as warnings and skipped during seeding.

---

## 11. Tips and Best Practices

### Naming Conventions

- **PolicySet IDs**: Use kebab-case grouping names: `"organization-policies"`, `"finance-approval"`.
- **Policy IDs**: Describe the domain scope: `"doctor-patient-access"`, `"tenant-boundary"`.
- **Rule IDs**: Describe the intent: `"allow-finance-read"`, `"deny-after-hours"`, `"deny-default"`.
- **Obligation/Advice IDs**: Describe the action: `"audit-log"`, `"notify-user"`, `"security-alert"`.

### Combining Algorithm Selection

| Algorithm | Use When |
|-----------|----------|
| `DenyOverrides` | Security-critical: any Deny wins. Default choice. |
| `PermitOverrides` | Discretionary access: any Permit wins. |
| `FirstApplicable` | Priority ordering: first match wins. Use with `WithPriority`. |
| `DenyUnlessPermit` | Strict closed-world: deny unless explicitly permitted. Safest. |
| `PermitUnlessDeny` | Open-world: permit unless explicitly denied. |
| `OnlyOneApplicable` | Diagnostic: overlapping policies indicate configuration errors. |
| `OrderedDenyOverrides` | Like `DenyOverrides`, but obligation ordering is deterministic. |
| `OrderedPermitOverrides` | Like `PermitOverrides`, but obligation ordering is deterministic. |

### Default Deny Rule Pattern

Always include an explicit default deny rule as the last rule in a policy. This makes
the authorization intent self-documenting:

```csharp
policy.AddRule("deny-default", Effect.Deny, _ => { });
```

A rule with no target and no condition matches all requests and produces its stated effect.

### Target vs. Condition

- **Target** (`WithTarget`) -- Lightweight applicability check using `Match` elements. Determines
  whether the policy/rule even applies to the request. Think of it as a filter.
- **Condition** (`WithCondition`) -- Full expression tree evaluated only when the target matches.
  Supports logical connectives, comparisons, and variable references. Think of it as the logic.

Use targets for simple attribute-value matching. Use conditions for complex multi-attribute logic.

### Obligations vs. Advice

| | Obligations | Advice |
|---|-------------|--------|
| **Enforcement** | Mandatory. PEP must deny if handler fails. | Optional. PEP may ignore. |
| **Use case** | Audit logging, MFA escalation, rate limiting. | User notifications, UI hints, debug info. |
| **Builder** | `ObligationBuilder` | `AdviceBuilder` |
| **Option** | `FailOnMissingObligationHandler = true` | `IncludeAdvice = true` |

### Disabled Policies for Feature Flags

Use `.Disabled()` to pre-define policies that can be toggled at runtime via the PAP:

```csharp
new PolicyBuilder("beta-feature-access")
    .Disabled() // Off by default
    .AddRule("allow-beta-users", Effect.Permit, rule => rule
        .WithCondition(ConditionBuilder.Equal(
            ConditionBuilder.Attribute(AttributeCategory.Subject, "betaAccess", XACMLDataTypes.Boolean),
            ConditionBuilder.BoolValue(true))))
    .Build();
```

### Variable Reuse for DRY Conditions

When the same sub-expression appears in multiple rules, define it once with `DefineVariable`
and reference it with `ConditionBuilder.Variable`:

```csharp
policy
    .DefineVariable("is-owner",
        ConditionBuilder.Equal(
            ConditionBuilder.Attribute(AttributeCategory.Subject, "userId", XACMLDataTypes.String),
            ConditionBuilder.Attribute(AttributeCategory.Resource, "ownerId", XACMLDataTypes.String)))
    .AddRule("allow-owner-read", Effect.Permit, rule => rule
        .WithCondition(ConditionBuilder.Function(
            XACMLFunctionIds.BooleanEqual,
            ConditionBuilder.Variable("is-owner"),
            ConditionBuilder.BoolValue(true))))
    .AddRule("allow-owner-write", Effect.Permit, rule => rule
        .WithCondition(ConditionBuilder.And(
            ConditionBuilder.Function(
                XACMLFunctionIds.BooleanEqual,
                ConditionBuilder.Variable("is-owner"),
                ConditionBuilder.BoolValue(true)),
            ConditionBuilder.Equal(
                ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
                ConditionBuilder.StringValue("write")))));
```

### Testing Policies

Policies are plain immutable records. You can assert their structure in unit tests without
needing a running PDP:

```csharp
var policy = PolicyDefinitions.FinanceApprovalPolicies();

Assert.Equal("finance-approval", policy.Id);
Assert.Equal("2.0", policy.Version);
Assert.Equal(CombiningAlgorithmId.OrderedDenyOverrides, policy.Algorithm);
Assert.True(policy.IsEnabled);
Assert.Equal(2, policy.Policies.Count);
Assert.Single(policy.Obligations);
```
