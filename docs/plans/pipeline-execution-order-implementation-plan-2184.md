# Implementation Plan: Pipeline Execution-Order Contract (`Encina` core) — closed named stages, typed processors adapted into stages, fail-closed startup validation

> **Issue**: [#2184](https://github.com/dlrivada/Encina/issues/2184) (implementation of the [#1783](https://github.com/dlrivada/Encina/issues/1783) spike, with [#1678](https://github.com/dlrivada/Encina/issues/1678) merged into it by maintainer decision of 2026-10-09)
> **Type**: Feature (labels: `enhancement`, `area-pipeline`, `p0-mandatory`)
> **Milestone**: v0.14.0 — Hardening (SPEC-000 DEC-005, block 1)
> **Research**: spike report `artifacts/spikes/1783-execution-order-contract.md` and prior-art study `artifacts/research/pre-post-processors.md` (both kept locally, summarised in "Research" below); maintainer decisions Q1-Q6 recorded on #1783 on 2026-10-09
> **ADR**: ADR-049 "Pipeline stages: a closed, core-owned execution-order contract for behaviors" (reserved in `docs/architecture/adr/index.md` with this plan; ADR-048 is the inbox ADR of #2084)
> **Related**: #751 (ABAC decision audit; its 9090 order warning was dropped in favour of this contract, decision A6), #718 (transactional outbox; its outbox behavior is born in the `Transaction` stage), #1782 (Recoverability registered twice), #1642 (Audit outcome classified by message text), #2029, #2030, #2031, #2032 (registration defects found by the spike), #2039 (no check for synchronous database calls), #2088 (claims EventId 176), ADR-018 (cross-cutting functions), ADR-048 (inbox inside the business transaction)
> **Complexity**: High (one core mechanism plus a mechanical migration of about 55 built-in behaviors in about 40 packages)
> **Estimated Scope**: ~1,400 lines of new production code in `Encina` core, ~400 lines changed across satellites (attributes and registrations), ~2,200 lines of tests

---

## Summary

Today the position of every behavior in the request pipeline is decided by the order of the `AddEncina*` calls in the host. `PipelineBuilder.Build` resolves `GetServices<IPipelineBehavior<TRequest, TResponse>>()` (`src/Encina/Pipeline/PipelineBuilder.cs:63`) and wraps the handler in reverse index order (`:72-80`), so the first registered behavior is the outermost. Pre- and post-processors are resolved separately (`:64-65`) and run outside every behavior (`:82-83`, loops at `:102-122`), so an application pre-processor runs before authentication and authorization. `StreamPipelineBuilder.Build` does the same for stream behaviors (`src/Encina/Pipeline/StreamPipelineBuilder.cs:65`, `:72-80`). Nothing in `src/` declares a stage, an order or a priority; the only fixed relative order inside one package is Polly's, by the order of its `AddTransient` calls (`src/Encina.Polly/ServiceCollectionExtensions.cs:55-65`).

The consequences, all found by the spike and still true on `main` (`5b4a6a16`):

- **Security semantics depend on host code.** With `AddEncinaABAC` before `AddEncinaSecurity`, ABAC evaluates (and, after #751, audits a grant) before `SecurityPipelineBehavior` rejects an anonymous caller. The only protection is documentation (`src/Encina.Security.ABAC/README.md:92`, "Register before AddEncinaABAC()").
- **Docs contradict the code.** `IPipelineBehavior.cs:11`, `IStreamPipelineBehavior.cs:21` and `Modules/IModulePipelineBehavior.cs:18` say "reverse registration order"; the builder documents registration order (`PipelineBuilder.cs:30-31`, `:54-55`). `src/Encina.AspNetCore/README.md:597` and `:612` recommend "Validation → Authorization → Handler", which leaks validation errors to unauthorized callers. `SoftDeleteQueryFilterBehavior.cs:26` and `MessagingServiceCollectionExtensions.cs:516-517` ask to run "before validation and authorization"; `Encina.Compliance.LawfulBasis/ServiceCollectionExtensions.cs:38-40` asks to call `AddEncinaLawfulBasis` before `AddEncinaGDPR`.
- **Registrations that silently never run.** The shadow sharding behaviors are registered only as `ICommandPipelineBehavior<,>` / `IQueryPipelineBehavior<,>` (`src/Encina/Sharding/Shadow/ShadowShardingServiceCollectionExtensions.cs:49-55`), which `PipelineBuilder` never resolves (#2032); `EncinaConfiguration.RegisterPipelineBehavior` adds the same dead descriptors for every configured command or query behavior (`src/Encina/Core/EncinaConfiguration.cs:242-251`). Four `ReadWriteRoutingPipelineBehavior`s (ADO.MySQL, ADO.PostgreSQL, Dapper.MySQL, Dapper.PostgreSQL) and Marten's `EventPublishingPipelineBehavior` are never registered (#2029, #2030).
- **Duplicates.** About 20 built-in registrations use plain `AddTransient` / `AddScoped` (for example `Encina.Polly/ServiceCollectionExtensions.cs:56-65`, `Encina.Caching/ServiceCollectionExtensions.cs:81-91`, `Encina.Messaging/MessagingServiceCollectionExtensions.cs:90,:120,:292,:535`), so a second `AddEncina*` call runs a behavior twice; `RecoverabilityPipelineBehavior` is registered twice today (`MessagingServiceCollectionExtensions.cs:120` and `Recoverability/RecoverabilityServiceCollectionExtensions.cs:68`, #1782).

**What this plan implements** (spike Option C, chosen by the maintainer on 2026-10-09, plus the six decisions Q1-Q6):

1. A **closed `PipelineStage` enum in core** with 16 stages listed outermost to innermost (values in steps of 100). Satellites and applications pick a stage; they cannot add one.
2. **`[PipelineStage(stage, Order = n)]`** on every behavior type, with a registration-time override (`cfg.AddPipelineBehavior(type, stage, order)`, Q3). The final order key is `(stage, order, tie-break)`; it is computed once per closed pipeline and cached.
3. **Typed processors adapted into staged steps** (Q2 = d): `IRequestPreProcessor<T>` and `IRequestPostProcessor<T, R>` stay, are placed by the stage system (default `Application`, inside every gate), get an explicit stable order (never DI order), and return `Either`; two new hooks are added: `IRequestErrorProcessor<T, R>` (runs only on `Left`) and `IRequestPostCommitProcessor<T, R>` (runs after the transaction commits).
4. **Fail-closed startup validation**: a core hosted service validates every pipeline it can enumerate at host start, and each closed pipeline is validated again before its first request. An invalid order never runs.
5. **Migration of every built-in behavior**: the attribute on about 55 types, `TryAddEnumerable` for every registration, the false order instructions deleted, and an inspector API (`IEncinaPipelineInspector`) so applications can assert their own pipelines in tests.

**One correction to the spike's stage table, forced by ADR-048.** The spike placed `Idempotency` (the inbox) outside `Transaction` (its constraint C8). ADR-048, accepted on 2026-10-09 after the spike was written, requires the inbox to run **inside** the business transaction: `MarkAsProcessedAsync` is an enlisted write and the independent writes survive the rollback (`docs/architecture/adr/048-inbox-record-vs-business-transaction.md:9`, `:22-25`); two unit tests pin "transaction before inbox" (`tests/Encina.UnitTests/Messaging/MessagingServiceCollectionExtensionsRecoverabilityTests.cs:39`, `tests/Encina.UnitTests/EntityFrameworkCore/Inbox/InboxStoreEFIsolationTests.cs:45`). Only one order complies with that accepted ADR, so this plan puts `Idempotency` inside `Transaction` (and `Resilience`, `DataRouting` and `Transaction` move one slot out). `DistributedIdempotencyPipelineBehavior`, which stores its result in a cache and must not record a success whose commit later fails, moves to the `Caching` stage, outside `Transaction`. The full table is in Research, "Stage list and built-in placement".

**How the contract makes the spike's defect classes impossible or detected at startup:**

| Defect class (example) | Mechanism in this plan |
| --- | --- |
| Security semantics depend on `AddEncina*` call order (#751, #1678) | The order key ignores registration order for built-ins: stage precedence is fixed in core, and a contract test proves that every permutation of the built-in registrations gives the same pipeline (Phase 8) |
| A behavior registered under a service type the builder never resolves (#2032) | Startup rule R4: any descriptor whose service type is `ICommandPipelineBehavior<,>` or `IQueryPipelineBehavior<,>` (open or closed) fails startup with the type names; the dead descriptors of `EncinaConfiguration.cs:242-251` are deleted. The rule cannot be satisfied by a registration that never runs |
| A built-in behavior that no `AddEncina*` method registers (#2029, #2030) | Contract test "every built-in step is reachable": for every type carrying `[PipelineStage]` in an assembly of `src/`, a registration table names the `AddEncina*` call and options that enable it; the test runs that call and asserts the type appears in `IEncinaPipelineInspector.Describe`. A new behavior without a row, or a row whose call does not register it, fails CI |
| A behavior that runs twice (#1782, plain `AddTransient`) | Q4: duplicates of one step identity are collapsed (EventId 178, Debug); every built-in registration moves to `TryAddEnumerable`; an architecture rule fails any `AddTransient`/`AddScoped` of an open `IPipelineBehavior<,>` in `src/` |
| An undeclared or out-of-range stage | Rules R1 and R3: a cast integer that is not a `PipelineStage` member, or a built-in type with no stage, fails startup; the architecture test catches the built-in case at build time |
| Synchronous database calls inside a stage behavior (#2031) | Not an ordering defect. The contract does not prevent it; detection stays with #2039 (S6966 re-enabled). The `Transaction` stage contract test of Phase 8 runs each transaction behavior against a connection whose synchronous `Open`/`BeginTransaction` throws, so a sync fallback is caught for the stage the contract owns |

**Affected packages**: `Encina` (core: stages, attribute, resolver, validator, inspector, processor contracts, builders, configuration, scanner), and, for the attribute and registration migration only, `Encina.AspNetCore`, `Encina.Security`, `Encina.Security.ABAC`, `Encina.Security.Audit`, `Encina.Security.AntiTampering`, `Encina.Security.Sanitization`, `Encina.Security.Encryption`, `Encina.Security.PII`, `Encina.Security.Secrets`, `Encina.Caching`, `Encina.Polly`, `Encina.Extensions.Resilience`, `Encina.OpenTelemetry`, `Encina.Messaging`, `Encina.EntityFrameworkCore`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.MongoDB`, `Encina.Marten`, `Encina.FluentValidation`, `Encina.DataAnnotations`, `Encina.MiniValidator` and the 15 `Encina.Compliance.*` packages with a behavior.

**Provider category**: none for the mechanism (provider-independent core feature). The migration touches every provider family that ships a behavior: the 8 read/write routers of the Database category (EF Core, ADO.NET x3, Dapper x3, MongoDB) get the same stage and order; the 3 validation providers share one `ValidationPipelineBehavior`; no caching, transport, lock or cloud provider code changes.

**SPEC-000**: a 1.0 item. It is a correctness and fail-closed property of the security and compliance behaviors SPEC-002 relies on, and the maintainer set it to p0.

---

## Design Choices

The overall mechanism (spike Option C: closed named stages, a per-type declaration, an order inside the stage and fail-closed startup validation) was chosen by the maintainer on 2026-10-09 and is not repeated here. Design Choices 1 to 6 are the spike's questions Q1 to Q6, in the same order; Design Choice 7 was raised by the review of PR #2186 and decided by the maintainer on 2026-10-10.

<details>
<summary><strong>1. Undeclared custom behaviors (spike Q1) — placed in <code>Application</code>, logged once per type</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Place an undeclared custom behavior in `PipelineStage.Application` and log EventId 177 once per type** | No friction for applications and third-party behaviors; safe, because `Application` is inside every gate (authentication, authorization, compliance, validation, transaction) | A behavior its author meant to be outer runs inner without a startup error; the log line is the only signal |
| **(b) Fail startup until every behavior declares a stage** | Explicit everywhere; no silent placement | Every application and every third-party behavior must carry the attribute or be registered with a stage; adds friction without adding safety |

### Chosen Option: **(a) — Application stage plus EventId 177**

### Rationale

- Safety does not depend on the choice: the default stage is inside every security, compliance and transaction stage, so an undeclared behavior can never run around a gate.
- Built-in behaviors are not covered by this default: a type declared in a package shipped from `src/` without a stage is a bug and fails startup (rule R3) and the architecture test.
- EventId 177 (Information) names the type once per process, so the placement is visible in logs without flooding them; `IEncinaPipelineInspector` shows it with `PipelineStepSource.Default`.

</details>

<details>
<summary><strong>2. Pre- and post-processors (spike Q2) — typed sugar adapted into staged steps</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Keep them outermost and document it** | No change | Application code runs before the security gates (`PipelineBuilder.cs:82-83`); breaks the fail-closed rule (AGENTS.md section 3) |
| **(b) Run them only in `Application`: pre just before the handler, post just after** | Fail closed with no API removal | Two fixed positions only; post-processors still receive `Left` results and cannot fail the request (`IRequestPostProcessor.cs:51-55` returns `Task`); two parallel mechanisms remain |
| **(c) Remove both interfaces; behaviors are the only mechanism** | One concept | Loses the ergonomic value (closed per-request-type units, no `next` to forget or call twice, readable intent) and the post-commit point that Wolverine's `AfterCommit` gives; users re-create the type-check boilerplate |
| **(d) Keep them as typed sugar adapted into behaviors placed by the stage system** | Keeps the ergonomics; the default stage `Application` is inside every gate; an optional declared stage and an explicit stable order remove the ordering ambiguity of MediatR #885; pre returns `Either` and can abort; post runs only on `Right`; an error hook and a post-commit hook cover the remaining use cases | Changes the signatures of both interfaces (pre-1.0: acceptable); adds two small interfaces and four internal adapter steps |

### Chosen Option: **(d) — Typed sugar adapted into staged steps**

### Rationale

- This is what MediatR does internally (`RequestPreProcessorBehavior` is an `IPipelineBehavior`) and what martinothamar/Mediator does with base classes; the only addition is the declared stage, which removes the documented pain (no control of order; `artifacts/research/pre-post-processors.md` section 2).
- Shape of the contracts (Phase 1):
  - `IRequestPreProcessor<in TRequest>.Process(TRequest, IRequestContext, CancellationToken)` returns `ValueTask<Either<EncinaError, Unit>>`; a `Left` aborts the pipeline before the inner steps run (Wolverine's `HandlerContinuation.Stop`).
  - `IRequestPostProcessor<in TRequest, TResponse>.Process(TRequest, IRequestContext, TResponse response, CancellationToken)` returns `ValueTask<Either<EncinaError, Unit>>`; it runs only when the inner pipeline returned `Right` (the LiteBus rule); its `Left` fails the request, because errors are never swallowed.
  - New `IRequestErrorProcessor<in TRequest, TResponse>.Process(TRequest, IRequestContext, EncinaError error, CancellationToken)` returns `ValueTask`; it runs only on `Left`, observes it and cannot replace it (replacing an error is catching, which belongs to a behavior).
  - New `IRequestPostCommitProcessor<in TRequest, TResponse>.Process(TRequest, IRequestContext, TResponse response, CancellationToken)` returns `ValueTask<Either<EncinaError, Unit>>`; its default position is the `Transaction` stage with `Order = -100`, outside `TransactionPipelineBehavior` (`Order = 0`), so it runs after the commit returned `Right`. Without a transaction behavior it runs after a successful inner pipeline. A `Left` from it fails the request even though the commit happened; its XML docs say that durable side effects belong in the outbox (#718), not in a post-commit hook.
- Placement: each processor type becomes one step with `(stage, order)` from its own `[PipelineStage]`, or from the registration overload (`cfg.AddRequestPreProcessor(type, stage, order)` and siblings), or from the default (`Application`, `Order = -100` for pre, post and error; `Transaction`, `Order = -100` for post-commit). At equal `(stage, order)` processor steps come before behaviors and are ordered by `Type.FullName` (ordinal), never by DI order. Rule R7 fails startup when a post-commit processor is declared in a stage inside `Transaction` (or with an `Order` not lower than every transaction behavior), where it would run before the commit.
- Anything that needs `next`, wrapping, timing or catching stays a behavior; the XML docs of the four interfaces and the how-to guide say so (Phase 9).
- The only built-in post-processor, `OutboxPostProcessor` (`src/Encina.Messaging/Outbox/OutboxPostProcessor.cs:14`), moves to the new signature in Phase 5 so it compiles and its store `Left`s are no longer discarded; #718 deletes it later.

</details>

<details>
<summary><strong>3. How a stage is declared (spike Q3) — attribute on the type, registration-time override</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) `[PipelineStage(stage, Order = n)]` on the type; `AddPipelineBehavior(type, stage, order)` overrides it** | The position travels with the type whatever registration path is used (raw `services.AddTransient`, assembly scanning, `TryAddEnumerable`, module adapters); an application can still move a type it does not own | Two sources with a precedence rule (registration beats attribute) |
| **(b) Registration metadata only (`AddPipelineBehavior(type, stage, order)`), no attribute** | One source; no attribute read | Behaviors registered through raw DI calls or scanning (`MediatorAssemblyScanner.cs:54-57`) arrive undeclared; every satellite would need to route its registration through `EncinaConfiguration` |

### Chosen Option: **(a) — Attribute plus registration-time override**

### Rationale

- Satellites register behaviors with raw DI calls today (for example `Encina.Security/ServiceCollectionExtensions.cs:81`); with an attribute their position is correct without touching how they register.
- The override is recorded in an internal singleton `PipelineStageRegistry` that every `AddEncina` call appends to (Phase 4); `IEncinaPipelineInspector` reports `PipelineStepSource.Registration` for an overridden step, so the precedence is visible.
- The attribute is read once per closed pipeline and cached (trim-safe; ADR-005 rejects source generators, so there is no compile-time ordering).

</details>

<details>
<summary><strong>4. The same behavior type twice in one pipeline (spike Q4) — collapse and log at Debug</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Fail startup** | Surfaces registration bugs | Punishes an application that calls an `AddEncina*` method twice, which `TryAdd*` registrations otherwise tolerate |
| **(b) Collapse to one instance and log EventId 178 at Debug; built-in registrations move to `TryAddEnumerable`** | Idempotent like `TryAddEnumerable`; running a behavior twice is never intended | Hides a misconfiguration behind a Debug log |

### Chosen Option: **(b) — Collapse and log at Debug; built-ins move to `TryAddEnumerable`**

### Rationale

- The duplicates the spike found are all plain `AddTransient`/`AddScoped` registrations (#1782 and the list in Summary); Phase 5 moves them to `TryAddEnumerable`, and an architecture rule keeps them there, so the collapse is a safety net, not the normal path.
- Identity of a step: the runtime type for a behavior, the wrapped module behavior type for `ModuleBehaviorAdapter<TModule, TRequest, TResponse>` (two different module behaviors on one module and request produce adapters of the same closed type and must not be collapsed), and the processor type for a processor step. The first occurrence (lowest registration index) is kept.

</details>

<details>
<summary><strong>5. Admission before or after authentication (spike Q5) — before</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) `Admission` (bulkhead, rate limit) runs before `Authentication`** | Overload is rejected cheaply before identity, HMAC or ABAC work; matches the intent stated in `Encina.Polly/ServiceCollectionExtensions.cs:55-58` | Anonymous floods take bulkhead slots; per-user partitioning must read the identity the host already put in `IRequestContext` |
| **(b) `Admission` runs after `Authorization`** | Unauthorized traffic never takes a slot | Expensive authorization lookups run without throttling |

### Chosen Option: **(a) — Admission before authentication**

### Rationale

- The identity is populated by the host before the pipeline starts (`IRequestContext.Identity`, #1705), so per-user rate-limit partitions still see it.
- `Admission` (200) also sits outside `Audit` (300): a request rejected for overload is not an access attempt, so it is not audited; it is still traced by the `Observability` stage (100).

</details>

<details>
<summary><strong>6. Application behaviors in the gate stages (spike Q6) — allowed, ordered among the built-ins</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Applications may place behaviors in `Authentication` and `Authorization`, with an `Order` between the built-ins** | Custom authentication schemes and custom authorization gates fit where they belong | A careless application behavior sits among the gates |
| **(b) Reserve both stages for `Encina.*` assemblies and fail startup otherwise** | The gate stages contain only Encina code | A custom gate must go to `Compliance` or later, after the built-in authorization |

### Chosen Option: **(a) — Allowed, ordered among the built-ins**

### Rationale

- Being among the gates is not a weakening: the built-in gates still run before every handler-facing stage, whatever the application adds.
- Built-in orders inside the gate stages leave room (HMAC 0, Security 10; AspNetCore authorization 0, ABAC 10); an application chooses an integer before, between or after them, and the inspector shows the result.

</details>

<details>
<summary><strong>7. Placement of the bidirectional encryption behavior — split into a decrypt step and an encrypt step</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Whole `EncryptionPipelineBehavior` in `HandlerPreparation` with `Order = -10` (before `SecretInjection` 0)** | No code change to the behavior; one step | `[DecryptOnReceive]` payloads are validated as ciphertext, and a validator of an `[Encrypt]` property sees the value before encryption only by accident of order, not by contract |
| **(b) Split the behavior into a decrypt step in `InputPreparation` and an encrypt step in `HandlerPreparation`** | Decrypt-on-receive runs before validation; request encryption runs after validation and before persistence; a validator sees plaintext of an `[Encrypt]` property | A behavior code change (the plan otherwise changes no behavior logic); two steps to register and test |

### Chosen Option: **(b) — Split decrypt (`InputPreparation`) and encrypt (`HandlerPreparation`)**

### Rationale

- `EncryptionPipelineBehavior.RunPreHandlerAsync` (`src/Encina.Security.Encryption/EncryptionPipelineBehavior.cs:129-155`) first decrypts `[DecryptOnReceive]` data, then encrypts the `[Encrypt]` request properties (`:148-152`, `EncryptRequestAsync` at `:173-188`); the response is encrypted afterwards (`:108-110`). One stage cannot serve both halves.
- Maintainer decision of 2026-10-10 on #2184 after the review of PR #2186 (D7).

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions and Models

> **Goal**: The public vocabulary of the contract, and the new processor contracts, with no behavior change yet.

<details>
<summary>Tasks</summary>

All in `src/Encina`, namespace `Encina` unless noted.

1. `Pipeline/Ordering/PipelineStage.cs`: `public enum PipelineStage` with the 16 members of Research, "Stage list and built-in placement" (`Observability = 100`, `Admission = 200`, `Audit = 300`, `Authentication = 400`, `Authorization = 500`, `Compliance = 600`, `Presentation = 700`, `InputPreparation = 800`, `Validation = 900`, `Caching = 1000`, `Resilience = 1100`, `DataRouting = 1200`, `Transaction = 1300`, `Idempotency = 1400`, `Application = 1500`, `HandlerPreparation = 1600`). XML docs per member: what runs there and the constraint it serves (C1-C14 of the spike).
2. `Pipeline/Ordering/PipelineStageAttribute.cs`: `[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)] public sealed class PipelineStageAttribute(PipelineStage stage) : Attribute` with `PipelineStage Stage { get; }` and `int Order { get; init; }` (lower is further out; negative values allowed).
3. `Pipeline/Ordering/PipelineStepDescription.cs`: `public sealed record PipelineStepDescription(Type StepType, PipelineStepKind Kind, PipelineStage Stage, int Order, PipelineStepSource Source)`; `PipelineStepKind` (`Behavior`, `ModuleBehavior`, `PreProcessor`, `PostProcessor`, `ErrorProcessor`, `PostCommitProcessor`); `PipelineStepSource` (`Attribute`, `Registration`, `Default`).
4. `Pipeline/Ordering/IEncinaPipelineInspector.cs`: `IReadOnlyList<PipelineStepDescription> Describe<TRequest, TResponse>() where TRequest : IRequest<TResponse>`; `IReadOnlyList<PipelineStepDescription> DescribeStream<TRequest, TItem>() where TRequest : IStreamRequest<TItem>`. Outermost first.
5. `Abstractions/IRequestPreProcessor.cs`: `ValueTask<Either<EncinaError, Unit>> Process(TRequest request, IRequestContext context, CancellationToken cancellationToken)`; XML docs: default stage `Application`, a `Left` aborts, "Runs before any behavior" (`:8`) is deleted.
6. `Abstractions/IRequestPostProcessor.cs`: `ValueTask<Either<EncinaError, Unit>> Process(TRequest request, IRequestContext context, TResponse response, CancellationToken cancellationToken)`; runs only on `Right`; the "Runs even when the handler returned a functional error" remark (`:11-12`) and the `Either` parameter are removed; fix the XML example (`:14-37`) to the new signature.
7. `Abstractions/IRequestErrorProcessor.cs` (new): `ValueTask Process(TRequest request, IRequestContext context, EncinaError error, CancellationToken cancellationToken)`; observe only.
8. `Abstractions/IRequestPostCommitProcessor.cs` (new): `ValueTask<Either<EncinaError, Unit>> Process(TRequest request, IRequestContext context, TResponse response, CancellationToken cancellationToken)`; default stage `Transaction`, `Order = -100`.
9. Interface XML fixes: `Abstractions/IPipelineBehavior.cs:11`, `Abstractions/IStreamPipelineBehavior.cs:21`, `Modules/IModulePipelineBehavior.cs:18` ("reverse registration order") become a reference to `PipelineStage` and `PipelineStageAttribute`.
10. `PublicAPI.Unshipped.txt` lines for every new symbol and the changed signatures (via `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina core (src/Encina). Pipeline behaviors run in DI registration order today (Pipeline/PipelineBuilder.cs:63-80); pre/post processors run outside every behavior (:82-83). Issue #2184 introduces a closed stage contract (plan docs/plans/pipeline-execution-order-implementation-plan-2184.md, ADR-049).
TASK: Add PipelineStage (16 members, values 100..1600 in the plan's order), PipelineStageAttribute (Stage, Order), PipelineStepDescription/PipelineStepKind/PipelineStepSource, IEncinaPipelineInspector; change IRequestPreProcessor.Process to return ValueTask<Either<EncinaError, Unit>>; change IRequestPostProcessor.Process to take TResponse (not Either) and return ValueTask<Either<EncinaError, Unit>>; add IRequestErrorProcessor and IRequestPostCommitProcessor; fix the "reverse registration order" XML in IPipelineBehavior.cs:11, IStreamPipelineBehavior.cs:21, Modules/IModulePipelineBehavior.cs:18. Do not change PipelineBuilder yet (Phase 2); make it compile with the new signatures only.
KEY RULES: .NET 10 / C# 14, nullable; XML docs with <example> on every public type; no [Obsolete], no compatibility overloads (pre-1.0: change signatures completely); every new public symbol in src/Encina/PublicAPI.Unshipped.txt through mechanical-fixer; CRAP <= 10 on every method you add or change.
REFERENCE FILES: src/Encina/Abstractions/IPipelineBehavior.cs, IStreamPipelineBehavior.cs, IRequestPreProcessor.cs, IRequestPostProcessor.cs, Modules/IModulePipelineBehavior.cs, Pipeline/PipelineBuilder.cs; artifacts/research/pre-post-processors.md (local, optional).
```

</details>

---

### Phase 2: Default Implementation (order resolver, step adapters, builders)

> **Goal**: The builders run steps in `(stage, order, tie-break)` order from a cached permutation; processors become steps.

<details>
<summary>Tasks</summary>

1. `Pipeline/Ordering/PipelineStageRegistry.cs` (internal, singleton): holds registration-time overrides `Type -> (PipelineStage, int)` appended by every `AddEncina` call (Phase 4), keyed by the open generic definition when the type is generic.
2. `Pipeline/Ordering/IPipelineStepIdentity.cs` (internal): `Type StepType { get; }` and `PipelineStage? DeclaredStage`, `int? DeclaredOrder`; implemented by `ModuleBehaviorAdapter<,,>` (returns the wrapped module behavior type and its attribute) and by the processor steps.
3. `Pipeline/Ordering/PipelineOrderResolver.cs` (internal, singleton; ctor `(PipelineStageRegistry registry, ILogger<PipelineOrderResolver> logger)`): `int[] Resolve(Type pipelineKey, IReadOnlyList<PipelineStepCandidate> candidates)` returns the execution order (outermost first). Key: stage, then order, then processor-before-behavior, then `Type.FullName` for processors and registration index for behaviors. Source precedence: registry, then attribute, then default (`Application`, `Order = 0` for behaviors; processor defaults from Design Choice 2). Collapses duplicate step identities (Design Choice 4). Cache: `ConcurrentDictionary<Type, CachedOrder>` keyed by the closed pipeline type; `CachedOrder` keeps the candidate type sequence and is recomputed only if the sequence differs (an O(N) reference comparison per `Build`). Runs the validator (Phase 3) before caching.
4. `Pipeline/Ordering/PipelineStepCandidate.cs` (internal `readonly record struct`): runtime type, step identity, kind, registration index.
5. `Pipeline/Steps/PreProcessorStep.cs`, `PostProcessorStep.cs`, `ErrorProcessorStep.cs`, `PostCommitProcessorStep.cs` (internal sealed, each `IPipelineBehavior<TRequest, TResponse>` + `IPipelineStepIdentity`, wrapping one processor instance): pre runs `Process` and returns its `Left` without calling `next`; post calls `next` and runs `Process` on `Right`; error calls `next` and runs `Process` on `Left`, returning the original `Left`; post-commit is post with its own default stage. Cancellation maps to the existing `EncinaErrorCodes.PreProcessorCancelled` / `PostProcessorCancelled` codes (`PipelineBuilder.cs:204-211`, `:230-237`); other exceptions propagate (fail fast, as today).
6. `Pipeline/PipelineBuilder.cs`: `Build` resolves behaviors and the four processor kinds, wraps processors in steps, asks the resolver for the permutation and composes the delegates in that order (same Russian-doll loop as `:72-80`, over the permuted array). `ExecutePipelineAsync` and the two processor executors (`:93-125`, `:191-240`) are deleted. Fast path: no processors registered means no step allocation. Remarks at `:29-31` and `:53-56` describe the stage order.
7. `Pipeline/StreamPipelineBuilder.cs`: the same permutation for `IStreamPipelineBehavior<,>` (`:65-80`); stream pipelines have no processors. Remarks at `:28-29`, `:55-57`.
8. `Modules/ModuleBehaviorAdapter.cs:29`: implement `IPipelineStepIdentity` (wrapped type, its attribute; default `Application`).
9. `Core/Encina.cs:179-180` and `Core/StreamDispatcher.cs:102`: pass the resolver (resolved once per scope) to the builders.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 added PipelineStage, PipelineStageAttribute and the processor contracts. PipelineBuilder (src/Encina/Pipeline/PipelineBuilder.cs) still composes behaviors in registration order (:72-80) and runs processors outside them (:82-125). StreamPipelineBuilder does the same for streams (:65-80).
TASK: Implement PipelineStageRegistry, IPipelineStepIdentity, PipelineStepCandidate and PipelineOrderResolver (order key: stage, order, processors before behaviors, then Type.FullName for processors / registration index for behaviors; source precedence registry > attribute > default; duplicate step identities collapsed keeping the first, EventId 178 at Debug; per-closed-pipeline cache validated by comparing the candidate type sequence). Add the four internal processor steps (pre aborts on Left; post runs on Right and its Left fails the request; error observes Left and returns it unchanged; post-commit = post with default stage Transaction, Order -100). Rewrite PipelineBuilder.Build and StreamPipelineBuilder.Build to compose in the resolved order; delete ExecutePipelineAsync and the processor executors. ModuleBehaviorAdapter implements IPipelineStepIdentity with the wrapped type.
KEY RULES: hot path: no LINQ in Build, no allocation when no processors are registered, cache hit is O(N); thread-safe first Build (one permutation per closed pipeline); exceptions other than cancellation propagate as today (pure ROP, fail fast); CRAP <= 10 on every method (extract small helpers; measure locally with the crap-gate table); defaults: behaviors Application/0, pre/post/error Application/-100, post-commit Transaction/-100.
REFERENCE FILES: src/Encina/Pipeline/PipelineBuilder.cs, src/Encina/Pipeline/StreamPipelineBuilder.cs, src/Encina/Modules/ModuleBehaviorAdapter.cs, src/Encina/Modules/ModuleBehaviorServiceCollectionExtensions.cs:59-69, src/Encina/Core/Encina.cs:165-184, src/Encina/Core/StreamDispatcher.cs:102, src/Encina/Errors (EncinaErrorCodes).
```

</details>

---

### Phase 3: Fail-Closed Startup Validation

> **Goal**: No request ever runs on an order that breaks a rule; misconfigurations stop the host.

<details>
<summary>Tasks</summary>

1. `Pipeline/Ordering/PipelineOrderValidator.cs` (internal static): `void Validate(Type pipelineKey, ReadOnlySpan<ResolvedStep> steps)` throws `InvalidOperationException` naming the pipeline, the step types and the broken rule. Rules:
   - **R1** the stage is a defined `PipelineStage` member (`Enum.IsDefined`), never an arbitrary cast integer.
   - **R2** duplicates of one step identity were collapsed (informational; never throws, Design Choice 4).
   - **R3** a step whose type is declared in a package shipped from this repository (the assembly is listed in `PipelineOrderValidator.BuiltInAssemblies`, a fixed list generated from `src/`, or carries `[assembly: EncinaBuiltInPipelineSteps]` defined in core) has an explicit stage (attribute or registry); a built-in on the default fails startup. The assembly-name prefix `Encina.` is not used: it also matches test projects and applications.
   - **R4** no `ServiceDescriptor` has the service type `ICommandPipelineBehavior<,>` or `IQueryPipelineBehavior<,>` (open, or closed over any arguments): such a registration is never resolved by the builder (the #2032 class). Checked once on the `IServiceCollection` snapshot (see task 3), not per pipeline.
   - **R5** two built-in steps in one closed pipeline do not share `(stage, order)` (for example two read/write routers or two transaction behaviors from different provider families registered together).
   - **R6** `PipelineStageRegistry` holds no two registration-time declarations of one type with different `(stage, order)` (for example two `AddEncina` calls that place the same behavior differently).
   - **R7** a post-commit processor must run after every transaction behavior has returned: it is declared in `Transaction` with an `Order` lower than every transaction behavior in the pipeline, or in a stage outside (before) `Transaction`; any other placement fails startup.

   **Enablement**: the validator code may land with Phases 1-4, but R3, R4 and the registration of `PipelineOrderValidationHostedService` are switched on only in the Phase 5 PR (the one that migrates the built-ins), so `main` never contains a validator that rejects a built-in the plan has not yet migrated. If Phases 1-5 ship in one pull request, they are enabled there.
2. `Pipeline/Ordering/PipelineOrderValidationHostedService.cs` (internal sealed, `IHostedService`; ctor `(IServiceProvider, PipelineRegistrationSnapshot, PipelineOrderResolver, ILogger<...>)`): in `StartAsync`, opens a scope and, for every closed `IRequestHandler<TRequest, TResponse>` and `IStreamRequestHandler<TRequest, TItem>` service type in the snapshot, reads the registered descriptors (`ImplementationType`, closing open generics over the handler's type arguments) and instantiates only factory-registered steps (`ModuleBehaviorAdapter`), because ordering depends on types, not instances; it then calls the resolver (which validates and warms the cache). A constructor that throws at start is reported as a resolution error naming the step type, separate from the order rules. Open-generic handlers are skipped (they are validated lazily on first use). Logs EventId 180 (Error) with type names only, then throws, so `StartAsync` fails and the host does not start.
3. `Pipeline/Ordering/PipelineRegistrationSnapshot.cs` (internal singleton, created by the first `AddEncina` call with a reference to the `IServiceCollection` itself): read in `StartAsync`, when the collection holds every descriptor the host added (including those added after `AddEncina`, such as `AddShadowSharding`); yields the closed handler service types and the R4 offenders. Without a host, R4 is checked by the resolver on first use from the same snapshot.
4. Lazy path: the resolver validates every new closed pipeline before caching it, so an application without a host fails on the first `Send` of that pipeline, before any step or handler runs. `Encina.cs` lets the `InvalidOperationException` propagate (a configuration bug, fail fast; the handler call count is 0).
5. No configuration switch: an order that cannot be proven safe never runs (the maintainer chose no `PipelineOrderingOptions`, because Q1 and Q4 are fixed policies).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phase 2 added PipelineOrderResolver and the step adapters. The contract must fail closed: a pipeline whose order breaks a rule never runs, at host start or, without a host, before its first request.
TASK: Implement PipelineOrderValidator with rules R1-R7 of the plan (R3 keys on the fixed BuiltInAssemblies list or the EncinaBuiltInPipelineSteps assembly marker, never on the "Encina." name prefix; R3, R4 and the hosted-service registration are enabled only in the Phase 5 PR; the hosted service reads descriptors and instantiates only factory-registered steps; R4 flags any ICommandPipelineBehavior<,>/IQueryPipelineBehavior<,> service registration, the #2032 class); PipelineRegistrationSnapshot; PipelineOrderValidationHostedService that resolves every closed request and stream pipeline in a scope at StartAsync, logs EventId 180 with type names only and throws InvalidOperationException. The resolver validates before caching so the lazy path is covered.
KEY RULES: fail closed (AGENTS.md section 3); never log EncinaError.Message; the exception message names pipeline, step types and rule id; no options class; Microsoft.Extensions.Hosting.Abstractions is already referenced by src/Encina/Encina.csproj:18; follow the hosted-service registration style of src/Encina/Modules/ModuleServiceCollectionExtensions.cs:86; CRAP <= 10.
REFERENCE FILES: src/Encina/Core/ServiceCollectionExtensions.cs:41-91, src/Encina/Abstractions/CqrsContracts.cs:113,:129, src/Encina/Sharding/Shadow/ShadowShardingServiceCollectionExtensions.cs:49-55, src/Encina/Core/EncinaConfiguration.cs:237-253, src/Encina/Modules/ModuleLifecycleHostedService.cs.
```

</details>

---

### Phase 4: Configuration and DI

> **Goal**: Registration-time declarations, the inspector, and the core registrations, proven with `ValidateOnBuild` and `ValidateScopes`.

<details>
<summary>Tasks</summary>

1. `Core/EncinaConfiguration.cs`: add `AddPipelineBehavior(Type pipelineBehaviorType, PipelineStage stage, int order = 0)` and `AddPipelineBehavior<TBehavior>(PipelineStage stage, int order = 0)`; the same overloads for `AddRequestPreProcessor`, `AddRequestPostProcessor`; new `AddRequestErrorProcessor(Type)`/`<T>()` and `AddRequestPostCommitProcessor(Type)`/`<T>()` with and without stage; new `AddStreamPipelineBehavior(Type)`/`<T>()` with and without stage. Overrides are stored in a list and copied into `PipelineStageRegistry` at the end of `AddEncina`. The existing validation of concrete, assignable types (`:121-141`, `:153-205`) is reused.
2. `Core/EncinaConfiguration.cs:237-253`: `RegisterPipelineBehavior` registers only `IPipelineBehavior<,>`; the `ICommandPipelineBehavior<,>` / `IQueryPipelineBehavior<,>` descriptors (`:242-251`) are deleted (they are never resolved; R4 would now reject them).
3. `Core/ServiceCollectionExtensions.cs:41-91`: `TryAddSingleton<PipelineStageRegistry>` (shared across `AddEncina` calls), `TryAddSingleton<PipelineOrderResolver>`, `TryAddSingleton<PipelineRegistrationSnapshot>`, `TryAddScoped<IEncinaPipelineInspector, EncinaPipelineInspector>`, `TryAddEnumerable` of the hosted service; register scanned error and post-commit processors (new scanner cases, task 4).
4. `EncinaAssemblyScanner` (`Dispatchers/MediatorAssemblyScanner.cs:40-82`): scan `IRequestErrorProcessor<,>` and `IRequestPostCommitProcessor<,>`; delete the shadow sharding skip (`:107-113`) only if #2032 has registered them as `IPipelineBehavior<,>` (otherwise keep it; the skip exists because their dependencies are optional).
5. `Pipeline/Ordering/EncinaPipelineInspector.cs` (internal sealed): resolves candidates in its own scope, runs the resolver (validation included) and maps to `PipelineStepDescription`.
6. DI test (unit): `AddEncina` with every new registration builds with `ValidateOnBuild = true` and `ValidateScopes = true`; a second `AddEncina` call does not duplicate the registry, resolver or hosted service.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1-3 added the stage model, resolver, steps and validator. EncinaConfiguration (src/Encina/Core/EncinaConfiguration.cs) has AddPipelineBehavior(Type) and processor registration without stages, and registers dead ICommand/IQueryPipelineBehavior descriptors at :242-251.
TASK: Add the stage-aware registration overloads (behaviors, the four processor kinds, stream behaviors), copy overrides into PipelineStageRegistry, delete the dead ICommand/IQuery descriptors, register registry/resolver/snapshot as singletons, the inspector as scoped and the validation hosted service with TryAddEnumerable, extend the scanner to the two new processor interfaces, implement EncinaPipelineInspector.
KEY RULES: registration completeness (AGENTS.md section 3): a DI test builds the provider with ValidateOnBuild and ValidateScopes; TryAdd* everywhere so repeated AddEncina calls are idempotent; XML docs with examples on every new public overload; PublicAPI.Unshipped.txt via mechanical-fixer; CRAP <= 10.
REFERENCE FILES: src/Encina/Core/EncinaConfiguration.cs, src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina/Dispatchers/MediatorAssemblyScanner.cs, tests/Encina.UnitTests/Core/PipelineBehaviorRegistrationTests.cs.
```

</details>

---

### Phase 5: Built-in Behavior Migration (all packages)

> **Goal**: Every built-in behavior declares its stage and is registered idempotently; the order instructions that become false are deleted.

<details>
<summary>Tasks</summary>

1. Add `[PipelineStage(PipelineStage.X, Order = n)]` to every type in Research, "Stage list and built-in placement" (about 55 types, the 8 `ReadWriteRoutingPipelineBehavior`s and `EventPublishingPipelineBehavior` included). Internal behaviors (`SecretInjectionPipelineBehavior`, `InputSanitizationPipelineBehavior`, `OutputEncodingPipelineBehavior`, the shadow behaviors) get it too.
2. Move every plain registration to `TryAddEnumerable(ServiceDescriptor.Transient|Scoped(typeof(IPipelineBehavior<,>), typeof(X<,>)))`, keeping the current lifetime: `Encina.Polly/ServiceCollectionExtensions.cs:56,:59,:62,:65,:145`; `Encina.Caching/ServiceCollectionExtensions.cs:81,:86,:91`; `Encina.Compliance.LawfulBasis/ServiceCollectionExtensions.cs:105`; `Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs:102`; `Encina.Extensions.Resilience/ServiceCollectionExtensions.cs:89`; `Encina.Messaging/MessagingServiceCollectionExtensions.cs:90,:120,:292,:535` and `Recoverability/RecoverabilityServiceCollectionExtensions.cs:68` (this closes the double registration of #1782); `Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:224,:272,:300`; `Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:200`; `Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs:201`; `Encina.MongoDB/ServiceCollectionExtensions.cs:824`; `Encina.Security.Audit/ServiceCollectionExtensions.cs:89`.
3. `Encina.Messaging/MessagingServiceCollectionExtensions.cs:273`: `OutboxPostProcessor` moves to `TryAddEnumerable` and to the new `IRequestPostProcessor` signature; its store results become `Left`s it returns (today they are discarded, `OutboxPostProcessor.cs:95,:98`). If #718 has already deleted it, skip this task.
4. Delete the order instructions that the contract makes false and replace them with a stage reference: `Encina.Polly/ServiceCollectionExtensions.cs:55,:58` comments; `Encina.Compliance.LawfulBasis/ServiceCollectionExtensions.cs:38-40`; `Encina.Messaging/SoftDelete/SoftDeleteQueryFilterBehavior.cs:26-34`; `Encina.Messaging/MessagingServiceCollectionExtensions.cs:516-517`; any other order instruction found by `Get-ChildItem src -Recurse -Include *.cs,*.md | Select-String -Pattern 'reverse registration order|registration order|Register before AddEncinaABAC|AddEncinaLawfulBasis. before|Validation.{0,20}Authorization|Pipeline order: '` (for example `src/Encina.Security.ABAC/README.md:92`). Review each hit: only instructions about the order of behaviors are deleted; `TryAdd` override remarks (register your implementation before `AddEncinaX`) stay.
5. Prerequisites checked before this phase starts: #2032 (shadow behaviors registered as `IPipelineBehavior<,>`; otherwise R4, enabled in this phase, fails every shadow-sharding host), #2029 (the four missing read/write registrations) and #2030 (Marten event publishing registration), because the reachability contract test of Phase 8 fails on them.
6. Enable R3, R4 and the registration of `PipelineOrderValidationHostedService` (Phase 3 enablement note) in this PR.
7. Split `EncryptionPipelineBehavior` (maintainer decision D7, Design Choice 7) into a decrypt step in `InputPreparation` (`[DecryptOnReceive]` data, Order 10) and an encrypt step in `HandlerPreparation` (`[Encrypt]` request properties and response encryption, Order -10, before `SecretInjection` 0). This is a behavior code change, the one exception to "do not change any behavior's logic"; keep the existing logic of both halves unchanged and register both steps in `AddEncinaEncryption` with `TryAddEnumerable`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The stage contract is in core (Phases 1-4). About 55 built-in behaviors in about 40 packages carry no stage, and about 20 registrations use plain AddTransient/AddScoped, so a second AddEncina* call runs them twice (#1782).
TASK: Add [PipelineStage(stage, Order = n)] to every built-in behavior exactly as in the plan's "Stage list and built-in placement" table (note: Idempotency is inside Transaction because of ADR-048; DistributedIdempotency is in Caching); move every behavior registration listed in Phase 5 task 2 to TryAddEnumerable keeping its lifetime; port OutboxPostProcessor to the new post-processor signature returning its store Lefts (skip if #718 deleted it); delete the order instructions listed in task 4 and replace them with a reference to the stage.
KEY RULES: do not change any behavior's logic, with ONE exception: split EncryptionPipelineBehavior into a decrypt step (InputPreparation, Order 10) and an encrypt step (HandlerPreparation, Order -10), moving the existing code of each half unchanged (task 7, decision D7); enable R3, R4 and the validation hosted service in this PR only; one registration per type; keep lifetimes; check that #2029, #2030 and #2032 are merged before starting and stop if not (report it); XML docs mention the stage; English only; CRAP <= 10 on any method you touch; no [Obsolete].
REFERENCE FILES: the plan's stage table (Research), src/Encina.Polly/ServiceCollectionExtensions.cs, src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina.Messaging/Recoverability/RecoverabilityServiceCollectionExtensions.cs, src/Encina.Caching/ServiceCollectionExtensions.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, docs/architecture/adr/048-inbox-record-vs-business-transaction.md.
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: The functions marked ✅ in the matrix are wired: module isolation, audit placement, transactions and idempotency per ADR-048, and the #751 regression.

<details>
<summary>Tasks</summary>

1. Module isolation: `ModuleBehaviorAdapter` reports the wrapped behavior's stage (Phase 2 task 8); a module behavior with `[PipelineStage(Validation)]` runs in `Validation`, not in registration order. `ModuleExecutionContextBehavior` (`Encina.EntityFrameworkCore/Modules/ModuleExecutionContextBehavior.cs:49`) sits in `DataRouting`, outside `Transaction`.
2. Audit trail (placement only): `AuditPipelineBehavior` (`Encina.Security.Audit/AuditPipelineBehavior.cs:34`) in `Audit`, outside `Authentication`, `Authorization` and `Validation`, so the `Left`s that `MapErrorToOutcome` classifies (`:111`, `:195-205`) reach it; it stays outside `Transaction`, so a rolled-back command is still audited. No new audit event. The message-text classification itself is #1642.
3. Transactions and idempotency (ADR-048): `Transaction` (1300) is outside `Idempotency` (1400), so the inbox's enlisted `MarkAsProcessedAsync` joins the business transaction for every registration order; the two existing tests that pin the order by registration index (`MessagingServiceCollectionExtensionsRecoverabilityTests.cs:39`, `InboxStoreEFIsolationTests.cs:45`) are rewritten to assert the order through `IEncinaPipelineInspector`. `Resilience` (1100) is outside `Transaction`, so every retry attempt gets a fresh transaction and the inbox records one failure per attempt (ADR-048, "`MarkAsFailedAsync` is the only place where `RetryCount` grows").
4. #751 regression: with `AddEncinaABAC` registered before `AddEncinaSecurity`, an anonymous request is denied by `SecurityPipelineBehavior` (`Authentication`, Order 10) and `ABACPipelineBehavior` (`Authorization`, Order 10) is never evaluated. This replaces the 9090 warning that #751 dropped (decision A6).
5. Coordinate with #718: its outbox behavior is born with `[PipelineStage(PipelineStage.Transaction, Order = 10)]`, inside `TransactionPipelineBehavior` (Order 0) and outside the inbox.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: The built-ins carry stages (Phase 5). Cross-cutting functions that depend on the order: module isolation (ModuleBehaviorAdapter, ModuleExecutionContextBehavior), audit (AuditPipelineBehavior must see authorization and validation Lefts and rolled-back commands), transactions and idempotency (ADR-048: inbox inside the business transaction), and the #751 ordering problem.
TASK: Verify and pin each integration of Phase 6 with tests that build the real registrations (AddEncinaSecurity, AddEncinaABAC, AddEncinaAudit, AddMessagingServices with UseTransactions and UseInbox, the EF Core registration, a module behavior) and assert the order through IEncinaPipelineInspector and through execution; rewrite the two registration-index tests named in task 3 to use the inspector; add the #751 regression for both registration orders.
KEY RULES: tests execute real package code (no reflection-only asserts); Shouldly through Encina.Testing.Shouldly; one behavior per test; no Thread.Sleep; do not change ABAC, Audit or inbox logic (only order); record the #718 coordination in the PR description.
REFERENCE FILES: docs/architecture/adr/048-inbox-record-vs-business-transaction.md, src/Encina.Security.Audit/AuditPipelineBehavior.cs, src/Encina.Security.ABAC/ABACPipelineBehavior.cs, src/Encina/Modules/ModuleBehaviorAdapter.cs, tests/Encina.UnitTests/Messaging/MessagingServiceCollectionExtensionsRecoverabilityTests.cs, tests/Encina.UnitTests/EntityFrameworkCore/Inbox/InboxStoreEFIsolationTests.cs.
```

</details>

---

### Phase 7: Observability

> **Goal**: The four log events of the contract, in the registered `Core` range.

<details>
<summary>Tasks</summary>

1. `Diagnostics/PipelineOrderLog.cs` (internal static partial, `[LoggerMessage]` source generator), XML doc "Event IDs: 177-180 (see EventIdRanges.Core)":
   - 177 `UndeclaredBehaviorPlaced` (Information): an undeclared custom behavior was placed in `Application` (Design Choice 1); once per type (a `ConcurrentDictionary<Type, byte>` guard in the resolver).
   - 178 `DuplicateStepCollapsed` (Debug): a duplicate step identity was collapsed (Design Choice 4).
   - 179 `PipelineOrderResolved` (Debug): the resolved order of a closed pipeline as `stage:type` pairs, once per closed pipeline.
   - 180 `PipelineOrderInvalid` (Error): validation failed; rule id and type names only, logged before the exception.
2. `EventIdRanges.Core` is already registered (`src/Encina/Diagnostics/EventIdRanges.cs:31`) and `Encina` is already mapped to it in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; no new range. Before coding, re-read the highest EventId in `src/Encina` (175 today, `src/Encina/Diagnostics/RequestIdentityLog.cs:69`) and the open #2088, whose proposed fix claims 176; take the next four free ids and update this plan if they moved.
3. No `ActivitySource` or `Meter`: the order is computed once per closed pipeline and each behavior keeps its own spans and metrics (matrix rows 2 and 3).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The resolver and validator (Phases 2-3) need four log events. The Core range is EventIdRanges.Core = (100, 199) (src/Encina/Diagnostics/EventIdRanges.cs:31); 100-175 are used; #2088's proposed fix claims 176.
TASK: Add Diagnostics/PipelineOrderLog.cs with [LoggerMessage] events 177 UndeclaredBehaviorPlaced (Information, once per type), 178 DuplicateStepCollapsed (Debug), 179 PipelineOrderResolved (Debug, once per closed pipeline), 180 PipelineOrderInvalid (Error); call them from the resolver and validator; run the EventId architecture tests.
KEY RULES: ADR-021: packed sequentially, inside the registered range, XML doc names the range; log type names and rule ids only, never EncinaError.Message or request data; re-verify the next free id before coding (another PR may have taken 176-180).
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina/Diagnostics/RequestIdentityLog.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, docs/architecture/adr/021-eventid-uniqueness-enforcement.md.
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag of the touched files reaches its per-file target; the contract is proven on the real packages.

<details>
<summary>Tasks</summary>

1. **Unit** (`tests/Encina.UnitTests/Core/Pipeline/Ordering/`):
   - Sorting by stage, then order, then the tie-break (processors by name before behaviors; behaviors by registration index; `EncinaTests.cs:18` keeps its expected order for two custom behaviors, whose types live in the `Encina.UnitTests` assembly and so land in `Application` with EventId 177).
   - A validator sees the plaintext of an `[Encrypt]` property (decrypt step in `InputPreparation`, encrypt step in `HandlerPreparation`, decision D7).
   - A step constructor that throws at host start is reported as a resolution error naming the step type, not as an order rule.
   - Every permutation of the registration order of Security, ABAC, Audit, a validation package, Caching, Polly and Messaging gives the same `Describe()` result.
   - Registration override beats the attribute; an undeclared custom behavior goes to `Application` and logs 177 once.
   - Duplicates collapsed (178), including two different module behaviors on one module and request that are not collapsed.
   - Each rule R1, R3, R4, R5, R6, R7 fails with the rule id and type names; the handler call count is 0; the hosted service fails `StartAsync`.
   - Processor steps: pre `Left` aborts; post runs only on `Right` and its `Left` fails the request; error runs only on `Left` and returns it unchanged; post-commit runs after `TransactionPipelineBehavior` commits and never after a rollback.
   - Concurrent first `Build` yields one permutation; the cache recomputes when the candidate sequence changes.
   - DI: `ValidateOnBuild` and `ValidateScopes` for the new registrations; repeated `AddEncina` idempotent.
   - Phase 6 integrations (module, audit, ADR-048 order, #751 regression).
2. **Guard** (`tests/Encina.GuardTests/Core/Pipeline/`): every new public overload of `EncinaConfiguration` and the `IEncinaPipelineInspector` implementation (`PipelineStageAttribute` has no guard test: its only constructor parameter is an enum); extend `PipelineBuilderGuardTests.cs`.
3. **Contract** (`tests/Encina.ContractTests/Core/PipelineStages/` plus a rule in `src/Encina.Testing.Architecture/EncinaArchitectureRules.cs` next to `PipelineBehaviorsShouldImplementCorrectInterface` at `:600-616`):
   - Every `IPipelineBehavior<,>` / `IStreamPipelineBehavior<,>` implementation in an assembly of `src/` (the architecture rule loads the `src/` assemblies explicitly, so test projects and applications named `Encina.*` are not caught) carries `PipelineStageAttribute`.
   - No two built-ins share `(stage, order)` except the declared provider variants (read/write routers, the two transaction behaviors), which R5 forbids in one pipeline.
   - Reachability: for every built-in step type, a table row names the `AddEncina*` call and options that enable it; the test runs that call and asserts the type appears in `Describe` (the #2029/#2030 class).
   - Precedence C1-C14 (with the ADR-048 correction) holds over the resolved real registrations, not only over attributes.
   - `Transaction` stage contract: each transaction behavior begins, commits and rolls back through the async API (a connection whose synchronous `Open`/`BeginTransaction` throws).
   - Architecture rule: no `AddTransient`/`AddScoped` of an open `IPipelineBehavior<,>` in `src/` (only `TryAddEnumerable`).
4. **Property** (`tests/Encina.PropertyTests/Core/PipelineOrderingProperties.cs`, FsCheck through `Encina.Testing.FsCheck`): random sets of steps with random stages and orders under random registration permutations give a total order that is permutation-invariant when keys are unique; no step of a stage after `Authorization` ever precedes the last `Authorization` step; extend `ConfigurationProperties.cs:283` (`Send_ComposesPipelineDeterministically`) to processors in stages.
5. **Benchmark** (`tests/Encina.BenchmarkTests/Encina.Benchmarks/PipelineOrderingBenchmarks.cs`): `Send` with 0, 5 and 15 behaviors and with 0 and 3 processors, before and after; registered with `BenchmarkSwitcher` (verify the filter with `--list flat`); results under `artifacts/performance/`.
6. **Integration**: justification file `tests/Encina.IntegrationTests/Core/PipelineStages.md` (no database or external dependency; DI-level tests use the real packages). **Load**: justification file `tests/Encina.LoadTests/Core/PipelineStages.md` (the only concurrency is the one-time cache, covered by the unit test).
7. Coverage manifest `.github/coverage-manifest/Encina.json` (package defaults unit 70, guard 20, contract 15): per-file targets with one-sentence justifications for every new and touched file, proposed: `PipelineOrderResolver.cs` unit 95, guard 0 (internal, no public parameters), property 70, contract 60; `PipelineOrderValidator.cs` unit 95, contract 40; step adapters unit 95; `PipelineBuilder.cs` unit 95, guard 40, property 60; `StreamPipelineBuilder.cs` unit 90, guard 40; `EncinaConfiguration.cs` unit 90, guard 60; `PipelineOrderValidationHostedService.cs` unit 90; `EncinaPipelineInspector.cs` unit 90, guard 50, contract 60. Every file in the Estimated File Count table gets a per-file entry in its package manifest (new entries where none exists) with per-flag targets and a one-sentence justification. Declarative files (`PipelineStage`, `PipelineStageAttribute`, `PipelineStepDescription`, `PipelineStepCandidate`, `IPipelineStepIdentity`, `PipelineOrderLog`) use `unit 0` with the reason "no executable logic", or the flag that exercises them. New logic files get entries too: `PipelineStageRegistry.cs` unit 95; `PipelineRegistrationSnapshot.cs` unit 90; `PipelineOrderLog.cs` unit 0 (generated logging, exercised through the resolver tests). Touched files: `EncinaAssemblyScanner` (`MediatorAssemblyScanner.cs`) unit 90; `ServiceCollectionExtensions.cs` unit 90, guard 60; `Encina.cs` unit 90; `StreamDispatcher.cs` unit 90; `ModuleBehaviorAdapter.cs` unit 90; `OutboxPostProcessor.cs` unit 90. Satellite files touched only for the attribute or `TryAddEnumerable` keep their targets and get an entry if they have none. Measure with `coverage-report.cs` and `--check-justifications` before the PR.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: The stage contract (Phases 1-7) changes how every request pipeline is composed. Tests must prove the contract on the real Encina packages, not only on fakes.
TASK: Write the unit, guard, contract, property and benchmark tests of Phase 8, the reachability table test, the architecture rule against plain AddTransient/AddScoped of IPipelineBehavior<,>, the Transaction stage async contract test, the two justification files (integration, load), and the per-file coverage targets with justifications in .github/coverage-manifest/Encina.json.
KEY RULES: tests execute real package code (no reflection-only tests); Shouldly via Encina.Testing.Shouldly, FsCheck via Encina.Testing.FsCheck; AAA, one behavior per test, deterministic, no Thread.Sleep; BenchmarkSwitcher, never BenchmarkRunner.Run<T>; outputs under artifacts/; each flag must reach its own per-file target (measure with dotnet run --file .github/scripts/coverage-report.cs and --check-justifications); CRAP <= 10 on every changed method.
REFERENCE FILES: tests/Encina.UnitTests/Core/EncinaTests.cs, tests/Encina.UnitTests/Core/PipelineBehaviorRegistrationTests.cs, tests/Encina.GuardTests/Core/Pipeline/PipelineBuilderGuardTests.cs, tests/Encina.PropertyTests/ConfigurationProperties.cs:283, tests/Encina.BenchmarkTests/Encina.Benchmarks/MediatorBenchmarks.cs, src/Encina.Testing.Architecture/EncinaArchitectureRules.cs:600-616, .github/coverage-manifest/Encina.json, docs/testing/coverage-measurement-methodology.md.
```

</details>

---

### Phase 9: Documentation and Finalization (always last)

> **Goal**: ADR-049, the pages that described registration order, the changelog and the public API.

<details>
<summary>Tasks</summary>

1. `docs/architecture/adr/049-pipeline-stages-execution-order.md`: ADR-049 "Pipeline stages: a closed, core-owned execution-order contract for behaviors" (context, the 16 stages and constraints, Q1-Q6, the ADR-048 correction, the validation rules, alternatives A/B/#1678-B/C from the spike, consequences); move its row from "Reserved numbers" to the ADR table of `docs/architecture/adr/index.md`; link it from ADR-018's cross-cutting table.
2. New explanation page `docs/architecture/pipeline-stages.md` (Diátaxis explanation; `encina-docs` skill; delegated to `docs-writer`): a Mermaid diagram of the stages and the built-in table.
3. Rewrite the order passages: `docs/architecture/request-pipeline.md` (`:30` and the composition section); `docs/features/pipeline-behaviors.md` (the "Ordering notes" column at `:16` becomes "Stage / Order"; `:22`); `docs/guides/how-to-write-a-pipeline-behavior.md` section 4 (`:91-93`): declaring a stage, and when to write a processor instead of a behavior; `docs/architecture/patterns-guide.md` and `docs/architecture/extensibility-analysis.md` (the order diagrams and "reverse registration order"; re-read the line numbers before editing); `src/Encina.AspNetCore/README.md:592,:597,:612`; `src/Encina.DataAnnotations/README.md:223`; `src/Encina.Security.ABAC/README.md:92`; the ADR-002, ADR-007 and component-diagram mentions of pre/post processors.
4. `changelog.d/2184-pipeline-stages.changed.md` (breaking: stage-ordered pipeline, processor signatures, post-processors only on `Right`, removed `ICommand/IQueryPipelineBehavior` descriptors) and `changelog.d/2184-pipeline-stages.added.md` (stages, attribute, inspector, error and post-commit hooks).
5. `src/Encina/PublicAPI.Unshipped.txt` complete (and the satellites' files if an attribute changes a public surface; it does not for attributes alone); `docs/INVENTORY.md` (new files); `ROADMAP.md` and `docs/releases/v0.14.0/README.md` if the milestone notes list pipeline work.
6. Build: `dotnet build Encina.slnx --configuration Release` with 0 errors and 0 warnings; tests: `dotnet test Encina.slnx --configuration Release` all green; every coverage flag at its per-file target; local CRAP table of the changed methods at or under 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT: The pipeline stage contract (#2184) is implemented and tested (Phases 1-8). ADR-049 is reserved in docs/architecture/adr/index.md. Several pages and READMEs describe registration order or recommend Validation before Authorization.
TASK: Write ADR-049 and move its index row; write docs/architecture/pipeline-stages.md (explanation, Mermaid diagram, built-in table) through docs-writer; rewrite the order passages listed in Phase 9 task 3; add the two changelog fragments; complete PublicAPI.Unshipped.txt, docs/INVENTORY.md and the release notes; run the Release build and the full test suite and the coverage and CRAP checks.
KEY RULES: encina-docs skill (one Diátaxis quadrant per page, junior-readable English, no hand-typed coverage figures, link ADRs and SPECs, no emojis); never edit CHANGELOG.md [Unreleased] by hand; zero warnings; English only; no AI attribution.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, docs/architecture/adr/index.md, docs/architecture/adr/018-cross-cutting-integration-principle.md, docs/architecture/request-pipeline.md, docs/features/pipeline-behaviors.md, docs/guides/how-to-write-a-pipeline-behavior.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards and Prior Art

| Source | Relevance to this plan |
|--------|------------------------|
| ASP.NET Core MVC filters (fixed filter types, `IOrderedFilter.Order` inside a type) | Closest precedent: fixed coarse stages in the framework, fine order only inside a stage |
| NServiceBus stages and `RegisterStep` with `InsertBefore`/`InsertAfter` | Stages fixed by context type; throws on duplicate or missing step ids; implicit order inside a stage regressed in a patch release |
| Wolverine middleware phases (`Before`, handler, `After`, commit, outbox flush, `AfterCommit`, `Finally`) | Phases remove cross-cutting order questions; `AfterCommit` is the model of `IRequestPostCommitProcessor` |
| MediatR pipeline behaviors and processors (12.1 stopped scanning behaviors; issue #885 on processor order) | Registration order only; processors are behaviors at an unspecified slot; the documented pain this plan removes |
| martinothamar/Mediator and LiteBus | Processors as sugar over behaviors (Mediator); pre, post and error stages, post only on success (LiteBus) |
| ASP.NET Core middleware order and analyzer ASP0001 | Order of `Use*` calls is "critical for security" and only partially checked; shows the cost of registration-order contracts |
| OWASP ASVS V4 (access control) and V5 (validation) | Authorization before data access and before revealing validation details (C1, the AspNetCore README fix) |
| GDPR Art. 5(2) and 30 (accountability), SPEC-002 | Denied attempts are audit evidence: `Audit` outside the gates (C3) |
| ADR-018 (cross-cutting functions), ADR-021 (EventIds), ADR-005 (no source generators), ADR-048 (inbox inside the business transaction) | Constraints on placement, logging and the mechanism |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `PipelineBuilder<TRequest, TResponse>` | `src/Encina/Pipeline/PipelineBuilder.cs:34-241` | Composition loop reused over the permuted array; processor executors deleted |
| `StreamPipelineBuilder<TRequest, TItem>` | `src/Encina/Pipeline/StreamPipelineBuilder.cs:32-108` | Same permutation for stream behaviors |
| `EncinaConfiguration` | `src/Encina/Core/EncinaConfiguration.cs:14-274` | Stage-aware overloads; dead descriptors `:242-251` removed |
| `AddEncina` | `src/Encina/Core/ServiceCollectionExtensions.cs:41-91` | Registers resolver, registry, snapshot, inspector, hosted service |
| `EncinaAssemblyScanner` (`Dispatchers/MediatorAssemblyScanner.cs`) | `src/Encina/Dispatchers/MediatorAssemblyScanner.cs:40-128` | Scans the two new processor interfaces |
| `ModuleBehaviorAdapter<,,>` | `src/Encina/Modules/ModuleBehaviorAdapter.cs:29`; registered by factory at `ModuleBehaviorServiceCollectionExtensions.cs:59-69,:109` | Reports the wrapped behavior's stage and identity |
| `RequestHandlerWrapper` | `src/Encina/Core/Encina.cs:165-184` | Creates the builder per `Send` |
| `ICommandPipelineBehavior<,>`, `IQueryPipelineBehavior<,>` | `src/Encina/Abstractions/CqrsContracts.cs:113,:129` | Kept as type constraints; never a service type (R4) |
| Hosted-service pattern | `src/Encina/Modules/ModuleServiceCollectionExtensions.cs:86`; `Microsoft.Extensions.Hosting.Abstractions` at `src/Encina/Encina.csproj:18` | Startup validation |
| `EventIdRanges.Core` | `src/Encina/Diagnostics/EventIdRanges.cs:31` | EventIds 177-180 |
| `EncinaArchitectureRules.PipelineBehaviorsShouldImplementCorrectInterface` | `src/Encina.Testing.Architecture/EncinaArchitectureRules.cs:600-616` | Sibling rules: attribute present, `TryAddEnumerable` only |
| Order tests to rewrite | `tests/Encina.UnitTests/Core/EncinaTests.cs:18`, `tests/Encina.PropertyTests/ConfigurationProperties.cs:283`, `tests/Encina.UnitTests/Messaging/MessagingServiceCollectionExtensionsRecoverabilityTests.cs:39`, `tests/Encina.UnitTests/EntityFrameworkCore/Inbox/InboxStoreEFIsolationTests.cs:45` | Kept or moved to the inspector |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (core) | `Core` 100-199 (`EventIdRanges.cs:31`) | 177-180 new, packed: 177 `UndeclaredBehaviorPlaced` (Information), 178 `DuplicateStepCollapsed` (Debug), 179 `PipelineOrderResolved` (Debug), 180 `PipelineOrderInvalid` (Error). 100-175 used on `main` (highest: `RequestIdentityLog.cs:69`); 176 is claimed by the proposed fix of the open #2088. No new range; `Encina` is already mapped to `Core` in `EncinaEventIdAllocationTests`. Re-verify before coding (Phase 7 task 2) |
| Satellites | unchanged | The migration adds attributes and changes registrations only; no new log events |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New core production files | 18 | `PipelineStage`, `PipelineStageAttribute`, `PipelineStepDescription` (+ 2 enums), `IEncinaPipelineInspector`, `IRequestErrorProcessor`, `IRequestPostCommitProcessor`, registry, step identity, candidate, resolver, validator, snapshot, hosted service, inspector, 4 steps (grouped), `PipelineOrderLog` |
| Modified core files | 14 | `PipelineBuilder`, `StreamPipelineBuilder`, `EncinaConfiguration`, `ServiceCollectionExtensions`, `EncinaAssemblyScanner`, `Encina.cs`, `OutboxPostProcessor`, `StreamDispatcher.cs`, `ModuleBehaviorAdapter`, `IPipelineBehavior`, `IStreamPipelineBehavior`, `IModulePipelineBehavior`, `IRequestPreProcessor`, `IRequestPostProcessor`, `PublicAPI.Unshipped.txt` |
| Satellite behavior files (attribute) | ~55 | One line plus a `using` each |
| Satellite registration files | ~20 | `TryAddEnumerable` moves and comment deletions |
| Test files | ~20 | Unit ~9, guard 2, contract 3 + architecture rule, property 1, benchmark 1, 2 justification files, 2 rewritten order tests |
| Documentation | ~14 | ADR-049, ADR index, ADR-018 link, new explanation page, 5 docs pages, 3 READMEs, 2 changelog fragments, INVENTORY |

### Stage List and Built-in Placement

Outermost first. Order inside a stage: lower is further out. "Variant" marks provider alternatives that share a slot; R5 rejects two of them in one closed pipeline.

| Stage (value) | Built-in steps (`Order`) | Constraint served |
|---------------|--------------------------|-------------------|
| `Observability` (100) | `CommandActivityPipelineBehavior` (0, `src/Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs:30`), `QueryActivityPipelineBehavior` (5, `:25`), `CommandMetricsPipelineBehavior` (10, `:25`), `QueryMetricsPipelineBehavior` (15, `:25`), `MessagingEnricherPipelineBehavior` (20, `src/Encina.OpenTelemetry/Behaviors/MessagingEnricherPipelineBehavior.cs:16`) | Spans and metrics cover denials and failures |
| `Admission` (200) | `BulkheadPipelineBehavior` (0, `src/Encina.Polly/Behaviors/BulkheadPipelineBehavior.cs:23`), `RateLimitingPipelineBehavior` (10, `:23`) | C14, Design Choice 5 |
| `Audit` (300) | `AuditPipelineBehavior` (0, `src/Encina.Security.Audit/AuditPipelineBehavior.cs:34`), `BreachDetectionPipelineBehavior` (10, `src/Encina.Compliance.BreachNotification/BreachDetectionPipelineBehavior.cs:61`) | C3, C4 |
| `Authentication` (400) | `HMACValidationPipelineBehavior` (0, `src/Encina.Security.AntiTampering/Pipeline/HMACValidationPipelineBehavior.cs:64`), `SecurityPipelineBehavior` (10, `src/Encina.Security/SecurityPipelineBehavior.cs:48`) | C1, C2 |
| `Authorization` (500) | `AuthorizationPipelineBehavior` (0, `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs:75`), `ABACPipelineBehavior` (10, `src/Encina.Security.ABAC/ABACPipelineBehavior.cs:81`) | C1, C2 (#751) |
| `Compliance` (600) | LawfulBasis (0, `src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs:51`), GDPR (10, `src/Encina.Compliance.GDPR/GDPRCompliancePipelineBehavior.cs:60`), Consent (20, `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:54`), ProcessingRestriction (30, `src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs:86`), DataResidency (40, `src/Encina.Compliance.DataResidency/DataResidencyPipelineBehavior.cs:58`), TransferBlocking (50, `src/Encina.Compliance.CrossBorderTransfer/Pipeline/TransferBlockingPipelineBehavior.cs:66`), ProcessorValidation (60, `src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs:74`), DPIARequired (70, `src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs:68`), AIAct (80, `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs:54`), NIS2 (90, `src/Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs:56`), RetentionValidation (100, `src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs:71`), DataMinimization (110, `src/Encina.Compliance.PrivacyByDesign/DataMinimizationPipelineBehavior.cs:92`), Attestation (200, `src/Encina.Compliance.Attestation/Behaviors/AttestationPipelineBehavior.cs:49`) | C5; LawfulBasis before GDPR replaces the call-order instruction at `Encina.Compliance.LawfulBasis/ServiceCollectionExtensions.cs:38-40` |
| `Presentation` (700) | `OutputEncodingPipelineBehavior` (0, `src/Encina.Security.Sanitization/OutputEncodingPipelineBehavior.cs:46`), `PIIMaskingPipelineBehavior` (10, `src/Encina.Security.PII/PIIMaskingPipelineBehavior.cs:65`), `AnonymizationPipelineBehavior` (20, `src/Encina.Compliance.Anonymization/AnonymizationPipelineBehavior.cs:71`) | C6: cached responses are masked and encoded too |
| `InputPreparation` (800) | `SoftDeleteQueryFilterBehavior` (0, `src/Encina.Messaging/SoftDelete/SoftDeleteQueryFilterBehavior.cs:62`), decrypt step of `EncryptionPipelineBehavior` (10, `src/Encina.Security.Encryption/EncryptionPipelineBehavior.cs:58`; decision D7), `InputSanitizationPipelineBehavior` (20, `src/Encina.Security.Sanitization/InputSanitizationPipelineBehavior.cs:45`) | C7 |
| `Validation` (900) | `ValidationPipelineBehavior` (0, `src/Encina/Validation/ValidationPipelineBehavior.cs:35`) | C7 |
| `Caching` (1000) | `CacheInvalidationPipelineBehavior` (0, `src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs:28`), `QueryCachingPipelineBehavior` (10, `:39`), `DistributedIdempotencyPipelineBehavior` (20, `src/Encina.Caching/Behaviors/DistributedIdempotencyPipelineBehavior.cs:39`) | C5, C6, C12; DistributedIdempotency stays outside `Transaction` so it never stores a result whose commit fails |
| `Resilience` (1100) | `RecoverabilityPipelineBehavior` (0, `src/Encina.Messaging/Recoverability/RecoverabilityPipelineBehavior.cs:36`), `StandardResiliencePipelineBehavior` (10, `src/Encina.Extensions.Resilience/Behaviors/StandardResiliencePipelineBehavior.cs:25`), `RetryPipelineBehavior` (20, `src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs:15`), `CircuitBreakerPipelineBehavior` (30, `:15`), `DatabaseCircuitBreakerPipelineBehavior` (40, `:46`) | C9: each attempt gets a fresh transaction |
| `DataRouting` (1200) | `ModuleExecutionContextBehavior` (0, `src/Encina.EntityFrameworkCore/Modules/ModuleExecutionContextBehavior.cs:49`), `ReadWriteRoutingPipelineBehavior` (10, variant x8: EF Core, ADO.NET x3, Dapper x3, MongoDB, each at `ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs:59`), `ShadowReadPipelineBehavior` (20, `src/Encina/Sharding/Shadow/Behaviors/ShadowReadPipelineBehavior.cs:27`), `ShadowWritePipelineBehavior` (25, `ShadowWritePipelineBehavior.cs:26`) | C10 |
| `Transaction` (1300) | post-commit processors (default -100), `TransactionPipelineBehavior` (0, variant: `src/Encina.Messaging/TransactionPipelineBehavior.cs:40`, `src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs:48`), #718 outbox behavior (10) | C11; Design Choice 2 |
| `Idempotency` (1400) | `InboxPipelineBehavior` (0, `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs:27`) | ADR-048: the inbox runs inside the business transaction (replaces the spike's C8) |
| `Application` (1500) | pre, post and error processors (default -100), undeclared custom behaviors (0), `EventPublishingPipelineBehavior` (100, `src/Encina.Marten/EventPublishingPipelineBehavior.cs:14`; registered by #2030), module behaviors without a stage (0) | Inside every gate (Design Choices 1 and 2) |
| `HandlerPreparation` (1600) | encrypt step of `EncryptionPipelineBehavior` (-10; decision D7), `SecretInjectionPipelineBehavior` (0, `src/Encina.Security.Secrets/Injection/SecretInjectionPipelineBehavior.cs:42`) | C13: no other step sees injected secrets |

### Public API Changes (`src/Encina/PublicAPI.Unshipped.txt`)

| Symbol | Change |
|--------|--------|
| `Encina.PipelineStage`, `Encina.PipelineStageAttribute`, `Encina.PipelineStepDescription`, `Encina.PipelineStepKind`, `Encina.PipelineStepSource`, `Encina.IEncinaPipelineInspector` | Added |
| `Encina.IRequestErrorProcessor<TRequest, TResponse>`, `Encina.IRequestPostCommitProcessor<TRequest, TResponse>` | Added |
| `Encina.IRequestPreProcessor<TRequest>.Process` | Returns `ValueTask<Either<EncinaError, Unit>>` |
| `Encina.IRequestPostProcessor<TRequest, TResponse>.Process` | Takes `TResponse`, returns `ValueTask<Either<EncinaError, Unit>>` |
| `Encina.EncinaConfiguration` | Stage-aware overloads; `AddRequestErrorProcessor`, `AddRequestPostCommitProcessor`, `AddStreamPipelineBehavior` |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 mediator library with Railway Oriented Programming (Either<EncinaError, T>), opt-in features and provider coherence (AGENTS.md). Pipeline behaviors run in DI registration order today (src/Encina/Pipeline/PipelineBuilder.cs:63-80) and pre/post processors run outside every behavior (:82-125). Issue #2184 (the implementation of the #1783 spike, with #1678 merged) replaces this with a closed stage contract; the maintainer decided Option C and Q1-Q6 on 2026-10-09 (plan: docs/plans/pipeline-execution-order-implementation-plan-2184.md; ADR-049).

IMPLEMENTATION OVERVIEW:
1. Core abstractions: PipelineStage (16 stages, 100..1600, outermost first), PipelineStageAttribute(stage) { Order }, PipelineStepDescription/Kind/Source, IEncinaPipelineInspector; pre-processor returns ValueTask<Either<EncinaError, Unit>>; post-processor takes TResponse, runs only on Right, returns Either; new IRequestErrorProcessor (observe Left) and IRequestPostCommitProcessor (after commit).
2. PipelineOrderResolver: key (stage, order, processors-before-behaviors, Type.FullName for processors / registration index for behaviors); registry > attribute > default; duplicates collapsed; cached permutation per closed pipeline; processors wrapped in four internal step adapters; builders compose in resolved order.
3. Validation rules R1-R7 (defined stage, duplicates collapsed, built-ins declared: assemblies of src/ via the fixed BuiltInAssemblies list or the EncinaBuiltInPipelineSteps marker, never the "Encina." name prefix; R3, R4 and the hosted service enabled only in the Phase 5 PR; no ICommand/IQueryPipelineBehavior service registrations, no built-in tie in one pipeline, no conflicting registration-time declarations, post-commit outside the transaction); hosted service at StartAsync plus lazy validation before first use; InvalidOperationException.
4. EncinaConfiguration stage-aware overloads; dead ICommand/IQuery descriptors removed; registry, resolver, snapshot, inspector, hosted service registered with TryAdd*.
5. Migration: [PipelineStage] on ~55 built-ins per the plan's table (Idempotency inside Transaction per ADR-048; DistributedIdempotency in Caching; EncryptionPipelineBehavior split into decrypt/InputPreparation and encrypt/HandlerPreparation per D7); every behavior registration via TryAddEnumerable; false order instructions deleted.
6. Cross-cutting: module adapter stage, audit placement, ADR-048 order, #751 regression, #718 coordination.
7. Observability: EventIds 177-180 in EventIdRanges.Core.
8. Tests: unit, guard, contract (attribute present, reachability table, precedence, Transaction async contract, architecture rule), property, benchmark; justification files for integration and load; per-file coverage targets.
9. Docs: ADR-049, pipeline-stages explanation page, order passages rewritten, changelog fragments, PublicAPI.

KEY PATTERNS: fail closed (an invalid order never runs); no registration-order semantics for built-ins; attributes read once per closed pipeline and cached (no source generators, ADR-005); [LoggerMessage] with EventIds in a registered range (ADR-021), type names only in logs; TryAddEnumerable for every behavior; DI tests with ValidateOnBuild and ValidateScopes; tests execute real package code; Shouldly via Encina.Testing.Shouldly; CRAP <= 10 on every changed method; no [Obsolete] or compatibility overloads; English only; changelog via changelog.d fragments.

REFERENCE FILES: src/Encina/Pipeline/PipelineBuilder.cs, src/Encina/Pipeline/StreamPipelineBuilder.cs, src/Encina/Core/EncinaConfiguration.cs, src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina/Dispatchers/MediatorAssemblyScanner.cs, src/Encina/Modules/ModuleBehaviorAdapter.cs, src/Encina/Abstractions/IRequestPreProcessor.cs, src/Encina/Abstractions/IRequestPostProcessor.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/architecture/adr/048-inbox-record-vs-business-transaction.md, docs/architecture/adr/018-cross-cutting-integration-principle.md, .github/coverage-manifest/Encina.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The permutation cache is an internal in-process memo per closed pipeline, not data that benefits from `ICacheProvider`; the contract only fixes where the caching behaviors sit (`Caching` stage, outside `Transaction`, inside `Presentation`) |
| 2 | OpenTelemetry | ❌ | The order is computed once per closed pipeline; there is no per-request operation to trace or meter, and each behavior keeps its own spans and metrics (the `Observability` stage is outermost so spans cover denials) |
| 3 | Structured Logging | ✅ | EventIds 177-180 in `EventIdRanges.Core` (Phase 7): undeclared placement, duplicate collapsed, resolved order, validation failure; type names only |
| 4 | Health Checks | ❌ | No external dependency; an invalid order fails startup instead of degrading health |
| 5 | Validation | ✅ | Startup validation of the order is the feature (rules R1-R7, Phase 3); registration arguments are validated in `EncinaConfiguration`. Request input is not touched |
| 6 | Resilience | ❌ | No external call; the contract fixes where resilience behaviors sit (`Resilience` outside `Transaction`, C9) |
| 7 | Distributed Locks | ❌ | No shared state across processes; the per-process cache is guarded by `ConcurrentDictionary` |
| 8 | Transactions | ✅ | The mechanism opens no transaction, but it fixes and tests the `Transaction` stage: outside `Idempotency` (ADR-048), inside `Resilience` and `DataRouting`, post-commit processors after the commit (Phases 2 and 6) |
| 9 | Idempotency | ✅ | Placement only: the inbox inside the business transaction (ADR-048) and inside the gates, so replays are re-authorized; `DistributedIdempotency` outside `Transaction`; duplicate behavior registrations collapsed (Design Choice 4) |
| 10 | Multi-Tenancy | ❌ | The order is per request and response type, not per tenant; tenant resolution happens before the pipeline (`IRequestContext`) and is not a behavior |
| 11 | Module Isolation | ✅ | `ModuleBehaviorAdapter` reports the wrapped behavior's stage and identity; `ModuleExecutionContextBehavior` in `DataRouting` (Phase 6) |
| 12 | Audit Trail | ✅ | Placement only: `Audit` outside the gates, validation and the transaction, so denials and rolled-back commands are audited (C3, C4); no new audit events (Phase 6) |

No function is deferred, so no follow-up issue is needed for the matrix.

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #2032 shadow behaviors registered only as `ICommand/IQueryPipelineBehavior` | open | Prerequisite of the phase that enables R4 (the Phase 5 PR): rule R4 fails every host that enables shadow sharding until it is fixed |
| #2029 four read/write routers never registered | open | Prerequisite of the Phase 8 reachability contract test |
| #2030 Marten `EventPublishingPipelineBehavior` never registered | open | Prerequisite of the Phase 8 reachability contract test; its fix gives the type `[PipelineStage(Application, Order = 100)]` |
| #1782 Recoverability registered twice | open | Fixed by Phase 5 task 2 (`TryAddEnumerable`); reference it in the PR |
| #718 transactional outbox | open (plan) | Coordinated: its outbox behavior is born in `Transaction`, Order 10; whichever lands second adapts `OutboxPostProcessor` (Phase 5 task 3) or deletes it |
| #2031 sync fallbacks in `TransactionPipelineBehavior` (`src/Encina.Messaging/TransactionPipelineBehavior.cs:126,:133,:142,:153`) and #2039 (S6966 ignored) | open | Not ordering defects; the Phase 8 `Transaction` stage contract test detects a sync fallback in the stage this plan owns |
| #2088 (claims EventId 176) | open | Coordination only: this plan takes 177-180 |
| #1642 Audit outcome by message text | open | Related: Phase 6 guarantees the audit behavior sees the `Left`s; classification is #1642 |
| #751 ABAC decision audit (A6: no 9090 warning) | open | Phase 6 task 4 is the regression that replaces the warning |
| ADR-048 | accepted | Forces `Idempotency` inside `Transaction` |

**Risks**: the hosted service resolves every closed pipeline at start (startup time for very large applications; measured in the benchmark of Phase 8 and documented); applications with their own pre- or post-processors must change signatures (pre-1.0, breaking by design, changelog `changed`); rule R5 stops a host that registers two read/write routers or two transaction behaviors together (intended: two routers or two transactions on one request are never correct); a step constructor that throws at start is reported as a resolution error naming the step type, separate from the order rules (the hosted service instantiates only factory-registered steps; Phase 8 unit test); R3, R4 and the hosted service are enabled only in the Phase 5 PR, so `main` never holds a validator that rejects an unmigrated built-in.

---

## Next Steps

1. Merge this plan with the ADR-049 reservation.
2. Re-check the prerequisites (#2032, #2029, #2030) and the EventIds (Phase 7 task 2) when the work is picked up; the orchestrator writes one worker brief per phase group (1-4 core, 5-6 migration, 7-9 observability, tests and docs).
3. Implement Phases 1 to 9 in order; Phase 5 starts only when #2032, #2029 and #2030 are merged. Phases 1-4 may land earlier with the validator rules present but not enforced; R3, R4 and the registration of `PipelineOrderValidationHostedService` are switched on in the Phase 5 PR (or Phases 1-5 ship in one PR), so `main` never contains a validator that rejects a built-in the plan has not yet migrated.
4. Delegate the documentation of Phase 9 to `docs-writer` and the review of every PR to `adversarial-reviewer`.
5. Close #2184 with the implementation PR.

---

## Maintainer Decisions

Design Choices 1 to 6 were answered by the maintainer on #1783 (comment "Maintainer decisions (2026-10-09)"); they are recorded here on 2026-10-10 with this plan. Design Choice 7 was decided on #2184 on 2026-10-10.

### D1 (2026-10-10) Undeclared custom behaviors — decided on #1783

Q1 = (a): a custom behavior that declares no stage goes to `PipelineStage.Application` (inside every gate) and EventId 177 is logged once per type. Applied: Design Choice 1, Phase 2 task 3, Phase 7.

### D2 (2026-10-10) Pre- and post-processors — decided on #1783

Q2 = (d): `IRequestPreProcessor<T>` / `IRequestPostProcessor<T, R>` stay as typed sugar adapted into behaviors placed by the stage system: default stage `Application`, an optional declared stage, an explicit stable order (never DI order); pre returns `Either` and can abort; post runs only on `Right`; an error hook and a post-commit hook (as Wolverine's `AfterCommit`) are added; anything that needs `next`, wrapping, timing or catching stays a behavior and the docs say so. Option "keep them outside every behavior" was listed only as the status quo and not recommended, because it breaks the fail-closed rule. Applied: Design Choice 2, Phases 1, 2, 5 and 9.

### D3 (2026-10-10) How a stage is declared — decided on #1783

Q3 = (a): the stage is an attribute on the behavior type; a registration-time declaration (`AddPipelineBehavior(type, stage, order)`) overrides it. Applied: Design Choice 3, Phases 1, 2 and 4.

### D4 (2026-10-10) Duplicate behavior types — decided on #1783

Q4 = (b): the same behavior type twice in one pipeline is collapsed to one instance and logged at Debug; built-in registrations move to `TryAddEnumerable`. Applied: Design Choice 4, Phase 2 task 3, Phase 5 task 2, EventId 178.

### D5 (2026-10-10) Admission before authentication — decided on #1783

Q5 = (a): `Admission` (bulkhead, rate limit) runs before authentication; per-user partitioning reads the identity already in the request context. Applied: Design Choice 5, stage table.

### D6 (2026-10-10) Application behaviors in the gate stages — decided on #1783

Q6 = (a): applications may place their own behaviors in `Authentication` and `Authorization`, ordered among the built-ins. Applied: Design Choice 6, Phase 3 (no rule forbids it).

### D7 (2026-10-10) Placement of the bidirectional encryption behavior — decided on #2184 after the review of PR #2186

Option (b): `EncryptionPipelineBehavior` is split into a decrypt step in `InputPreparation` (so validators see plaintext) and an encrypt step in `HandlerPreparation` (`Order = -10`, before `SecretInjection`). It is a behavior code change, allowed for this one behavior in the Phase 5 prompt. Applied: Design Choice 7, stage table, Phase 5 task 7, Phase 8 unit test.
