# Implementation Plan: Dev Containers and GitHub Codespaces Support

> **Issue**: [#447](https://github.com/dlrivada/Encina/issues/447)
> **Type**: Feature
> **Complexity**: Low (7 phases, 1 new CLI service, 1 repository configuration, no provider code)
> **Estimated Scope**: ~350 lines of production code in `Encina.Cli` + ~500 lines of tests + ~120 lines of configuration and documentation

---

## Summary

Provide a ready-to-use Dev Container for developing Encina itself (`.devcontainer/` at the repository root, also the base of GitHub Codespaces), and an `encina add devcontainer [--services ...]` command that writes a dev container into an existing project, plus a `--devcontainer` option of `encina new`.

State of the code (checked on `origin/main`, 2026-11-11): there is no `.devcontainer/` directory and no `devcontainer` string in `src/` or `tools/`; the only mentions are in `ROADMAP.md` (lines 6553 and 6737-6743, plus the architecture diagram) and `docs/INVENTORY.md`. `docker-compose.yml` already defines the services with the profiles `databases`, `core`, `caching`, `messaging`, `cloud`, `observability` and `full`. `src/Encina.Cli` has `NewCommand`, `GenerateCommand` and `AddCommand`; `AddCommand.Create()` registers the subcommands `caching`, `database`, `transport`, `validation`, `resilience` and `observability`, each adding package references through `PackageManager`. `ProjectScaffolder` (public static, `Services/ProjectScaffolder.cs`) writes files from string templates and refuses a non-empty directory unless `ProjectOptions.Force` is set. The CLI scope is fixed by ADR-058 (D35: no `dotnet new` templates, no `remove` or `list` commands in 1.0).

- **Standards**: Dev Containers specification (containers.dev), GitHub Codespaces `hostRequirements`.
- **Estimated scope**: 15 files, about 970 lines in total (see Research).
- **Affected packages**: `Encina.Cli` (`AddCommand`, `NewCommand`, a new `DevContainerScaffolder`), repository root (`.devcontainer/`), `docs/`.
- **Provider category**: none. The feature starts the services that the existing provider tests already use; it adds no provider-specific code.
- **Prerequisites**: none open. #2983 (CLI command host, ADR-058) is the host of the new subcommand; #2521 (the overwrite behaviour of `encina generate`) is the behaviour this command must not repeat; #2526 is the sibling "add only adds package references" issue.

---

## Design Choices

<details>
<summary><strong>1. Docker access of the repository dev container (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `docker-in-docker` feature** | Self-contained: the Testcontainers fixtures and `docker compose --profile core up -d` run inside the container; supported in Codespaces; no dependency on the host | Privileged container, slower start, larger disk use |
| **B) `docker-outside-of-docker` (host daemon)** | Lighter and faster, shares the host image cache | Not equivalent in Codespaces, bind-mount paths differ from the host, host containers are visible to the dev container |
| **C) Compose-based dev container (`dockerComposeFile`)** | Services start with the container as siblings | Testcontainers-based tests still need a daemon, so A or B is needed anyway |

### Chosen Option: **A — `docker-in-docker` feature** (recommended, pending the maintainer)

### Rationale

- Option A wins for Codespaces and for contributors without a local Docker daemon, which is the onboarding scenario of the issue; B wins for local VS Code users with Docker Desktop who want speed.
- The integration test fixtures of `AGENTS.md` section 9 (`[Collection("<Family>-<Database>")]`) need a daemon the tests own, which A provides without host coupling.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. When the repository dev container builds the solution (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `postCreateCommand: dotnet restore && dotnet build` (the issue sketch)** | The container is ready to run tests right after creation | The full `Encina.slnx` builds with `-maxcpucount:1 -nodeReuse:false` (`Directory.Build.rsp`), so creation is slow on small machines |
| **B) `onCreateCommand: dotnet restore Encina.slnx` only; the build runs on demand or in an optional Codespaces prebuild** | Fast creation; a prebuild (configured in the repository settings by the maintainer) amortizes the build in Codespaces | The first `dotnet test` of a local container pays the build |

### Chosen Option: **B — restore on create, build on demand** (recommended, pending the maintainer)

### Rationale

- B wins in Codespaces, where prebuilds exist and machine time is billed; A wins for a local contributor with a fast machine who wants zero steps after creation.
- Either way `hostRequirements` declares at least 4 CPUs and 16 GB, because the solution is large; the exact numbers are measured in Phase 2.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Source of the services in a generated project</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The CLI writes `.devcontainer/docker-compose.yml` with only the selected services, images and ports taken from one table in the CLI** | Works offline, deterministic, tested by parsing | The image tags are a second copy of those in the repository `docker-compose.yml` |
| **B) The CLI downloads the repository compose file** | Single source | Needs network and a pinned URL; a generated project would depend on the Encina repository layout |
| **C) Tell the user to write the compose file** | No code | Defeats the command |

### Chosen Option: **A — a generated compose file from one service table**

### Rationale

- A project that uses Encina has no Encina `docker-compose.yml`, so the "reuse" of the issue applies to the Encina repository dev container (it uses the repository file and its profiles) and not to generated projects.
- The service table (name, image, ports, environment) lives in one class and a test asserts that every image tag equals the tag in the repository `docker-compose.yml`, so the two copies cannot drift.

**Class:** Trivial
**Settled by:** `docker-compose.yml` (pinned image tags and ports); `docs/infrastructure/docker-infrastructure.md`; the lifetime-support rule of the maintainer's LTS decision (every default image is the vendor LTS or stable release, one source per provider)

</details>

<details>
<summary><strong>4. Overwrite behaviour of `encina add devcontainer`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Refuse when `.devcontainer/` already has the target files unless `--force` is given** | Never loses user edits; same flag as `encina new` | One more flag |
| **B) Overwrite silently** | Simple | Repeats the bug class of #2521 |
| **C) Merge into the existing `devcontainer.json`** | Keeps user edits | JSON merge rules are arguable and large for a low-priority feature |

### Chosen Option: **A — refuse unless `--force`**

### Rationale

- `ProjectScaffolder` already refuses a non-empty directory without `Force`, and `NewCommand` exposes `--force`; the new command follows the same contract.
- The check names the conflicting files in the error and writes nothing (all-or-nothing), so a second run is a safe no-op.

**Class:** Settled
**Settled by:** `src/Encina.Cli/Services/ProjectScaffolder.cs` (the `Force` contract); #2521; `AGENTS.md` section 3 (errors are never swallowed)

</details>

<details>
<summary><strong>5. Shape of the CLI surface</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina add devcontainer [--services a,b]` plus a `--devcontainer` option on `encina new`** | Matches the issue and the existing command family; no new top-level command | `add` otherwise adds package references, so the help text must say what this one writes |
| **B) A new top-level `encina devcontainer` command** | Clear name | A second place to look; outside the command contract |
| **C) A `dotnet new` template** | Familiar to .NET users | Excluded by ADR-058 D35 |

### Chosen Option: **A — sibling subcommand of `add` and an option of `new`**

### Rationale

- Keeps the surface decided by ADR-058; no `list` or `remove` is added.

**Class:** Settled
**Settled by:** ADR-058 (D35); the issue text (`encina add devcontainer --services postgres,redis`)

</details>

---

## Implementation Phases

### Phase 1: Core API Design — service catalog and options

> **Goal**: define what the scaffolder knows and how it reports, before any file is written.

<details>
<summary><strong>Tasks</strong></summary>

1. Create `src/Encina.Cli/Services/DevContainerScaffolder.cs` in the namespace `Encina.Cli.Services`, `public static class DevContainerScaffolder`, next to `ProjectScaffolder`.
2. In it, `public sealed class DevContainerOptions` with `OutputDirectory` (required), `Services` (`IReadOnlyList<string>`), `Force`, mirroring `ProjectOptions`.
3. A private service table: key (`postgres`, `sqlserver`, `mysql`, `mongodb`, `redis`, `rabbitmq`, `kafka`, `nats`), compose service name, image, ports; the keys and ports equal those of `docker-compose.yml`.
4. `public static Task<ScaffoldResult> CreateAsync(DevContainerOptions options, CancellationToken cancellationToken = default)` returning the existing `ScaffoldResult` (`Ok(files)` or `Error(message)`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Cli (net10.0, C# 14). ScaffoldResult and ProjectOptions live in src/Encina.Cli/Services/ProjectScaffolder.cs.
TASK: Create DevContainerScaffolder with DevContainerOptions and the service table (postgres, sqlserver, mysql, mongodb, redis, rabbitmq, kafka, nats). Do not write files yet.
KEY RULES: XML docs on public members; no [Obsolete]; ROP style errors through ScaffoldResult.Error; nullable enabled; keep methods at cyclomatic complexity 5 or less (CRAP gate).
REFERENCE FILES: src/Encina.Cli/Services/ProjectScaffolder.cs, docker-compose.yml.
```

</details>

### Phase 2: Implementation — generated files and the repository dev container

> **Goal**: write the files for a generated project, and add the repository `.devcontainer/`.

<details>
<summary><strong>Tasks</strong></summary>

1. In `DevContainerScaffolder.CreateAsync`: validate the service names against the table (unknown name gives `ScaffoldResult.Error` naming the allowed keys); refuse when `.devcontainer/devcontainer.json` or `.devcontainer/docker-compose.yml` exist and `Force` is false (nothing written); then write `devcontainer.json` (built with `System.Text.Json` so it is always valid) and, when services were selected, `docker-compose.yml`.
2. The generated `devcontainer.json` uses the `dotnet` feature with `"version": "10.0"` (`AGENTS.md` section 1), the VS Code extensions `ms-dotnettools.csdevkit` and `ms-dotnettools.csharp`, `forwardPorts` for the selected services, and `dockerComposeFile` only when services exist. No credential is written; the development passwords are the documented defaults of the service table.
3. Add `.devcontainer/devcontainer.json` for the repository (decisions D1 and D2): `dotnet` feature 10.0, the Docker feature chosen by D1, extensions, `forwardPorts` `[5432, 6379, 5672]`, and `hostRequirements`. It starts services with `docker compose --profile core up -d` from the repository `docker-compose.yml`; no second compose file.
4. Add `.devcontainer/README.md` (what starts, how to open in Codespaces).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created DevContainerScaffolder with the service table. Decisions D1 (Docker access) and D2 (build timing) are in the Maintainer Decisions section or marked recommended.
TASK: Implement CreateAsync (validate, refuse-without-force, write files) and add .devcontainer/devcontainer.json and README.md at the repository root.
KEY RULES: .NET 10 only (feature version "10.0"); no secrets in generated files; all-or-nothing write (check every target first); file IO asynchronous; JSON built with System.Text.Json; pwsh or C# only for any helper script (AGENTS.md section 2).
REFERENCE FILES: src/Encina.Cli/Services/ProjectScaffolder.cs (Force contract), docker-compose.yml, Directory.Build.rsp.
```

</details>

### Phase 3: CLI Integration — `add devcontainer` and `new --devcontainer`

> **Goal**: expose the scaffolder through the existing command family.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Cli/Commands/AddCommand.cs`: add `CreateDevContainerCommand()` to the collection in `Create()`; options `--services` (comma separated), `--output` (default the current directory) and `--force`; an `ExecuteDevContainerAsync` that prints the written files with `AnsiConsole` and returns 0, or the error with exit code 1 (the pattern of `ExecuteCachingAsync`).
2. `src/Encina.Cli/Commands/NewCommand.cs`: add `--devcontainer` (bool); after `ProjectScaffolder.CreateProjectAsync` succeeds, call `DevContainerScaffolder.CreateAsync` for the project folder, using `--database`, `--caching` and `--transport` to preselect services when the dev container option is on.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: DevContainerScaffolder is complete. AddCommand.Create() lists subcommands; NewCommand builds ProjectOptions and calls ProjectScaffolder.
TASK: Add the devcontainer subcommand to AddCommand and the --devcontainer option to NewCommand, wired to DevContainerScaffolder.
KEY RULES: no list or remove command (ADR-058 D35); exit code 0 on success and 1 on error as in the existing commands; keep each method simple (CRAP <= 10); do not log the exception message of an Either error; update the command help text.
REFERENCE FILES: src/Encina.Cli/Commands/AddCommand.cs, src/Encina.Cli/Commands/NewCommand.cs.
```

</details>

### Phase 4: Cross-Cutting Integration

> **Goal**: apply the two integrations the matrix marks (validation and idempotency).

<details>
<summary><strong>Tasks</strong></summary>

1. Validation: unknown `--services` names and an output folder that does not exist are rejected before any write, with one message that lists the allowed names.
2. Idempotency: a second run without `--force` writes nothing and exits 1 with the list of existing files; with `--force` it rewrites the same content (a byte-for-byte equal result, asserted by a test).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Phases 1 to 3 are done.
TASK: Add the validation and the no-silent-overwrite checks to DevContainerScaffolder and surface them through both commands.
KEY RULES: all-or-nothing; the error names files, never contents; deterministic output (sorted services, fixed property order).
REFERENCE FILES: src/Encina.Cli/Services/ProjectScaffolder.cs, tests/Encina.UnitTests/Cli/Commands/AddCommandTests.cs.
```

</details>

### Phase 5: Testing

> **Goal**: reach the Encina.Cli manifest targets for the touched files and prove the generated files.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit tests (`tests/Encina.UnitTests/Cli/Services/DevContainerScaffolderTests.cs`, `tests/Encina.UnitTests/Cli/Commands/AddCommandTests.cs`, `NewCommandTests.cs`): files written for no service, one service and all services; unknown service; refusal without `--force`; rewrite with `--force`; real file system under a temp folder.
2. Guard tests (`tests/Encina.GuardTests/CLI/DevContainerScaffolderGuardTests.cs`): `ArgumentNullException` on `options`.
3. Integration test (`tests/Encina.IntegrationTests/Cli/Services/DevContainerGeneratedFilesTests.cs`): the generated `devcontainer.json` parses, every selected service exists in the repository `docker-compose.yml`, and every image tag equals the repository tag. No Docker needed.
4. Contract, property, load and benchmark tests: not applicable (no public interface or abstract class, no complex logic); the load and benchmark justification files are written under `tests/Encina.LoadTests/Cli/` and `tests/Encina.BenchmarkTests/Cli/` (see the AGENTS.md justification format).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: The feature is implemented. Coverage is per flag (unit, guard, contract, property, integration) from .github/coverage-manifest/Encina.Cli.json.
TASK: Write the unit, guard and integration tests listed in the plan, and the two justification .md files.
KEY RULES: tests execute the real scaffolder; Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; temp folders cleaned up; Arrange-Act-Assert; measure per-flag coverage of the touched files and report it against the plan targets.
REFERENCE FILES: tests/Encina.UnitTests/Cli/Commands/AddCommandTests.cs, tests/Encina.GuardTests/CLI/ProjectScaffolderGuardTests.cs, tests/Encina.IntegrationTests/Cli/Services/ProjectScaffolderCompilationTests.cs.
```

</details>

### Phase 6: Documentation & Finalization

> **Goal**: setup documentation, metadata and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. A setup page under `docs/` (how-to quadrant, through the `encina-docs` skill): open the repository in a dev container and in Codespaces, the VS Code extensions, `encina add devcontainer`.
2. `changelog.d/447-devcontainer-support.added.md` (a bullet), `ROADMAP.md` entry update, `docs/INVENTORY.md` update.
3. `.github/coverage-manifest/Encina.Cli.json`: per-file entries for `Services/DevContainerScaffolder.cs` (and a justification for `AddCommand.cs` and `NewCommand.cs`) as in Research.
4. `dotnet build --configuration Release` with 0 warnings; the three CLI test projects pass; `check-plan` and the link check stay green.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: Code and tests are done. Documentation goes through the docs-writer; changelog fragments, PublicAPI lines and manifest entries through the mechanical-fixer.
TASK: Add the setup page, the changelog fragment, the ROADMAP and INVENTORY entries and the manifest entries.
KEY RULES: never type coverage figures by hand (use covref markers); English only; no emojis; the page names the reference application generically when needed.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md, .github/coverage-manifest/Encina.Cli.json.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance | Use in this feature |
|----------|-----------|---------------------|
| Dev Containers specification (containers.dev) | `devcontainer.json` properties, Features, `hostRequirements` | Format of the generated and repository files |
| GitHub Codespaces | Machine types, prebuilds | `hostRequirements`, optional prebuild (D2) |
| ADR-058 (CLI scope, D35) | No `dotnet new`, no `remove` or `list` | CLI shape |
| `AGENTS.md` sections 1, 2, 8, 9 | .NET 10 only, PowerShell or C# scripts, build settings, local services and tests | Container content and test obligations |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| Compose services and profiles | `docker-compose.yml` | The services of the repository dev container; profiles core, databases, messaging, caching, cloud, observability, full |
| Docker guide | `docs/infrastructure/docker-infrastructure.md` | Documents the profiles; the new setup page links to it |
| CLI command host | `src/Encina.Cli/Commands/AddCommand.cs` | Gets a sibling `devcontainer` subcommand |
| Project creation command | `src/Encina.Cli/Commands/NewCommand.cs` | Gets a `--devcontainer` option |
| Project scaffolder | `src/Encina.Cli/Services/ProjectScaffolder.cs` (`ProjectOptions`, `ScaffoldResult`) | File-writing pattern and the `Force` semantics to reuse |
| Existing CLI tests | `tests/Encina.UnitTests/Cli/Commands/AddCommandTests.cs`, `NewCommandTests.cs` | Patterns for the new tests |
| Coverage manifest | `.github/coverage-manifest/Encina.Cli.json` | Per-flag targets for the touched files |
| Build settings | `Directory.Build.rsp` | `-maxcpucount:1 -nodeReuse:false` that the container must keep |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cli` | none | The CLI reports through its console output; no `[LoggerMessage]`, so no EventId range and no `EncinaEventIdAllocationTests` change |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Repository dev container | 2 | `.devcontainer/devcontainer.json`, `.devcontainer/README.md` |
| CLI production code | 3 | New `Services/DevContainerScaffolder.cs`, edits to `AddCommand.cs` and `NewCommand.cs` |
| Tests | 4 | Unit tests for the scaffolder and the commands, guard tests, one integration test of the generated files |
| Test justification files | 2 | Load and benchmark `.md` |
| Documentation and metadata | 4 | Setup page under `docs/`, ROADMAP.md entry, changelog fragment, coverage manifest entry |
| **Total** | 15 | One worker pull request (limit 15 files, 1500 lines) |

### Per-file coverage targets (Encina.Cli)

The manifest `.github/coverage-manifest/Encina.Cli.json` sets package targets (guard 15, unit 60) and no per-file targets today; the implementation adds them for the files it touches.

| File | Unit | Guard | Integration | Justification |
|------|:----:|:-----:|:-----------:|---------------|
| `Services/DevContainerScaffolder.cs` | 90 | 100 | 60 | Pure file generation under a temp folder; every branch (validation, refusal, force, services) is reachable without Docker |
| `Commands/AddCommand.cs` | 60 | 0 | 0 | The new subcommand is thin; the 0 for guard is because the command takes no object parameter that can be null |
| `Commands/NewCommand.cs` | 60 | 0 | 0 | One new option wired to the scaffolder; guard 0 for the same reason |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing Dev Containers and Codespaces support for Issue #447.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; the CLI is src/Encina.Cli (tool command "encina").
- Pre-1.0: best solution, no compatibility layers, no [Obsolete].

IMPLEMENTATION OVERVIEW:
1. DevContainerScaffolder (service table, options, ScaffoldResult).
2. Write devcontainer.json (System.Text.Json) and an optional docker-compose.yml; refuse overwrite without --force.
3. Repository .devcontainer/ (decisions D1 Docker access, D2 build timing).
4. encina add devcontainer [--services] and encina new --devcontainer.
5. Unit, guard and integration tests; load and benchmark justification files.
6. Docs page, changelog fragment, ROADMAP, INVENTORY, coverage manifest entries.

KEY PATTERNS:
- All-or-nothing file writes, errors through ScaffoldResult.Error, never the contents of a file in a message.
- Image tags in one table, asserted equal to docker-compose.yml by a test.
- Methods small (CRAP <= 10 on changed methods).

REFERENCE FILES:
- src/Encina.Cli/Services/ProjectScaffolder.cs, src/Encina.Cli/Commands/AddCommand.cs, src/Encina.Cli/Commands/NewCommand.cs
- docker-compose.yml, docs/infrastructure/docker-infrastructure.md, .github/coverage-manifest/Encina.Cli.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Development environment setup; no read path to cache |
| 2 | OpenTelemetry | ❌ | Provisioning, not runtime |
| 3 | Structured Logging | ❌ | The CLI reports through `AnsiConsole`; no `[LoggerMessage]` in `Encina.Cli` |
| 4 | Health Checks | ❌ | Environment setup, not a service |
| 5 | Validation | ✅ | The CLI validates `--services` names and the target folder before writing (Phase 4) |
| 6 | Resilience | ❌ | No call to an external system at run time |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts |
| 8 | Transactions | ❌ | The all-or-nothing check is a pre-write validation, not a unit of work |
| 9 | Idempotency | ✅ | A second run must not silently overwrite `.devcontainer/` (Phase 4, #2521 class) |
| 10 | Multi-Tenancy | ❌ | Development infrastructure; no tenant data |
| 11 | Module Isolation | ❌ | Development infrastructure |
| 12 | Audit Trail | ❌ | No compliance or security implication |

---

## Next Steps

1. Review and approve this plan.
2. The maintainer answers D1 and D2 when work on the issue starts; the answers go into the Maintainer Decisions section.
3. Implement Phases 1 to 6 as one worker pull request (15 files, about 970 lines).
4. The final commit references `Fixes #447`.
