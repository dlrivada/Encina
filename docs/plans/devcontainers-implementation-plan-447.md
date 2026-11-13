# Implementation Plan: Dev Containers and GitHub Codespaces Support

> **Issue**: [#447](https://github.com/dlrivada/Encina/issues/447)
> **Type**: Feature
> **Complexity**: Low (6 phases, 1 new CLI service, 1 repository configuration, no provider code)
> **Estimated Scope**: ~350 lines of production code in `Encina.Cli` + ~500 lines of tests + ~120 lines of configuration and documentation, delivered as 3 pull requests (22 file changes in total, see Estimated File Count)

---

## Summary

Provide a ready-to-use Dev Container for developing Encina itself (`.devcontainer/` at the repository root, also the base of GitHub Codespaces), and an `encina add devcontainer [--services ...]` command that writes a dev container into an existing project, plus a `--devcontainer` option of `encina new`.

State of the code (checked on `origin/main`, 2026-11-11): there is no `.devcontainer/` directory and no `devcontainer` string in `src/` or `tools/`; the only mentions are in `ROADMAP.md` (lines 297, the architecture diagram, and 648, the backlog entry for #447) and `docs/INVENTORY.md` (lines 265, 6553 and 6737-6743). `docker-compose.yml` already defines the services with the profiles `databases`, `core`, `caching`, `messaging`, `cloud`, `observability` and `full`. `src/Encina.Cli` has `NewCommand`, `GenerateCommand` and `AddCommand`; `AddCommand.Create()` registers the subcommands `caching`, `database`, `transport`, `validation`, `resilience` and `observability`, each adding package references through `PackageManager`. `ProjectScaffolder` (public static, `Services/ProjectScaffolder.cs`) writes files from string templates and refuses a non-empty directory unless `ProjectOptions.Force` is set. The CLI scope is fixed by ADR-058 (D35: no `dotnet new` templates, no `remove` or `list` commands in 1.0).

- **Standards**: Dev Containers specification (containers.dev), GitHub Codespaces `hostRequirements`.
- **Estimated scope**: 22 file changes (19 distinct files), about 1250 lines in total, split into three pull requests of 6, 11 and 5 files (see Research, Estimated File Count).
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
- Either way `hostRequirements` declares a CPU and memory minimum because the solution is large. 4 CPUs and 16 GB is a proposal, not a measured fact: the implementer measures a restore and a build in a Codespace in Phase 2 and the numbers are part of decision 9.

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
- The service table (name, image, ports, password variable name, see decision 6) lives in one class and a test asserts that every image tag equals the tag in the repository `docker-compose.yml`, so the two copies cannot drift.

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

### Chosen Option: **A — refuse unless `--force`** (recommended, pending the maintainer)

### Rationale

- A and C both avoid a silent overwrite, so the cited rules do not make A the only option. A wins on simplicity: `ProjectScaffolder` already refuses a non-empty directory without `Force`, and `NewCommand` exposes `--force`, so the new command follows the same contract.
- C wins for a user who edited `devcontainer.json` and wants the generated entries added to it; its cost is merge rules for JSON that are arguable.
- The check names the conflicting files in the error and writes nothing (all-or-nothing), so a second run is a safe no-op.

**Class:** Dilemma

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

- The surface `encina add devcontainer --services postgres,redis` is given by the issue text, so it is a maintainer-given choice. ADR-058 D35 excludes only option C (and `list` and `remove`); it does not exclude option B, so ADR-058 is not the reason for A.
- No `list` or `remove` is added.

**Class:** Settled
**Settled by:** the issue text of #447 (`encina add devcontainer --services postgres,redis`, maintainer-given); ADR-058 (D35) for the exclusion of option C only

</details>

<details>
<summary><strong>6. Passwords in the generated `docker-compose.yml` (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Required interpolation without a value: `${POSTGRES_PASSWORD:?set POSTGRES_PASSWORD}`** | No credential is written, as constraint C7 of the issue says; fails closed (the service does not start without a password); the user supplies it from the environment or a `.env` file | One extra step before the first start |
| **B) Interpolation with the documented development default: `${POSTGRES_PASSWORD:-YourStrong@Passw0rd}`, as the repository `docker-compose.yml` does** | Starts with no setup; the same defaults as the repository file | Writes a well-known password literal into the generated project, which the literal text of C7 ("the CLI never writes a credential") excludes |
| **C) Generate a `.env` file with a random password** | Starts with no setup and no well-known default | Writes a credential (a generated one), needs `.gitignore` handling, and a second file to refuse or overwrite |

### Chosen Option: **A — required interpolation, no value written** (recommended, pending the maintainer)

### Rationale

- postgres, sqlserver, mysql, mongodb and rabbitmq do not start without a password, so the generated file needs a password source; the service table therefore stores the variable name per service (for example `POSTGRES_PASSWORD`, `SQL_PASSWORD`, `MYSQL_ROOT_PASSWORD`, `MONGO_PASSWORD`, `RABBITMQ_PASSWORD`, the names the repository file uses), never a value.
- B is what the repository file does and what C7 calls "the documented defaults"; the two sentences of C7 point at different options (the default is documented, the CLI writes no credential), which is why the maintainer decides.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Service keys and the default of `--services` (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 8 keys (`postgres`, `sqlserver`, `mysql`, `mongodb`, `redis`, `rabbitmq`, `kafka`, `nats`); `--services` omitted writes `devcontainer.json` only, no compose file** | Small, deterministic; the 8 are the services the three provider families and the common transports use | `docker-compose.yml` also defines `mosquitto`, `garnet`, `valkey`, `dragonfly`, `keydb`, `vault`, `localstack`, `azurite` and `seq`, which a generated project cannot select |
| **B) Every service of the repository `docker-compose.yml` is a key** | Complete | A large table, and the drift test covers 17 images (cloud emulators and `latest` tags included) |
| **C) `--services` omitted selects a default set** | Useful out of the box | Starts services nobody asked for (pay-for-what-you-use) |

### Chosen Option: **A — the 8 keys, nothing when omitted** (recommended, pending the maintainer)

### Rationale

- A follows pay-for-what-you-use (`AGENTS.md` section 1): no service unless asked.
- B wins for completeness and can be added later without breaking the keys of A.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Mapping of `encina new --devcontainer` options to service keys (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A fixed map; options with no key are ignored and the command prints one line naming them** | `new` keeps working with any provider value | The dev container has fewer services than the project uses |
| **B) A fixed map; an option with no key makes `--devcontainer` fail before any write** | No silent gap | `--caching memory` and `--database sqlite` need no service, so they must map to "none" and not fail |
| **C) `--devcontainer` takes no mapping; the user runs `encina add devcontainer --services` after `new`** | No mapping to maintain | Two commands for the common case |

The map of A and B, from the values the `new` options document: `--database` `postgresql` to `postgres`, `sqlserver`, `mysql` and `mongodb` to the key of the same name, `sqlite` to none; `--caching` `redis` and `hybrid` to `redis`, `memory` to none; `--transport` `rabbitmq` and `kafka` to the key of the same name, `azureservicebus` and `sqs` to none (a cloud service with no local key in decision 7). Dapper and EF Core are a code choice of the provider family, not a service, so they do not change the map.

### Chosen Option: **A — fixed map, unmapped values reported** (recommended, pending the maintainer)

### Rationale

- A never blocks project creation for a dev container extra; B wins when a silent gap would surprise the user.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Content of the generated and repository `devcontainer.json` (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Minimal: the `dotnet` feature 10.0, the extensions `ms-dotnettools.csdevkit` and `ms-dotnettools.csharp`, `forwardPorts` of the selected services only; the repository file adds `hostRequirements` (proposal 4 CPUs, 16 GB) and ports `[5432, 6379, 5672]`** | Small and easy to review | Contributors add their own extensions |
| **B) A larger set (analyzers, YAML, GitHub Actions extensions, more ports)** | More complete out of the box | More to maintain and to justify |

### Chosen Option: **A — minimal content** (recommended, pending the maintainer)

### Rationale

- The extension list, the ports and the `hostRequirements` numbers are proposals; the numbers are confirmed by the Phase 2 measurement of a restore and a build.
- The output folder of `encina add devcontainer` defaults to the current directory and a folder that does not exist is rejected (the command adds to an existing project); `encina new` creates its folder (`ProjectScaffolder` calls `Directory.CreateDirectory`) and then writes the dev container into it, so the two behaviours do not conflict.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core API Design — service catalog and options

> **Goal**: define what the scaffolder knows and how it reports, before any file is written.

<details>
<summary><strong>Tasks</strong></summary>

1. Create `src/Encina.Cli/Services/DevContainerScaffolder.cs` in the namespace `Encina.Cli.Services`, `public static class DevContainerScaffolder`, next to `ProjectScaffolder`.
2. In it, `public sealed class DevContainerOptions` with `OutputDirectory` (required), `Services` (`IReadOnlyList<string>`), `Force`, mirroring `ProjectOptions`.
3. A private service table: key (`postgres`, `sqlserver`, `mysql`, `mongodb`, `redis`, `rabbitmq`, `kafka`, `nats`, decision 7), compose service name, image, ports and the name of the password variable (never a value, decision 6); the keys, images and ports equal those of `docker-compose.yml`.
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

1. In `DevContainerScaffolder.CreateAsync`: validate the service names against the table (unknown name gives `ScaffoldResult.Error` naming the allowed keys); refuse when `.devcontainer/devcontainer.json` or `.devcontainer/docker-compose.yml` exist and `Force` is false (nothing written); then write `devcontainer.json` (built with `System.Text.Json` so it is always valid) and, only when `--services` names at least one service (decision 7), `docker-compose.yml`.
2. The generated `devcontainer.json` uses the `dotnet` feature with `"version": "10.0"` (`AGENTS.md` section 1), the VS Code extensions `ms-dotnettools.csdevkit` and `ms-dotnettools.csharp`, `forwardPorts` for the selected services, and `dockerComposeFile` only when services exist (decision 9). The generated `docker-compose.yml` follows decision 6: with the recommended option the passwords are required interpolations (`${POSTGRES_PASSWORD:?...}`) and no password value is written, which keeps constraint C7 of the issue ("the CLI never writes a credential") literally true.
3. Add `.devcontainer/devcontainer.json` for the repository (decisions D1 and D2): `dotnet` feature 10.0, the Docker feature chosen by D1, extensions, `forwardPorts` `[5432, 6379, 5672]`, and `hostRequirements` (the numbers measured in this phase, decision 9). It starts services with `docker compose --profile core up -d` from the repository `docker-compose.yml`, whose development defaults already exist there; no second compose file.
4. Add `.devcontainer/README.md` (what starts, how to open in Codespaces).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created DevContainerScaffolder with the service table. Decisions 1, 2, 4, 6, 7, 8 and 9 (Docker access, build timing, overwrite, passwords, service keys, `new` mapping, content) are either answered by the maintainer in the `## Maintainer Decisions` section or still marked recommended; implement the recommended option only for the ones not yet answered.
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
2. `src/Encina.Cli/Commands/NewCommand.cs`: add `--devcontainer` (bool); after `ProjectScaffolder.CreateProjectAsync` succeeds, call `DevContainerScaffolder.CreateAsync` for the project folder, using `--database`, `--caching` and `--transport` to preselect services through the fixed map of decision 8 when the dev container option is on. `ExecuteAsync` gains the new parameter; its only caller is the command's own action, and `NewCommandTests.cs` is checked for calls of the changed signature (the private method is reached through the command).

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

1. Validation: unknown `--services` names and, for `encina add devcontainer`, an output folder that does not exist are rejected before any write, with one message that lists the allowed names (`encina new` creates its folder, decision 9).
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

Pull request 2 (scaffolder and `add devcontainer`):

1. Unit tests (`tests/Encina.UnitTests/Cli/Services/DevContainerScaffolderTests.cs`, new; `tests/Encina.UnitTests/Cli/Commands/AddCommandTests.cs`, existing, edited): files written for no service, one service and all services; unknown service; refusal without `--force`; rewrite with `--force`; real file system under a temp folder; the password variables carry no value.
2. Guard tests (`tests/Encina.GuardTests/CLI/DevContainerScaffolderGuardTests.cs`, new): `ArgumentNullException` on `options`; empty and whitespace `OutputDirectory`.
3. Integration test (`tests/Encina.IntegrationTests/Cli/Services/DevContainerGeneratedFilesTests.cs`, new): the generated `devcontainer.json` parses, every selected service exists in the repository `docker-compose.yml`, and every image tag equals the repository tag. No Docker needed.
4. Contract and property tests: not applicable (no public interface or abstract class, no complex logic; a contract-test justification `.md` is never allowed, so none is written). The load and benchmark justification files are written under `tests/Encina.LoadTests/Cli/` and `tests/Encina.BenchmarkTests/Cli/` (see the AGENTS.md justification format).

Pull request 3 (`new --devcontainer`): `tests/Encina.UnitTests/Cli/Commands/NewCommandTests.cs` (existing, edited): the fixed map of decision 8, unmapped values reported, the dev container written into the new project folder.

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

1. Pull request 1: a setup page under `docs/` (how-to quadrant, through the `encina-docs` skill): open the repository in a dev container and in Codespaces, the VS Code extensions; `changelog.d/447-devcontainer-repository.added.md`; the `ROADMAP.md` backlog entry (line 648) and `docs/INVENTORY.md` (lines 265, 6553, 6737-6743) updates.
2. Pull request 2: the same setup page gains the `encina add devcontainer` section; `changelog.d/447-devcontainer-cli.added.md`; `.github/coverage-manifest/Encina.Cli.json` gains the entry for `Services/DevContainerScaffolder.cs` and the targets of `Commands/AddCommand.cs`, and `totalFiles` goes from 7 to 8 (see Research).
3. Pull request 3: the setup page gains the `encina new --devcontainer` section; `changelog.d/447-devcontainer-new-option.added.md`; the manifest target of `Commands/NewCommand.cs`.
4. `dotnet build --configuration Release` with 0 warnings; the three CLI test projects pass; `check-plan` and the link check stay green.
5. Verification of the acceptance criterion "the dev container builds and runs the unit tests" (pull request 1, manual, not automated: CI has no dev container job and a workflow is a shared hot spot): with the `devcontainer` CLI run `devcontainer up --workspace-folder .` and, in the container, `dotnet restore Encina.slnx`, `dotnet build Encina.slnx` and `dotnet test tests/Encina.UnitTests`; the result is reported in the pull request. A Codespaces prebuild (decision D2) is configured by the maintainer in the repository settings and is outside the pull request.

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

The count is recomputed from the Phase tasks and includes edited existing files. 22 file changes in three pull requests, each within 15 files and 1500 lines; no pull request uses a type, file or page created by a later one (pull request 3 uses the scaffolder of pull request 2, which uses nothing of pull request 3; the setup page of pull request 1 is edited by 2 and 3).

| Pull request | File | Kind |
| :--- | :--- | :--- |
| 1 | `.devcontainer/devcontainer.json` | new |
| 1 | `.devcontainer/README.md` | new |
| 1 | setup page under `docs/` | new |
| 1 | `ROADMAP.md` | edit |
| 1 | `docs/INVENTORY.md` | edit |
| 1 | `changelog.d/447-devcontainer-repository.added.md` | new |
| 2 | `src/Encina.Cli/Services/DevContainerScaffolder.cs` | new |
| 2 | `src/Encina.Cli/Commands/AddCommand.cs` | edit |
| 2 | `tests/Encina.UnitTests/Cli/Services/DevContainerScaffolderTests.cs` | new |
| 2 | `tests/Encina.UnitTests/Cli/Commands/AddCommandTests.cs` | edit |
| 2 | `tests/Encina.GuardTests/CLI/DevContainerScaffolderGuardTests.cs` | new |
| 2 | `tests/Encina.IntegrationTests/Cli/Services/DevContainerGeneratedFilesTests.cs` | new |
| 2 | load-test justification `.md` under `tests/Encina.LoadTests/Cli/` | new |
| 2 | benchmark justification `.md` under `tests/Encina.BenchmarkTests/Cli/` | new |
| 2 | `.github/coverage-manifest/Encina.Cli.json` | edit |
| 2 | setup page (`encina add devcontainer` section) | edit |
| 2 | `changelog.d/447-devcontainer-cli.added.md` | new |
| 3 | `src/Encina.Cli/Commands/NewCommand.cs` | edit |
| 3 | `tests/Encina.UnitTests/Cli/Commands/NewCommandTests.cs` | edit |
| 3 | `.github/coverage-manifest/Encina.Cli.json` | edit |
| 3 | setup page (`encina new --devcontainer` section) | edit |
| 3 | `changelog.d/447-devcontainer-new-option.added.md` | new |

| Pull request | Files | About lines |
| :--- | :--- | :--- |
| 1 Repository dev container and setup page | 6 | 200 |
| 2 Scaffolder and `add devcontainer` | 11 | 800 |
| 3 `new --devcontainer` | 5 | 250 |
| **Total** | 6 + 11 + 5 = 22 file changes, 19 distinct files (the manifest is edited in 2 and 3, the setup page created in 1 and edited in 2 and 3) | about 1250 |

`Encina.Cli` has no `PublicAPI.*.txt` files today (checked on `origin/main`), so no PublicAPI edit is counted; if the implementer finds the analyzer requires them, they are added to pull request 2 (12 files, still within the limit).

### Per-file coverage targets (Encina.Cli)

The manifest `.github/coverage-manifest/Encina.Cli.json` sets package targets (guard 15, unit 60), `totalFiles` 7, and per-file `defaultTests` rules with the reason "Command DTO" and no per-file targets today. Each target below is entered in the same pull request as the tests that meet it (decision: manifest edit and tests travel together). The implementer first measures the baseline of the two command files with `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`, and the target of a command file is the greater of the number below and its measured baseline. The manifest edit also adds `Services/DevContainerScaffolder.cs` and raises `totalFiles` from 7 to 8.

| File | Unit | Guard | Integration | Justification |
|------|:----:|:-----:|:-----------:|---------------|
| `Services/DevContainerScaffolder.cs` (PR 2) | 90 | 100 | 60 | Pure file generation under a temp folder; every branch (validation, refusal, force, services) is reachable without Docker |
| `Commands/AddCommand.cs` (PR 2) | 75 | 0 | 0 | The new subcommand is thin and every branch (success, error exit code) is reachable with the real scaffolder; guard is 0 because the file has no argument checks (the command methods take parsed values and no object parameter that is checked) |
| `Commands/NewCommand.cs` (PR 3) | 75 | 0 | 0 | One new option wired to the scaffolder and the map of decision 8, reachable with the real scaffolder; guard is 0 because the file has no argument checks |

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
6. Docs page, changelog fragments, ROADMAP, INVENTORY, coverage manifest entries (each in the pull request of its tests).

KEY PATTERNS:
- All-or-nothing file writes, errors through ScaffoldResult.Error, never the contents of a file in a message.
- Image tags in one table, asserted equal to docker-compose.yml by a test.
- Passwords: the generated compose file names password variables and writes no value (decision 6, recommended option).
- Delivery: three pull requests (repository dev container and page; scaffolder and add command; new option), split as in Estimated File Count.
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
2. The maintainer answers the Dilemmas (decisions 1, 2, 4, 6, 7, 8 and 9) when work on the issue starts; the answers go into a new `## Maintainer Decisions` section added as the last section of this plan at that time (it does not exist yet).
3. Implement Phases 1 to 6 as three worker pull requests (6, 11 and 5 files, about 1250 lines in total).
4. The final commit references `Fixes #447`.
