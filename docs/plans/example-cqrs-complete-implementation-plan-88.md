# Implementation Plan: Example `02-cqrs-complete` — commands, queries, a logging behavior, FluentValidation and Problem Details over HTTP

> **Issue**: [#88](https://github.com/dlrivada/Encina/issues/88) (family: #87, #89, #188, #344)
> **Type**: Feature (documentation and examples; no change under `src/`)
> **Research**: `src/Encina.AspNetCore/ProblemDetailsExtensions.cs`, `src/Encina.FluentValidation/ServiceCollectionExtensions.cs`, `src/Encina/Validation/ValidationOrchestrator.cs`, `docs/guides/how-to-write-a-pipeline-behavior.md`, `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj`
> **ADR**: none (ADR number assigned when the ADR pull request opens, if one is needed)
> **Related**: #87 (owns the `examples/` scaffold and Dilemma D1), #89 (database and cache examples), #188 (dashboard guide), #344 (adoption guide), and the validation defect described in Prerequisites (issue file written, number assigned when it is opened)
> **Complexity**: Medium (an ASP.NET Core project, 5 handlers, a behavior, validators, a smoke test)
> **Estimated Scope**: about 15 files, about 700 lines

---

## Summary

This plan adds `examples/02-cqrs-complete`, an ASP.NET Core API that shows the production-shaped path of a request: HTTP endpoint, `IEncina.Send`, validation, a hand-written logging behavior, a handler returning `Either<EncinaError, T>`, and a Problem Details response chosen from the error. #87 is the minimal first example; this one adds the pieces a real service needs. The folder `examples/`, its `Directory.Build.props` and the `Encina.slnx` entry are created by the #87 plan and are not repeated here.

**What the example contains** (all names verified in `src/`):

- Commands `CreateOrder`, `UpdateOrder`, `CancelOrder` (`ICommand<TResponse>`), queries `GetOrderById`, `GetOrders` (`IQuery<TResponse>`) and one handler each (`src/Encina/Abstractions/CqrsContracts.cs`).
- A hand-written `LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>`, written on purpose to teach the extension point (`docs/guides/how-to-write-a-pipeline-behavior.md`). The core has no logging behavior (only activity and metrics behaviors), so nothing is duplicated. It logs the request type and the error code on a `Left`, never `EncinaError.Message`.
- Validation through the library, not by hand: `services.AddEncinaFluentValidation(typeof(Program).Assembly)` (`src/Encina.FluentValidation/ServiceCollectionExtensions.cs:50`) registers the provider, the `ValidationOrchestrator` and `ValidationPipelineBehavior<,>` (AGENTS.md section 3, validation orchestrator pattern). The example adds only the `AbstractValidator<T>` classes. The original issue text proposed a hand-written `ValidationBehavior`; that would duplicate the library.
- HTTP error mapping with `Encina.AspNetCore`: `EncinaError.ToProblemDetails(HttpContext, int?)` returns `IResult` and `ToActionResult(HttpContext, int?)` returns `IActionResult` (`src/Encina.AspNetCore/ProblemDetailsExtensions.cs:67`, `:161`), registered by `AddEncinaAspNetCore()`. The status comes from the error code: `validation.*` gives 400, `*.not_found` 404, `*.conflict` 409, anything else 500 (`:172-215`). The response carries the code and, only when `EncinaAspNetCoreOptions.IncludeErrorMessageInProblemDetails` is enabled in Development, the message; the example leaves that option off.

**Finding that blocks the 400 case.** `ValidationOrchestrator.ValidateAsync` returns `EncinaError.New(errorMessage)` for an invalid request (`src/Encina/Validation/ValidationOrchestrator.cs:71`): an error with no code. `MapErrorCodeToStatusCode` reads the code with `GetCode().IfNone(string.Empty)` and falls through to 500 (`ProblemDetailsExtensions.cs:174-207`). So with today's `main`, an invalid `CreateOrder` produces a 500, not the 400 the issue's acceptance criteria require. This is a library defect, not an example problem; see Prerequisites. The example must not hide it with a status override that cannot tell a validation failure from any other error.

**Provider category**: none. The example keeps orders in an in-memory store; the real stores are #89.

**Affected packages**: `Encina` (consumed), `Encina.AspNetCore` (consumed), `Encina.FluentValidation` (consumed), the new example project `examples/02-cqrs-complete`.

**Size**: 1 plan file; the implementation is one worker pull request of about 15 files and 700 lines, within the 15-file and 1500-line limit (the smoke test and its project reference are counted).

---

## Design Choices

<details>
<summary><strong>1. Where the example lives (D1, shared; full text in the #87 plan)</strong></summary>

**Class:** Dilemma

The question and the three options are written once in the #87 plan, Design Choice 1, and are not re-argued here. In short: `examples/` with `ProjectReference` inside `Encina.slnx` and CI wins while the API moves before 1.0; the same folder outside the solution on a local package feed wins after 1.0; snippet pages under `docs/examples/` win only for tiny examples. For this example the smoke test of Phase 4 only exists under option (a).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) `examples/` with `ProjectReference`, in `Encina.slnx` and CI** | Compiles against current code; the smoke test can reference the project | Widens the solution build |
| **(b) `examples/` outside the solution, on a local package feed** | Looks like user code | CI never notices an API change; no smoke test in the test solution |
| **(c) Snippets under `docs/examples/`** | No build cost | No proof the code compiles |

### Chosen Option: **(a) — `examples/` with `ProjectReference`, inside `Encina.slnx` and CI** (recommended, pending the maintainer)

### Rationale

Same as #87: only (a) enforces mechanically that the example compiles. The answer is recorded once, in the #87 plan's `## Maintainer Decisions` section, and this plan follows it.

</details>

<details>
<summary><strong>2. Controllers or minimal APIs for the HTTP layer (D2)</strong></summary>

**Class:** Dilemma

Both helpers exist in `Encina.AspNetCore`: `ToProblemDetails` returns `IResult` (minimal APIs) and `ToActionResult` returns `IActionResult` (controllers).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Controllers (`OrdersController`) with `ToActionResult`** | Familiar to most .NET teams and to MediatR users migrating controller code; the original issue text shows it | More ceremony (attribute routing, base class, constructor injection); the .NET 10 default hosting shape is the minimal one |
| **(b) Minimal APIs (`MapGroup("/orders")`) with `ToProblemDetails`** | Shortest path from handler to response; the default hosting model of .NET 10 (AGENTS.md section 1 names minimal hosting); easiest to read in one file | A reader coming from MVC sees a different shape; a large API needs more structure than one `Program.cs` |

### Chosen Option: **(b) — Minimal APIs with `ToProblemDetails`** (recommended, pending the maintainer)

### Rationale

We recommend (b): the example's subject is the Encina request path, and minimal endpoints show it in the fewest lines. (a) wins for an audience that is migrating controller code from MediatR; the README then shows the `ToActionResult` form in a four-line snippet, since both helpers are supported either way.

</details>

<details>
<summary><strong>3. Validation: the library integration, not a hand-written behavior</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Validation orchestrator pattern: `AddEncinaFluentValidation(assembly)` registers provider, orchestrator and behavior), AGENTS.md section 5 (Validation category: all three providers integrate with `ValidationOrchestrator`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `AddEncinaFluentValidation(assembly)` plus `AbstractValidator<T>` classes** | The documented path; one registration; shows what users will actually write | None found |
| **B — A hand-written `ValidationBehavior`** | Teaches behaviors | Duplicates `ValidationPipelineBehavior<,>`; two validation paths in one pipeline |

### Chosen Option: **A — Library integration**

### Rationale

The hand-written behavior in this example is the logging one (choice 4); validation uses the library so the example shows the supported path. The README notes that `Encina.DataAnnotations` and `Encina.MiniValidator` plug into the same orchestrator (AGENTS.md section 5), without demonstrating them.

</details>

<details>
<summary><strong>4. A hand-written logging behavior</strong></summary>

**Class:** Trivial
**Settled by:** `docs/guides/how-to-write-a-pipeline-behavior.md` (the guide names writing a behavior as the extension exercise), AGENTS.md section 3 (`EncinaError.Message` never reaches logs)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `LoggingBehavior<,>` in the example, registered with `AddEncina(cfg => cfg.AddPipelineBehavior(typeof(LoggingBehavior<,>)))` or the equivalent verified API, logging type and error code through `[LoggerMessage]`** | Teaches the extension point and structured logging; no library duplication | The exact registration call must be verified when written |
| **B — No custom behavior** | Smaller | The issue's "pipeline behaviors work correctly" criterion is then only validation |

### Chosen Option: **A — Hand-written `LoggingBehavior<,>`**

### Rationale

Its logs follow the repository rule (code or exception type, never the message). The example's `[LoggerMessage]` EventIds are local to the example: the architecture test reads `src/**` only, and the example assembly is named without the `Encina` prefix; Phase 4 task 4 re-checks both.

</details>

<details>
<summary><strong>5. Where orders are kept</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 5 (providers: a database feature is for the 10 providers, an example is not a store feature), #89 (the database and cache examples)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — In-memory `IOrderStore` singleton (`ConcurrentDictionary`) inside the example** | No Docker; the smoke test is fast and deterministic; no provider is implied | Data is lost on restart |
| **B — EF Core or Dapper store** | Realistic | Duplicates #89; makes the example depend on Docker; implies one provider |

### Chosen Option: **A — In-memory store**

### Rationale

#89 owns the persistence examples. This one keeps the subject on the request path.

</details>

<details>
<summary><strong>6. Where the smoke test lives</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 9 (test projects are consolidated, one per type, under `tests/`; integration tests go in `Encina.IntegrationTests`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `tests/Encina.IntegrationTests/Examples/CqrsCompleteSmokeTests.cs` with `WebApplicationFactory<Program>`** | Follows the consolidated layout; runs in the existing integration job; the project already references `Encina.AspNetCore`, `Encina.FluentValidation` and `Microsoft.AspNetCore.TestHost`; `Microsoft.AspNetCore.Mvc.Testing` is already in `Directory.Packages.props` | Adds a `ProjectReference` to the example and a package reference to `Mvc.Testing`; needs `public partial class Program` in the example |
| **B — A test project inside `examples/`** | Self-contained | A new test project outside the consolidated layout |

### Chosen Option: **A — A class in `Encina.IntegrationTests`**

### Rationale

One test class (no database, so no collection fixture applies) with `[Trait("Category", "Integration")]` checks the 200, 400, 404 and 409 responses in process.

</details>

---

## Implementation Phases

### Phase 1: Domain, messages and handlers

> **Goal**: The five operations work against an in-memory store. Depends on the #87 scaffold (`examples/Directory.Build.props`, solution folder).

<details>
<summary>Tasks</summary>

1. `examples/02-cqrs-complete/02-cqrs-complete.csproj`: `Microsoft.NET.Sdk.Web`, `net10.0`, `ProjectReference` to `Encina`, `Encina.AspNetCore` and `Encina.FluentValidation`.
2. `Domain/Order.cs`: a record with `Id`, `Customer`, `Total`, `Status` (`Open`, `Cancelled`).
3. `Domain/IOrderStore.cs` and `Domain/InMemoryOrderStore.cs`: add, get, list with a status filter, replace.
4. `Commands/CreateOrder.cs` (`ICommand<Guid>`), `UpdateOrder.cs`, `CancelOrder.cs`; `Queries/GetOrderById.cs` (`IQuery<Order>`), `GetOrders.cs` (`IQuery<IReadOnlyList<Order>>`, optional status filter).
5. `Handlers/`: five handlers. Failures use codes the Problem Details mapping understands: `order.not_found` (404), `order.conflict` for cancelling a cancelled order (409). No exception for a business failure.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: The #87 plan created examples/ (Directory.Build.props, solution folder). Encina handlers implement ICommandHandler<,> / IQueryHandler<,> (src/Encina/Abstractions/CqrsContracts.cs) and return Either<EncinaError, T>; business failures are EncinaErrors.Create(code, message) (src/Encina/Results/EncinaErrors.cs).
TASK: Create examples/02-cqrs-complete with the csproj, the Order domain, the in-memory store, the three commands, two queries and five handlers listed in the plan (Phase 1 tasks 1-5). Use codes order.not_found and order.conflict so ToProblemDetails maps them to 404 and 409.
KEY RULES: Verify every type in src/ before use; no exceptions for business failures; nullable enabled; zero warnings (TreatWarningsAsErrors); comments teach the concept; English only.
REFERENCE FILES: src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Results/EncinaErrors.cs, src/Encina.AspNetCore/ProblemDetailsExtensions.cs, docs/plans/example-basic-encina-implementation-plan-87.md (#87 plan, if merged).
```

</details>

### Phase 2: Behavior and validation

> **Goal**: The pipeline has a logging behavior and FluentValidation through the library.

<details>
<summary>Tasks</summary>

1. `Behaviors/LoggingBehavior.cs`: `IPipelineBehavior<TRequest, TResponse>`; logs the request type name before, and the outcome after (success, or the code from `error.GetCode()`); uses `[LoggerMessage]` through a `partial` logger class with local EventIds; never logs `EncinaError.Message` or the request body.
2. `Validators/CreateOrderValidator.cs`, `UpdateOrderValidator.cs`: `AbstractValidator<T>` rules (customer not empty, total above zero).
3. `Program.cs` registration: `AddEncina(...)` with the logging behavior registered through the verified configuration call (confirm the exact name in `src/Encina/Core/EncinaConfiguration.cs` before writing it), `AddEncinaFluentValidation(typeof(Program).Assembly)`, `AddEncinaAspNetCore()`, `AddSingleton<IOrderStore, InMemoryOrderStore>()`.
4. Order: the stage contract is already on `main` (`PipelineStage`, `PipelineStageAttribute`, `AddPipelineBehavior<TBehavior>(PipelineStage stage, int order = 0)` in `src/Encina/Core/EncinaConfiguration.cs`). Declare the logging behavior in `PipelineStage.Observability` (outermost, value 100) so a request rejected by validation (`PipelineStage.Validation`, 900) is still logged; confirm the attribute or the registration overload when writing it.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 exists. IPipelineBehavior<TRequest, TResponse> is in src/Encina/Abstractions/IPipelineBehavior.cs; the how-to is docs/guides/how-to-write-a-pipeline-behavior.md. Validation is registered by AddEncinaFluentValidation(assembly) (src/Encina.FluentValidation/ServiceCollectionExtensions.cs:50), which also registers ValidationPipelineBehavior<,>.
TASK: Add LoggingBehavior, the two validators and the Program.cs registrations (Phase 2 tasks 1-4). Confirm the behavior registration API in src/Encina/Core/EncinaConfiguration.cs and how pipeline order is declared today.
KEY RULES: Log the request type and the error code only, never EncinaError.Message; do not write a validation behavior; use [LoggerMessage] (source generator); zero warnings; the example may use local EventIds but the assembly name must not start with "Encina".
REFERENCE FILES: docs/guides/how-to-write-a-pipeline-behavior.md, src/Encina/Abstractions/IPipelineBehavior.cs, src/Encina/Core/EncinaConfiguration.cs, src/Encina.FluentValidation/ServiceCollectionExtensions.cs.
```

</details>

### Phase 3: HTTP layer with Problem Details (D2)

> **Goal**: Each operation is an endpoint; every `Left` becomes a Problem Details response.

<details>
<summary>Tasks</summary>

1. `Api/OrderEndpoints.cs` (recommended option (b)): `MapGroup("/orders")` with `POST /`, `PUT /{id}`, `POST /{id}/cancel`, `GET /{id}`, `GET /` (query string `status`). Each endpoint calls `encina.Send(...)` and ends with `result.Match(Right: ..., Left: error => error.ToProblemDetails(httpContext))`. If the maintainer chooses controllers, the same file becomes `Api/OrdersController.cs` with `ToActionResult`.
2. `Program.cs`: `public partial class Program;` so the smoke test can host it.
3. Leave `IncludeErrorMessageInProblemDetails` at its default (off) in the example configuration, so responses carry the code and not the message.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1-2 exist. Encina.AspNetCore maps error codes to statuses in ProblemDetailsExtensions.MapErrorCodeToStatusCode (validation.* 400, *.not_found 404, *.conflict 409, else 500).
TASK: Add the HTTP layer exactly as the maintainer's D2 answer says (minimal API group by default) and make Program a public partial class. Every Left goes through ToProblemDetails(httpContext) (or ToActionResult for controllers).
KEY RULES: Never return EncinaError.Message to the client; do not set a status override to hide a missing validation code (see the plan's Prerequisites); verify every method name in src/Encina.AspNetCore before use; English only.
REFERENCE FILES: src/Encina.AspNetCore/ProblemDetailsExtensions.cs, src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs, src/Encina.AspNetCore/ServiceCollectionExtensions.cs.
```

</details>

### Phase 4: Testing

> **Goal**: The responses are proven in process.

<details>
<summary>Tasks</summary>

1. `tests/Encina.IntegrationTests/Examples/CqrsCompleteSmokeTests.cs`: `[Trait("Category", "Integration")]`, `IClassFixture<WebApplicationFactory<Program>>` (no database fixture applies); cases: create returns 201 or 200 with an id, get returns the order, an invalid create returns 400, a missing id returns 404, cancelling twice returns 409.
2. `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj`: a `ProjectReference` to the example and a `PackageReference` to `Microsoft.AspNetCore.Mvc.Testing` (already in `Directory.Packages.props`). If the 400 case is not green because of the Prerequisite defect, the test stays but is merged only after the defect is fixed; it is never skipped.
3. Assertions use Shouldly through `Encina.Testing.Shouldly`.
4. Check that the architecture tests (`EncinaEventIdAllocationTests`) still pass with the example assembly loaded in the integration test process (they read `src/**`, so none expected).
5. Zero-warnings build of the solution and `dotnet format --verify-no-changes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The API exists (Phase 3). Integration tests live in tests/Encina.IntegrationTests; no database is involved so no [Collection] fixture is needed.
TASK: Add CqrsCompleteSmokeTests with the five cases of Phase 4 task 1, the ProjectReference and the Mvc.Testing reference, then run dotnet test tests/Encina.IntegrationTests --filter "FullyQualifiedName~CqrsCompleteSmoke" and the solution build with -warnaserror.
KEY RULES: Shouldly via Encina.Testing.Shouldly; no Thread.Sleep; tests execute the real example host; if a status is wrong, spawn ci-diagnoser instead of changing the expectation.
REFERENCE FILES: tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj, docs/testing/integration-tests.md, examples/02-cqrs-complete/Program.cs.
```

</details>

### Phase 5: Documentation and finalization

> **Goal**: The README explains the architecture decisions.

<details>
<summary>Tasks</summary>

1. `examples/02-cqrs-complete/README.md` (by `docs-writer`): the file tree, how a request flows (a Mermaid sequence: endpoint, `Send`, logging behavior, validation, handler, `Match`, Problem Details), the pre-1.0 note, the D2 alternative snippet, a table of the error codes and statuses the example uses, and links to the pipeline-behavior how-to and to the `AddEncinaFluentValidation` README.
2. A row in `examples/README.md` (created by the #87 plan).
3. No changelog fragment, ADR, ROADMAP or `PublicAPI` change (no library change).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The example and its smoke test pass (Phases 1-4). Documentation follows the encina-docs skill.
TASK: Spawn docs-writer, naming the worktree path, for the example README and the row in examples/README.md; then run the link and lint checks of the skill (or Test-Path on every relative link if lychee is unavailable).
KEY RULES: English only; paste real outputs; no hand-typed figures; say pre-1.0; do not name any private client application.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, docs/guides/how-to-write-a-pipeline-behavior.md, src/Encina.FluentValidation/README.md.
```

</details>

---

## Research

### Relevant Standards and Prior Art

| Source | Relevance to this plan |
|--------|------------------------|
| RFC 9457 Problem Details for HTTP APIs | The response shape produced by `ToProblemDetails` |
| ADR-001 Railway Oriented Programming | Handlers and endpoints work on `Either<EncinaError, T>` |
| AGENTS.md section 3 (validation orchestrator, `EncinaError.Message` rule) | Library validation; code-only logging |
| ADR-018 cross-cutting integration principle | The matrix below |
| Comparable samples (MediatR, Wolverine sample APIs) | An API plus validators plus a logging behavior is the standard teaching sample |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IPipelineBehavior<,>` | `src/Encina/Abstractions/IPipelineBehavior.cs` | The hand-written logging behavior |
| `ValidationPipelineBehavior<,>`, `ValidationOrchestrator` | `src/Encina/Validation/` | Validation through the library |
| `AddEncinaFluentValidation(params Assembly[])` | `src/Encina.FluentValidation/ServiceCollectionExtensions.cs:50` | Provider, orchestrator and behavior registration |
| `ToProblemDetails`, `ToActionResult`, `EncinaAspNetCoreOptions` | `src/Encina.AspNetCore/` | Error mapping to HTTP |
| `GetCode()` | `src/Encina/Results/EncinaErrors.cs:109` | Error code for logs and mapping |
| Integration test project | `tests/Encina.IntegrationTests/` | The smoke test |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | No library code. The example's `LoggingBehavior` uses local `[LoggerMessage]` EventIds in a non-`Encina` assembly outside `src/`; no range is registered in `EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Example source | 11 | csproj, `Program.cs`, `Domain/Order.cs` with the store interface and implementation in two files, the 5 messages in 2 files (`Commands.cs`, `Queries.cs`), the 5 handlers in 2 files, `LoggingBehavior.cs`, `Validators.cs`, `OrderEndpoints.cs` (the worker may split them; the 15-file limit is the check) |
| Test | 2 | smoke test class and the `Encina.IntegrationTests.csproj` edit |
| Documentation | 2 | example README and the row in `examples/README.md` |
| **Total** | **15** | About 700 lines; if the worker needs more than 15 files it splits the smoke test into its own pull request |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 mediator library with Railway Oriented Programming (Either<EncinaError, T>) and opt-in features (AGENTS.md). Issue #88 adds examples/02-cqrs-complete (plan docs/plans/example-cqrs-complete-implementation-plan-88.md) on top of the #87 scaffold. D1 (location) is recommended as examples/ with ProjectReference; D2 (HTTP layer) is recommended as minimal APIs; both are pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. Domain and messages: Order, IOrderStore, in-memory store, 3 commands, 2 queries, 5 handlers returning Either with codes order.not_found and order.conflict.
2. Pipeline: hand-written LoggingBehavior (type and error code only); validation through AddEncinaFluentValidation(assembly) and AbstractValidator<T> classes.
3. HTTP: minimal API group (or controllers per D2); every Left through ToProblemDetails(httpContext); public partial class Program.
4. Tests: WebApplicationFactory smoke test in tests/Encina.IntegrationTests/Examples covering 200, 400, 404, 409.
5. Docs: README via docs-writer and a row in examples/README.md.
6. Prerequisite: the validation orchestrator returns a code-less error (ValidationOrchestrator.cs:71), so ToProblemDetails answers 500 instead of 400; the 400 case needs the library fix first.

KEY PATTERNS: verify every identifier in src/ before use; business failures are Left, not exceptions; never log or return EncinaError.Message; do not duplicate the library validation behavior; no status override to hide a missing code; zero warnings; English only; no AI attribution; no private client application names.

REFERENCE FILES: src/Encina.AspNetCore/ProblemDetailsExtensions.cs, src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs, src/Encina.FluentValidation/ServiceCollectionExtensions.cs, src/Encina/Validation/ValidationOrchestrator.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Abstractions/IPipelineBehavior.cs, docs/guides/how-to-write-a-pipeline-behavior.md, tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Query caching is shown by #89 (example 05); `GetOrders` stays uncached here |
| 2 | OpenTelemetry | ❌ | Example code only; the dashboard guide and its sample are #188 |
| 3 | Structured Logging | ✅ | The hand-written logging behavior uses `[LoggerMessage]`; no library EventIds are added |
| 4 | Health Checks | ❌ | No checkable dependency (in-memory store) |
| 5 | Validation | ✅ | FluentValidation through the library orchestrator (Phase 2); blocked on the code-less validation error for the 400 case |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | Single-instance demo |
| 8 | Transactions | ❌ | In-memory store; atomic multi-operation work is shown by #89 (example 03) |
| 9 | Idempotency | ❌ | Out of scope for the example |
| 10 | Multi-Tenancy | ❌ | No tenant data |
| 11 | Module Isolation | ❌ | Single module |
| 12 | Audit Trail | ❌ | Demo data with no compliance implication |

No function is deferred, so no follow-up issue is needed for the matrix; the validation defect has its own issue file.

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #87 plan: `examples/` scaffold and D1 | open (plan in this family) | Phase 1 starts after the scaffold merges |
| Validation errors carry no error code, so `ToProblemDetails` answers 500 instead of 400 (`ValidationOrchestrator.cs:71`, `ProblemDetailsExtensions.cs:174-207`) | defect found by this plan; issue file written (`[BUG]`), number assigned when the orchestrator opens it | Prerequisite of the Phase 4 400 case and of the acceptance criterion "400 on invalid input" |
| #2184 pipeline execution-order contract | open; its core types (`PipelineStage`, `PipelineStageAttribute`, stage-aware `AddPipelineBehavior`) are already on `main` | Phase 2 task 4 declares the logging behavior's stage; re-check at pick-up whether the issue's remaining phases changed the declaration API |
| #89, #188 | open (plans in this family) | Siblings; no dependency in either direction |

**Risks**: the example breaks whenever `Encina.AspNetCore` or the validation packages change a public signature (intended under option (a)); the smoke test adds a `ProjectReference` from `Encina.IntegrationTests` to an example, which `Encina.IntegrationTests` consumers must not treat as a library dependency.

---

## Next Steps

1. Merge this plan.
2. The orchestrator opens the validation defect issue from the issue file and sequences it before this implementation.
3. When the issue is picked up, re-check D1 (recorded in the #87 plan) and ask D2; record the answer in `## Maintainer Decisions`.
4. One worker brief for Phases 1 to 5 once the #87 scaffold and the validation fix are merged; delegate the README to `docs-writer` and the review to `adversarial-reviewer`.
5. Close #88 with the implementation pull request.
