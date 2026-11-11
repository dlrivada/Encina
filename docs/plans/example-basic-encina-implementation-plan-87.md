# Implementation Plan: Example `01-basic-encina` — a minimal command and query, and the shared `examples/` scaffold

> **Issue**: [#87](https://github.com/dlrivada/Encina/issues/87) (family: #88, #89, #188; the guide of #344 links to the finished examples)
> **Type**: Feature (documentation and examples; no change under `src/`)
> **Research**: `docs/tutorials/quickstart.md` (the tutorial this example complements), `src/Encina/Abstractions/IEncina.cs`, `src/Encina/Abstractions/CqrsContracts.cs`, `src/Encina/Results/EncinaErrors.cs`, `Directory.Build.props`, `Directory.Packages.props`, `Encina.slnx`, `.github/workflows/ci.yml`
> **ADR**: none (ADR number assigned when the ADR pull request opens, if the maintainer's answer to D1 asks for one)
> **Related**: #88 (`02-cqrs-complete`), #89 (`03-entity-framework`, `04-dapper-integration`, `05-redis-caching`), #188 (Aspire Dashboard guide, sample application), #344 (adoption guide, links here)
> **Complexity**: Low (one small console project) plus the shared scaffold every other example in the family needs
> **Estimated Scope**: about 11 files, about 330 lines (project, 5 source files, 2 READMEs, one scaffold props file, the solution entry)

---

## Summary

Encina has no runnable example today. `examples/` does not exist, `samples/` does not exist, and `docs/examples/` holds documentation pages about CDC only (`cdc-basic-setup.md` and three siblings plus `index.md`). The only first contact is `docs/tutorials/quickstart.md`, which builds a local package feed because Encina is pre-1.0 and is not published to nuget.org, and walks a reader through a console app step by step. This plan adds the finished version of that app as a project to run and read, and it creates the folder and build rules that the other examples of the family (#88, #89, #188) reuse.

**What the example is.** A console application with one command (`CreateGreeting`) and one query (`GetGreeting`) and their handlers, sent through `IEncina`, that prints the success track and the error track of the result:

- `IEncina.Send<TResponse>(IRequest<TResponse>, CancellationToken)` returns `ValueTask<Either<EncinaError, TResponse>>` (`src/Encina/Abstractions/IEncina.cs`). The original issue text said `mediator.SendAsync`; no such method exists. The call is `encina.Send(...)`.
- `ICommand<TResponse>`, `IQuery<TResponse>`, `ICommandHandler<,>` and `IQueryHandler<,>` come from `src/Encina/Abstractions/CqrsContracts.cs`. A handler returns `Either<EncinaError, TResponse>` and signals a business failure with `EncinaErrors.Create("greeting.name_required", "...")`, never an exception (ADR-001, AGENTS.md section 3).
- The error branch prints the error code with `error.GetCode()` (`Option<string>`, `src/Encina/Results/EncinaErrors.cs:109`), never `EncinaError.Message`. The issue marked this accessor as "confirmed when the example is written"; it is confirmed here.
- Registration is `services.AddEncina(typeof(Program).Assembly)` (`src/Encina/Core/ServiceCollectionExtensions.cs`), the same call as the quickstart.

**What this plan owns for the whole family.** The folder `examples/`, its `Directory.Build.props` (the `IsPackable=false` rule), its index `README.md`, the `/examples/` folder entry in `Encina.slnx` and the CI wiring, all depending on the maintainer's answer to Dilemma D1. #88, #89 and #188 reference this plan for those pieces and add only their own project.

**Provider category**: none. The example touches no store, no transport and no cache (AGENTS.md section 5).

**Affected packages**: `Encina` (consumed, not changed); the new example project `examples/01-basic-encina` (folder per D1).

**Size**: 1 plan file; the implementation is one worker pull request of about 11 files and 330 lines, within the 15-file and 1500-line limit.

---

## Design Choices

Five choices follow. D1 is a Dilemma shared by #87, #88, #89 and #188; this plan owns its text and the other three plans point here.

<details>
<summary><strong>1. Where the examples live and how they reference Encina (D1, shared with #88, #89, #188)</strong></summary>

**Class:** Dilemma

Encina is pre-1.0 and is not published to nuget.org (`docs/tutorials/quickstart.md` builds a local feed). `examples/` does not exist. Today's build facts that the options depend on: `Directory.Build.props` applies `TreatWarningsAsErrors` and the recommended analyzer set to every project under the repository; `Directory.Packages.props` turns on central package version management, so any package an example uses must have a `PackageVersion` entry; `IsPackable` defaults to true only under `src/` and to false only under `tests/`, so a console project elsewhere must set it explicitly; `.github/workflows/ci.yml` builds `Encina.slnx` with `-warnaserror` and runs `dotnet format --verify-no-changes`, and its path filter lists `src/**`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Top-level `examples/` folder of runnable projects with `ProjectReference` to `src/`, listed in `Encina.slnx` under an `/examples/` folder and built by CI** | Always compiles against the current code; the zero-warnings rule applies; an API change that breaks an example breaks the pull request that made it; the reader sees the code that is in the same commit | Widens the solution build; every example follows every API change; examples that need Docker (#89) can only be built, not run, in CI; the CI path filter must add `examples/**` (a workflow file, owned by the orchestrator) |
| **(b) The same folder outside `Encina.slnx`, referencing a locally packed feed like the quickstart** | Looks exactly like what a user copies into their own project (`PackageReference`, not `ProjectReference`) | Nothing in CI breaks when an API changes, so examples rot silently; every reader first has to pack a feed |
| **(c) Snippet-only pages under `docs/examples/`** | No build cost; reads inside the documentation site | Nothing proves the code compiles; the issue asks for an example "compiles and runs" project, and the `encina-docs` skill says an example longer than a few lines belongs in a compiling project |

### Chosen Option: **(a) — `examples/` with `ProjectReference`, inside `Encina.slnx` and CI** (recommended, pending the maintainer)

### Rationale

We recommend (a) while the API still moves before 1.0: the repository rule is that documentation never promises code that does not compile (the `encina-docs` skill, house rule 3), and only (a) enforces it mechanically. (b) wins after 1.0, when packages are published and an example should show the `PackageReference` a user types. (c) wins only for an example too small to need a project, which none of the five are. The README of each example states the `ProjectReference` is a stand-in for `dotnet add package Encina` once 1.0 ships (pre-1.0 statement, no promise).

</details>

<details>
<summary><strong>2. Method name and result handling shown by the example</strong></summary>

**Class:** Settled
**Settled by:** ADR-001 (Railway Oriented Programming: operations return `Either<EncinaError, T>`), AGENTS.md section 3 (Railway Oriented Programming), `IEncina.Send` in `src/Encina/Abstractions/IEncina.cs`

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `encina.Send(...)` and `Match` on both tracks** | The real API; the reader sees the success and the error track in one place | None found |
| **B — The original text's `mediator.SendAsync(...)`** | Familiar to MediatR users | The method does not exist; the example would not compile |
| **C — Unwrap with `IfLeft` throwing** | Short | Turns a `Left` into an exception, against the ROP rule |

### Chosen Option: **A — `Send` with `Match`**

### Rationale

Only A compiles and complies with ADR-001. The code comment next to the call explains that `Send` returns an `Either` and that exactly one branch of `Match` runs.

</details>

<details>
<summary><strong>3. What the error branch prints</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (`EncinaError.Message` never reaches logs, activity tags, health-check results or plaintext storage; record only the error code or the exception type)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Print `error.GetCode()` (the `Option<string>` code, with a fallback text)** | Complies with the rule; teaches the habit users should copy into logging code | The reader does not see the human-readable message |
| **B — Print `error.Message`, as `docs/tutorials/quickstart.md` does** | Friendlier output | Teaches the pattern the rule forbids for logs; users copy examples into their logging code |

### Chosen Option: **A — Print the code**

### Rationale

An example is copied. Printing the code keeps the copied habit compliant. The quickstart is a different page (a tutorial) and is not changed by this plan.

</details>

<details>
<summary><strong>4. Host model of the console app</strong></summary>

**Class:** Trivial
**Settled by:** #81 (`docs/tutorials/quickstart.md` uses `new ServiceCollection()`, `AddEncina(typeof(Program).Assembly)` and `BuildServiceProvider()`), AGENTS.md section 1 (minimal hosting is the .NET 10 default for hosts; a console demo needs no host)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Plain `ServiceCollection` and `BuildServiceProvider`, as the quickstart** | Smallest possible; the same lines the reader met in the tutorial | No lifetime management beyond `await using` |
| **B — Generic Host (`Host.CreateApplicationBuilder`)** | Closer to a real service | Adds a package and a concept the first example should not carry |

### Chosen Option: **A — Plain `ServiceCollection`**

### Rationale

The first example removes distractions (issue motivation: "fundamentals without distractions"). #88 moves to the Web host where a host is needed.

</details>

<details>
<summary><strong>5. Where the query reads its data and how the scaffold keeps examples out of the packages</strong></summary>

**Class:** Trivial
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use; every feature opt-in), `Directory.Build.props` (`IsPackable` defaults to true only under `src/`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — A tiny in-memory `GreetingStore` (a `ConcurrentDictionary`) registered as a singleton; `examples/Directory.Build.props` sets `IsPackable` to false** | The query has something to read; no database, no Docker; the examples never ship as packages | One file more than the issue's list |
| **B — The query returns a constant** | One file fewer | A query that reads nothing teaches nothing about the read side |
| **C — Reuse an Encina store (`Encina.Testing.Fakes`)** | No new class | Pulls a testing package into the first example |

### Chosen Option: **A — In-memory store and a scaffold props file**

### Rationale

The example needs state to make the command and the query meaningful, and the smallest state is a dictionary in the project itself. The props file is the single place that records "examples are not packages", so no example repeats it.

</details>

---

## Implementation Phases

### Phase 1: Scaffold of `examples/` (owned here, reused by #88, #89, #188)

> **Goal**: The folder, the build rules and the solution entry exist once. Depends on the answer to D1: the tasks below are for the recommended option (a); for (b) the solution entry and the CI task are dropped and the project uses a `PackageReference` to the local feed; for (c) the phase ends at the page under `docs/examples/`.

<details>
<summary>Tasks</summary>

1. `examples/Directory.Build.props`: imports the repository `Directory.Build.props` (`<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />`), then sets `<IsPackable>false</IsPackable>` and `<IsPublishable>false</IsPublishable>`. No other property: the zero-warnings and analyzer rules come from the root file.
2. `examples/README.md`: index of the examples (a table: folder, what it shows, needs Docker yes/no), the statement that the `ProjectReference` lines are the pre-1.0 stand-in for `PackageReference`, and the way to run one (`dotnet run --project examples/01-basic-encina`). Added by `docs-writer`.
3. `Encina.slnx`: a new `<Folder Name="/examples/">` with the project entries, next to the existing `/src/`, `/tests/` and `/tools/` folders.
4. CI wiring (orchestrator task, shared hot spot `.github/workflows/*`): add `examples/**` to the path filters that list `src/**` in `ci.yml`, so a change under `examples/` builds the solution; no new job.
5. `Directory.Packages.props`: no change for this example (it uses only `Encina`). #88 and #89 add their own `PackageVersion` entries only for packages not yet listed (checked: `FluentValidation`, `Microsoft.EntityFrameworkCore.SqlServer`, `Dapper`, `Microsoft.Data.SqlClient` and `StackExchange.Redis` are already listed).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. The repository has no examples folder. Directory.Build.props sets TreatWarningsAsErrors and the recommended analyzers for every project; Directory.Packages.props manages package versions centrally; IsPackable is true only under src/. Issue #87 (plan docs/plans/example-basic-encina-implementation-plan-87.md) creates examples/ for #87, #88, #89 and #188.
TASK: Create examples/Directory.Build.props (import the root file with GetPathOfFileAbove, set IsPackable and IsPublishable to false). Add an /examples/ folder to Encina.slnx. Do not touch .github/workflows (the orchestrator does). Ask docs-writer for examples/README.md.
KEY RULES: Edit files only with the Edit/Write tools; zero warnings; no code under src/; English only; no AI attribution in commits.
REFERENCE FILES: Directory.Build.props, Directory.Packages.props, Encina.slnx, docs/tutorials/quickstart.md.
```

</details>

### Phase 2: The example project

> **Goal**: `examples/01-basic-encina` builds with zero warnings and prints a success line and an error line.

<details>
<summary>Tasks</summary>

1. `examples/01-basic-encina/01-basic-encina.csproj`: `OutputType` Exe, `net10.0`, nullable enabled, `ProjectReference` to `src/Encina/Encina.csproj` (D1 option a).
2. `Commands/CreateGreeting.cs`: `public sealed record CreateGreeting(string Name) : ICommand<Guid>;` the command returns the new id.
3. `Queries/GetGreeting.cs`: `public sealed record GetGreeting(Guid Id) : IQuery<string>;`.
4. `GreetingStore.cs`: `internal sealed class GreetingStore` wrapping a `ConcurrentDictionary<Guid, string>`; registered with `services.AddSingleton<GreetingStore>()`.
5. `Handlers/CreateGreetingHandler.cs`: `ICommandHandler<CreateGreeting, Guid>`; an empty name returns `EncinaErrors.Create("greeting.name_required", "Name must not be empty.")`; otherwise stores `Hello, {Name}!` and returns the id as `Right`.
6. `Handlers/GetGreetingHandler.cs`: `IQueryHandler<GetGreeting, string>`; a missing id returns `EncinaErrors.Create("greeting.not_found", "...")`.
7. `Program.cs`: builds the `ServiceCollection`, calls `AddEncina(typeof(Program).Assembly)`, sends `CreateGreeting("World")`, `GetGreeting(id)` and `CreateGreeting("")`, and uses `Match` on each; the error branch prints `error.GetCode().IfNone("unknown")`. Comments explain `Either`, `Match` and the two tracks.
8. `README.md` (by `docs-writer`): purpose, the file tree, one paragraph per file, the expected output (pasted from a real run), the link to `docs/tutorials/quickstart.md`, and the pre-1.0 note.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: examples/ scaffold exists (Phase 1). Encina sends requests with IEncina.Send, which returns ValueTask<Either<EncinaError, TResponse>>. Handlers implement ICommandHandler<,> / IQueryHandler<,> from src/Encina/Abstractions/CqrsContracts.cs and return business failures as EncinaErrors.Create(code, message), never exceptions.
TASK: Create examples/01-basic-encina with the csproj, CreateGreeting, GetGreeting, GreetingStore, two handlers and Program.cs as listed in the plan (Phase 2 tasks 1-7). Print success on the Right branch and only the error code on the Left branch (error.GetCode()), never EncinaError.Message.
KEY RULES: Verify every type and member against src/ before using it (Grep); the call is encina.Send, there is no SendAsync; comments explain the concepts for a reader new to Encina; zero warnings (TreatWarningsAsErrors is on); no [Obsolete]; English only.
REFERENCE FILES: docs/tutorials/quickstart.md, src/Encina/Abstractions/IEncina.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Results/EncinaErrors.cs, src/Encina/Core/ServiceCollectionExtensions.cs.
```

</details>

### Phase 3: Testing and CI

> **Goal**: The example is proven to build and run.

<details>
<summary>Tasks</summary>

1. `dotnet build examples/01-basic-encina -c Release -warnaserror` exits 0; `dotnet build Encina.slnx -c Release -warnaserror` still exits 0 with the new folder.
2. `dotnet run --project examples/01-basic-encina` prints a success line for the command, the stored greeting for the query and an error line with `greeting.name_required`; the three lines are pasted into the README.
3. No test project is added: the example is not a package, no file under `src/` changes and no per-flag coverage target applies (AGENTS.md section 9, obligations apply to `src/`). The solution build in CI is the regression gate (option (a)).
4. `dotnet format Encina.slnx --verify-no-changes` exits 0.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: examples/01-basic-encina exists (Phase 2).
TASK: Run the build, the run and the format check listed in the plan (Phase 3 tasks 1, 2, 4) from the worktree root and paste the real output into the report. If a command fails, spawn ci-diagnoser; do not guess.
KEY RULES: PowerShell only; run from the worktree; never skip the zero-warnings build; report output verbatim.
REFERENCE FILES: examples/01-basic-encina/Program.cs, Directory.Build.props, .github/workflows/ci.yml.
```

</details>

### Phase 4: Documentation and finalization

> **Goal**: The example is findable from the documentation.

<details>
<summary>Tasks</summary>

1. `examples/01-basic-encina/README.md` and `examples/README.md` (by `docs-writer`; Phase 1 task 2 and Phase 2 task 8).
2. One link from `docs/tutorials/quickstart.md` to the finished example ("the finished project is in `examples/01-basic-encina`"), added by `docs-writer`.
3. Cross-cutting evaluation recorded: see the matrix; every function is not applicable for this example.
4. No changelog fragment: no library change (the issue states "not needed"); no ADR, no ROADMAP change, no `PublicAPI` line.
5. Report the answer to D1 on the issue, and tell #88, #89 and #188 that the scaffold is merged.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The example builds and runs (Phase 3). Documentation pages follow the encina-docs skill: one Diataxis quadrant per page, real API names, no hand-typed figures, just-the-docs front matter for pages under docs/.
TASK: Spawn docs-writer for the two READMEs and the one link in docs/tutorials/quickstart.md; name the worktree path in the prompt. Then run the link and lint checks the encina-docs skill lists (lychee offline, markdownlint-cli2) or, if unavailable, check every relative link with Test-Path.
KEY RULES: English only; the README states "pre-1.0" and shows the pasted output of a real run; no migration paths; no private client application names.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, docs/tutorials/quickstart.md, examples/01-basic-encina/Program.cs.
```

</details>

---

## Research

### Relevant Standards and Prior Art

| Source | Relevance to this plan |
|--------|------------------------|
| ADR-001 Railway Oriented Programming (`docs/architecture/adr/`) | The example shows `Either<EncinaError, T>` on both tracks |
| AGENTS.md section 3, `EncinaError.Message` rule | The error branch prints the code only |
| `.claude/skills/encina-docs/SKILL.md`, house rule 3 | Examples longer than a few lines belong in a compiling project the page links to |
| `docs/tutorials/quickstart.md` (#81) | Teaches the same first command step by step; the example is the finished project |
| .NET sample repositories of comparable libraries (MediatR samples, Wolverine samples) | Runnable `examples/` or `samples/` folders inside the main repository, built in CI |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IEncina.Send` | `src/Encina/Abstractions/IEncina.cs` | Sends the command and the query |
| `ICommand<T>`, `IQuery<T>`, handler interfaces | `src/Encina/Abstractions/CqrsContracts.cs` | The messages and handlers |
| `EncinaErrors.Create`, `GetCode` | `src/Encina/Results/EncinaErrors.cs` | Business failure and its code |
| `AddEncina(params Assembly[])` | `src/Encina/Core/ServiceCollectionExtensions.cs` | Registration by assembly scan |
| Root build rules | `Directory.Build.props`, `Directory.Packages.props` | Zero warnings; central versions; `IsPackable` rule |
| Solution file and CI | `Encina.slnx`, `.github/workflows/ci.yml` | Solution entry; build with `-warnaserror` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | The example adds no `[LoggerMessage]` and no library code, so no range is registered in `EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Example project | 7 | csproj, `Program.cs`, 2 messages, 2 handlers, `GreetingStore.cs` |
| Scaffold | 3 | `examples/Directory.Build.props`, `examples/README.md`, `Encina.slnx` entry |
| Documentation | 2 | the example `README.md`, one link in `docs/tutorials/quickstart.md` |
| **Total** | **12** | About 330 lines; within one worker pull request |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 mediator library with Railway Oriented Programming (Either<EncinaError, T>), opt-in features and provider coherence (AGENTS.md). The repository has no runnable examples. Issue #87 adds examples/01-basic-encina and the shared examples/ scaffold (plan docs/plans/example-basic-encina-implementation-plan-87.md). Dilemma D1 (where examples live) is recommended as option (a): top-level examples/ with ProjectReference, in Encina.slnx and CI; it is pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. Scaffold: examples/Directory.Build.props (import root, IsPackable and IsPublishable false), examples/README.md, an /examples/ folder in Encina.slnx; CI path filter by the orchestrator.
2. Project: csproj with ProjectReference to src/Encina; CreateGreeting (ICommand<Guid>), GetGreeting (IQuery<string>), GreetingStore (in-memory), two handlers, Program.cs with Send and Match on both tracks.
3. Error output: error.GetCode() only, never EncinaError.Message.
4. Verification: zero-warnings build of the project and of Encina.slnx, dotnet run output pasted into the README, dotnet format check.
5. Docs: two READMEs and one link from the quickstart via docs-writer.

KEY PATTERNS: the call is encina.Send (no SendAsync); handlers return Either, never throw for business failures; verify every identifier in src/ before using it; TreatWarningsAsErrors is on; examples are never packable; English only; no AI attribution; no private client application names.

REFERENCE FILES: docs/tutorials/quickstart.md, src/Encina/Abstractions/IEncina.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Results/EncinaErrors.cs, src/Encina/Core/ServiceCollectionExtensions.cs, Directory.Build.props, Directory.Packages.props, Encina.slnx, .github/workflows/ci.yml, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Example code only; adds no runtime component to the library (caching is shown by #89, example 05) |
| 2 | OpenTelemetry | ❌ | Console demo with nothing to trace; the dashboard guide is #188 |
| 3 | Structured Logging | ❌ | No library code and no EventIds; the logging behavior is shown by #88 |
| 4 | Health Checks | ❌ | No checkable dependency |
| 5 | Validation | ❌ | The empty-name check is a business rule returned as an error; the validation pipeline is shown by #88 |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | No store; transactions are shown by #89 (example 03) |
| 9 | Idempotency | ❌ | No duplicate input |
| 10 | Multi-Tenancy | ❌ | No tenant data |
| 11 | Module Isolation | ❌ | Single module |
| 12 | Audit Trail | ❌ | Demo data with no compliance implication |

No function is deferred, so no follow-up issue is needed for the matrix.

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| D1 (this plan) | pending the maintainer | Blocks the scaffold shape for #87, #88, #89 and #188 |
| `docs/tutorials/quickstart.md` (#81) | merged | Linked; one line added to it |
| CI path filter for `examples/**` | to do (orchestrator, `.github/workflows/ci.yml`) | Only for option (a); a shared hot spot, not edited by the worker |
| #88, #89, #188 | open (plans in this family) | Consume the scaffold; start after Phase 1 merges |

**Risks**: under option (a) every public API change in `src/` can break an example and the pull request that changes the API has to fix it (intended: that is the purpose); `dotnet format --verify-no-changes` now covers the example files; the examples are not covered by `coverage-report.cs` or the crap gate (they classify test projects and `src/` files), which is intended.

---

## Next Steps

1. Merge this plan.
2. When the issue is picked up, re-check D1 against today's repository and ask the maintainer (the orchestrator asks the Dilemma; the answer goes into `## Maintainer Decisions`).
3. The orchestrator writes one worker brief for Phases 1 to 4 (about 12 files, 330 lines); Phase 1 task 4 (workflow file) stays with the orchestrator.
4. Delegate the READMEs and the quickstart link to `docs-writer`, the review to `adversarial-reviewer`.
5. Close #87 with the implementation pull request.
