## Scope

Closed by commit b02647b3c (timeline `closed` event; no PR). Files it changed and where they live today (all verified present):

- `src/Encina.Cli/` (Program.cs, Commands/AddCommand.cs, GenerateCommand.cs, NewCommand.cs, Services/CodeGenerator.cs, PackageManager.cs, ProjectScaffolder.cs, Encina.Cli.csproj). Later commits: 3a6bdf0ec, 0e8b3b86b, a61a9d27f, e998f6cc3, 7b586da4e, 9d5f3f9a7, ed8135a75 (S6966 InvokeAsync), 4cf675ef4 (added `encina generate stryker`, issue #437).
- Tests: the original projects `tests/Encina.Cli.Tests` and `tests/Encina.Cli.GuardTests` no longer exist (consolidated by 658263027). Today: `tests/Encina.UnitTests/Cli/Services/{CodeGenerator,CodeGeneratorExtended,ProjectScaffolder,ProjectScaffolderExtended,PackageManager}Tests.cs` and `tests/Encina.GuardTests/CLI/{CodeGenerator,ProjectScaffolder,PackageManager}GuardTests.cs` plus `*HappyPathGuardTests.cs`.
- `.github/coverage-manifest/Encina.Cli.json` (unit and guard targets only), `Encina.slnx` line 86.
- `CHANGELOG.md` line 8723 (entry for #47), `ROADMAP.md` line 325, `docs/INVENTORY.md` line 4409, `README.md` package listing.
- `docs/history/2025-12.md` (named by the close comment) no longer exists in the tree (deleted by d098e5425); `docs/history` is gone, so that destination is not a regression of this issue.
- The csproj embeds `Templates\**` but `src/Encina.Cli/Templates` does not exist; there is no `src/Encina.Cli/README.md`.
- No `src/` code was removed on purpose.

## Destinations

- Decision "Encina.Cli with new/generate/add": CHANGELOG.md and ROADMAP.md present (grep evidence above). All 4 generate subcommands and 6 add subcommands exist in the command files; `--response` and `--steps` options exist in GenerateCommand.cs.
- Pre-draft candidate "regression-test: integrate the 65 tests into CI" and "Rules and lessons" (unit plus guard rule for new packages, CLI docs rule) were dropped: no source states them as decisions.
- Issue task "Create `dotnet new` templates": missing (no Templates folder). "Add integration tests": missing (no IntegrationTests file references Encina.Cli). "Add documentation": only the CHANGELOG/ROADMAP/INVENTORY mentions; no package README or guide. Recorded as a gotcha with `current: unknown`, no tracker found (searched open and closed issues for "Encina.Cli"; hits #2305, #201, #329, #365, #2316, #437 are other work).
- Saga generation: acceptance criterion "Saga generation works" is contradicted by open bug #2305 (generated saga does not compile); tracked there as a planned backlog destination. #2316 is a related open test issue about the low-ceremony saga API.
- Outcome chosen `partial`: three of seven tasks (templates, integration tests, documentation) unmet at close, and saga generation broken.
- Timing: created 2025-12-24T13:27:10Z, closed 2025-12-27T14:25:36Z (3 d 0 h 58 min); the commit (2025-12-27 15:29 +01:00) postdates the manual close.

## Successor and duplicate issues

None (not rejected, superseded or duplicate). Related, verified today: #2305 OPEN (saga generator bug), #201 OPEN (VSA templates for CLI), #437 CLOSED (Stryker templates; added `generate stryker`).

## Lessons for the pipeline

- (issue-archivist) The worktree had no `artifacts/knowledge/predraft/47.md`; read it from the main checkout (D:\Proyectos\Encina), as in earlier lessons.
- (issue-archivist) The pre-draft had `closed_at: 12/27/2025`, `linked_prs: []` and a packages list that included dependencies and test projects (System.CommandLine, Encina.Cli.Tests); the record lists only `Encina.Cli`. It also invented a regression-test destination and rules; dropped.
- (issue-archivist) The pre-draft read the close comment's feature list as full delivery; comparing the issue body's task list (`dotnet new` templates, integration tests, docs) with the tree (a csproj `EmbeddedResource` for a Templates folder that does not exist) showed three tasks never done, so `partial`.
- (issue-archivist) `gh issue view <n> --comments` printed only the comment and not the body; read the body with `--json body --jq .body`.
