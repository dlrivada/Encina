# Implementation Plan: Vertical Slice Feature Templates for the Encina CLI (`generate feature`, Composed Slices, Safe Custom Templates)

> **Issue**: [#201](https://github.com/dlrivada/Encina/issues/201)
> **Type**: Feature (developer experience, CLI)
> **Related**: #365 (vertical slice support: its plan puts `generate slice`, `SliceOptions` and `SliceCodeGenerator` in `Encina.Cli`, and `SliceTestBase<T>` in `Encina.Testing`), #2521 (generators silently overwrite files), #2522 (names are never validated: code that does not compile or a path outside the working folder), #2525 (missing output folder), #2305 (the generated saga does not compile), #2607 (`generate test` skeletons, open, `needs-decision`), #2528 (Encina.Cli README), ADR-058 (CLI command architecture), #190 (REPR endpoints, a different style), #476 (closed: `FeatureSlice` and `IUseCaseHandler` in `Encina.DomainModeling`)
> **Complexity**: Medium (a composer over the slice generator, a template renderer with a path-confinement rule, interactive prompts)
> **Estimated Scope**: ~700 lines of production code + ~1,000 lines of tests, in five pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-23, main `ce352001f`)

- `Encina.Cli` (`src/Encina.Cli/`, a global tool, command `encina`) has three command factories: `NewCommand` (`encina new <template> <name>`; `ProjectScaffolder` accepts the templates `api`, `worker` and `console`), `GenerateCommand` (`generate handler|query|saga|notification|stryker`, alias `g`) and `AddCommand` (adds packages). It uses `System.CommandLine` and `Spectre.Console` (`Encina.Cli.csproj`). `Encina.Cli.csproj` declares `<EmbeddedResource Include="Templates\**\*" />`, but there is no `Templates` folder: templates are string literals in `Services/CodeGenerator.cs` (590 lines, static methods, plain `File.WriteAllText`).
- `CodeGenerator` never validates a name and never checks for an existing file (`CodeGenerator.cs` lines 83 to 101: `Path.Combine(options.OutputDirectory, $"{options.Name}.cs")` then `File.WriteAllText`). The open bugs #2521, #2522, #2525 and #2305 record this, plus a saga template that does not compile. A feature generator that writes many files on top of that would multiply the defects.
- `encina generate query GetOrderById --response OrderDto` already exists; the issue's `encina new query ...` and `encina new command ...` duplicate `generate query` and `generate handler`. `encina new` takes a project template as its first argument, so `encina new feature OrderManagement` collides with the template argument.
- There is no custom template support, no interactive mode and no `config` command in the CLI; none of the files in the issue's "Generated Structure" (`<Op>Command.cs`, `<Op>CommandHandler.cs`, `<Op>CommandValidator.cs`, `<Op>Endpoint.cs`, `<Op>Tests.cs`, `<Feature>Module.cs`, `<Feature>ServiceExtensions.cs`) exists as a template today. The plan of #365 (merged on main as `docs/plans/vertical-slice-implementation-plan-365.md`) adds `SliceOptions` and `SliceCodeGenerator` for one slice (`generate slice <name> --module <m> --type command|query --response <t> [--validator ...] [--endpoint]`), writing `Features/<Module>/<Name>/`; neither type is on `main` yet.
- ADR-058 keeps scaffolding standalone and in the global tool; commands that touch data run inside the application's container (#2983). A scaffolding command writes files only, starts no application and writes no audit entry.
- Tests today: `tests/Encina.UnitTests/Cli/` (commands and services), `tests/Encina.GuardTests/CLI/` (the generators are `public static`, so the guard tests reach them), `tests/Encina.IntegrationTests/Cli/Services/ProjectScaffolderCompilationTests.cs` (a compile test of generated output, the pattern the feature generator copies).

### What this plan delivers

- **`encina generate feature <Feature>`** (alias of the group `g`): one command that writes a feature folder `Features/<Feature>/` made of the operations the caller names (`--command <Op>` and `--query <Op>`, each repeatable, at least one required), each written by the slice generator of #365 (`Features/<Feature>/<Op>/`), plus the feature-level files: `<Feature>Module.cs` (a `Map<Feature>Endpoints(this IEndpointRouteBuilder)` calling every operation's endpoint mapping when `--endpoint` is given), `<Response>.cs` for the DTO named by `--response` (default `<Feature>Dto`), and `<Feature>ServiceExtensions.cs` only when the feature has something to register (saga definition), never an empty method.
- **Options**: `--validator fluentvalidation|dataannotations|miniValidator|none` (the three providers of `AGENTS.md` section 5, as #365 D7), `--endpoint`, `--with-tests`, `--with-saga`, `--output`, `--namespace`, `--template <dir>`, `--force` is **not** offered (an existing file is an error, #2521).
- **Safety**: every name is validated as a C# identifier before any path is built (#2522); the whole file plan is computed and checked for collisions first, files are created with `FileMode.CreateNew`, and a failure removes the files created by that run.
- **Custom templates** (`--template ./templates/my-feature`): plain text with `{{Token}}` placeholders, no code execution, no template engine; a directory with `feature/` (rendered once) and `operation/` (rendered per operation); an unknown token is an error; every rendered path must stay inside the output directory.
- **Interactive mode**: when required values are missing and the terminal is interactive, `Spectre.Console` prompts for them; when input is redirected (CI), the command fails with the usage text and never waits.
- **Not delivered**: `encina config set templates.path` and persisted CLI settings (not an acceptance criterion; recorded as a follow-up issue file), `encina new feature|query|command` (Design Choice 1), and compile-time generation.
- Affected packages: `Encina.Cli` only. Provider category: none.

---

## Design Choices

<details>
<summary><strong>1. Where the command lives: `generate feature` or `new feature`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina generate feature <Feature> ...`, next to `generate slice` of #365** | `new` keeps meaning "create a project", `generate` keeps meaning "add a component to a project"; no collision with the `<template>` argument; `generate query` and `generate handler` already cover the issue's `new query` and `new command`; consistent with ADR-058 ("`generate slice` stays scaffolding under `generate`") | The commands differ from the issue's examples (`encina new feature`) |
| **B) `encina new feature ...` as subcommands of `new`** | The issue's exact commands | `new` already takes `<template> <name>`; subcommands named `feature`, `query` and `command` make those words unusable as project templates and duplicate `generate query`; the parser needs a grammar with both arguments and subcommands |
| **C) Both** | Every example works | Two names for one command is a compatibility alias, which `AGENTS.md` section 3 forbids |

### Chosen Option: **A - `generate feature`** (recommended, pending the maintainer)

### Rationale

- A wins on consistency with the existing verbs and with #365; B wins if the maintainer wants the issue's command lines verbatim for documentation and accepts the `new` grammar; C is excluded by the no-alias rule. The acceptance criterion `encina new feature <name> command` is then amended to `encina generate feature <name>` (Next Steps).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Build on the slice generator of #365 or write an independent one</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Compose: `FeatureCodeGenerator` calls #365's `SliceCodeGenerator` once per operation and writes only the feature-level files** | One template set for command, query, handler, validator and endpoint; validation, no-overwrite and the three validator templates are tested once; the issue's slice layout is #365's layout | The first pull request waits for #365's CLI pull request and for #2521 and #2522 |
| **B) An independent generator with its own templates** | Starts now | Two template sets for the same files, two places to fix #2305-style defects; the folder convention could drift from #365's |

### Chosen Option: **A - Compose over #365** (recommended, pending the maintainer)

### Rationale

- A wins unless #365 is dropped; B wins if the maintainer wants the feature command before slices land, accepting that its templates are replaced by #365's. The feature generator takes a `SliceOptions` list, so the dependency is one type.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the operations of a feature are named</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The caller lists them: `--command CreateOrder --command CancelOrder --query GetOrder`; at least one is required** | Exact names, no guessing; any domain verb works | More typing for plain CRUD |
| **B) A CRUD set derived from an entity name (`--entity Order` gives `CreateOrder`, `GetOrder`, `ListOrders`, `UpdateOrder`, `DeleteOrder`)** | One flag for the common case | Pluralization and verbs are guesses; wrong for anything that is not CRUD |

### Chosen Option: **A - Explicit operations** (recommended, pending the maintainer)

### Rationale

- A wins for domain-driven features (the issue's own examples are `CreateOrder` and `GetOrder`, not generic CRUD); B wins for scaffolding many plain CRUD resources, which a later `--crud <Entity>` flag can add without changing A. In interactive mode the prompt asks for the operation names.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How a custom template is rendered</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Plain `{{Token}}` substitution over the files of a template directory; unknown token, unvalidated value or an escaping path is an error** | No dependency, no code execution; a template is data; the same renderer can serve the built-in templates later | No conditionals or loops in custom templates |
| **B) A template engine (for example Scriban)** | Conditionals and loops | A new dependency in a tool run by developers on arbitrary template folders; an engine that can call .NET members widens the attack surface of a `--template` path |
| **C) No custom templates** | Nothing to secure | The issue's "Custom template support" criterion is not met |

### Chosen Option: **A - Token substitution** (recommended, pending the maintainer)

### Rationale

- A wins because the tool must be safe to point at a template folder from a repository the developer does not control; B wins for teams that need conditionals in templates and accept the dependency and its review; C wins only if the criterion is dropped. Tokens: `Feature`, `Operation`, `Response`, `Namespace`, `Kind` (`Command` or `Query`).

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | An existing target file is an error and nothing is written: the file plan is computed first, each file is created with `FileMode.CreateNew`, and a failure deletes the files that run created | Settled | #2521 (generators must not overwrite) |
| C2 | Every name (feature, operation, response, namespace segment) must be a valid C# identifier and not a reserved word before any path is built; separators and `..` can never reach a path | Settled | #2522 |
| C3 | A rendered path is normalized and must stay inside the output directory (also after resolving symbolic links of existing parent folders); a template file name and content may use only the five tokens of Design Choice 4 | Settled | `AGENTS.md` section 3 (fail closed); #2522 (path outside the working folder) |
| C4 | `--with-tests` writes the tests with `SliceTestBase<T>` of #365 into `--tests-output` (required with the flag): a test file inside the application project would not compile without a test framework reference, so the issue's in-folder `<Op>Tests.cs` is not generated | Settled | #365 plan (`SliceTestBase<T>` in `Encina.Testing`); `AGENTS.md` section 9 (Shouldly through `Encina.Testing.Shouldly`) |
| C5 | `--with-saga` uses the saga template of `generate saga` once #2305 is fixed; this plan adds no second saga template | Settled | #2305 |
| C6 | No `Features/.../<Feature>ServiceExtensions.cs` is generated unless the feature registers something: an empty extension method serves no purpose | Settled | `AGENTS.md` section 3 ("every line serves a current purpose", section 1 design principles) |
| C7 | Interactive prompts use `Spectre.Console` (already referenced); when `Console.IsInputRedirected` is true and a required value is missing the command exits 1 with the usage text and never waits | Trivial | `src/Encina.Cli/Encina.Cli.csproj` (`Spectre.Console`) |
| C8 | The command writes files only: no application is started, no audit entry (ADR-058), no telemetry, no logging, so no EventId is claimed and the 12-function matrix is "not applicable" except where noted | Settled | ADR-058 ("scaffolding writes files only"); `AGENTS.md` section 7 |
| C9 | The feature layout is `Features/<Feature>/<Operation>/` (slices) and `Features/<Feature>/` (module, DTO); names derive from the feature name only, which fixes the issue sketch's `OrderManagement` folder against `OrdersModule.cs` | Trivial | Issue "Generated Structure"; #365 plan (`Features/<Module>/<Name>/`) |
| C10 | `encina config set templates.path` is not built: no CLI settings store exists and it is not an acceptance criterion; follow-up issue file `artifacts/issues/cli-config-command.md` | Settled | `AGENTS.md` section 11 (every identified problem is recorded) |

---

## Implementation Phases

### Delivery order and size

Five pull requests, each building and passing alone with its own tests. F1 waits for the CLI pull request of #365 (its `SliceCodeGenerator`) and for #2521 and #2522 (the shared no-overwrite and name rules); F4's saga option waits for #2305.

| PR | Phase | Content | Files |
|----|-------|---------|-------|
| F1 | 1 | `FeatureOptions`, `FeatureCodeGenerator`, `generate feature` (explicit operations, validator, endpoint) | 9 |
| F2 | 2 | Custom templates: `FeatureTemplateRenderer`, `--template` | 9 |
| F3 | 3 | Interactive mode | 9 |
| F4 | 4 | `--with-tests` and `--with-saga` | 8 |
| F5 | 5 | Guide, INVENTORY, ROADMAP | 3 |

### Phase 1: Feature Generator and Command (pull request F1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Cli/Services/FeatureOptions.cs` (`public sealed record`: `Name`, `Commands`, `Queries`, `ResponseType`, `Validator`, `Endpoint`, `OutputDirectory`, `Namespace`; names validated by the shared rule of C2) and `src/Encina.Cli/Services/FeatureCodeGenerator.cs` (`public static class`, `Task<GenerationResult> GenerateFeatureAsync(FeatureOptions options)`: validates, builds the file plan from #365's `SliceCodeGenerator` for each operation plus `<Feature>Module.cs` and the DTO, applies C1, returns `GenerationResult`).
2. **Modify** `src/Encina.Cli/Commands/GenerateCommand.cs` (add `CreateFeatureCommand()` to the list at `Create()`: `generate feature <name> --command <op>... --query <op>... [--response] [--validator] [--endpoint] [--output] [--namespace]`) and `.github/coverage-manifest/Encina.Cli.json` (per-file entries) and add `changelog.d/201-cli-generate-feature.added.md` (the manifest and the fragment through `mechanical-fixer`).
3. Tests: new `tests/Encina.UnitTests/Cli/Services/FeatureCodeGeneratorTests.cs` (files written for two commands and a query, validator per provider, endpoint on and off, collision writes nothing, failure rollback, invalid names, path traversal names), edit `tests/Encina.UnitTests/Cli/Commands/GenerateCommandTests.cs` (option wiring and at-least-one-operation rule), new `tests/Encina.GuardTests/CLI/FeatureCodeGeneratorGuardTests.cs` (null options, blank name, blank output), new `tests/Encina.IntegrationTests/Cli/Services/FeatureCodeGeneratorCompilationTests.cs` (the generated feature compiles against the Encina assemblies, the pattern of `ProjectScaffolderCompilationTests.cs`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #201, plan docs/plans/vsa-cli-templates-implementation-plan-201.md, phase 1.
#365's SliceOptions and SliceCodeGenerator, and the fixes of #2521 and #2522, are merged. Encina.Cli is a System.CommandLine tool.

TASK:
1. Create FeatureOptions and FeatureCodeGenerator composing SliceCodeGenerator per operation plus the module and DTO files.
2. Add the generate feature command; manifest and changelog through mechanical-fixer.
3. Unit, guard and compile tests as listed in the plan.

KEY RULES:
- Validate every name as a C# identifier before building a path; never overwrite (CreateNew, rollback on failure).
- No second template set for slice files; no empty extension class; no new compatibility alias.
- Guard tests for every public parameter; zero warnings.

REFERENCE FILES:
- src/Encina.Cli/Commands/GenerateCommand.cs, src/Encina.Cli/Services/CodeGenerator.cs
- docs/plans/vertical-slice-implementation-plan-365.md, tests/Encina.IntegrationTests/Cli/Services/ProjectScaffolderCompilationTests.cs
```

</details>

---

### Phase 2: Custom Templates (pull request F2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Cli/Services/FeatureTemplateRenderer.cs` (`public static class`; `RenderAsync(string templateDirectory, FeatureOptions options)` reads `feature/**` once and `operation/**` per operation, substitutes the five tokens in names and contents, rejects an unknown token, an escaping path and a symbolic link that leaves the template directory, returns the file plan that `FeatureCodeGenerator` writes under the rules of C1).
2. **Modify** `FeatureOptions.cs` (`TemplateDirectory`), `FeatureCodeGenerator.cs` (uses the renderer when set), `GenerateCommand.cs` (`--template <dir>`), the `Encina.Cli.json` manifest and a changelog fragment `changelog.d/201-cli-feature-templates.added.md` (manifest and fragment through `mechanical-fixer`).
3. Tests: new `FeatureTemplateRendererTests.cs` (tokens, unknown token, `..` in a name, absolute path, link escape, missing `operation/` folder), new `FeatureTemplateRendererGuardTests.cs`, edit `GenerateCommandTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #201 plan docs/plans/vsa-cli-templates-implementation-plan-201.md, phase 2. F1 merged.

TASK:
1. FeatureTemplateRenderer over a template directory (feature/ once, operation/ per operation), wired to --template.
2. Manifest and changelog through mechanical-fixer; unit and guard tests.

KEY RULES:
- Plain {{Token}} substitution only (Feature, Operation, Response, Namespace, Kind); no code execution, no engine.
- An unknown token or any rendered path outside the output directory is an error; nothing is written on error.

REFERENCE FILES:
- src/Encina.Cli/Services/FeatureCodeGenerator.cs, src/Encina.Cli/Services/ProjectScaffolder.cs
```

</details>

---

### Phase 3: Interactive Mode (pull request F3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Cli/Services/FeaturePrompter.cs` (`public static class`; `FeatureOptions Prompt(FeatureOptions partial, IAnsiConsole console)` asks for the missing feature name, operations, response type, validator and endpoint with `Spectre.Console` prompts that reuse the validators of C2; returns an error result instead of prompting when `Console.IsInputRedirected`).
2. **Modify** `GenerateCommand.cs` (run the prompter when required values are missing), `Directory.Packages.props` and `tests/Encina.UnitTests/Encina.UnitTests.csproj` (`Spectre.Console.Testing`, at the version matching `Spectre.Console` 0.57.2), the manifest and a changelog fragment `changelog.d/201-cli-feature-interactive.added.md` through `mechanical-fixer`.
3. Tests: new `FeaturePrompterTests.cs` (`Spectre.Console.Testing.TestConsole` scripted answers; invalid answer is re-asked; redirected input fails with the usage text) and `FeaturePrompterGuardTests.cs`; edit `GenerateCommandTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #201 plan docs/plans/vsa-cli-templates-implementation-plan-201.md, phase 3. F1 merged.

TASK:
1. FeaturePrompter with Spectre.Console prompts for the missing values.
2. Wire it into generate feature; manifest and changelog through mechanical-fixer; tests with TestConsole.

KEY RULES:
- Never wait on redirected input: exit 1 with the usage text. Reuse the name validators of the generator.

REFERENCE FILES:
- src/Encina.Cli/Commands/NewCommand.cs, src/Encina.Cli/Services/FeatureCodeGenerator.cs
```

</details>

---

### Phase 4: Tests and Saga Options (pull request F4)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `FeatureOptions.cs` (`WithTests`, `TestsOutput`, `WithSaga`), `FeatureCodeGenerator.cs` (writes one test file per operation into `TestsOutput` using `SliceTestBase<T>` of #365; with `--with-saga` adds the saga file and a `<Feature>ServiceExtensions.cs` that registers it, the only case that generates that file, C6) and `GenerateCommand.cs` (`--with-tests`, `--tests-output`, `--with-saga`); manifest and changelog `changelog.d/201-cli-feature-tests-saga.added.md` through `mechanical-fixer`.
2. Tests: edit `FeatureCodeGeneratorTests.cs`, add a compile case to `FeatureCodeGeneratorCompilationTests.cs`, edit `GenerateCommandTests.cs` (`--with-tests` without `--tests-output` is an error).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #201 plan docs/plans/vsa-cli-templates-implementation-plan-201.md, phase 4. F1 merged; #365 SliceTestBase merged; #2305 fixed.

TASK:
1. --with-tests (test files in --tests-output) and --with-saga (saga plus the service extensions that register it).
2. Manifest and changelog through mechanical-fixer; unit and compile tests.

KEY RULES:
- No test file inside the application project; the saga template is the one of generate saga, never a copy.

REFERENCE FILES:
- src/Encina.Cli/Services/CodeGenerator.cs (saga), docs/plans/vertical-slice-implementation-plan-365.md
```

</details>

---

### Phase 5: Documentation and Finalization (pull request F5)

<details>
<summary><strong>Tasks</strong></summary>

1. Through `docs-writer`: `docs/features/cli-generate-feature.md` (usage: the command, operations, validators, endpoints, tests, custom template folders with the five tokens, interactive mode, exit codes), `docs/INVENTORY.md` and `ROADMAP.md` lines (`ROADMAP.md` names "VSA Templates for CLI #201"). The package README is #2528's.
2. Build with zero warnings; every coverage flag reaches the targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #201 plan docs/plans/vsa-cli-templates-implementation-plan-201.md, phase 5. F1 to F4 merged.

TASK:
1. Ask docs-writer for the guide and the INVENTORY and ROADMAP lines.
2. Report measured per-file coverage against the plan's targets.

KEY RULES:
- No hand-typed coverage figures; examples must use generate feature (never new feature).

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, docs/features/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-058 | CLI command architecture: scaffolding stays standalone, in the global tool, writes files only | Command placement, no audit |
| `AGENTS.md` section 5 | Validation: FluentValidation, DataAnnotations, MiniValidator | The three `--validator` values |
| #2521, #2522, #2525 | No overwrite, validated names, output folder created | Rules C1 to C3 |
| #365 plan | `SliceCodeGenerator`, `SliceTestBase<T>`, `Features/<Module>/<Name>/` | Composition target |
| Vertical Slice Architecture (the issue's References) | Feature folders | Layout |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `GenerateCommand` | `src/Encina.Cli/Commands/GenerateCommand.cs` | Registration point of the new subcommand |
| `CodeGenerator`, `GenerationResult` | `src/Encina.Cli/Services/CodeGenerator.cs` | Result type, saga template |
| `SliceOptions`, `SliceCodeGenerator` | #365 (not on `main`) | Per-operation files |
| `ProjectScaffolderCompilationTests` | `tests/Encina.IntegrationTests/Cli/Services/` | Compile-proof pattern |
| Existing CLI tests | `tests/Encina.UnitTests/Cli/`, `tests/Encina.GuardTests/CLI/` | Style of unit and guard tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cli` | none | A command-line tool: it writes to the console, not to `[LoggerMessage]` (register row C8); no block is claimed |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| F1 | Phase 1 | 2 | 1 (`GenerateCommand.cs`) | 3 (unit, guard, compile) | 1 (`GenerateCommandTests.cs`) | 1 | 1 (changelog) | 9 | ~850 |
| F2 | Phase 2 | 1 | 3 (`FeatureOptions.cs`, `FeatureCodeGenerator.cs`, `GenerateCommand.cs`) | 2 | 1 (`GenerateCommandTests.cs`) | 1 | 1 (changelog) | 9 | ~600 |
| F3 | Phase 3 | 1 | 1 (`GenerateCommand.cs`) | 2 | 1 (`GenerateCommandTests.cs`) | 1 | 3 (changelog, `Directory.Packages.props`, `Encina.UnitTests.csproj`) | 9 | ~450 |
| F4 | Phase 4 | 0 | 3 (`FeatureOptions.cs`, `FeatureCodeGenerator.cs`, `GenerateCommand.cs`) | 0 | 3 (generator tests, compile test, command tests) | 1 | 1 (changelog) | 8 | ~420 |
| F5 | Phase 5 | 0 | 0 | 0 | 0 | 0 | 3 (guide, `INVENTORY.md`, `ROADMAP.md`) | 3 | ~200 |
| **Total** | | **4** | **8** | **7** | **6** | **4** | **9** | **38** | **~2,500** |

Columns sum: 4 + 8 + 7 + 6 + 4 + 9 = 38 = 9 + 9 + 9 + 8 + 3. F4 is 8 files: three source edits, three test edits, the manifest and the fragment. F3 adds the `Spectre.Console.Testing` package (`Spectre.Console` is pinned at 0.57.2 in `Directory.Packages.props`; the testing package is not referenced anywhere today) to `Directory.Packages.props` and `tests/Encina.UnitTests/Encina.UnitTests.csproj`. `GenerateCommand.cs` is edited in four pull requests; each edit is independent because the command list in `Create()` is touched only in F1. Test doubles and `new X(` sites: no interface changes; the new records have one construction site each (the command) and the tests; `GenerateCommandTests` is the only existing test class touched.

Per-file coverage targets (the `Encina.Cli.json` package targets today are unit 60, guard 15, integration 75; the new files have no per-file entries and the plan adds them above those values, a rise, never a lowering):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Services/FeatureCodeGenerator.cs` | 90 | 70 | 0 | 0 | 60 | Plan building, collision, rollback and validation are table-driven by unit tests; guard covers the null and blank arguments; the compile test drives the real generated output |
| `Services/FeatureOptions.cs` | 85 | 60 | 0 | 0 | 0 | A record with a validating factory: valid and invalid names are unit tested and guarded |
| `Services/FeatureTemplateRenderer.cs` | 90 | 60 | 0 | 0 | 0 | Every token, unknown-token and path-escape case is a unit test; guard covers the null and blank arguments |
| `Services/FeaturePrompter.cs` | 85 | 40 | 0 | 0 | 0 | Scripted `TestConsole` answers cover each prompt and the redirected-input path; guard covers null arguments |
| `Commands/GenerateCommand.cs` | 75 | 0 | 0 | 0 | 0 | Option wiring is unit tested through the command tests; the class is `internal static` without a guard-reachable entry (no `InternalsVisibleTo` for `Encina.GuardTests` in `Encina.Cli.csproj`, so guard stays with the public generators) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #201 - vertical slice feature templates for the Encina CLI - in five small pull requests (F1 to F5).
F1 starts after #365's CLI pull request (SliceCodeGenerator) and the fixes of #2521 and #2522 have merged; F4's saga option needs #2305.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers, no command aliases. Edit with Edit/Write only; manifests and
  changelog fragments through mechanical-fixer; the guide and INVENTORY/ROADMAP lines through docs-writer.
- Encina.Cli writes files only: no application started, no audit entry, no telemetry.

IMPLEMENTATION OVERVIEW:
1. F1: FeatureOptions, FeatureCodeGenerator (composes SliceCodeGenerator), generate feature, compile test.
2. F2: FeatureTemplateRenderer and --template (token substitution only, path confinement).
3. F3: FeaturePrompter (Spectre.Console), never waits on redirected input.
4. F4: --with-tests (SliceTestBase, --tests-output) and --with-saga. 5. F5: documentation.

KEY PATTERNS:
- Validate names as C# identifiers first; compute the file plan, check collisions, create with CreateNew, roll back on failure.
- A custom template is data: five tokens, no engine, no code execution, rendered paths stay inside the output directory.
- Guard tests for every public method parameter; one behavior per test; no Thread.Sleep.

REFERENCE FILES:
- src/Encina.Cli/Commands/GenerateCommand.cs, src/Encina.Cli/Services/CodeGenerator.cs
- docs/plans/vertical-slice-implementation-plan-365.md, docs/architecture/adr/058-encina-cli-command-architecture.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A scaffolding tool that writes source files |
| 2 | OpenTelemetry | ❌ N/A | A command-line tool with no request pipeline to trace |
| 3 | Structured Logging | ❌ N/A | Console output only; no `[LoggerMessage]`, no EventId (register row C8) |
| 4 | Health Checks | ❌ N/A | No dependency to check |
| 5 | Validation | ✅ Phase 1 | Name validation before any path is built, and the three `--validator` templates for the generated code |
| 6 | Resilience | ❌ N/A | Local file writes only |
| 7 | Distributed Locks | ❌ N/A | A single local process |
| 8 | Transactions | ❌ N/A | Not a database transaction; the file plan is all-or-nothing with rollback (C1) |
| 9 | Idempotency | ❌ N/A | Running twice fails on the existing files (C1) rather than repeating |
| 10 | Multi-Tenancy | ❌ N/A | Writes source code, no tenant data; generated code relies on the pipeline's tenant handling |
| 11 | Module Isolation | ❌ N/A | The `Features/<Feature>` folder is a convention, not an Encina module; #365 covers module scoping of slices |
| 12 | Audit Trail | ❌ N/A | Scaffolding writes no audit entry (ADR-058) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #365 (CLI pull request: `SliceOptions`, `SliceCodeGenerator`) | Open, plan on main | F1 composes it (Design Choice 2) |
| #2521 and #2522 | Open, `p0-mandatory` bugs | The no-overwrite and name rules the feature generator shares |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Saga generator fix | #2305 | F4's `--with-saga` |
| `SliceTestBase<T>` | #365 | F4's `--with-tests` |
| Output folder creation | #2525 | Part of the shared rules |
| `generate test` skeletons | #2607 | Open, `needs-decision`; this plan does not depend on it |
| Encina.Cli README | #2528 | Documents the package; F5 only adds the guide |
| `encina config` command | issue file `artifacts/issues/cli-config-command.md` | Register row C10 |

---

## Next Steps

1. Link this plan from #201 and ask the maintainer to answer Design Choices 1 to 4 when work starts.
2. Implement F1 after #365's CLI pull request and #2521 and #2522, then F2, F3 and F4 (independent after F1), then F5.
3. Maintainer action: amend the acceptance criteria of #201 that name `encina new feature|query|command`, an in-folder `<Op>Tests.cs`, `OrdersModule.cs` and `encina config set templates.path` to the outcome of this plan (this plan never comments on an issue).
