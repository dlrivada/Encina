# Phase 10: ABAC Testing — Implementation Plan

> **Status**: 🟡 Pending Approval
> **Issue**: #401
> **Date**: 2026-03-08

## Objective

Create comprehensive tests for `Encina.Security.ABAC` across all test categories: Unit, Guard, Contract, Property, Load, and Benchmark. Special emphasis on EEL (Encina Expression Language) conformance testing.

## Estimated Test Count: ~450-500 tests

## File Listing

### Step 1: Unit Tests (~250 tests)

#### 1.1 Combining Algorithms (8 files, ~120 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/DenyOverridesAlgorithmTests.cs` | ~15 | All 4 effects, empty input, mixed results |
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/PermitOverridesAlgorithmTests.cs` | ~15 | All 4 effects, permit overrides deny |
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/FirstApplicableAlgorithmTests.cs` | ~15 | First match wins, skip NotApplicable |
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/OnlyOneApplicableAlgorithmTests.cs` | ~15 | Zero/one/two applicable → correct effect |
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/DenyUnlessPermitAlgorithmTests.cs` | ~15 | Never returns NotApplicable/Indeterminate |
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/PermitUnlessDenyAlgorithmTests.cs` | ~15 | Never returns NotApplicable/Indeterminate |
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/OrderedDenyOverridesAlgorithmTests.cs` | ~15 | Order matters, deny wins |
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/OrderedPermitOverridesAlgorithmTests.cs` | ~15 | Order matters, permit wins |

Each algorithm test file covers:
- `CombineRuleResults_AllPermit_ReturnsPermit`
- `CombineRuleResults_AllDeny_ReturnsDeny`
- `CombineRuleResults_AllNotApplicable_Returns{Expected}`
- `CombineRuleResults_AllIndeterminate_Returns{Expected}`
- `CombineRuleResults_MixedPermitDeny_Returns{Expected}`
- `CombineRuleResults_EmptyResults_Returns{Expected}`
- `CombineRuleResults_SinglePermit_ReturnsPermit`
- `CombineRuleResults_SingleDeny_ReturnsDeny`
- `CombinePolicyResults_AllPermit_ReturnsPermit`
- `CombinePolicyResults_AllDeny_ReturnsDeny`
- `CombinePolicyResults_MixedEffects_Returns{Expected}`
- `CombinePolicyResults_WithObligations_PreservesObligations`
- `CombinePolicyResults_WithAdvice_PreservesAdvice`
- `AlgorithmId_ReturnsCorrectId`

Plus one shared test file:
| `tests/Encina.UnitTests/Security/ABAC/CombiningAlgorithms/CombiningAlgorithmFactoryTests.cs` | ~10 | GetAlgorithm for all 8 IDs, invalid ID |

#### 1.2 XACML Functions (10 files, ~70 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.UnitTests/Security/ABAC/Functions/EqualityFunctionsTests.cs` | ~10 | string, int, double, bool, date, datetime, time, anyuri equality |
| `tests/Encina.UnitTests/Security/ABAC/Functions/ComparisonFunctionsTests.cs` | ~8 | <, >, <=, >= for integer, double, string, date |
| `tests/Encina.UnitTests/Security/ABAC/Functions/ArithmeticFunctionsTests.cs` | ~6 | add, subtract, multiply, div, mod |
| `tests/Encina.UnitTests/Security/ABAC/Functions/StringFunctionsTests.cs` | ~10 | concat, substring, length, upper, lower, normalize, contains, starts/ends-with |
| `tests/Encina.UnitTests/Security/ABAC/Functions/LogicalFunctionsTests.cs` | ~6 | and, or, not with edge cases |
| `tests/Encina.UnitTests/Security/ABAC/Functions/BagFunctionsTests.cs` | ~8 | bag, bag-size, is-in, one-and-only |
| `tests/Encina.UnitTests/Security/ABAC/Functions/SetFunctionsTests.cs` | ~8 | intersection, union, subset, set-equals |
| `tests/Encina.UnitTests/Security/ABAC/Functions/TypeConversionFunctionsTests.cs` | ~6 | string-to-int, int-to-string, etc. |
| `tests/Encina.UnitTests/Security/ABAC/Functions/RegexFunctionsTests.cs` | ~4 | regexp-match: valid, no match, invalid regex |
| `tests/Encina.UnitTests/Security/ABAC/Functions/HigherOrderFunctionsTests.cs` | ~6 | map, any-of, all-of |

Plus:
| `tests/Encina.UnitTests/Security/ABAC/Functions/DefaultFunctionRegistryTests.cs` | ~8 | Register, GetFunction, GetAllFunctionIds, custom registration |

#### 1.3 EEL (Encina Expression Language) (~40 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.UnitTests/Security/ABAC/EEL/EELCompilerTests.cs` | ~20 | Valid/invalid expressions, caching, disposal |
| `tests/Encina.UnitTests/Security/ABAC/EEL/EELGlobalsTests.cs` | ~6 | Dynamic property access, attribute binding |
| `tests/Encina.UnitTests/Security/ABAC/EEL/EELConformanceTests.cs` | ~14 | Data-driven JSON conformance tests (see Step 1.7) |

**EELCompilerTests key tests:**
- `CompileAsync_ValidExpression_ReturnsRight`
- `CompileAsync_InvalidExpression_ReturnsLeftWithDiagnostics`
- `CompileAsync_EmptyExpression_ReturnsLeftInvalidCondition`
- `CompileAsync_CachesDelegate_ReturnsSameRunner`
- `CompileAsync_ConcurrentCompilation_ThreadSafe` (100 tasks)
- `EvaluateAsync_TrueExpression_ReturnsTrue`
- `EvaluateAsync_FalseExpression_ReturnsFalse`
- `EvaluateAsync_ExpressionWithGlobals_AccessesAttributes`
- `EvaluateAsync_NonBoolReturn_ReturnsLeft`
- `EvaluateAsync_StatementRejected_ReturnsLeft`
- `EvaluateAsync_NullGlobals_ThrowsArgumentNullException`
- `Dispose_ReleasesResources`
- `CompileAsync_AfterDispose_ThrowsObjectDisposedException`
- `CompileAsync_DeeplyNestedParens_Compiles`
- `CompileAsync_Unicode_HandledCorrectly`

**EELConformanceTests key categories** (JSON data-driven):
- Literals: `"true"`, `"false"`, `"1 == 1"`
- Comparisons: `"user.age > 18"`, `"user.role == \"admin\""`
- Logical: `"user.active && user.verified"`, `"!user.banned"`
- Arithmetic: `"user.age + 1 > 18"`
- Null safety: `"user.email != null"`
- Property access: `"resource.owner == user.id"`
- String methods: `"user.name.Contains(\"admin\")"`
- Collections: `"user.roles.Contains(\"editor\")"`
- Complex: `"(user.role == \"admin\" || user.department == \"IT\") && resource.classification != \"top-secret\""`
- Error cases: `""`, `"var x = 1;"`, `"System.IO.File.Delete(\"a\")"` (rejected)

#### 1.4 Evaluation Engine (~20 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.UnitTests/Security/ABAC/Evaluation/ConditionEvaluatorTests.cs` | ~10 | AttributeValue, AttributeDesignator, Apply, VariableReference evaluation |
| `tests/Encina.UnitTests/Security/ABAC/Evaluation/TargetEvaluatorTests.cs` | ~10 | Target matching, AnyOf/AllOf nesting, null target |

#### 1.5 Pipeline & Enforcement (~25 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.UnitTests/Security/ABAC/ABACPipelineBehaviorTests.cs` | ~15 | All 4 effects, enforcement modes (Block/Warn/Disabled), obligation execution |
| `tests/Encina.UnitTests/Security/ABAC/ObligationExecutorTests.cs` | ~10 | Success, handler failure → deny, no handler, advice execution |

#### 1.6 Builders, Admin, Config (~25 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.UnitTests/Security/ABAC/Builders/PolicyBuilderTests.cs` | ~6 | Build valid policy, fluent API chaining |
| `tests/Encina.UnitTests/Security/ABAC/Builders/RuleBuilderTests.cs` | ~4 | Build rule with conditions, obligations |
| `tests/Encina.UnitTests/Security/ABAC/Builders/ConditionBuilderTests.cs` | ~8 | Function, Value, Attribute, And/Or/Not, Equal, comparisons |
| `tests/Encina.UnitTests/Security/ABAC/Administration/InMemoryPolicyAdministrationPointTests.cs` | ~8 | Add/Remove/Get policies and policy sets |
| `tests/Encina.UnitTests/Security/ABAC/ABACOptionsTests.cs` | ~4 | Defaults, AddFunction |
| `tests/Encina.UnitTests/Security/ABAC/ABACErrorsTests.cs` | ~10 | All 17 error factory methods produce correct codes |

#### 1.7 EEL Conformance JSON Test Data

| File | Description |
|------|-------------|
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-literals.json` | Boolean, numeric, string literals |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-comparison.json` | >, <, >=, <=, == with attribute binding |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-logical.json` | &&, ||, ! operators |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-arithmetic.json` | +, -, *, / in conditions |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-strings.json` | String methods and comparisons |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-nullsafety.json` | Null checks, null propagation |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-propertyaccess.json` | user.x, resource.y dynamic access |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-collections.json` | Contains, Any, All on collections |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-complex.json` | Multi-operator compound expressions |
| `tests/Encina.UnitTests/Security/ABAC/EEL/TestData/eel-conformance-errors.json` | Invalid expressions, rejected statements, sandbox violations |

**JSON test data structure:**
```json
{
  "category": "comparison",
  "tests": [
    {
      "name": "greater_than_true",
      "expression": "user.age > 18",
      "globals": {
        "user": { "age": 25 },
        "resource": {},
        "environment": {},
        "action": {}
      },
      "expected": true
    },
    {
      "name": "invalid_syntax",
      "expression": "user.age >>>",
      "globals": { "user": {}, "resource": {}, "environment": {}, "action": {} },
      "expectedError": "abac.invalid_condition"
    }
  ]
}
```

#### 1.8 Health & Diagnostics (~10 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.UnitTests/Security/ABAC/Health/ABACHealthCheckTests.cs` | ~6 | Healthy (policies exist), Degraded (empty PAP), exception handling |
| `tests/Encina.UnitTests/Security/ABAC/Diagnostics/ABACDiagnosticsTests.cs` | ~4 | Counter/histogram creation verification |

### Step 2: Guard Tests (~60 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.GuardTests/Security/ABAC/ABACGuardTests.cs` | ~60 | All public constructors and methods |

**Covers null checks for:**
- `ABACPipelineBehavior` constructor (5 params: pdp, executor, options, accessor, logger)
- `ObligationExecutor` constructor (handlers, adviceHandlers, logger)
- `XACMLPolicyDecisionPoint` constructor (functionRegistry, logger, conditionEvaluator, targetEvaluator)
- `ConditionEvaluator` constructor (functionRegistry)
- `TargetEvaluator` constructor (functionRegistry)
- `EELCompiler` constructor (if any params)
- `InMemoryPolicyAdministrationPoint.AddPolicyAsync(null)`
- `InMemoryPolicyAdministrationPoint.AddPolicySetAsync(null)`
- `DefaultFunctionRegistry.Register(null, ...)` and `Register(..., null)`
- `DefaultFunctionRegistry.GetFunction(null)`
- `ABACOptions.AddFunction(null, ...)` and `AddFunction(..., null)`
- All builder methods with null parameters
- `PolicyBuilder.Build()` without required fields
- `RuleBuilder.Build()` without required fields

### Step 3: Contract Tests (~30 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.ContractTests/Security/ABAC/ABACContractTests.cs` | ~30 | Interface shapes and API surface stability |

**Covers:**
- `ICombiningAlgorithm` interface: must have 3 members (AlgorithmId, CombineRuleResults, CombinePolicyResults)
- `ICombiningAlgorithm` return types: Effect for rules, PolicyEvaluationResult for policies
- All 8 implementations exist and implement `ICombiningAlgorithm`
- `IPolicyDecisionPoint` interface shape (1 method)
- `IPolicyAdministrationPoint` interface shape
- `IAttributeProvider` interface shape (3 methods)
- `IObligationHandler` interface shape (2 methods)
- `IFunctionRegistry` interface shape (3 methods)
- `IXACMLFunction` interface shape
- `Effect` enum values: exactly 4 (Permit, Deny, NotApplicable, Indeterminate)
- `ABACErrors` error codes: 17 constant strings all start with "abac."
- `ABACOptions` default values (EnforcementMode=Block, DefaultNotApplicableEffect=Deny, etc.)
- Policy model immutability contracts
- `PolicyDecision` structure (Effect, Obligations, Advice, Duration)

### Step 4: Property Tests (~40 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.PropertyTests/Security/ABAC/CombiningAlgorithmPropertyTests.cs` | ~20 | Invariants for all 8 algorithms |
| `tests/Encina.PropertyTests/Security/ABAC/EELPropertyTests.cs` | ~12 | EEL evaluation invariants |
| `tests/Encina.PropertyTests/Security/ABAC/FunctionPropertyTests.cs` | ~8 | XACML function invariants |

**CombiningAlgorithmPropertyTests key properties:**
- `DenyUnlessPermit_NeverReturnsNotApplicable` — For ANY input, effect ∈ {Permit, Deny}
- `DenyUnlessPermit_NeverReturnsIndeterminate` — For ANY input, effect ∈ {Permit, Deny}
- `PermitUnlessDeny_NeverReturnsNotApplicable` — For ANY input, effect ∈ {Permit, Deny}
- `PermitUnlessDeny_NeverReturnsIndeterminate` — For ANY input, effect ∈ {Permit, Deny}
- `DenyOverrides_AnyDeny_ResultIsDeny` — If ANY result is Deny, combined is Deny
- `PermitOverrides_AnyPermit_ResultIsPermit` — If ANY result is Permit, combined is Permit
- `FirstApplicable_EmptyInput_ReturnsNotApplicable`
- `AllAlgorithms_EmptyInput_NeverThrows`
- `AllAlgorithms_SingleResult_Deterministic` — Same input always produces same output
- FsCheck generators for `RuleEvaluationResult` and `PolicyEvaluationResult`

**EELPropertyTests key properties:**
- `Evaluate_DeterministicForPureExpressions` — Same expression + same globals = same result
- `Evaluate_DoubleNegation_Identity` — `!!x` ≡ `x` for boolean expressions
- `Evaluate_And_Commutative` — `a && b` ≡ `b && a` for pure expressions
- `Evaluate_Or_Commutative` — `a || b` ≡ `b || a` for pure expressions
- `Evaluate_TrueAndX_EqualsX` — `true && x` ≡ `x` (identity law)
- `Evaluate_FalseOrX_EqualsX` — `false || x` ≡ `x` (identity law)

**FunctionPropertyTests key properties:**
- `EqualityFunction_Reflexive` — `x == x` always true
- `EqualityFunction_Symmetric` — `x == y` ↔ `y == x`
- `Comparison_Antisymmetric` — `x < y` → `¬(y < x)` (for same type)
- `ArithmeticAdd_Commutative` — `x + y == y + x`

### Step 5: Benchmark Tests (~15 benchmarks)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/ABAC/ABACBenchmarks.cs` | ~15 | Performance benchmarks |

**Benchmarks:**
- `EvaluateFlatPolicy_SingleRule` — Baseline: single policy, single rule
- `EvaluateFlatPolicy_TenRules` — 10 rules in one policy
- `EvaluateNestedPolicySet_ThreePolicies` — PolicySet with 3 child policies
- `EvaluateNestedPolicySet_DeepNesting` — 5 levels of PolicySet nesting
- `EELCompiler_ColdCompilation` — First compilation of new expression
- `EELCompiler_WarmCachedEvaluation` — Cached expression evaluation (hot path)
- `CombiningAlgorithm_DenyOverrides_100Rules` — Large rule set combination
- `CombiningAlgorithm_PermitOverrides_100Rules` — Large rule set combination
- `FunctionRegistry_LookupKnownFunction` — Function lookup performance
- `ConditionEvaluator_SimpleEquality` — Simple condition evaluation
- `ConditionEvaluator_ComplexNestedApply` — Complex expression tree
- `TargetEvaluator_SimpleMatch` — Single match target
- `TargetEvaluator_ComplexAnyOfAllOf` — Nested AnyOf/AllOf
- `ObligationExecutor_SingleObligation` — Single obligation execution
- `ObligationExecutor_TenObligations` — Batch obligation execution

Uses `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config)`.

### Step 6: Load Tests (~10 tests)

| File | Tests | Description |
|------|-------|-------------|
| `tests/Encina.LoadTests/Security/ABAC/ABACLoadTests.cs` | ~10 | Concurrent access patterns |

**Load tests:**
- `ConcurrentEvaluation_100Tasks_AllSucceed` — 100 concurrent policy evaluations
- `ConcurrentEELCompilation_50Tasks_NoCacheCorruption` — SemaphoreSlim contention
- `ConcurrentEELEvaluation_100Tasks_AllDeterministic` — Thread-safe evaluation
- `ConcurrentPolicyAdministration_AddRemoveWhileEvaluating` — Read/write concurrency
- `HighThroughput_1000Evaluations_WithinTimeout` — Throughput test
- `ConcurrentFunctionRegistry_ReadWhileRegister` — Registry thread safety

## Implementation Order

1. **Step 1.7** — EEL conformance JSON test data files (no dependencies)
2. **Step 1.6** — ABACErrorsTests, ABACOptionsTests (foundation types, no complex deps)
3. **Step 1.1** — CombiningAlgorithm tests (core decision logic)
4. **Step 1.2** — Function tests (XACML function categories)
5. **Step 1.3** — EEL tests (depends on understanding functions and errors)
6. **Step 1.4** — Evaluation engine tests (depends on functions, algorithms)
7. **Step 1.5** — Pipeline & enforcement tests (depends on everything above)
8. **Step 1.8** — Health & diagnostics tests
9. **Step 2** — Guard tests (all public constructors/methods)
10. **Step 3** — Contract tests (API surface)
11. **Step 4** — Property tests (invariants)
12. **Step 5** — Benchmark tests
13. **Step 6** — Load tests

## Build Verification

After each step, verify:
```bash
dotnet build Encina.slnx --configuration Release --no-restore
dotnet test tests/Encina.UnitTests --filter "FullyQualifiedName~Security.ABAC" --no-build -c Release
```

Final verification:
```bash
dotnet build Encina.slnx --configuration Release
dotnet test Encina.slnx --configuration Release --no-build
```
