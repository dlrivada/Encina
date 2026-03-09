# Implementation Plan: Persistent PAP — Database-Backed ABAC Policy Storage

> **Issue**: [#691](https://github.com/dlrivada/Encina/issues/691)
> **Type**: Feature
> **Complexity**: High (13 phases, 13 providers, ~76 files)
> **Estimated Scope**: ~7,000–9,000 lines of code + tests

---

## Summary

Implement persistent policy storage for `Encina.Security.ABAC` using Encina's 13 database providers (ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1). The current `InMemoryPolicyAdministrationPoint` is a `ConcurrentDictionary`-based implementation suitable only for development/testing. Production systems need durable, multi-instance-consistent policy storage with audit timestamps.

The implementation introduces four new core abstractions in `Encina.Security.ABAC` (`IPolicyStore`, `IPolicySerializer`, `PersistentPolicyAdministrationPoint`, `CachingPolicyStoreDecorator`) and 13 provider implementations following the established store pattern (`Either<EncinaError, T>`, `TimeProvider` injection, `EitherHelpers.TryAsync`). The serializer handles the complex XACML expression tree (`IExpression` polymorphic hierarchy: `Apply`, `AttributeDesignator`, `AttributeValue`, `VariableReference`) using System.Text.Json discriminated unions. Provider-specific schema uses 2 tables (`abac_policy_sets`, `abac_policies`) with JSON column storage for the full policy graph. An opt-in `CachingPolicyStoreDecorator` integrates with Encina's caching ecosystem (`ICacheProvider` + `IPubSubProvider`) for stampede protection, write-through invalidation, and cross-instance cache coherence.

**Affected packages**: `Encina.Security.ABAC`, `Encina.EntityFrameworkCore`, `Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySql`, `Encina.MongoDB`

**Provider category**: Database (13 providers) — all required per CLAUDE.md.

---

## Design Choices

<details>
<summary><strong>1. IPolicyStore vs Extending IPolicyAdministrationPoint</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Separate `IPolicyStore`** | Clean SRP — PAP is administration logic, store is persistence. Easy to test independently. PAP can add caching, validation, or parent-child tracking on top. Mirrors existing pattern (IOutboxStore ≠ OutboxProcessor). | Extra abstraction layer. Users registering custom persistence must implement IPolicyStore, not IPolicyAdministrationPoint. |
| **B) Extend `IPolicyAdministrationPoint`** directly | Simpler — one interface for consumers. No translation layer between PAP and store. | Mixes administration logic with persistence. The 10-method PAP interface includes parent-child tracking logic that varies by PAP type (in-memory vs persistent). Each provider must reimplement relationship management. |
| **C) Abstract base class `PersistentPolicyAdministrationPointBase`** | Code reuse for common logic. Providers only override database calls. | Inheritance-based, less flexible than composition. Testing harder. Breaks the interface-based patterns used everywhere else. |

### Chosen Option: **A — Separate `IPolicyStore`**

### Rationale
- Matches the established Encina store pattern: `IOutboxStore` is not `IOutboxProcessor`, `ISagaStore` is not `ISagaManager`
- `PersistentPolicyAdministrationPoint` wraps `IPolicyStore` and adds parent-child relationship tracking, just like `InMemoryPolicyAdministrationPoint` does with dictionaries
- Providers implement the simpler `IPolicyStore` (CRUD for serialized policies), not the full PAP contract
- `InMemoryPolicyAdministrationPoint` remains unchanged — no breaking changes
- Users can swap storage backend by registering a different `IPolicyStore` implementation

</details>

<details>
<summary><strong>2. Serialization Strategy for Polymorphic IExpression Trees</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) System.Text.Json with `JsonPolymorphic` attribute** | Built-in .NET 10 support. `$type` discriminator natively. Source-gen compatible. Best performance. | Requires decorating `IExpression` with `[JsonDerivedType]` attributes — couples model to serialization. |
| **B) Custom `JsonConverter<IExpression>`** | Full control over format. No model decoration. Can use clean discriminator names. | More code to maintain. Must handle all expression types manually (but there are only 4). |
| **C) Newtonsoft.Json with `TypeNameHandling`** | Proven, flexible. | Adds dependency. Security risk with type name handling. Not idiomatic for .NET 10. |
| **D) MessagePack / Protobuf binary** | Compact storage, fast. | Not human-readable. Harder to debug. Overkill for policy storage (not a hot path). |

### Chosen Option: **B — Custom `JsonConverter<IExpression>`**

### Rationale
- Keeps the domain model (`IExpression`, `Apply`, `AttributeDesignator`, `AttributeValue`, `VariableReference`) free of serialization attributes
- Only 4 concrete types to handle — a custom converter is trivial and maintainable
- Uses a `"$type"` discriminator field with clean string values (`"apply"`, `"designator"`, `"value"`, `"reference"`)
- Full control over the serialization format (important for cross-version compatibility)
- Encina already uses System.Text.Json elsewhere (`JsonMessageSerializer`) — consistent stack
- `IPolicySerializer` interface allows alternative implementations (e.g., XML for XACML standard compliance in the future)

</details>

<details>
<summary><strong>3. Database Schema: JSON Column vs Normalized Tables</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) JSON column storage (2 tables)** | Simple schema. Policies are read/written as complete graphs. Matches how XACML PAPs work (full policy load). No complex JOINs. Easy migration. | Cannot query individual rules/conditions via SQL. Serialization overhead. |
| **B) Fully normalized (10+ tables)** | SQL-queryable at every level. Maximum flexibility. Standard relational design. | Extreme complexity (PolicySet, Policy, Rule, Target, AnyOf, AllOf, Match, Apply, Obligation, Advice, VariableDefinition). Poor write performance. Difficult migrations. Over-engineered for the read-full-graph access pattern. |
| **C) EF Core `ToJson()` owned entities** | Type-safe with EF Core. Automatic mapping. | **Technical limitation**: EF Core 10 owned entities cannot model polymorphic `IExpression` trees (4 concrete types behind a marker interface) or recursive `PolicySet → PolicySet` self-references. These are fundamental constraints of the owned entity JSON mapping, not merely a provider coherence concern. |

### Chosen Option: **A — JSON column storage (2 tables)**

### Rationale
- XACML PAPs always read complete policy graphs — there's no use case for "get rule #5 from policy X" via SQL
- Metadata columns (`id`, `version`, `description`, `is_enabled`, `priority`, `created_at_utc`, `updated_at_utc`) enable filtering without deserialization
- JSON column works identically across all 13 providers: `TEXT` (SQLite), `NVARCHAR(MAX)` (SQL Server), `JSONB` (PostgreSQL), `JSON` (MySQL), native document (MongoDB)
- 2-table schema is trivial to create, maintain, and migrate
- The serializer handles the complexity — the store just saves/loads strings

> **Note on provider coherence**: "Provider coherence" means all 13 providers implement `IPolicyStore` with the same contract and behavior — it does NOT mean they must use identical internal mechanisms. Each provider may exploit its native capabilities (MongoDB uses BSON, EF Core uses change tracking + LINQ + migrations, etc.). EF Core `ToJson()` owned entities are rejected due to the technical limitations above, not because exploiting EF Core's capabilities would violate coherence.

</details>

<details>
<summary><strong>4. PersistentPAP Caching Strategy — Integrated with Encina Caching</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No caching** | Simple. Always-fresh data. No invalidation logic. | Every PDP evaluation (every authorization check, every HTTP request) triggers database reads. Unacceptable latency in production. Ignores Encina's existing caching infrastructure. |
| **B) Custom `ConcurrentDictionary` cache in PAP** | Simple in-memory cache. Fast. No external dependencies. | Reinvents the wheel — Encina already has 8 cache providers. No distributed invalidation. No stampede protection. No TTL management. Incoherent with the rest of the platform. |
| **C) `CachingPolicyStoreDecorator` using `ICacheProvider` + `IPubSubProvider`** | Fully integrated with Encina's caching ecosystem (Memory, Redis, Hybrid, Valkey, Dragonfly, Garnet, KeyDB, Memcached). Stampede protection via `GetOrSetAsync`. Cross-instance invalidation via `IPubSubProvider`. Tag-based invalidation. Resilient (`ThrowOnCacheErrors = false`). Configurable TTL. Opt-in. | Requires `ICacheProvider` to be registered (user must configure a cache provider). Slightly more complex DI setup. |
| **D) Scoped PAP (per-request, no cache)** | Always-fresh per request. | Breaks Singleton lifetime of `IPolicyAdministrationPoint`. Multiple PDP evaluations per request hit DB multiple times. |

### Chosen Option: **C — `CachingPolicyStoreDecorator` integrated with `ICacheProvider`**

### Rationale

The PDP calls `GetPolicySetsAsync()` and `GetAllStandalonePoliciesAsync()` on **every authorization evaluation**. Without caching, every HTTP request through `ABACPipelineBehavior` generates database queries for policies that change infrequently (hours/days). This is a textbook caching scenario.

Encina already provides a sophisticated caching infrastructure:

| Encina Component | Role in PAP Caching |
|---|---|
| `ICacheProvider` (8 providers) | Store cached policies (Memory for dev, Redis/Hybrid for production) |
| `GetOrSetAsync<T>` | Stampede protection — 100 concurrent requests with cold cache → only 1 DB query |
| `IPubSubProvider` | Broadcast invalidation across all application instances |
| `RemoveByPatternAsync("abac:*")` | Invalidate all ABAC cache entries after policy changes |
| Tag-based invalidation (HybridCache) | `RemoveByTagAsync("abac-policies")` for surgical bulk invalidation |
| `ThrowOnCacheErrors = false` | Resilience — if cache fails, falls back to database transparently |

**Architecture**:

```
IPolicyStore (interface)
├── PolicyStoreEF / PolicyStoreDapper / PolicyStoreADO / PolicyStoreMongo  (inner store)
└── CachingPolicyStoreDecorator (decorator, wraps any inner IPolicyStore)
        ├── Read operations → ICacheProvider.GetOrSetAsync (stampede-safe)
        ├── Write operations → inner.Save/Delete → invalidate cache → publish via PubSub
        └── Fallback → if cache unavailable, delegates directly to inner store
```

**Cache key structure**:
- `abac:policy-set:{id}` — individual policy set
- `abac:policy-sets:all` — all policy sets (for PDP bulk load)
- `abac:policy:{id}` — individual standalone policy
- `abac:policies:all` — all standalone policies (for PDP bulk load)
- Tag: `"abac-policies"` — for bulk invalidation via `RemoveByTagAsync`

**Configuration** (opt-in, coherent with existing `CachingOptions` patterns):

```csharp
services.AddEncinaABAC(options =>
{
    options.UsePersistentPAP = true;
    options.PolicyCaching.Enabled = true;                        // Opt-in (default: false)
    options.PolicyCaching.Duration = TimeSpan.FromMinutes(10);   // TTL
    options.PolicyCaching.EnablePubSubInvalidation = true;       // Multi-instance invalidation
    options.PolicyCaching.InvalidationChannel = "abac:cache:invalidate"; // PubSub channel
    options.PolicyCaching.CacheTag = "abac-policies";            // Tag for bulk invalidation
});
```

**Write-through invalidation flow**:
1. `SavePolicySetAsync(policySet)` → delegate to inner store
2. On success → `cache.RemoveAsync("abac:policy-set:{id}")` + `cache.RemoveAsync("abac:policy-sets:all")`
3. If PubSub enabled → `pubSub.PublishAsync(channel, new PolicyCacheInvalidation { ... })`
4. All subscribers → `cache.RemoveByPatternAsync("abac:*")` or `cache.RemoveByTagAsync("abac-policies")`

**Lifecycle**: `CachingPolicyStoreDecorator` is Singleton (wraps the inner store, caches are thread-safe). When `PolicyCaching.Enabled = false`, the decorator is not registered and `IPolicyStore` resolves directly to the provider implementation.

</details>

<details>
<summary><strong>5. MongoDB Implementation Strategy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Native document storage (no IPolicySerializer)** | MongoDB natively stores documents as BSON. No JSON serialization needed. Maximum performance. Natural fit for document store. | Different code path than relational providers. Must handle `IExpression` polymorphism via MongoDB's BSON serializer instead of System.Text.Json. Requires BSON discriminator configuration. |
| **B) Use IPolicySerializer (JSON string in string field)** | Identical code path as relational providers. Simple implementation. Reuses the same serializer. | Stores JSON as a string inside BSON — double-serialized. Wastes MongoDB's native document capabilities. Cannot use MongoDB queries on policy content. |

### Chosen Option: **A — Native document storage**

### Rationale
- MongoDB is a document database — storing JSON strings in string fields defeats its purpose
- Native BSON storage allows future MongoDB-native queries on policy content (e.g., find policies by target attributes)
- The `PolicyStoreMongo` implementation will use MongoDB's built-in discriminator convention for `IExpression` polymorphism (BSON has native support via `[BsonDiscriminator]`)
- This is consistent with how `OutboxStoreMongoDB` already stores messages natively, not as serialized JSON strings
- The `IPolicySerializer` is used only by relational providers (ADO, Dapper, EF Core) — MongoDB bypasses it

</details>

<details>
<summary><strong>6. Seeding Strategy for Persistent Store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend existing `ABACPolicySeedingHostedService`** | Reuses existing code. Single seeding path. Checks if store is empty before seeding. | Seeding service currently writes to `IPolicyAdministrationPoint` — works for both InMemory and Persistent since both implement the same interface. No change needed. |
| **B) Separate `PersistentPolicySeedingHostedService`** | Could use `IPolicyStore` directly for bulk insert. | Duplicates seeding logic. Two services to maintain. Unnecessary when the existing service already works with any PAP implementation. |

### Chosen Option: **A — Reuse existing `ABACPolicySeedingHostedService` unchanged**

### Rationale
- The existing `ABACPolicySeedingHostedService` calls `IPolicyAdministrationPoint.AddPolicySetAsync()` and `AddPolicyAsync()`
- `PersistentPolicyAdministrationPoint` implements `IPolicyAdministrationPoint` — the seeding service works without modification
- Seed policies are added only if they don't already exist (the PAP returns `DuplicatePolicySet` error which the seeder logs as a warning and skips)
- For persistent stores, seeds are inserted on first startup and survive restarts — the duplicate check prevents re-insertion

</details>

---

## Implementation Phases

### Phase 1: Policy Serializer — Core Abstraction & Implementation

> **Goal**: Create `IPolicySerializer` and `DefaultPolicySerializer` with full polymorphic `IExpression` support.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Security.ABAC/`

1. **`Persistence/IPolicySerializer.cs`** — Interface for serialization
   - Namespace: `Encina.Security.ABAC.Persistence`
   - Methods:
     - `string SerializePolicySet(PolicySet policySet)`
     - `string SerializePolicy(Policy policy)`
     - `Either<EncinaError, PolicySet> DeserializePolicySet(string json)`
     - `Either<EncinaError, Policy> DeserializePolicy(string json)`

2. **`Persistence/Converters/ExpressionJsonConverter.cs`** — Custom converter for `IExpression`
   - Namespace: `Encina.Security.ABAC.Persistence.Converters`
   - Handles 4 types: `Apply`, `AttributeDesignator`, `AttributeValue`, `VariableReference`
   - Uses `"$type"` discriminator: `"apply"`, `"designator"`, `"value"`, `"reference"`
   - Recursive handling for `Apply.Arguments` (list of `IExpression`)

3. **`Persistence/Converters/EffectJsonConverter.cs`** — Enum-to-string converter for `Effect`

4. **`Persistence/Converters/CombiningAlgorithmIdJsonConverter.cs`** — Enum-to-string converter for `CombiningAlgorithmId`

5. **`Persistence/Converters/AttributeCategoryJsonConverter.cs`** — Enum-to-string converter for `AttributeCategory`

6. **`Persistence/Converters/FulfillOnJsonConverter.cs`** — Enum-to-string converter for `FulfillOn`

7. **`Persistence/DefaultPolicySerializer.cs`** — Default implementation using System.Text.Json
   - Namespace: `Encina.Security.ABAC.Persistence`
   - Constructor: parameterless (creates `JsonSerializerOptions` internally)
   - Registers all custom converters
   - Options: `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, `WriteIndented = false`
   - Deserialization wraps in `try/catch` → returns `Either<EncinaError, T>` via `ABACErrors`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of the Persistent PAP feature for Encina.Security.ABAC.

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- The ABAC module models XACML 3.0 policies with a deep object graph
- IExpression is a marker interface with 4 implementations: Apply, AttributeDesignator, AttributeValue, VariableReference
- These types form recursive expression trees (Apply.Arguments contains IExpression[])
- Enums: Effect, CombiningAlgorithmId, AttributeCategory, FulfillOn, ConditionOperator
- Records: Policy, PolicySet, Rule, Target, AnyOf, AllOf, Match, Obligation, AdviceExpression, VariableDefinition

TASK:
Create IPolicySerializer interface and DefaultPolicySerializer implementation in src/Encina.Security.ABAC/Persistence/.

1. IPolicySerializer interface with 4 methods (Serialize/Deserialize for PolicySet and Policy)
2. ExpressionJsonConverter — custom JsonConverter<IExpression> using "$type" discriminator
   - "apply" → Apply (has FunctionId + Arguments list of IExpression — recursive)
   - "designator" → AttributeDesignator (Category, AttributeId, DataType, MustBePresent)
   - "value" → AttributeValue (DataType, Value as object)
   - "reference" → VariableReference (VariableId)
3. Enum converters for Effect, CombiningAlgorithmId, AttributeCategory, FulfillOn (string serialization)
4. DefaultPolicySerializer using System.Text.Json with all converters registered

KEY RULES:
- .NET 10, C# 14, nullable enabled
- Deserialization returns Either<EncinaError, T> — never throws
- Use ABACErrors for error creation (add new error codes if needed: "abac.serialization_failed", "abac.deserialization_failed")
- PropertyNamingPolicy = CamelCase, WriteIndented = false
- XML doc comments on all public types and methods
- No [Obsolete] attributes
- Sealed classes where possible

REFERENCE FILES:
- src/Encina.Security.ABAC/Model/IExpression.cs (marker interface)
- src/Encina.Security.ABAC/Model/Apply.cs (FunctionId + Arguments)
- src/Encina.Security.ABAC/Model/AttributeDesignator.cs (Category, AttributeId, DataType, MustBePresent)
- src/Encina.Security.ABAC/Model/AttributeValue.cs (DataType, Value)
- src/Encina.Security.ABAC/Model/VariableReference.cs (VariableId)
- src/Encina.Security.ABAC/Model/Policy.cs (sealed record)
- src/Encina.Security.ABAC/Model/PolicySet.cs (sealed record, recursive PolicySets)
- src/Encina.Security.ABAC/Model/Rule.cs
- src/Encina.Security.ABAC/Model/Target.cs
- src/Encina.Security.ABAC/Model/Match.cs
- src/Encina.Security.ABAC/Model/AllOf.cs
- src/Encina.Security.ABAC/Model/AnyOf.cs
- src/Encina.Security.ABAC/Model/Obligation.cs
- src/Encina.Security.ABAC/Model/AdviceExpression.cs
- src/Encina.Security.ABAC/Model/VariableDefinition.cs
- src/Encina.Security.ABAC/Model/Effect.cs (enum)
- src/Encina.Security.ABAC/Model/CombiningAlgorithmId.cs (enum)
- src/Encina.Security.ABAC/Model/AttributeCategory.cs (enum)
- src/Encina.Security.ABAC/Model/FulfillOn.cs (enum)
- src/Encina.Security.ABAC/ABACErrors.cs (error factory)
```

</details>

---

### Phase 2: IPolicyStore Interface & Persistence Entities

> **Goal**: Define the persistence contract and shared database entities.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Security.ABAC/`

1. **`Persistence/IPolicyStore.cs`** — Store interface
   - Namespace: `Encina.Security.ABAC.Persistence`
   - Methods (all return `ValueTask<Either<EncinaError, T>>`):
     - `GetAllPolicySetsAsync(CancellationToken)` → `IReadOnlyList<PolicySet>`
     - `GetPolicySetAsync(string policySetId, CancellationToken)` → `Option<PolicySet>`
     - `SavePolicySetAsync(PolicySet policySet, CancellationToken)` → `Unit` (upsert)
     - `DeletePolicySetAsync(string policySetId, CancellationToken)` → `Unit`
     - `ExistsPolicySetAsync(string policySetId, CancellationToken)` → `bool`
     - `GetAllStandalonePoliciesAsync(CancellationToken)` → `IReadOnlyList<Policy>`
     - `GetPolicyAsync(string policyId, CancellationToken)` → `Option<Policy>`
     - `SavePolicyAsync(Policy policy, CancellationToken)` → `Unit` (upsert)
     - `DeletePolicyAsync(string policyId, CancellationToken)` → `Unit`
     - `ExistsPolicyAsync(string policyId, CancellationToken)` → `bool`
     - `GetPolicySetCountAsync(CancellationToken)` → `int`
     - `GetPolicyCountAsync(CancellationToken)` → `int`

2. **`Persistence/PolicySetEntity.cs`** — Database entity for policy sets
   - Namespace: `Encina.Security.ABAC.Persistence`
   - Properties: `Id` (string), `Version` (string?), `Description` (string?), `PolicyJson` (string), `IsEnabled` (bool), `Priority` (int), `CreatedAtUtc` (DateTime), `UpdatedAtUtc` (DateTime)

3. **`Persistence/PolicyEntity.cs`** — Database entity for standalone policies
   - Same properties as `PolicySetEntity`

4. **`Persistence/PolicyEntityMapper.cs`** — Maps between domain models and entities
   - `ToPolicySetEntity(PolicySet, IPolicySerializer, TimeProvider)` → `PolicySetEntity`
   - `ToPolicySet(PolicySetEntity, IPolicySerializer)` → `Either<EncinaError, PolicySet>`
   - `ToPolicyEntity(Policy, IPolicySerializer, TimeProvider)` → `PolicyEntity`
   - `ToPolicy(PolicyEntity, IPolicySerializer)` → `Either<EncinaError, Policy>`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of the Persistent PAP feature for Encina.Security.ABAC.

CONTEXT:
- Phase 1 created IPolicySerializer and DefaultPolicySerializer for JSON serialization
- Now we create the persistence layer: IPolicyStore interface and shared entities
- The store is the database abstraction; the PAP wraps it with administration logic
- 13 providers will implement IPolicyStore (ADO ×4, Dapper ×4, EF Core ×4, MongoDB)
- Store methods use ROP pattern: ValueTask<Either<EncinaError, T>>

TASK:
1. Create IPolicyStore interface in src/Encina.Security.ABAC/Persistence/IPolicyStore.cs
   - CRUD for PolicySets and standalone Policies
   - SavePolicySetAsync/SavePolicyAsync use UPSERT semantics (insert if new, update if exists)
   - Count methods for health check support
   - All methods return ValueTask<Either<EncinaError, T>>

2. Create PolicySetEntity and PolicyEntity in src/Encina.Security.ABAC/Persistence/
   - Simple POCOs with string Id, metadata columns, PolicyJson string
   - Used by relational providers (ADO, Dapper, EF Core)
   - MongoDB will use its own BSON documents

3. Create PolicyEntityMapper in src/Encina.Security.ABAC/Persistence/
   - Bidirectional mapping between domain models (Policy, PolicySet) and entities
   - Uses IPolicySerializer for JSON conversion
   - Uses TimeProvider for timestamp generation
   - Returns Either<EncinaError, T> for deserialization (can fail)

KEY RULES:
- .NET 10, C# 14, nullable enabled
- ROP: Either<EncinaError, T> on all methods that can fail
- Option<T> for methods that may return nothing (GetPolicySetAsync, GetPolicyAsync)
- Entities are simple POCOs — no attributes, no inheritance
- XML doc comments on all public types and methods
- Follow existing store patterns (IOutboxStore, ISagaStore)

REFERENCE FILES:
- src/Encina/Messaging/Outbox/IOutboxStore.cs (store interface pattern)
- src/Encina/Messaging/Sagas/ISagaStore.cs (store interface pattern)
- src/Encina.Security.ABAC/Model/Policy.cs (domain model)
- src/Encina.Security.ABAC/Model/PolicySet.cs (domain model)
- src/Encina.Security.ABAC/Persistence/IPolicySerializer.cs (from Phase 1)
```

</details>

---

### Phase 3: PersistentPolicyAdministrationPoint

> **Goal**: Implement the persistent PAP that delegates to `IPolicyStore`.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Security.ABAC/`

1. **`Administration/PersistentPolicyAdministrationPoint.cs`**
   - Namespace: `Encina.Security.ABAC.Administration`
   - Implements: `IPolicyAdministrationPoint`
   - Constructor dependencies: `IPolicyStore`, `ILogger<PersistentPolicyAdministrationPoint>`
   - Delegates all 10 IPolicyAdministrationPoint methods to IPolicyStore
   - Handles parent-child relationship tracking (policy → policy set) via the store
   - Uses structured logging for all operations
   - Sealed class

   Key method mapping:
   - `AddPolicySetAsync` → `store.ExistsPolicySetAsync` (duplicate check) + `store.SavePolicySetAsync`
   - `UpdatePolicySetAsync` → `store.ExistsPolicySetAsync` (existence check) + `store.SavePolicySetAsync`
   - `RemovePolicySetAsync` → `store.DeletePolicySetAsync`
   - `GetPolicySetsAsync` → `store.GetAllPolicySetsAsync`
   - `GetPolicySetAsync` → `store.GetPolicySetAsync`
   - `AddPolicyAsync(policy, parentPolicySetId)` → If parent specified, loads parent PolicySet, adds policy to its Policies list, saves updated PolicySet. If standalone, saves via `store.SavePolicyAsync`
   - `UpdatePolicyAsync` → Similar logic (update in parent PolicySet or standalone)
   - `RemovePolicyAsync` → Remove from parent PolicySet or standalone store
   - `GetPoliciesAsync(parentPolicySetId?)` → If parent specified, load PolicySet and extract Policies. If null, load standalone policies.
   - `GetPolicyAsync` → Search standalone policies first, then search across PolicySets

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of the Persistent PAP feature for Encina.Security.ABAC.

CONTEXT:
- Phase 1 created IPolicySerializer + DefaultPolicySerializer
- Phase 2 created IPolicyStore interface + entities + mapper
- The InMemoryPolicyAdministrationPoint uses 3 ConcurrentDictionaries:
  _policySets, _standalonePolicies, _policyToParent
- PersistentPAP must replicate this behavior using IPolicyStore
- Key complexity: policies can be standalone OR nested inside PolicySets
  - AddPolicyAsync(policy, parentPolicySetId=null) → standalone
  - AddPolicyAsync(policy, parentPolicySetId="ps1") → nested in PolicySet "ps1"
  - When nested, the policy is stored INSIDE the PolicySet's JSON (as part of PolicySet.Policies)
  - When standalone, the policy is stored in the abac_policies table

TASK:
Create PersistentPolicyAdministrationPoint in src/Encina.Security.ABAC/Administration/

It implements IPolicyAdministrationPoint (same 10 methods as InMemoryPolicyAdministrationPoint).
It delegates to IPolicyStore for persistence.
It must handle parent-child relationships:
- When adding a policy with a parentPolicySetId:
  1. Load the parent PolicySet from store
  2. Create a new PolicySet record with the policy added to its Policies list
  3. Save the updated PolicySet back to store
- When getting policies for a parent:
  1. Load the parent PolicySet
  2. Return its Policies list
- When getting a specific policy:
  1. Check standalone policies first
  2. If not found, search through all PolicySets' Policies lists

KEY RULES:
- Sealed class, .NET 10, C# 14
- All methods return ValueTask<Either<EncinaError, T>>
- Use ABACErrors factory methods for error creation
- Constructor guard clauses (ArgumentNullException.ThrowIfNull)
- Structured logging via ILogger (Debug for success, Warning for not-found)
- XML doc comments

REFERENCE FILES:
- src/Encina.Security.ABAC/Administration/InMemoryPolicyAdministrationPoint.cs (reference implementation)
- src/Encina.Security.ABAC/Abstractions/IPolicyAdministrationPoint.cs (interface contract)
- src/Encina.Security.ABAC/Persistence/IPolicyStore.cs (from Phase 2)
- src/Encina.Security.ABAC/ABACErrors.cs (error factory)
```

</details>

---

### Phase 4: Configuration & DI Registration

> **Goal**: Wire up persistent PAP via `ABACOptions` and `ServiceCollectionExtensions`.

<details>
<summary><strong>Tasks</strong></summary>

#### Modified files

1. **`src/Encina.Security.ABAC/ABACOptions.cs`** — Add persistent PAP configuration
   - New property: `bool UsePersistentPAP { get; set; }` (default: `false`)
   - When `true`, `PersistentPolicyAdministrationPoint` is registered instead of `InMemoryPolicyAdministrationPoint`
   - Requires `IPolicyStore` to be registered by a provider package
   - New nested class: `PolicyCachingOptions`
     - `bool Enabled { get; set; }` (default: `false`)
     - `TimeSpan Duration { get; set; }` (default: `TimeSpan.FromMinutes(10)`)
     - `bool EnablePubSubInvalidation { get; set; }` (default: `true`)
     - `string InvalidationChannel { get; set; }` (default: `"abac:cache:invalidate"`)
     - `string CacheTag { get; set; }` (default: `"abac-policies"`)
     - `string CacheKeyPrefix { get; set; }` (default: `"abac"`)
   - New property: `PolicyCachingOptions PolicyCaching { get; }` (initialized inline)

2. **`src/Encina.Security.ABAC/ServiceCollectionExtensions.cs`** — Conditional PAP registration
   - When `UsePersistentPAP = true`:
     - Register `IPolicySerializer` → `DefaultPolicySerializer` (Singleton, TryAdd)
     - Register `IPolicyAdministrationPoint` → `PersistentPolicyAdministrationPoint` (Singleton)
     - Do NOT register `IPolicyStore` — this comes from the provider package
   - When `PolicyCaching.Enabled = true`:
     - Register `CachingPolicyStoreDecorator` wrapping the inner `IPolicyStore`
     - Pattern: `services.Decorate<IPolicyStore, CachingPolicyStoreDecorator>()` or manual factory
     - Requires `ICacheProvider` to be registered (from Encina.Caching.*)
   - When `UsePersistentPAP = false` (default):
     - Keep existing `InMemoryPolicyAdministrationPoint` registration

3. **Provider `ServiceCollectionExtensions` updates** (each provider package):
   - Add `bool UseABACPolicyStore { get; set; }` to each provider's configuration class
   - When `true`, register `IPolicyStore` → provider-specific implementation
   - Pattern: `services.TryAddScoped<IPolicyStore, PolicyStoreEF>()` (or Singleton for ADO/Dapper)

#### New/Modified files

4. **`src/Encina.Security.ABAC/ABACErrors.cs`** — Add new error codes
   - `SerializationFailed(string detail)` → `"abac.serialization_failed"`
   - `DeserializationFailed(string detail)` → `"abac.deserialization_failed"`
   - `StoreOperationFailed(string operation, string detail)` → `"abac.store_operation_failed"`
   - `PersistentStoreNotRegistered()` → `"abac.persistent_store_not_registered"`
   - `CacheProviderNotRegistered()` → `"abac.cache_provider_not_registered"`

5. **`src/Encina.Security.ABAC/Persistence/PolicyCacheInvalidationMessage.cs`** — PubSub message
   - Namespace: `Encina.Security.ABAC.Persistence`
   - Record: `PolicyCacheInvalidationMessage(string EntityType, string? EntityId, string Operation, DateTime TimestampUtc)`
   - Serializable via System.Text.Json for cross-instance broadcasting

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of the Persistent PAP feature for Encina.Security.ABAC.

CONTEXT:
- Phases 1-3 created: IPolicySerializer, IPolicyStore, PersistentPolicyAdministrationPoint
- Now we need to wire them into the DI container
- Current registration: services.TryAddSingleton<IPolicyAdministrationPoint, InMemoryPolicyAdministrationPoint>()
- New behavior: when UsePersistentPAP = true, register PersistentPAP instead
- IPolicyStore is registered by provider packages (EF Core, Dapper, ADO, MongoDB)

TASK:
1. Add UsePersistentPAP property and PolicyCachingOptions nested class to ABACOptions
2. Update ServiceCollectionExtensions.AddEncinaABAC():
   - If UsePersistentPAP = true:
     - TryAddSingleton<IPolicySerializer, DefaultPolicySerializer>()
     - Replace InMemoryPAP with PersistentPAP (not TryAdd — must override)
   - If PolicyCaching.Enabled = true:
     - Decorate IPolicyStore with CachingPolicyStoreDecorator
     - Validate ICacheProvider is registered (log warning if not)
   - If UsePersistentPAP = false: keep existing InMemory registration
3. Add new error codes to ABACErrors.cs for serialization, store, and cache failures
4. Create PolicyCacheInvalidationMessage record for PubSub broadcasting
5. Add startup validation: if UsePersistentPAP=true but no IPolicyStore registered, log a warning

KEY RULES:
- TryAdd pattern for serializer (allows user override)
- PersistentPAP must be Singleton (same lifetime as InMemoryPAP)
- Provider packages will add their own IPolicyStore registrations
- PolicyCachingOptions follows same conventions as CachingOptions (EnablePubSubInvalidation, Duration, KeyPrefix)
- ABACOptions configuration happens via Action<ABACOptions> configure delegate
- XML doc comments on new properties

REFERENCE FILES:
- src/Encina.Security.ABAC/ABACOptions.cs (current options)
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs (current DI)
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (provider DI pattern)
- src/Encina.Security.ABAC/ABACErrors.cs (error factory)
- src/Encina.Caching/CachingOptions.cs (caching options pattern)
- src/Encina.Caching/ICacheProvider.cs (cache provider interface)
```

</details>

---

### Phase 5: CachingPolicyStoreDecorator

> **Goal**: Implement `CachingPolicyStoreDecorator` that integrates with Encina's `ICacheProvider` and `IPubSubProvider` for production-grade policy caching.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Security.ABAC/`

1. **`Persistence/CachingPolicyStoreDecorator.cs`**
   - Namespace: `Encina.Security.ABAC.Persistence`
   - Implements: `IPolicyStore`
   - Sealed class
   - Constructor dependencies:
     - `IPolicyStore innerStore` — the actual database-backed store
     - `ICacheProvider cacheProvider` — from Encina.Caching (Memory, Redis, Hybrid, etc.)
     - `IPubSubProvider? pubSubProvider` — optional, for cross-instance invalidation
     - `IOptions<ABACOptions> options` — for `PolicyCaching.*` configuration
     - `ILogger<CachingPolicyStoreDecorator> logger`
     - `TimeProvider? timeProvider`
   - **Read methods** (cache-aside with stampede protection):
     - `GetAllPolicySetsAsync` → `cacheProvider.GetOrSetAsync("abac:policy-sets:all", () => innerStore.GetAllPolicySetsAsync(...), options.PolicyCaching.Duration)`
     - `GetPolicySetAsync(id)` → `cacheProvider.GetOrSetAsync($"abac:policy-set:{id}", () => innerStore.GetPolicySetAsync(id, ...), options.PolicyCaching.Duration)`
     - Same pattern for Policy methods
     - Count methods → delegate directly (cheap, used by health checks only)
   - **Write methods** (write-through + invalidation):
     - `SavePolicySetAsync(policySet)`:
       1. `await innerStore.SavePolicySetAsync(policySet, ct)` — persist first
       2. On success: `await cacheProvider.RemoveAsync($"abac:policy-set:{policySet.Id}")` — invalidate specific
       3. `await cacheProvider.RemoveAsync("abac:policy-sets:all")` — invalidate bulk cache
       4. If PubSub enabled: `await pubSubProvider.PublishAsync(channel, invalidationMessage)`
     - Same pattern for Delete, SavePolicy, DeletePolicy
   - **PubSub subscription** (initialized at construction or via `IHostedService`):
     - Subscribe to `options.PolicyCaching.InvalidationChannel`
     - On message: `cacheProvider.RemoveByPatternAsync("abac:*")` or `RemoveByTagAsync("abac-policies")`
   - **Resilience**: All cache operations wrapped in try/catch — on failure, delegate to inner store (log warning, never throw)

2. **`Persistence/PolicyCachePubSubHostedService.cs`**
   - Namespace: `Encina.Security.ABAC.Persistence`
   - Implements: `IHostedService`
   - Subscribes to PubSub channel at startup for cache invalidation
   - Constructor: `ICacheProvider`, `IPubSubProvider`, `IOptions<ABACOptions>`, `ILogger`
   - `StartAsync`: Subscribe to invalidation channel
   - `StopAsync`: Unsubscribe, dispose subscription
   - Only registered when `PolicyCaching.EnablePubSubInvalidation = true` AND `IPubSubProvider` is available

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of the Persistent PAP feature: CachingPolicyStoreDecorator.

CONTEXT:
- Phases 1-4 created: IPolicySerializer, IPolicyStore, PersistentPAP, DI wiring
- Encina has a sophisticated caching system with 8 providers (Memory, Hybrid, Redis, Valkey, etc.)
- ICacheProvider provides: GetAsync, SetAsync, RemoveAsync, RemoveByPatternAsync, GetOrSetAsync (stampede-safe)
- IPubSubProvider provides: PublishAsync, SubscribeAsync for cross-instance communication
- CachingPolicyStoreDecorator wraps any IPolicyStore and adds caching transparently
- This is the decorator pattern — same interface, added behavior

TASK:
1. Create CachingPolicyStoreDecorator in src/Encina.Security.ABAC/Persistence/
   - Implements IPolicyStore (decorates the inner store)
   - Read methods: cache-aside via ICacheProvider.GetOrSetAsync (stampede protection built-in)
   - Write methods: write-through → invalidate local cache → publish PubSub invalidation
   - Count methods: delegate directly (no cache — used by health checks)
   - All cache operations resilient: catch exceptions → log → fallback to inner store

2. Create PolicyCachePubSubHostedService for cross-instance invalidation subscription
   - Subscribe to PolicyCaching.InvalidationChannel
   - On invalidation message: RemoveByPatternAsync("abac:*")
   - Only active when EnablePubSubInvalidation = true

CACHE KEY STRUCTURE:
- "abac:policy-sets:all" → all policy sets (bulk load for PDP)
- "abac:policy-set:{id}" → individual policy set
- "abac:policies:all" → all standalone policies
- "abac:policy:{id}" → individual policy
- Tag: "abac-policies" → for RemoveByTagAsync (HybridCache)

INVALIDATION FLOW:
Save/Delete → 1) persist to DB → 2) remove specific cache key → 3) remove "all" cache key
→ 4) publish to PubSub channel → 5) all subscribers invalidate local cache

KEY RULES:
- .NET 10, C# 14, sealed class
- Decorator pattern: same IPolicyStore interface
- GetOrSetAsync provides stampede protection (concurrent cold-cache requests → 1 DB query)
- Cache errors NEVER break the application — fallback to inner store
- PubSub is optional (IPubSubProvider? nullable)
- Duration from ABACOptions.PolicyCaching.Duration
- Follow Encina caching conventions: CachingOptions, ICacheProvider, GetOrSetAsync<T>
- XML doc comments

REFERENCE FILES:
- src/Encina.Caching/ICacheProvider.cs (cache interface)
- src/Encina.Caching/IPubSubProvider.cs (pub/sub interface)
- src/Encina.Caching/CachingOptions.cs (configuration pattern)
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs (cache-aside pattern)
- src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs (invalidation pattern)
- src/Encina.Security.ABAC/Persistence/IPolicyStore.cs (interface to implement)
- src/Encina.Security.ABAC/ABACOptions.cs (PolicyCachingOptions from Phase 4)
```

</details>

---

### Phase 6: SQL Scripts & EF Core Entity Configuration

> **Goal**: Create schema scripts for all relational providers and EF Core entity mappings.

<details>
<summary><strong>Tasks</strong></summary>

#### SQL Scripts (ADO.NET & Dapper — shared per database)

1. **SQLite** — `src/Encina.ADO.Sqlite/Scripts/CreateABACPolicyTables.sql`
   ```sql
   CREATE TABLE IF NOT EXISTS abac_policy_sets (
       Id TEXT NOT NULL PRIMARY KEY,
       Version TEXT,
       Description TEXT,
       PolicyJson TEXT NOT NULL,
       IsEnabled INTEGER NOT NULL DEFAULT 1,
       Priority INTEGER NOT NULL DEFAULT 0,
       CreatedAtUtc TEXT NOT NULL,
       UpdatedAtUtc TEXT NOT NULL
   );
   CREATE TABLE IF NOT EXISTS abac_policies (...same columns...);
   ```

2. **SQL Server** — `src/Encina.ADO.SqlServer/Scripts/CreateABACPolicyTables.sql`
   - `NVARCHAR(256)` for Id, `NVARCHAR(MAX)` for PolicyJson, `DATETIME2(7)` for timestamps, `BIT` for IsEnabled

3. **PostgreSQL** — `src/Encina.ADO.PostgreSQL/Scripts/CreateABACPolicyTables.sql`
   - `VARCHAR(256)` for Id, `JSONB` for PolicyJson, `TIMESTAMPTZ` for timestamps, `BOOLEAN` for IsEnabled

4. **MySQL** — `src/Encina.ADO.MySql/Scripts/CreateABACPolicyTables.sql`
   - Backtick identifiers, `JSON` column type, `DATETIME(6)` for timestamps, `TINYINT(1)` for IsEnabled

#### EF Core Entity Configuration

5. **`src/Encina.EntityFrameworkCore/ABAC/PolicySetEntityConfiguration.cs`**
   - `IEntityTypeConfiguration<PolicySetEntity>`
   - Table name: `abac_policy_sets`
   - Key: `Id`, MaxLength(256)
   - `PolicyJson`: `IsRequired()`, no MaxLength (MAX/TEXT)
   - Indexes: `IX_abac_policy_sets_IsEnabled` on (`IsEnabled`, `Priority`)

6. **`src/Encina.EntityFrameworkCore/ABAC/PolicyEntityConfiguration.cs`**
   - Same pattern as PolicySetEntity but table `abac_policies`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of the Persistent PAP feature for Encina.Security.ABAC.

CONTEXT:
- 2-table schema: abac_policy_sets and abac_policies
- Both tables have the same columns: Id, Version, Description, PolicyJson, IsEnabled, Priority, CreatedAtUtc, UpdatedAtUtc
- PolicyJson stores the full serialized policy graph (JSON)
- Provider-specific SQL differences are critical (see table below)

PROVIDER DIFFERENCES:
| Provider   | String PK   | JSON Column      | DateTime        | Boolean    | Identifiers |
|------------|-------------|------------------|-----------------|------------|-------------|
| SQLite     | TEXT        | TEXT             | TEXT (ISO 8601) | INTEGER    | No quotes   |
| SQL Server | NVARCHAR(256)| NVARCHAR(MAX)   | DATETIME2(7)    | BIT        | [brackets]  |
| PostgreSQL | VARCHAR(256)| JSONB            | TIMESTAMPTZ     | BOOLEAN    | Double quotes|
| MySQL      | VARCHAR(256)| JSON             | DATETIME(6)     | TINYINT(1) | `backticks` |

TASK:
1. Create SQL scripts for 4 database types in src/Encina.ADO.{Provider}/Scripts/
2. Create EF Core entity configurations in src/Encina.EntityFrameworkCore/ABAC/
3. Scripts use CREATE TABLE IF NOT EXISTS (or equivalent per provider)
4. Add appropriate indexes

KEY RULES:
- SQLite: TEXT for all columns, ISO 8601 dates, INTEGER for booleans
- SQL Server: Use NVARCHAR, DATETIME2(7), BIT
- PostgreSQL: Use JSONB (not JSON) for PolicyJson — enables future query support
- MySQL: Use JSON column type, backtick identifiers
- EF Core: Use IEntityTypeConfiguration<T>, configure via OnModelCreating
- Scripts are embedded resources (set in .csproj)

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scripts/001_CreateOutboxMessagesTable.sql (SQL Server pattern)
- src/Encina.ADO.Sqlite/Scripts/001_CreateOutboxMessagesTable.sql (SQLite pattern)
- src/Encina.ADO.PostgreSQL/Scripts/001_CreateOutboxMessagesTable.sql (PostgreSQL pattern)
- src/Encina.ADO.MySQL/Scripts/001_CreateOutboxMessagesTable.sql (MySQL pattern)
- src/Encina.EntityFrameworkCore/Outbox/OutboxMessageConfiguration.cs (EF config pattern)
- src/Encina.Security.ABAC/Persistence/PolicySetEntity.cs (from Phase 2)
- src/Encina.Security.ABAC/Persistence/PolicyEntity.cs (from Phase 2)
```

</details>

---

### Phase 7: ADO.NET Provider Implementations (4 providers)

> **Goal**: Implement `PolicyStoreADO` for SQLite, SqlServer, PostgreSQL, MySQL.

<details>
<summary><strong>Tasks</strong></summary>

#### New files (4 per provider × 4 providers = patterns, but PolicyStoreADO is 1 class per provider)

1. **`src/Encina.ADO.Sqlite/ABAC/PolicyStoreADO.cs`**
   - Namespace: `Encina.ADO.Sqlite.ABAC`
   - Implements: `IPolicyStore`
   - Constructor: `IDbConnection`, `IPolicySerializer`, `TimeProvider?`, `string tablePrefixName = "abac_"`
   - SQLite-specific: `TEXT` for all types, parameterized `@NowUtc` (never `datetime('now')`)
   - Uses `SqlIdentifierValidator.ValidateTableName()` for table prefix

2. **`src/Encina.ADO.SqlServer/ABAC/PolicyStoreADO.cs`**
   - SQL Server-specific: `TOP (@N)`, `NVARCHAR(MAX)`, `DATETIME2`

3. **`src/Encina.ADO.PostgreSQL/ABAC/PolicyStoreADO.cs`**
   - PostgreSQL-specific: `LIMIT @N`, `JSONB` column

4. **`src/Encina.ADO.MySql/ABAC/PolicyStoreADO.cs`**
   - MySQL-specific: backtick identifiers, `JSON` column

#### Common ADO.NET implementation pattern

Each `PolicyStoreADO` follows the existing `OutboxStoreADO` pattern:
- Creates `IDbCommand` with parameterized SQL
- Uses `AddParameter(command, "@Name", value)` helper
- Maps results via `IDataReader` with `GetOrdinal()` + typed getters
- Wraps in `EitherHelpers.TryAsync()` for ROP
- Opens connection if not already open
- UPSERT via `INSERT ... ON CONFLICT` (SQLite/PostgreSQL) or `MERGE` (SQL Server) or `INSERT ... ON DUPLICATE KEY UPDATE` (MySQL)

#### DI Registration updates

5. **`src/Encina.ADO.Sqlite/ServiceCollectionExtensions.cs`** — Add `UseABACPolicyStore` flag
6. **`src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs`** — Same
7. **`src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs`** — Same
8. **`src/Encina.ADO.MySql/ServiceCollectionExtensions.cs`** — Same

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of the Persistent PAP feature: ADO.NET PolicyStore implementations.

CONTEXT:
- IPolicyStore interface created in Phase 2 with CRUD operations for PolicySets and Policies
- PolicySetEntity / PolicyEntity are the database POCOs (Phase 2)
- PolicyEntityMapper converts between domain models and entities using IPolicySerializer (Phase 2)
- SQL scripts for each provider created in Phase 6
- ADO.NET pattern: IDbConnection + IDbCommand + parameterized SQL + IDataReader

TASK:
Create PolicyStoreADO class in each of the 4 ADO.NET provider packages:
- src/Encina.ADO.Sqlite/ABAC/PolicyStoreADO.cs
- src/Encina.ADO.SqlServer/ABAC/PolicyStoreADO.cs
- src/Encina.ADO.PostgreSQL/ABAC/PolicyStoreADO.cs
- src/Encina.ADO.MySql/ABAC/PolicyStoreADO.cs

Each implements IPolicyStore with provider-specific SQL.

Update ServiceCollectionExtensions in each provider to register PolicyStoreADO when UseABACPolicyStore = true.

KEY RULES:
- Constructor: IDbConnection, IPolicySerializer, TimeProvider? (defaults to System)
- All methods wrapped in EitherHelpers.TryAsync()
- SavePolicySetAsync/SavePolicyAsync use UPSERT semantics
- SQLite: NEVER use datetime('now') — always @NowUtc parameter
- SQL Server: TOP (@BatchSize), MERGE for upsert
- PostgreSQL: INSERT ... ON CONFLICT DO UPDATE
- MySQL: INSERT ... ON DUPLICATE KEY UPDATE, backtick identifiers
- Use SqlIdentifierValidator.ValidateTableName() on table names
- Map between entities and domain models using PolicyEntityMapper

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs (ADO.NET store pattern)
- src/Encina.ADO.Sqlite/Outbox/OutboxStoreADO.cs (SQLite ADO.NET pattern)
- src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs (PostgreSQL ADO.NET pattern)
- src/Encina.ADO.MySql/Outbox/OutboxStoreADO.cs (MySQL ADO.NET pattern)
- src/Encina.Security.ABAC/Persistence/IPolicyStore.cs
- src/Encina.Security.ABAC/Persistence/PolicyEntityMapper.cs
```

</details>

---

### Phase 8: Dapper Provider Implementations (4 providers)

> **Goal**: Implement `PolicyStoreDapper` for SQLite, SqlServer, PostgreSQL, MySQL.

<details>
<summary><strong>Tasks</strong></summary>

#### New files

1. **`src/Encina.Dapper.Sqlite/ABAC/PolicyStoreDapper.cs`**
2. **`src/Encina.Dapper.SqlServer/ABAC/PolicyStoreDapper.cs`**
3. **`src/Encina.Dapper.PostgreSQL/ABAC/PolicyStoreDapper.cs`**
4. **`src/Encina.Dapper.MySQL/ABAC/PolicyStoreDapper.cs`**

Each implements `IPolicyStore` using Dapper's `QueryAsync<T>()` and `ExecuteAsync()`.

#### Common Dapper pattern

- Constructor: `IDbConnection`, `IPolicySerializer`, `TimeProvider?`, `string tablePrefix = "abac_"`
- Uses Dapper extensions for SQL: `connection.QueryAsync<PolicySetEntity>(sql, params)`
- Maps results via Dapper's POCO convention
- Uses `PolicyEntityMapper` for domain ↔ entity conversion
- UPSERT via provider-specific SQL (same as ADO.NET Phase 7)
- Wraps in `EitherHelpers.TryAsync()`

#### DI Registration updates

5–8. Update `ServiceCollectionExtensions.cs` in each Dapper provider package.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of the Persistent PAP feature: Dapper PolicyStore implementations.

CONTEXT:
- Same as Phase 7, but using Dapper instead of raw ADO.NET
- Dapper pattern: connection.QueryAsync<T>(sql, parameters) and connection.ExecuteAsync(sql, parameters)
- Dapper maps POCO properties to SQL columns by convention (same names)
- SQL is identical to ADO.NET (same provider-specific differences)

TASK:
Create PolicyStoreDapper class in each of the 4 Dapper provider packages.
Follow exact same SQL as ADO.NET (Phase 7), but use Dapper API instead of IDbCommand/IDataReader.

KEY RULES:
- Same constructor pattern as ADO.NET: IDbConnection, IPolicySerializer, TimeProvider?
- Dapper handles parameter binding and result mapping automatically
- SaveChangesAsync() is no-op (Dapper executes immediately)
- Use anonymous objects for parameters: new { Id = id, NowUtc = nowUtc }
- SQLite: NEVER use datetime('now')

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs (Dapper store pattern)
- src/Encina.Dapper.Sqlite/Outbox/OutboxStoreDapper.cs (SQLite Dapper pattern)
- src/Encina.Dapper.MySQL/Outbox/OutboxStoreDapper.cs (MySQL Dapper pattern)
- src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs (PostgreSQL Dapper pattern)
- src/Encina.Security.ABAC/Persistence/IPolicyStore.cs
- src/Encina.Security.ABAC/Persistence/PolicyEntityMapper.cs
```

</details>

---

### Phase 9: EF Core Provider Implementations (4 providers)

> **Goal**: Implement `PolicyStoreEF` for SQLite, SqlServer, PostgreSQL, MySQL.

<details>
<summary><strong>Tasks</strong></summary>

#### New files

1. **`src/Encina.EntityFrameworkCore/ABAC/PolicyStoreEF.cs`**
   - Single implementation for all 4 databases (EF Core abstracts the differences)
   - Constructor: `DbContext`, `IPolicySerializer`, `TimeProvider?`
   - Uses `_dbContext.Set<PolicySetEntity>()` for LINQ queries
   - UPSERT via EF Core: check if exists, add or update accordingly
   - `SaveChangesAsync()` delegates to `_dbContext.SaveChangesAsync()`

#### DI Registration

2. **`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`** — Update
   - Add `bool UseABACPolicyStore { get; set; }` to `MessagingConfiguration`
   - When `true`: `services.AddScoped<IPolicyStore, PolicyStoreEF>()`
   - Register entity configurations via `OnModelCreating`

#### DbContext Configuration

3. **`src/Encina.EntityFrameworkCore/ABAC/ABACModelBuilderExtensions.cs`**
   - Extension method: `ModelBuilder.ApplyABACConfiguration()`
   - Applies `PolicySetEntityConfiguration` and `PolicyEntityConfiguration`
   - Called from user's `DbContext.OnModelCreating` or via convention

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of the Persistent PAP feature: EF Core PolicyStore implementation.

CONTEXT:
- EF Core abstracts database differences — one PolicyStoreEF class works for all 4 databases
- Entity configurations (Phase 6) handle table mapping per provider
- EF Core pattern: DbContext + LINQ + change tracking + SaveChangesAsync
- The store is Scoped (per-request) because DbContext is Scoped

TASK:
1. Create PolicyStoreEF in src/Encina.EntityFrameworkCore/ABAC/PolicyStoreEF.cs
   - Implements IPolicyStore
   - Constructor: DbContext, IPolicySerializer, TimeProvider?
   - Uses DbContext.Set<PolicySetEntity>() and DbContext.Set<PolicyEntity>()
   - LINQ queries for GetAll, Get by Id, Exists
   - Add/Update via EF change tracking
   - SaveChangesAsync is meaningful (unlike Dapper/ADO which auto-commit)

2. Create ABACModelBuilderExtensions for easy DbContext configuration

3. Update ServiceCollectionExtensions to register PolicyStoreEF

KEY RULES:
- EF Core Scoped lifetime (matches DbContext)
- Use AnyAsync() for Exists, FindAsync() for single lookups
- UPSERT: check if tracked entity exists, if yes update, if no add
- IPolicySerializer for JSON ↔ domain model conversion
- PolicyEntityMapper for entity ↔ domain model conversion

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs (EF Core store pattern)
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (EF Core DI pattern)
- src/Encina.Security.ABAC/Persistence/IPolicyStore.cs
- src/Encina.EntityFrameworkCore/ABAC/PolicySetEntityConfiguration.cs (from Phase 6)
```

</details>

---

### Phase 10: MongoDB Provider Implementation

> **Goal**: Implement `PolicyStoreMongo` using native BSON document storage.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.MongoDB/`

1. **`ABAC/PolicySetDocument.cs`** — MongoDB document for PolicySets
   - BSON-decorated POCO
   - `[BsonId]` on Id
   - Native BSON storage for the full PolicySet graph (no JSON serialization)
   - BSON discriminator for `IExpression` polymorphism

2. **`ABAC/PolicyDocument.cs`** — MongoDB document for standalone Policies

3. **`ABAC/ABACBsonClassMapRegistration.cs`** — BSON class maps for IExpression types
   - Register discriminators for `Apply`, `AttributeDesignator`, `AttributeValue`, `VariableReference`
   - Register class maps for all ABAC model types

4. **`ABAC/PolicyStoreMongo.cs`** — MongoDB IPolicyStore implementation
   - Constructor: `IMongoClient`, `IOptions<EncinaMongoDbOptions>`, `ILogger`, `TimeProvider?`
   - Collection names: `abac_policy_sets`, `abac_policies`
   - Uses MongoDB filter builders for queries
   - Uses `ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true })` for upsert
   - Does NOT use `IPolicySerializer` (native BSON instead)

#### DI Registration

5. **`src/Encina.MongoDB/ServiceCollectionExtensions.cs`** — Update
   - Add `bool UseABACPolicyStore { get; set; }` to `EncinaMongoDbOptions`
   - Register `IPolicyStore` → `PolicyStoreMongo`
   - Register BSON class maps at startup

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of the Persistent PAP feature: MongoDB PolicyStore.

CONTEXT:
- MongoDB stores documents natively as BSON — no JSON serialization needed
- The IExpression polymorphic hierarchy must be handled via BSON discriminators
- MongoDB pattern: IMongoCollection<T>, filter builders, ReplaceOneAsync for upsert
- PolicyStoreMongo does NOT use IPolicySerializer (bypasses JSON, stores as BSON)

TASK:
1. Create BSON document types (PolicySetDocument, PolicyDocument) with BSON attributes
2. Create BSON class map registration for all ABAC model types + IExpression discriminators
3. Create PolicyStoreMongo implementing IPolicyStore with native MongoDB operations
4. Update ServiceCollectionExtensions for MongoDB

BSON Discriminator Setup:
- IExpression discriminator: "Apply", "AttributeDesignator", "AttributeValue", "VariableReference"
- Register via BsonClassMap.RegisterClassMap<T>() at startup
- Effect, CombiningAlgorithmId → stored as string representation

KEY RULES:
- MongoDB uses IMongoCollection<T> not DbContext
- ReplaceOneAsync with IsUpsert = true for save operations
- Builders<T>.Filter.Eq(x => x.Id, id) for queries
- CountDocumentsAsync for counts
- DeleteOneAsync for deletes
- TimeProvider for timestamps
- EitherHelpers.TryAsync for all operations

REFERENCE FILES:
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs (MongoDB store pattern)
- src/Encina.MongoDB/ServiceCollectionExtensions.cs (MongoDB DI pattern)
- src/Encina.Security.ABAC/Model/IExpression.cs
- src/Encina.Security.ABAC/Persistence/IPolicyStore.cs
```

</details>

---

### Phase 11: Observability — Metrics, Traces & Logging

> **Goal**: Extend ABAC diagnostics with PAP/store-specific telemetry.

<details>
<summary><strong>Tasks</strong></summary>

#### Modified files

1. **`src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs`** — Add PAP metrics
   - New counters:
     - `abac.pap.store.operations` (Counter<long>) — Tags: `operation` (read/write/delete), `entity` (policy_set/policy), `status` (success/failure)
   - New histograms:
     - `abac.pap.store.duration` (Histogram<double>) — Tags: `operation`, `entity`
     - `abac.pap.serialization.duration` (Histogram<double>) — Tags: `direction` (serialize/deserialize)
   - New counters:
     - `abac.pap.serialization.errors` (Counter<long>)
   - New activity helpers:
     - `StartStoreOperation(string operation, string entityType)` → Activity
     - `StartSerialization(string direction)` → Activity

2. **`src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs`** — Add PAP log events
   - EventId range: **9030–9049** (PAP store operations)
     - 9030: `PAPPolicySetLoaded` (Debug) — Policy set loaded from persistent store
     - 9031: `PAPPolicySetSaved` (Information) — Policy set saved to persistent store
     - 9032: `PAPPolicySetDeleted` (Information) — Policy set deleted from persistent store
     - 9033: `PAPPolicyLoaded` (Debug) — Policy loaded from persistent store
     - 9034: `PAPPolicySaved` (Information) — Policy saved
     - 9035: `PAPPolicyDeleted` (Information) — Policy deleted
     - 9036: `PAPSerializationSucceeded` (Debug) — Serialization/deserialization succeeded
     - 9037: `PAPSerializationFailed` (Error) — Serialization/deserialization failure
     - 9038: `PAPStoreOperationFailed` (Error) — Store operation failure
     - 9039: `PAPStoreHealthy` (Debug) — Store connectivity check passed
     - 9040: `PAPStoreUnhealthy` (Warning) — Store connectivity check failed

3. **`src/Encina.Security.ABAC/Health/ABACHealthCheck.cs`** — Extend for persistent store
   - When `IPolicyStore` is registered, additionally check store connectivity via `GetPolicySetCountAsync()`
   - Degraded: store is reachable but empty
   - Unhealthy: store is unreachable

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of the Persistent PAP feature: Observability.

CONTEXT:
- ABACDiagnostics.cs already has ActivitySource "Encina.Security.ABAC" and Meter
- ABACLogMessages.cs uses EventIds 9000-9022 (evaluation, obligations, advice)
- New EventId range for PAP operations: 9030-9049

TASK:
1. Add PAP-specific metrics to ABACDiagnostics.cs (counters, histograms, activity helpers)
2. Add PAP-specific log messages to ABACLogMessages.cs (EventIds 9030-9040)
3. Update ABACHealthCheck to verify IPolicyStore when persistent PAP is enabled

KEY RULES:
- Use [LoggerMessage] source generator (partial methods)
- Follow existing naming: "abac.pap.*" for metrics
- Activity names: "ABAC.PAP.Load", "ABAC.PAP.Save", "ABAC.PAP.Delete", "ABAC.PAP.Serialize"
- Tags: abac.operation, abac.entity_type, abac.status, abac.json_size
- Health check: inject IPolicyStore via IServiceProvider.CreateScope() (optional dependency)

REFERENCE FILES:
- src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs (existing metrics)
- src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs (existing log messages, EventId 9000-9022)
- src/Encina.Security.ABAC/Health/ABACHealthCheck.cs (existing health check)
```

</details>

---

### Phase 12: Testing

> **Goal**: Comprehensive test coverage for serializer, persistent PAP, and all 13 provider implementations.

<details>
<summary><strong>Tasks</strong></summary>

#### Unit Tests (`tests/Encina.UnitTests/Security/ABAC/Persistence/`)

1. **`DefaultPolicySerializerTests.cs`** — Round-trip serialization tests
   - Serialize + Deserialize PolicySet with all nested types
   - Serialize + Deserialize Policy with all Rule/Condition combinations
   - Handle recursive PolicySet → PolicySet → PolicySet
   - Handle all 4 IExpression types (Apply, Designator, Value, Reference)
   - Handle enums: Effect, CombiningAlgorithmId, AttributeCategory, FulfillOn
   - Handle nullable properties (Target?, Version?, Description?)
   - Handle empty collections (Policies=[], Rules=[], Obligations=[])
   - Error case: malformed JSON → Left(EncinaError)
   - Error case: unknown expression type → Left(EncinaError)

2. **`PersistentPolicyAdministrationPointTests.cs`** — PAP logic tests
   - Mock `IPolicyStore`, verify delegation
   - AddPolicySetAsync → calls store.SavePolicySetAsync
   - AddPolicyAsync with parent → loads parent PolicySet, adds policy, saves updated PolicySet
   - AddPolicyAsync standalone → calls store.SavePolicyAsync
   - UpdatePolicyAsync → update in parent or standalone
   - RemovePolicyAsync → remove from parent or standalone
   - GetPoliciesAsync(parentId) → loads parent, returns its Policies
   - GetPoliciesAsync(null) → returns standalone policies
   - GetPolicyAsync → searches standalone first, then PolicySets
   - Duplicate policy → returns ABACErrors.DuplicatePolicy
   - PolicySet not found → returns ABACErrors.PolicySetNotFound

3. **`PolicyEntityMapperTests.cs`** — Mapper tests
   - Domain → Entity → Domain round-trip
   - Timestamp generation via TimeProvider
   - Deserialization failure → Left(EncinaError)

#### Guard Tests (`tests/Encina.GuardTests/Security/ABAC/`)

4. **`PolicyStoreGuardTests.cs`** — Null checks for IPolicyStore implementations
5. **`PolicySerializerGuardTests.cs`** — Null checks for serializer methods
6. **`PersistentPAPGuardTests.cs`** — Null checks for constructor and methods

#### Property Tests (`tests/Encina.PropertyTests/Security/ABAC/`)

7. **`PolicySerializationPropertyTests.cs`** — FsCheck
   - Property: ∀ PolicySet p: Deserialize(Serialize(p)) == p
   - Property: ∀ Policy p: Deserialize(Serialize(p)) == p
   - Property: ∀ IExpression e: Deserialize(Serialize(e)) preserves type and data
   - Custom Arbitrary generators for PolicySet, Policy, Rule, Apply, etc.

#### Contract Tests (`tests/Encina.ContractTests/Security/ABAC/`)

8. **`PolicyStoreContractTests.cs`** — Abstract base class
   - Verifies IPolicyStore contract across all 13 providers
   - Abstract method: `IPolicyStore CreateStore()`
   - Tests: Save → Get (round-trip), Save → Delete → Get (returns None), Exists, Count
   - Upsert: Save twice with same Id → Get returns latest
   - Each provider creates a concrete test class inheriting from this base

#### Integration Tests (`tests/Encina.IntegrationTests/Security/ABAC/`)

9. **ADO.NET Integration Tests** (4 classes, one per database)
   - `PolicyStoreADO_SqliteTests.cs` — `[Collection("ADO-Sqlite")]`
   - `PolicyStoreADO_SqlServerTests.cs` — `[Collection("ADO-SqlServer")]`
   - `PolicyStoreADO_PostgreSqlTests.cs` — `[Collection("ADO-PostgreSQL")]`
   - `PolicyStoreADO_MySqlTests.cs` — `[Collection("ADO-MySQL")]`

10. **Dapper Integration Tests** (4 classes)
    - Same pattern, `[Collection("Dapper-*")]`

11. **EF Core Integration Tests** (4 classes)
    - Same pattern, `[Collection("EFCore-*")]`

12. **MongoDB Integration Tests** (1 class)
    - Uses MongoDB testcontainer

Each integration test:
- Creates schema (runs SQL script)
- Runs full CRUD cycle
- Verifies serialization round-trip with real database
- Uses `ClearAllDataAsync()` in `InitializeAsync()`

#### Load Tests & Benchmarks

13. **`tests/Encina.LoadTests/Security/ABAC/PolicyStoreLoadTests.md`** — Justification
    - PAP is administrative, not a hot path
    - Concurrent writes are rare (policy changes are infrequent)

14. **`tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/ABAC/PolicySerializerBenchmarks.cs`**
    - Benchmark serialization of small vs large policy graphs
    - Benchmark deserialization
    - Use BenchmarkSwitcher (not BenchmarkRunner)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```
You are implementing Phase 12 of the Persistent PAP feature: Testing.

CONTEXT:
- All implementation phases (1-11) are complete
- Tests must cover: serializer, PersistentPAP, CachingPolicyStoreDecorator, entity mapper, all 13 provider stores, observability
- Test organization follows Encina conventions (see CLAUDE.md Testing Standards)

TASK:
Create comprehensive test coverage:

1. UNIT TESTS in tests/Encina.UnitTests/Security/ABAC/Persistence/
   - DefaultPolicySerializerTests.cs (round-trip, edge cases, errors)
   - PersistentPolicyAdministrationPointTests.cs (mock store, verify logic)
   - PolicyEntityMapperTests.cs (domain ↔ entity conversion)

2. GUARD TESTS in tests/Encina.GuardTests/Security/ABAC/
   - Null checks for all public constructors and methods

3. PROPERTY TESTS in tests/Encina.PropertyTests/Security/ABAC/
   - FsCheck round-trip: Deserialize(Serialize(x)) == x
   - Custom Arbitrary generators for complex ABAC types

4. CONTRACT TESTS in tests/Encina.ContractTests/Security/ABAC/
   - Abstract base PolicyStoreContractTests<T> where T : IPolicyStore
   - Concrete implementations for each provider

5. INTEGRATION TESTS in tests/Encina.IntegrationTests/Security/ABAC/
   - 13 test classes (4 ADO, 4 Dapper, 4 EF Core, 1 MongoDB)
   - Use [Collection("Provider-Database")] shared fixtures
   - ClearAllDataAsync() in InitializeAsync()
   - Full CRUD + round-trip tests with real databases

6. BENCHMARK in tests/Encina.BenchmarkTests/
   - Serialization benchmarks for small/medium/large policy graphs

KEY RULES:
- AAA pattern (Arrange, Act, Assert)
- Descriptive test names (MethodUnderTest_Scenario_ExpectedBehavior)
- SQLite: NEVER dispose shared connection from fixtures
- Collection fixtures: [Collection("ADO-Sqlite")], [Collection("Dapper-SqlServer")], etc.
- Use existing fixture classes (SqliteFixture, SqlServerFixture, etc.)
- BenchmarkSwitcher, not BenchmarkRunner

REFERENCE FILES:
- tests/Encina.UnitTests/Security/ABAC/ (existing ABAC unit tests for pattern)
- tests/Encina.IntegrationTests/ADO/ (ADO integration test patterns)
- tests/Encina.IntegrationTests/Dapper/ (Dapper integration test patterns)
- tests/Encina.IntegrationTests/EntityFrameworkCore/ (EF Core integration test patterns)
- tests/Encina.PropertyTests/ (FsCheck pattern)
- tests/Encina.ContractTests/ (contract test pattern)
- tests/Encina.GuardTests/ (guard test pattern)
```

</details>

---

### Phase 13: Documentation & Finalization

> **Goal**: XML docs, feature documentation, CHANGELOG, ROADMAP, build verification.

<details>
<summary><strong>Tasks</strong></summary>

#### XML Documentation
1. All new public types and methods have `<summary>`, `<param>`, `<returns>`, `<example>` XML doc comments

#### Feature Documentation
2. **`docs/features/abac/reference/persistent-pap.md`** — Usage guide
   - Configuration for InMemory vs Persistent PAP
   - Provider-specific setup (EF Core, Dapper, ADO, MongoDB)
   - Schema creation (SQL scripts, EF migrations)
   - Seed policies with persistent store
   - Health check behavior with persistent store
   - Observability (metrics, traces, log events)
   - Code examples for each provider

3. **`docs/features/abac/xacml/architecture.md`** — Update
   - Add "Policy Storage" section explaining InMemory vs Persistent PAP
   - Add architecture diagram showing PAP → IPolicyStore → Provider

4. **`docs/features/abac/reference/configuration.md`** — Update
   - Add `UsePersistentPAP` option documentation
   - Add `UseABACPolicyStore` option for each provider

#### Project Documentation

5. **`CHANGELOG.md`** — Add under Unreleased → Added
   ```
   ### Added
   - `IPolicyStore` interface for persistent ABAC policy storage
   - `IPolicySerializer` with `DefaultPolicySerializer` (System.Text.Json, polymorphic IExpression support)
   - `PersistentPolicyAdministrationPoint` — database-backed PAP implementation
   - `CachingPolicyStoreDecorator` — opt-in caching via `ICacheProvider` with stampede protection and PubSub invalidation
   - `PolicyCachingOptions` — configurable TTL, PubSub invalidation channel, tag-based invalidation
   - ABAC policy store implementations for all 13 database providers
   - `ABACOptions.UsePersistentPAP` and `ABACOptions.PolicyCaching` configuration
   - PAP-specific OpenTelemetry metrics and structured logging (EventIds 9030-9049)
   - Extended `ABACHealthCheck` with persistent store connectivity verification
   ```

6. **`ROADMAP.md`** — Update v0.13.0 section (mark PAP as completed)

7. **`docs/INVENTORY.md`** — Update with new files and modules

8. **`PublicAPI.Unshipped.txt`** — Ensure all new public symbols are tracked
   - IPolicyStore, IPolicySerializer, DefaultPolicySerializer, PersistentPolicyAdministrationPoint
   - PolicySetEntity, PolicyEntity, PolicyEntityMapper
   - ABACOptions.UsePersistentPAP
   - New ABACErrors methods

#### Build Verification

9. `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
10. `dotnet test Encina.slnx --configuration Release` → All pass
11. Verify code coverage ≥ 85% for new code

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 13</strong></summary>

```
You are implementing Phase 13 of the Persistent PAP feature: Documentation & Finalization.

CONTEXT:
- All implementation and testing phases (1-12) are complete
- Need to add documentation, update project files, verify build

TASK:
1. Ensure all public APIs have XML doc comments (<summary>, <param>, <returns>, <example>)
2. Create docs/features/abac/reference/persistent-pap.md with comprehensive usage guide
3. Update docs/features/abac/xacml/architecture.md with persistent PAP section
4. Update docs/features/abac/reference/configuration.md with new options
5. Update CHANGELOG.md with Added entries under Unreleased
6. Update ROADMAP.md to mark persistent PAP as completed in v0.13.0
7. Update docs/INVENTORY.md with new files
8. Update PublicAPI.Unshipped.txt for Encina.Security.ABAC
9. Run: dotnet build Encina.slnx --configuration Release (0 errors, 0 warnings)
10. Run: dotnet test Encina.slnx --configuration Release (all pass)

KEY RULES:
- English only for code, comments, documentation
- No AI attribution in commits
- CHANGELOG follows Keep a Changelog format
- XML docs: <summary> is mandatory, <example> is encouraged for complex APIs
- PublicAPI.Unshipped.txt format: Namespace.Type.Member(params) -> ReturnType

REFERENCE FILES:
- docs/features/abac/ (existing ABAC docs)
- CHANGELOG.md (current format)
- ROADMAP.md (current format)
- docs/INVENTORY.md (current format)
- src/Encina.Security.ABAC/PublicAPI.Unshipped.txt (current public API)
```

</details>

---

## Research

### XACML & PAP References

| Reference | Source | Relevance |
|-----------|--------|-----------|
| XACML 3.0 §2 (Architecture) | OASIS Standard | PAP defined as durable administrative component |
| XACML 3.0 §7.18 (Obligations) | OASIS Standard | Obligation handling in PDP decisions |
| Enterprise PAP patterns | Axiomatics, AuthzForce, WSO2 | All provide persistent policy storage |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `IPolicyAdministrationPoint` | `src/Encina.Security.ABAC/Abstractions/` | Contract that PersistentPAP implements |
| `InMemoryPolicyAdministrationPoint` | `src/Encina.Security.ABAC/Administration/` | Reference implementation for behavior |
| `ABACErrors` | `src/Encina.Security.ABAC/ABACErrors.cs` | Error factory — extend with store/serialization errors |
| `ABACOptions` | `src/Encina.Security.ABAC/ABACOptions.cs` | Configuration — add `UsePersistentPAP` |
| `ABACDiagnostics` | `src/Encina.Security.ABAC/Diagnostics/` | Extend with PAP metrics |
| `ABACLogMessages` | `src/Encina.Security.ABAC/Diagnostics/` | Extend with PAP log events (9030-9049) |
| `ABACHealthCheck` | `src/Encina.Security.ABAC/Health/` | Extend with store connectivity check |
| `ABACPolicySeedingHostedService` | `src/Encina.Security.ABAC/` | Reuse unchanged — works with any PAP impl |
| `EitherHelpers.TryAsync` | `src/Encina/Results/EitherHelpers.cs` | Wraps store operations in ROP |
| `ICacheProvider` | `src/Encina.Caching/ICacheProvider.cs` | `GetOrSetAsync` for stampede protection, `RemoveByPatternAsync` for invalidation |
| `IPubSubProvider` | `src/Encina.Caching/IPubSubProvider.cs` | Cross-instance cache invalidation broadcasting |
| `CachingOptions` | `src/Encina.Caching/CachingOptions.cs` | Configuration pattern reference for `PolicyCachingOptions` |
| `SqlIdentifierValidator` | `src/Encina/` | Validates table names for SQL injection prevention |
| `OutboxStoreEF/Dapper/ADO/MongoDB` | Provider packages | Reference pattern for store implementations |
| Collection Fixtures | `tests/Encina.IntegrationTests/` | Shared Docker containers for integration tests |

### Event ID Allocation

| Package | Current Range | New Range (This Feature) | Notes |
|---------|---------------|-------------------------|-------|
| Encina.Security.ABAC | 9000–9022 | 9030–9049 | PAP store operations |
| Next available | — | 9050+ | Reserved for future ABAC features |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core abstractions | 8 | IPolicyStore, IPolicySerializer, DefaultPolicySerializer, converters, entities, mapper |
| PersistentPAP | 1 | PersistentPolicyAdministrationPoint |
| Caching Decorator | 3 | CachingPolicyStoreDecorator, PolicyCachingOptions, PolicyCachePubSubHostedService |
| Configuration & DI | 2 | ABACOptions update, ServiceCollectionExtensions update |
| SQL Scripts | 4 | One per database type (shared by ADO & Dapper) |
| EF Core Config | 3 | Entity configurations + model builder extension |
| ADO.NET Stores | 4 | One per database |
| ADO.NET DI updates | 4 | ServiceCollectionExtensions updates |
| Dapper Stores | 4 | One per database |
| Dapper DI updates | 4 | ServiceCollectionExtensions updates |
| EF Core Store | 1 | Single implementation |
| EF Core DI update | 1 | ServiceCollectionExtensions update |
| MongoDB Store | 4 | Document types, class maps, store, DI update |
| Observability | 2 | Diagnostics + LogMessages updates |
| Health Check | 1 | ABACHealthCheck update |
| Unit Tests | 3 | Serializer, PAP, Mapper |
| Guard Tests | 3 | Store, Serializer, PAP |
| Property Tests | 1 | Serialization round-trip |
| Contract Tests | 1 | Abstract base + concrete per provider |
| Integration Tests | 13 | One per provider |
| Benchmark | 1 | Serialization benchmarks |
| Load Test Justification | 1 | .md file |
| Documentation | 5 | Feature docs, architecture update, config update, CHANGELOG, ROADMAP |
| Caching unit tests | 2 | CachingPolicyStoreDecoratorTests, PolicyCachePubSubHostedServiceTests |
| **Total** | **~76** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Prompt — All Phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing Persistent Policy Administration Point (PAP) for Encina.Security.ABAC.
This enables database-backed ABAC policy storage across all 13 Encina database providers.

CURRENT STATE:
- Encina.Security.ABAC has InMemoryPolicyAdministrationPoint (ConcurrentDictionary-based)
- XACML 3.0 policy model: PolicySet → Policy → Rule → Target/Condition/Obligation/Advice
- IExpression polymorphic hierarchy: Apply, AttributeDesignator, AttributeValue, VariableReference
- Store patterns established: OutboxStoreEF/Dapper/ADO/MongoDB with Either<EncinaError, T>
- EventId range 9000-9022 used, new range 9030-9049 for PAP

IMPLEMENTATION OVERVIEW:
Phase 1:  IPolicySerializer + DefaultPolicySerializer (System.Text.Json, IExpression converter)
Phase 2:  IPolicyStore interface + PolicySetEntity/PolicyEntity + PolicyEntityMapper
Phase 3:  PersistentPolicyAdministrationPoint (wraps IPolicyStore)
Phase 4:  ABACOptions.UsePersistentPAP + PolicyCachingOptions + DI wiring
Phase 5:  CachingPolicyStoreDecorator (ICacheProvider + IPubSubProvider, stampede protection)
Phase 6:  SQL scripts (4 databases) + EF Core entity configs
Phase 7:  ADO.NET PolicyStoreADO (SQLite, SqlServer, PostgreSQL, MySQL)
Phase 8:  Dapper PolicyStoreDapper (SQLite, SqlServer, PostgreSQL, MySQL)
Phase 9:  EF Core PolicyStoreEF (single impl, 4 databases)
Phase 10: MongoDB PolicyStoreMongo (native BSON, no JSON serializer)
Phase 11: Observability (metrics 9030-9049, traces, health check)
Phase 12: Testing (unit, guard, property, contract, integration ×13, benchmarks)
Phase 13: Documentation (XML docs, feature docs, CHANGELOG, ROADMAP, build verify)

KEY PATTERNS:
- ROP: Either<EncinaError, T> on all fallible operations
- Store naming: PolicyStoreADO, PolicyStoreDapper, PolicyStoreEF, PolicyStoreMongo
- CachingPolicyStoreDecorator: wraps any IPolicyStore, uses ICacheProvider + IPubSubProvider
- Caching: GetOrSetAsync (stampede protection), write-through invalidation, PubSub broadcasting
- DI: TryAdd pattern, Singleton for serializer/PAP/decorator, Scoped for EF stores
- SQLite: TEXT for all types, @NowUtc (never datetime('now')), never dispose shared connection
- SQL Server: NVARCHAR(MAX), DATETIME2(7), MERGE for upsert
- PostgreSQL: JSONB, INSERT ... ON CONFLICT DO UPDATE
- MySQL: JSON type, backticks, INSERT ... ON DUPLICATE KEY UPDATE
- MongoDB: Native BSON, BsonDiscriminator for IExpression, ReplaceOneAsync with IsUpsert
- TimeProvider injection (defaults to TimeProvider.System)
- EitherHelpers.TryAsync() wraps all infrastructure calls
- [LoggerMessage] source generator for structured logging
- ActivitySource "Encina.Security.ABAC" for traces
- Integration tests: [Collection("Provider-Database")] shared fixtures

REFERENCE FILES:
- src/Encina.Security.ABAC/ (entire ABAC module)
- src/Encina/Messaging/Outbox/IOutboxStore.cs (store interface pattern)
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs (EF store pattern)
- src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs (Dapper store pattern)
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs (ADO store pattern)
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs (MongoDB store pattern)
- src/Encina.Caching/ICacheProvider.cs (caching interface — 8 providers)
- src/Encina.Caching/IPubSubProvider.cs (cross-instance invalidation)
- src/Encina.Caching/CachingOptions.cs (caching configuration pattern)
- src/Encina/Results/EitherHelpers.cs (ROP helper)
- CLAUDE.md (project guidelines, testing standards, naming conventions)
```

</details>

---

## Next Steps

1. **Review** this plan for completeness and design decisions
2. **Publish** as a comment on [#691](https://github.com/dlrivada/Encina/issues/691) for CodeRabbit analysis
3. **Implement** phase by phase (each phase is self-contained with its AI agent prompt)
4. **Final commit** with `Fixes #691` to auto-close the issue
